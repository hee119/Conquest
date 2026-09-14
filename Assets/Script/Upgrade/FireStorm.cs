using UnityEngine;
using UnityEngine.Rendering;

public class FireStorm : HomingProjectile
{
    public bool isChain = true;

    public GameObject player;

    // 이 오브젝트와 충돌하면 무시할 타겟
    public GameObject removeFromTarget;

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

        SortingGroup sortingGroup =
            GetComponent<SortingGroup>();

        if (sortingGroup == null)
            sortingGroup = gameObject.AddComponent<SortingGroup>();

        sortingGroup.sortingLayerName = "Upgrade";

        collider.isTrigger = true;

        player = GameObject.FindGameObjectWithTag("Player");
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
            // 내가 맞은 적을 저장
            removeFromTarget = other.gameObject;

            EnemyCombat enemyCombat =
                other.GetComponent<EnemyCombat>();

            Collider2D targetCollider =
                other.GetComponent<Collider2D>();

            Vector2 hitPoint =
                targetCollider.ClosestPoint(
                    transform.position
                );

            // =====================================
            // 데미지
            // =====================================

            if (enemyCombat != null)
            {
                enemyCombat.TakeDamage(
                    GetDamage(),
                    hitPoint
                );
            }

            // =====================================
            // 체인 생성
            // =====================================

            if (isChain)
            {
                int chainCount =
                    DataManager.Instance
                    .UpgradeDatasDic[id]
                    .chainCount;

                for (int i = 0; i < chainCount; i++)
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

                    // 체인으로 생성된 FireStorm는
                    // 원래 맞았던 적을 무시
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
        rb.linearVelocity = Vector2.zero;

        target = null;

        isChain = true;

        // 풀링으로 재사용되므로 반드시 초기화
        removeFromTarget = null;
    }
}