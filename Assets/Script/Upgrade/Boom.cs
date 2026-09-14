using UnityEngine;

public class Boom : MonoBehaviour
{
    private Animator animator;

    protected int id;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        UpgradeInfo upgradeInfo = GetComponent<UpgradeInfo>();

        if (upgradeInfo != null)
            id = upgradeInfo.id;
    }

    public void BoomStart()
    {
        animator.SetTrigger("Boom");
    }

    // 애니메이션 이벤트에서 호출
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