using UnityEngine;

public class Boom : MonoBehaviour
{
    private Animator animator;

    protected int id;

    private PooledObject pooledObject;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        UpgradeInfo upgradeInfo = GetComponent<UpgradeInfo>();

        if (upgradeInfo != null)
            id = upgradeInfo.id;

        pooledObject = GetComponent<PooledObject>();
    }

    public void BoomStart()
    {
        animator.SetTrigger("Boom");
    }

    public void BoomDamage()
    {
        UpgradeData data =
            DataManager.Instance.upgradeDatasDic[id];

        float range = data.explosionRange;
        float damage = data.boomDamage;

        // 범위 내 적 탐색
        Collider2D[] enemies = Physics2D.OverlapCircleAll(
            transform.position,
            range,
            LayerMask.GetMask("Enemy")
        );

        foreach (Collider2D col in enemies)
        {
            EnemyCombat enemy = col.GetComponent<EnemyCombat>();

            if (enemy == null)
                continue;

            if (enemy.isDead)
                continue;

            Vector2 hitPoint = col.ClosestPoint(
                transform.position
            );

            enemy.TakeDamage(id, damage, hitPoint);
        }
    }

    // 폭발 애니메이션 종료
    public void BoomEnd()
    {
        if (pooledObject != null)
            pooledObject.DestroyObj();
    }

#if UNITY_EDITOR

    [Header("폭발 범위 디버그")]
    [SerializeField] private bool showExplosionRange = true;

    private void OnDrawGizmos()
    {
        if (!showExplosionRange)
            return;

        if (!gameObject.activeInHierarchy)
            return;

        if (DataManager.Instance == null)
            return;

        if (!DataManager.Instance.upgradeDatasDic.TryGetValue(
                id, out var data))
            return;

        float range = data.explosionRange;

        // 반투명 빨간색 영역
        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        Gizmos.DrawSphere(transform.position, range);

        // 빨간색 테두리
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }

#endif
}