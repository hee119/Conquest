using UnityEngine;

public class HeatWave : HomingProjectile
{

    private Boom boom;
    private bool isBoom;

    protected override void Awake()
    {
        base.Awake();

        boom = GetComponent<Boom>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        isBoom = false;

        SoundManager.Instance?.Play(SoundIds.FireballLaunch);
    }

    protected override void OnTriggerEnter2D(Collider2D other)
{
    if (isBoom)
        return;

    if (other.gameObject.layer ==
        LayerMask.NameToLayer("Enemy"))
    {
        EnemyCombat enemyCombat =
            other.GetComponent<EnemyCombat>();

        Collider2D targetCollider =
            other.GetComponent<Collider2D>();

        if (enemyCombat != null &&
            targetCollider != null)
        {
            Vector2 hitPoint =
                targetCollider.ClosestPoint(
                    transform.position
                );

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
            // 데미지 계산
            // =====================================

            float damage =
                GetDamage();

            // 공격력 적용
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

            enemyCombat.TakeDamage(
                damage,
                hitPoint
            );
        }

        isBoom = true;

        // 이동 정지
        rb.linearVelocity =
            Vector2.zero;

        // 폭발 시작
        if (boom != null)
        {
            SoundManager.Instance?.Play(SoundIds.HeatwaveExplosion);

            boom.BoomStart();
        }
        else
        {
            UnityEngine.Debug.LogError(
                "[HeatWave] Boom 컴포넌트가 없음"
            );

            pooledObject.DestroyObj();
        }
    }

    if (other.CompareTag("Wall"))
    {
        pooledObject.DestroyObj();
    }
}
}