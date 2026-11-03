using System.Collections.Generic;
using UnityEngine;

/// <summary>A pooled pixel bolt that hits the first enemy along its travel path.</summary>
public sealed class MageProjectile : MonoBehaviour
{
    [SerializeField] private int projectileDataId = 201;

    private Vector2 direction;
    private float speed;
    private float launchLifetime;
    private float remainingLifetime;
    private float damage;
    private int remainingPierces;
    private int chainCount;
    private float chainRange;
    private bool chainTriggered;
    private bool isFlying;
    private readonly HashSet<int> hitEnemies = new HashSet<int>();
    private readonly HashSet<int> chainedEnemies = new HashSet<int>();
    private EnemyCombat homingTarget;
    private TrailRenderer trail;
    private SkillInfo skillInfo;
    private PooledObject pooledObject;

    private void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        skillInfo = GetComponent<SkillInfo>();
        pooledObject = GetComponent<PooledObject>();
    }

    public void Launch(Vector2 position, Vector2 travelDirection,
        float travelSpeed, float lifetime, float hitDamage,
        int pierceCount = 0, int chainedTargets = 0, float chainedRange = 0f,
        EnemyCombat target = null, IEnumerable<int> visitedTargets = null)
    {
        transform.position = position;
        direction = travelDirection.sqrMagnitude > 0.0001f
            ? travelDirection.normalized
            : Vector2.right;
        transform.rotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        speed = travelSpeed;
        launchLifetime = lifetime;
        remainingLifetime = lifetime;
        damage = hitDamage;
        remainingPierces = Mathf.Max(0, pierceCount);
        chainCount = Mathf.Max(0, chainedTargets);
        chainRange = Mathf.Max(0f, chainedRange);
        chainTriggered = false;
        homingTarget = target;
        hitEnemies.Clear();
        chainedEnemies.Clear();
        if (visitedTargets != null)
        {
            foreach (int targetId in visitedTargets)
                chainedEnemies.Add(targetId);
        }
        if (skillInfo != null)
        {
            skillInfo.skillDamage = damage;
        }
        if (trail != null)
        {
            trail.emitting = false;
            trail.Clear();
            trail.emitting = true;
        }
        isFlying = true;
    }

    private void OnDisable()
    {
        isFlying = false;
    }

    public void ClearTrail()
    {
        if (trail == null)
            trail = GetComponent<TrailRenderer>();
        if (trail == null)
            return;

        trail.emitting = false;
        trail.Clear();
    }

    private void FixedUpdate()
    {
        if (!isFlying)
            return;

        if (homingTarget != null && homingTarget.gameObject.activeInHierarchy &&
            !homingTarget.isDead)
        {
            Vector2 targetDirection =
                (Vector2)homingTarget.transform.position - (Vector2)transform.position;
            if (targetDirection.sqrMagnitude > 0.0001f)
            {
                direction = targetDirection.normalized;
                transform.rotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            }
        }
        else
        {
            homingTarget = null;
        }

        float distance = speed * Time.fixedDeltaTime;
        transform.position += (Vector3)(direction * distance);
        remainingLifetime -= Time.fixedDeltaTime;
        if (remainingLifetime <= 0f)
            ReturnToPool();
    }

    /// <summary>몬스터의 적중 판정에서 이미 맞힌 대상인지 확인한다.</summary>
    public bool TryRegisterEnemyHit(EnemyCombat enemy)
    {
        if (!isFlying || enemy == null) return false;
        int enemyId = enemy.GetInstanceID();
        return !chainedEnemies.Contains(enemyId) && hitEnemies.Add(enemyId);
    }

    /// <summary>피해 판정 후 연쇄를 생성하고 관통 횟수를 소비한다.</summary>
    public void HandleEnemyContact(EnemyCombat enemy)
    {
        int enemyId = enemy.GetInstanceID();
        chainedEnemies.Add(enemyId);

        if (!chainTriggered && chainCount > 0 && chainRange > 0f)
        {
            chainTriggered = true;
            SpawnNextChainProjectile(enemy);
        }

        if (remainingPierces <= 0)
        {
            ReturnToPool();
            return;
        }

        remainingPierces--;
    }

    private void SpawnNextChainProjectile(EnemyCombat currentTarget)
    {
        if (PoolManager.Instance == null)
            return;

        Vector2 origin = currentTarget.transform.position;
        EnemyCombat nextTarget = FindNearestChainTarget(origin);
        if (nextTarget == null)
            return;

        Vector2 nextDirection =
            (Vector2)nextTarget.transform.position - origin;
        if (nextDirection.sqrMagnitude <= 0.0001f)
            return;

        nextDirection.Normalize();
        origin += nextDirection * 0.1f;
        GameObject projectileObject = PoolManager.Instance.Spawn(projectileDataId, origin);
        if (projectileObject == null)
            return;

        MageProjectile projectile = projectileObject.GetComponent<MageProjectile>();
        if (projectile == null)
        {
            projectileObject.GetComponent<PooledObject>()?.DestroyObj();
            return;
        }

        CopySortingTo(projectileObject);
        projectile.Launch(
            origin,
            nextDirection,
            speed,
            launchLifetime,
            damage,
            0,
            chainCount - 1,
            chainRange,
            nextTarget,
            chainedEnemies);
    }

    private EnemyCombat FindNearestChainTarget(Vector2 origin)
    {
        int enemyLayer = LayerMask.GetMask("Enemy");
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, chainRange, enemyLayer);
        EnemyCombat nearest = null;
        float nearestDistance = float.PositiveInfinity;

        foreach (Collider2D hit in hits)
        {
            EnemyCombat candidate = hit != null
                ? hit.GetComponentInParent<EnemyCombat>()
                : null;
            if (candidate == null || candidate.isDead ||
                !candidate.gameObject.activeInHierarchy ||
                chainedEnemies.Contains(candidate.GetInstanceID()))
            {
                continue;
            }

            float distance = ((Vector2)candidate.transform.position - origin).sqrMagnitude;
            if (distance >= nearestDistance)
                continue;

            nearest = candidate;
            nearestDistance = distance;
        }

        return nearest;
    }

    private void CopySortingTo(GameObject projectileObject)
    {
        SpriteRenderer sourceRenderer = GetComponent<SpriteRenderer>();
        SpriteRenderer targetRenderer = projectileObject.GetComponent<SpriteRenderer>();
        if (sourceRenderer != null && targetRenderer != null)
        {
            targetRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            targetRenderer.sortingOrder = sourceRenderer.sortingOrder;
        }

        TrailRenderer targetTrail = projectileObject.GetComponent<TrailRenderer>();
        if (trail != null && targetTrail != null)
        {
            targetTrail.sortingLayerID = trail.sortingLayerID;
            targetTrail.sortingOrder = trail.sortingOrder;
        }
    }

    private void ReturnToPool()
    {
        if (!isFlying)
            return;

        // A physics step can queue several contacts before the bolt is disabled.
        // End this flight before releasing it so those callbacks cannot release it twice.
        isFlying = false;
        hitEnemies.Clear();
        chainedEnemies.Clear();
        homingTarget = null;
        ClearTrail();
        if (pooledObject != null)
            pooledObject.DestroyObj();
        else
            gameObject.SetActive(false);
    }
}
