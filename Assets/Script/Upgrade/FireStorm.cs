using UnityEngine;

public class FireStorm : HomingProjectile
{
    public bool isChain = true;

    public GameObject player;

    // 이 오브젝트와 충돌하면 무시할 타겟
    public GameObject removeFromTarget;

    protected override void OnEnable()
    {
        base.OnEnable();
        SoundManager.Instance?.Play(SoundIds.FireballLaunch);
    }

    protected override void Awake()
    {
        base.Awake();

        upgradeInfo = GetComponent<UpgradeInfo>();
        id = upgradeInfo.id;

        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0f;

        pooledObject = GetComponent<PooledObject>();

        if (pooledObject == null)
            pooledObject = gameObject.AddComponent<PooledObject>();

        Collider2D collider = GetComponent<Collider2D>();

        if (collider == null)
            collider = gameObject.AddComponent<BoxCollider2D>();

        collider.isTrigger = true;

        player =
            GameObject.FindGameObjectWithTag("Player");
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        // =====================================
        // 내가 무시해야 하는 타겟이면 아무것도 하지 않음
        // =====================================

        if (removeFromTarget != null &&
            other.gameObject == removeFromTarget)
        {
            return;
        }

        // =====================================
        // Enemy 충돌
        // =====================================

        if (other.gameObject.layer ==
            LayerMask.NameToLayer("Enemy"))
        {
            removeFromTarget =
                other.gameObject;

            EnemyCombat enemyCombat =
                other.GetComponent<EnemyCombat>();

            Collider2D targetCollider =
                other.GetComponent<Collider2D>();

            if (enemyCombat == null ||
                targetCollider == null)
            {
                return;
            }

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

            // =====================================
            // 체인 생성
            // =====================================

            if (isChain)
            {
                int chainCount =
                    DataManager.Instance
                        .upgradeDatasDic[id]
                        .chainCount;

                for (int i = 0;
                     i < chainCount;
                     i++)
                {
                    float angle =
                        360f / chainCount * i;

                    Vector2 direction =
                        new Vector2(
                            Mathf.Cos(
                                angle * Mathf.Deg2Rad
                            ),
                            Mathf.Sin(
                                angle * Mathf.Deg2Rad
                            )
                        );

                    GameObject fireStormObj =
                        PoolManager.Instance.Spawn(
                            id,
                            hitPoint
                        );

                    FireStorm fireStorm =
                        fireStormObj.GetComponent<FireStorm>();

                    fireStorm.removeFromTarget =
                        removeFromTarget;

                    fireStorm.isChain = false;

                    fireStorm.MoveToDirection(
                        direction
                    );
                }
            }

            // =====================================
            // HP 감소
            // =====================================

            hp -= 1;

            if (hp <= 0)
                pooledObject.DestroyObj();
        }

        // =====================================
        // Wall 충돌
        // =====================================

        if (other.CompareTag("Wall"))
        {
            pooledObject.DestroyObj();
        }
    }

    protected override void OnDisable()
    {
        rb.linearVelocity =
            Vector2.zero;

        target = null;

        isChain = true;

        // 풀링으로 재사용되므로 반드시 초기화
        removeFromTarget = null;
    }
}
