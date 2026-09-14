using UnityEngine;
using UnityEngine.Rendering;

public class FireStorm : HomingProjectile
{
    public bool isChain = true;
    public GameObject player;

    protected override void Awake()
    {
        base.Awake(); // 추가 — 부모의 target/speed 등 이동 관련 초기화 실행

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

        SortingGroup sortingGroup = GetComponent<SortingGroup>();
        if (sortingGroup == null)
            sortingGroup = gameObject.AddComponent<SortingGroup>();

        sortingGroup.sortingLayerName = "Upgrade";
        collider.isTrigger = true;

        player = GameObject.FindGameObjectWithTag("Player");
    }
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer ==
            LayerMask.NameToLayer("Enemy"))
        {
            EnemyCombat enemyCombat =
                other.GetComponent<EnemyCombat>();

            Collider2D targetCollider =
                other.GetComponent<Collider2D>();

            Vector2 hitPoint =
                targetCollider.ClosestPoint(
                    transform.position
                );

            if (enemyCombat != null)
            {
                enemyCombat.TakeDamage(
                    GetDamage(),
                    hitPoint
                );
            }

            if (isChain)
            {
                int chainCount =
                    DataManager.Instance.UpgradeDatasDic[id].chainCount;

                for (int i = 0; i < chainCount; i++)
                {
                    float angle = 360f / chainCount * i;

                    Vector2 direction =
                        new Vector3(
                            Mathf.Cos(angle * Mathf.Deg2Rad),
                            Mathf.Sin(angle * Mathf.Deg2Rad)
                        );

                    GameObject fireStormObj =
                        PoolManager.Instance.Spawn(id, hitPoint);

                    FireStorm fireStorm =
                        fireStormObj.GetComponent<FireStorm>();
                    
                    fireStorm.isChain = false;
                    fireStorm.MoveToDirection(direction);                }
            }

            hp -= 1;
            if (hp <= 0)
                pooledObject.DestroyObj();
        }

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
    }
}
