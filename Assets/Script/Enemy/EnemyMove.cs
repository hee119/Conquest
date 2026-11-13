using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMove : MonoBehaviour
{
    public GameObject target;
    public NavMeshAgent agent;

    public float moveSpeed;
    public float stoppingDistance;

    public bool isSlowDown;
    public int slowCount;

    [SerializeField] protected float pathUpdateInterval = 0.35f;
    [SerializeField, Min(0f)] protected float targetMoveThreshold = 0.1f;
    [SerializeField] protected float knockbackCooldown = 0.3f;

    [Header("Knockback")]
    [SerializeField] protected float knockbackPower = 5f;
    [SerializeField] protected float knockbackDuration = 0.15f;

    protected bool canKnockback = true;
    protected Coroutine knockbackCoroutine;

    private float nextPathUpdateTime;
    private Vector3 lastDestinationTargetPosition;
    private bool hasDestinationTargetPosition;
    private int bossPushFrame = -1;
    private bool resumeAgentAfterBossPush;
    private const float SeparationInterval = 0.15f;
    private const float SeparationSpeed = 0.8f;
    private readonly Collider2D[] nearbyEnemies = new Collider2D[32];
    private ContactFilter2D enemyFilter;
    private EnemyCombat ownCombat;
    private bool isBoss;
    private float nextSeparationTime;

    protected virtual void Awake()
    {
        if (target == null)
            target = GameObject.FindGameObjectWithTag("Player");

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        ownCombat = GetComponent<EnemyCombat>();
        isBoss = GetComponent<BossBodyObstacle>() != null;
        enemyFilter = new ContactFilter2D { useTriggers = false };
        enemyFilter.SetLayerMask(LayerMask.GetMask("Enemy"));

        if (agent != null)
        {
            agent.updateUpAxis = false;
            agent.updateRotation = false;
            agent.speed = moveSpeed;
        }
    }

    protected virtual void OnEnable()
    {
        if (target == null)
            target = GameObject.FindGameObjectWithTag("Player");

        canKnockback = true;
        nextPathUpdateTime = 0f;
        hasDestinationTargetPosition = false;
        nextSeparationTime = Time.time + Random.Range(0f, SeparationInterval);

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;
    }

    protected virtual void Update()
    {
        if (Time.time < nextPathUpdateTime)
            return;

        nextPathUpdateTime = Time.time + Mathf.Max(0.01f, pathUpdateInterval);
        Move();
    }

    protected virtual void Move()
    {
        if (target == null || !target.activeInHierarchy ||
            agent == null || !agent.enabled || !agent.isOnNavMesh ||
            agent.isStopped)
        {
            return;
        }

        Vector3 targetPosition = target.transform.position;
        if (!HasTargetMoved(targetPosition))
            return;

        if (agent.SetDestination(targetPosition))
            RememberTargetPosition(targetPosition);
    }

    protected bool HasTargetMoved(Vector3 targetPosition)
    {
        if (!hasDestinationTargetPosition)
            return true;

        float threshold = Mathf.Max(0f, targetMoveThreshold);
        return (targetPosition - lastDestinationTargetPosition).sqrMagnitude > threshold * threshold;
    }

    protected void RememberTargetPosition(Vector3 targetPosition)
    {
        lastDestinationTargetPosition = targetPosition;
        hasDestinationTargetPosition = true;
    }

    public void MoveSpeedRenewal()
    {
        if (agent != null)
            agent.speed = moveSpeed;
    }

    protected virtual void OnDisable()
    {
        EndBossPush();
        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = null;
        }
    }

    internal void PushFromBoss(Vector3 step)
    {
        if (bossPushFrame < 0 && isActiveAndEnabled && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            if (knockbackCoroutine != null)
            {
                StopCoroutine(knockbackCoroutine);
                knockbackCoroutine = null;
                canKnockback = true;
            }
            // Prevent avoidance from moving the visible object while it is being pushed.
            resumeAgentAfterBossPush = true;
            agent.enabled = false;
        }
        bossPushFrame = Time.frameCount;

        Vector3 position = transform.position;
        if (resumeAgentAfterBossPush && NavMesh.Raycast(position, position + step, out var hit, agent.areaMask))
        {
            Vector3 tangent = Vector3.ProjectOnPlane(step, hit.normal);
            if (tangent.sqrMagnitude < step.sqrMagnitude * 0.25f)
            {
                tangent = Vector3.Cross(Vector3.forward, step);
                if (NavMesh.Raycast(position, position + tangent, out _, agent.areaMask))
                    tangent = -tangent;
            }
            if (NavMesh.Raycast(position, position + tangent, out _, agent.areaMask))
                return;
            step = tangent;
        }
        transform.position = position + step;
    }

    private void LateUpdate()
    {
        // BossBodyObstacle runs later. Wait until the previous frame had no push.
        if (bossPushFrame >= 0 && Time.frameCount > bossPushFrame + 1)
            EndBossPush();

        SeparateOverlappingEnemies();
    }

    private void SeparateOverlappingEnemies()
    {
        if (isBoss || Time.timeScale <= 0f || Time.time < nextSeparationTime ||
            bossPushFrame >= 0 || knockbackCoroutine != null ||
            (ownCombat != null && ownCombat.isDead) || agent == null ||
            !agent.enabled || !agent.isOnNavMesh || agent.isStopped)
            return;

        nextSeparationTime = Time.time + SeparationInterval;
        float radius = Mathf.Max(0.5f, agent.radius * 2f);
        Vector2 origin = transform.position;
        int count = Physics2D.OverlapCircle(origin, radius, enemyFilter, nearbyEnemies);
        Vector2 push = Vector2.zero;
        for (int i = 0; i < count; i++)
        {
            Collider2D otherCollider = nearbyEnemies[i];
            if (otherCollider == null) continue;
            EnemyMove other = otherCollider.GetComponentInParent<EnemyMove>();
            if (other == null || other == this || other.isBoss ||
                (other.ownCombat != null && other.ownCombat.isDead)) continue;

            Vector2 delta = origin - (Vector2)other.transform.position;
            float distance = delta.magnitude;
            if (distance >= radius) continue;
            if (distance < 0.001f)
            {
                int a = GetInstanceID();
                int b = other.GetInstanceID();
                float angle = ((Mathf.Min(a, b) ^ Mathf.Max(a, b)) & 1023) * (Mathf.PI * 2f / 1024f);
                delta = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (a < b ? 1f : -1f);
                distance = 0f;
            }
            else delta /= distance;
            push += delta * (1f - distance / radius);
        }

        if (push.sqrMagnitude < 0.000001f) return;
        Vector3 step = (Vector3)(Vector2.ClampMagnitude(push, 1f) * (SeparationSpeed * SeparationInterval));
        if (NavMesh.Raycast(transform.position, transform.position + step, out NavMeshHit hit, agent.areaMask))
        {
            step = Vector3.ProjectOnPlane(step, hit.normal);
            if (step.sqrMagnitude < 0.000001f ||
                NavMesh.Raycast(transform.position, transform.position + step, out _, agent.areaMask)) return;
        }
        agent.Move(step);
    }

    private void EndBossPush()
    {
        if (bossPushFrame < 0) return;
        if (resumeAgentAfterBossPush && agent != null)
        {
            agent.enabled = true;
            hasDestinationTargetPosition = false;
            nextPathUpdateTime = 0f;
        }
        resumeAgentAfterBossPush = false;
        bossPushFrame = -1;
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Upgrade") || other.CompareTag("Skill"))
        {
            Vector2 direction =
                (Vector2)transform.position - (Vector2)other.transform.position;
            StartKnockback(direction, knockbackPower);
        }
    }

    public bool CanKnockback()
    {
        return canKnockback;
    }

    public void StartKnockback(Vector2 direction, float power)
    {
        if (!gameObject.activeInHierarchy || !canKnockback ||
            agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            return;
        }

        canKnockback = false;

        if (knockbackCoroutine != null)
            StopCoroutine(knockbackCoroutine);

        knockbackCoroutine = StartCoroutine(
            KnockBack(direction.normalized, power));
    }

    private IEnumerator KnockBack(Vector2 direction, float power)
    {
        agent.isStopped = true;

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            float strength = 1f - elapsed / knockbackDuration;
            Vector3 delta =
                (Vector3)(direction * power * strength) * Time.deltaTime;

            agent.Warp(transform.position + delta);

            elapsed += Time.deltaTime;
            yield return null;
        }

        agent.isStopped = false;
        hasDestinationTargetPosition = false;
        Move();

        yield return new WaitForSeconds(knockbackCooldown);

        canKnockback = true;
        knockbackCoroutine = null;
    }
}
