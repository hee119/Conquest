using UnityEngine;

public class Boom : MonoBehaviour
{
    private Animator animator;

    protected int id;

    private PooledObject pooledObject;

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

    public void BoomStart()
    {
        animator.SetTrigger("Boom");
    }

    public void BoomDamage()
    {
        UpgradeData data =
            DataManager.Instance.upgradeDatasDic[id];

        float range =
            data.explosionRange;

        int damage =
            data.boomDamage;

        // =====================================
        // 현재 캐릭터 스탯
        // =====================================

        int characterId =
            CharacterManager.Instance.currentCharacter;

        PlayerData playerData =
            DataManager.Instance.playerDatasDic[characterId];

        int playerDamage =
            playerData.damage;

        int critChance =
            playerData.criticalChance;

        float critDamage =
            playerData.criticalDamage;

        // =====================================
        // 공격력 적용
        // =====================================

        damage =
            (int)(
                damage *
                (1f + playerDamage / 100f)
            );

        // =====================================
        // 크리티컬 판정
        // 폭발 1회당 한 번만 판정
        // =====================================

        bool isCritical =
            Random.Range(0, 100) < critChance;

        if (isCritical)
        {
            damage =
                (int)(
                    damage *
                    (2f + critDamage / 100f)
                );
        }

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

            enemy.TakeDamage(
                damage,
                hitPoint
            );
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