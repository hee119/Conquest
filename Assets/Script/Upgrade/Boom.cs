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
            DataManager.Instance.UpgradeDatasDic[id];

        float range = data.explosionRange;
        int damage = data.boomDamage;

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

            Vector2 hitPoint =
                col.ClosestPoint(transform.position);

            enemy.TakeDamage(damage, hitPoint);
        }
    }

    // 폭발 애니메이션 마지막 프레임에 이벤트 추가
    public void BoomEnd()
    {
        if (pooledObject != null)
            pooledObject.DestroyObj();
    }

    private void OnDrawGizmosSelected()
    {
        if (DataManager.Instance == null)
            return;

        if (!DataManager.Instance.UpgradeDatasDic.ContainsKey(id))
            return;

        Gizmos.DrawWireSphere(
            transform.position,
            DataManager.Instance.UpgradeDatasDic[id].explosionRange
        );
    }
}