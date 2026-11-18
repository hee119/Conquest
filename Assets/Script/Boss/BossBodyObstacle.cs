using UnityEngine;
using UnityEngine.AI;

/// <summary>Separates ordinary enemies from the boss, including scripted dash movement.</summary>
[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class BossBodyObstacle : MonoBehaviour
{
    [SerializeField] private NavMeshObstacle navigationObstacle;
    private const float SeparationSpeed = 6f;
    private const float BoundaryReturnSpeed = 6f;
    private BoxCollider2D body;
    private BoxCollider2D playerBody;
    private EnemyCombat combat;
    private Rigidbody2D bossRigidbody;
    private NavMeshAgent agent;
    private RigidbodyType2D previousBodyType;
    private bool skillBodyActive;
    private readonly Collider2D[] overlaps = new Collider2D[256];
    private ContactFilter2D filter;
    private bool summoning;
    private bool waitingForClearance;

    private void Awake()
    {
        body = GetComponent<BoxCollider2D>();
        combat = GetComponent<EnemyCombat>();
        bossRigidbody = GetComponent<Rigidbody2D>();
        agent = GetComponent<NavMeshAgent>();
        filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(LayerMask.GetMask("Enemy"));

        // Keep the original Enemy collider available to attack queries.
        var blocker = new GameObject("Boss Player Collision");
        blocker.layer = LayerMask.NameToLayer("Boss");
        blocker.transform.SetParent(transform, false);
        playerBody = blocker.AddComponent<BoxCollider2D>();
        playerBody.offset = body.offset;
        playerBody.size = body.size;
        playerBody.edgeRadius = body.edgeRadius;
    }

    public void SetSkillBodyActive(bool active)
    {
        if (bossRigidbody == null) bossRigidbody = GetComponent<Rigidbody2D>();
        if (bossRigidbody == null || skillBodyActive == active) return;
        if (active)
        {
            previousBodyType = bossRigidbody.bodyType;
            bossRigidbody.bodyType = RigidbodyType2D.Kinematic;
        }
        else
        {
            bossRigidbody.bodyType = previousBodyType;
        }
        skillBodyActive = active;
    }

    private void OnDisable()
    {
        SetSkillBodyActive(false);
    }

    private void OnEnable()
    {
        summoning = false;
        waitingForClearance = true;
        if (navigationObstacle != null) navigationObstacle.enabled = false;
    }

    public void SetSummoning(bool active)
    {
        summoning = active;
        if (!active) return;

        waitingForClearance = true;
        if (navigationObstacle != null) navigationObstacle.enabled = false;
    }

    private void FixedUpdate()
    {
        if (!body.enabled || (combat != null && combat.isDead) ||
            bossRigidbody == null || bossRigidbody.bodyType != RigidbodyType2D.Dynamic)
            return;

        Vector2 position = bossRigidbody.position;
        Vector2 inward = (Vector2)ClampToMap(position) - position;
        if (inward.sqrMagnitude <= 0.000001f)
            return;

        Vector2 direction = inward.normalized;
        float desiredSpeed = Mathf.Min(BoundaryReturnSpeed, inward.magnitude / Time.fixedDeltaTime);
        float speedChange = desiredSpeed - Vector2.Dot(bossRigidbody.linearVelocity, direction);
        bossRigidbody.AddForce(direction * (speedChange * bossRigidbody.mass), ForceMode2D.Impulse);
    }

    private void LateUpdate()
    {
        // Scripted airborne movement intentionally leaves the ground plane.
        if (body.enabled && (combat == null || !combat.isDead) &&
            bossRigidbody != null && bossRigidbody.bodyType == RigidbodyType2D.Kinematic &&
            agent != null && agent.enabled && agent.updatePosition)
        {
            Vector3 bounded = ClampToMap(transform.position);
            if ((bounded - transform.position).sqrMagnitude > 0.000001f)
            {
                Vector3 next = Vector3.MoveTowards(transform.position, bounded, BoundaryReturnSpeed * Time.deltaTime);
                if (!agent.Warp(next))
                    bossRigidbody.position = next;
            }
        }

        bool solid = body.enabled && (combat == null || !combat.isDead);
        playerBody.enabled = solid;
        if (!solid)
        {
            waitingForClearance = true;
            if (navigationObstacle != null) navigationObstacle.enabled = false;
            return;
        }

        // Scripted boss movement happens outside the physics step.
        Physics2D.SyncTransforms();
        bool clearingObstacle = waitingForClearance && !summoning && navigationObstacle != null;
        Bounds clearance = clearingObstacle ? GetObstacleBounds() : body.bounds;
        int count = clearingObstacle
            ? Physics2D.OverlapBox(clearance.center, clearance.size, 0f, filter, overlaps)
            : body.Overlap(filter, overlaps);
        bool blocked = count == overlaps.Length;
        float maxStep = SeparationSpeed * Mathf.Min(Time.deltaTime, Time.fixedDeltaTime);
        for (int i = 0; i < count; i++)
        {
            Collider2D other = overlaps[i];
            EnemyMove enemy = other.GetComponentInParent<EnemyMove>();
            if (enemy == null || enemy.gameObject == gameObject ||
                enemy.GetComponent<BossBodyObstacle>() != null) continue;
            var health = enemy.GetComponent<EnemyCombat>();
            if (health != null && health.isDead) continue;
            ColliderDistance2D distance = body.Distance(other);
            Vector2 push;
            if (distance.isOverlapped)
                push = distance.normal * (0.02f - distance.distance);
            else if (clearingObstacle)
            {
                Vector2 outward = other.bounds.center - clearance.center;
                push = outward.normalized * maxStep;
            }
            else continue;

            blocked = true;
            Vector3 step = Vector2.ClampMagnitude(push, maxStep);
            enemy.PushFromBoss(step);
        }

        if (clearingObstacle && !blocked)
        {
            waitingForClearance = false;
            navigationObstacle.enabled = true;
        }
    }

    // Constrain the whole body, including its offset, to the painted map bounds.
    public Vector3 ClampToMap(Vector3 position)
    {
        var map = StageManager.Instance != null ? StageManager.Instance.backgroundTilemap : null;
        if (map == null) return position;
        Bounds bounds = map.localBounds;
        if (bounds.size.x <= 0f || bounds.size.y <= 0f) return position;
        if (body == null) body = GetComponent<BoxCollider2D>();
        if (body == null) return position;

        Transform mapTransform = map.transform;
        Vector3 centerOffset = mapTransform.InverseTransformVector(transform.TransformVector(body.offset));
        Vector3 x = mapTransform.InverseTransformVector(transform.TransformVector(Vector3.right * (body.size.x * 0.5f)));
        Vector3 y = mapTransform.InverseTransformVector(transform.TransformVector(Vector3.up * (body.size.y * 0.5f)));
        float extentX = Mathf.Abs(x.x) + Mathf.Abs(y.x);
        float extentY = Mathf.Abs(x.y) + Mathf.Abs(y.y);
        Vector3 local = mapTransform.InverseTransformPoint(position);
        float minX = bounds.min.x + 0.02f + extentX - centerOffset.x;
        float maxX = bounds.max.x - 0.02f - extentX - centerOffset.x;
        float minY = bounds.min.y + 0.02f + extentY - centerOffset.y;
        float maxY = bounds.max.y - 0.02f - extentY - centerOffset.y;
        local.x = minX <= maxX ? Mathf.Clamp(local.x, minX, maxX) : (minX + maxX) * 0.5f;
        local.y = minY <= maxY ? Mathf.Clamp(local.y, minY, maxY) : (minY + maxY) * 0.5f;
        Vector3 result = mapTransform.TransformPoint(local);
        result.z = position.z;
        return result;
    }

    private Bounds GetObstacleBounds()
    {
        Transform obstacleTransform = navigationObstacle.transform;
        Vector3 extents = navigationObstacle.shape == NavMeshObstacleShape.Capsule
            ? new Vector3(navigationObstacle.radius, navigationObstacle.height * 0.5f, navigationObstacle.radius)
            : navigationObstacle.size * 0.5f;
        Vector3 x = obstacleTransform.TransformVector(Vector3.right * extents.x);
        Vector3 y = obstacleTransform.TransformVector(Vector3.up * extents.y);
        Vector3 z = obstacleTransform.TransformVector(Vector3.forward * extents.z);
        Vector3 size = new Vector3(
            Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
            Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y), 0f) * 2f;
        var bounds = new Bounds(obstacleTransform.TransformPoint(navigationObstacle.center), size);
        bounds.Encapsulate(body.bounds);
        return bounds;
    }
}
