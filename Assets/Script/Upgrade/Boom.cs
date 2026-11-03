using UnityEngine;

/// <summary>Boom 증강 관련 동작과 상태를 관리한다.</summary>
public class Boom : MonoBehaviour
{
    private Animator animator;

    protected int id;

    private PooledObject pooledObject;

    /// <summary>필요한 컴포넌트와 초기 상태를 준비한다.</summary>
    private void Awake()
    {
        animator = GetComponent<Animator>();

        UpgradeInfo upgradeInfo =
            GetComponent<UpgradeInfo>();

        if (upgradeInfo != null)
            id = upgradeInfo.id;

        pooledObject =
            GetComponent<PooledObject>();
    }

    /// <summary>BoomStart 동작을 처리한다.</summary>
    public void BoomStart()
    {
        animator.SetTrigger("Boom");
    }

    /// <summary>BoomDamage 동작을 처리한다.</summary>
    public void BoomDamage()
    {
        UpgradeData data =
            DataManager.Instance.upgradeDatasDic[id];

        float range =
            data.explosionRange;

        float damage =
            data.boomDamage;

        // =====================================
        // 범위 내 적 탐색
        // =====================================

        Collider2D[] enemies =
            Physics2D.OverlapCircleAll(
                transform.position,
                range,
                LayerMask.GetMask("Enemy")
            );

        foreach (Collider2D col in enemies)
        {
            EnemyCombat enemy =
                col.GetComponent<EnemyCombat>();

            if (enemy == null)
                continue;

            if (enemy.isDead)
                continue;

            Vector2 hitPoint =
                col.ClosestPoint(
                    transform.position
                );

            enemy.TakeDamage(id, damage,
                hitPoint
            );
        }
    }

    // 폭발 애니메이션 마지막 프레임에 이벤트 추가
    /// <summary>BoomEnd 동작을 처리한다.</summary>
    public void BoomEnd()
    {
        if (pooledObject != null)
            pooledObject.DestroyObj();
    }

    /// <summary>DrawGizmosSelected 이벤트가 발생했을 때 필요한 처리를 수행한다.</summary>
    private void OnDrawGizmosSelected()
    {
        if (DataManager.Instance == null)
            return;

        if (!DataManager.Instance.upgradeDatasDic.ContainsKey(id))
            return;

        Gizmos.DrawWireSphere(
            transform.position,
            DataManager.Instance
                .upgradeDatasDic[id]
                .explosionRange
        );
    }
}