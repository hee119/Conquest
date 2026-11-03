using System.Collections;
using UnityEngine;

/// <summary>Lightning 증강 관련 동작과 상태를 관리한다.</summary>
public class Lightning : DetectUpgrade
{
    [SerializeField] protected LineRenderer lineRenderer;
    [SerializeField] protected float height = 6f;
    [SerializeField] protected float strikeDuration = 0.15f;
    [SerializeField] protected float damageDistance = 0.1f;

    private int id;
    private bool strikeRunning;

    protected override float ChainAttackInterval => strikeDuration;

    /// <summary>필요한 컴포넌트와 초기 상태를 준비한다.</summary>
    protected override void Awake()
    {
        base.Awake();
        id = GetComponent<UpgradeInfo>().id;
        lineRenderer = GetComponent<LineRenderer>();
    }

    /// <summary>활성화될 때 필요한 이벤트와 동작을 시작한다.</summary>
    protected virtual void OnEnable()
    {
        strikeRunning = false;
        ClearLine();
    }

    /// <summary>비활성화될 때 이벤트와 진행 중인 동작을 정리한다.</summary>
    protected virtual void OnDisable()
    {
        StopAllCoroutines();
        strikeRunning = false;
        ClearLine();
    }

    /// <summary>Attack 동작을 처리한다.</summary>
    public override void Attack(
        int id,
        Vector3 targetPos,
        GameObject target = null,
        bool isChain = false,
        Vector3? startPosition = null,
        bool destroyAfter = true)
    {
        if (!isActiveAndEnabled || strikeRunning)
            return;

        strikeRunning = true;
        StartCoroutine(Strike(targetPos, target,
            isChain ? startPosition : null, destroyAfter));
    }

    /// <summary>Strike 동작을 처리한다.</summary>
    private IEnumerator Strike(
        Vector3 targetPos,
        GameObject target,
        Vector3? startPos,
        bool destroyAfter)
    {
        EnemyCombat enemy = target != null ? target.GetComponent<EnemyCombat>() : null;
        PooledObject targetPool = enemy != null ? enemy.GetComponent<PooledObject>() : null;
        uint targetVersion = targetPool != null ? targetPool.SpawnVersion : 0;

        try
        {
            if (lineRenderer == null || !IsValidTarget(enemy, targetPool, targetVersion) ||
                !TryGetDamage(out float damage))
                yield break;

            Vector3 strikeStart = startPos ?? (targetPos + Vector3.up * height);
            float duration = Mathf.Max(0.001f, strikeDuration);
            float elapsed = 0f;
            bool damaged = false;
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = true;

            while (elapsed < duration)
            {
                // A pooled enemy may have died and been reused at another position.
                if (!IsValidTarget(enemy, targetPool, targetVersion))
                    yield break;

                Vector3 endPos = enemy.transform.position;
                elapsed += Time.deltaTime;
                Vector3 currentPos = Vector3.Lerp(strikeStart, endPos,
                    Mathf.Clamp01(elapsed / duration));
                lineRenderer.SetPosition(0, strikeStart);
                lineRenderer.SetPosition(1, currentPos);

                if (!damaged && (currentPos - endPos).sqrMagnitude <=
                    damageDistance * damageDistance)
                {
                    damaged = true;
                    DealDamage(enemy, damage);
                }

                yield return null;
            }

            if (!damaged && IsValidTarget(enemy, targetPool, targetVersion))
                DealDamage(enemy, damage);
        }
        finally
        {
            strikeRunning = false;
            ClearLine();
            if (destroyAfter && pooledObject != null && pooledObject.IsSpawned)
                pooledObject.DestroyObj();
        }
    }

    /// <summary>IsValidTarget 동작을 처리한다.</summary>
    private static bool IsValidTarget(EnemyCombat enemy, PooledObject targetPool, uint version)
    {
        return enemy != null && !enemy.isDead && enemy.gameObject.activeInHierarchy &&
            (targetPool == null || targetPool.SpawnVersion == version);
    }

    /// <summary>GetDamage 동작의 가능 여부를 확인하고 처리한다.</summary>
    private bool TryGetDamage(out float damage)
    {
        damage = 0f;
        if (DataManager.Instance == null ||
            !DataManager.Instance.upgradeDatasDic.TryGetValue(id, out var data))
            return false;

        int level = data.UpgradeLevel;
        if (data.damage == null || level < 0 || level >= data.damage.Length)
            return false;

        damage = data.damage[level];
        return true;
    }

    /// <summary>DealDamage 동작을 처리한다.</summary>
    private void DealDamage(EnemyCombat enemy, float damage)
    {
        Collider2D targetCollider = enemy.GetComponent<Collider2D>();
        if (targetCollider == null)
            return;

        Vector2 hitPoint = targetCollider.ClosestPoint(transform.position);
        SoundManager.Instance?.Play(SoundIds.LightningStrike, enemy: enemy);
        enemy.TakeDamage(id, damage, hitPoint);
    }

    /// <summary>ClearLine 동작을 처리한다.</summary>
    private void ClearLine()
    {
        if (lineRenderer == null)
            return;
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
    }
}
