using UnityEngine;

/// <summary>FireStorm 증강 관련 동작과 상태를 관리한다.</summary>
public class FireStorm : HomingProjectile
{
    public bool isChain = true;

    public GameObject player;

    // 이 오브젝트와 충돌하면 무시할 타겟
    public GameObject removeFromTarget;

    /// <summary>활성화될 때 필요한 이벤트와 동작을 시작한다.</summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        SoundManager.Instance?.Play(SoundIds.FireballLaunch);
    }

    /// <summary>필요한 컴포넌트와 초기 상태를 준비한다.</summary>
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

    /// <summary>다른 콜라이더가 진입했을 때 적중 효과를 처리한다.</summary>
    public override void HandleEnemyContact(Collider2D other)
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
                        /// <summary>Vector2 동작을 처리한다.</summary>
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

    /// <summary>비활성화될 때 이벤트와 진행 중인 동작을 정리한다.</summary>
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
