using UnityEngine;

public class Shuriken : MonoBehaviour
{
    public UpgradeInfo upgradeInfo;

    public void Awake()
    {
        upgradeInfo = GetComponent<UpgradeInfo>();
    }
    
   public void OnTriggerEnter2D(Collider2D collision)
{
    if (collision.CompareTag("Enemy"))
    {
        Collider2D targetCollider =
            collision.GetComponent<Collider2D>();

        EnemyCombat enemyCombat =
            collision.GetComponent<EnemyCombat>();

        if (targetCollider == null ||
            enemyCombat == null)
        {
            return;
        }

        Vector2 hitPoint =
            targetCollider.ClosestPoint(
                transform.position
            );

        // =====================================
        // 기본 데미지
        // =====================================

        UpgradeData upgradeData =
            DataManager.Instance
                .upgradeDatasDic[upgradeInfo.id];

        int level =
            upgradeData.UpgradeLevel;

        int damage =
            upgradeData.damage[level];

        // =====================================
        // 현재 캐릭터 스탯
        // =====================================

        int characterId =
            CharacterManager.Instance.currentCharacter;

        PlayerData playerData =
            DataManager.Instance
                .playerDatasDic[characterId];

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
        // 데미지 적용
        // =====================================

        enemyCombat.TakeDamage(
            damage,
            hitPoint
        );
    }
}
}
