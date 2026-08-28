using UnityEngine;
using UnityEngine.Rendering;

public abstract class HomingProjectile : MonoBehaviour
{
    public int id;

    protected Rigidbody2D rb;
    protected PooledObject pooledObject;

    protected Vector2 moveDirection;

    protected virtual void Awake()
    {
        // Rigidbody2D가 없으면 추가
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0f;

        // PooledObject가 없으면 추가
        pooledObject = GetComponent<PooledObject>();

        if (pooledObject == null)
            pooledObject = gameObject.AddComponent<PooledObject>();

        // Collider2D가 하나도 없으면 추가
        Collider2D collider = GetComponent<Collider2D>();

        if (collider == null)
            collider = gameObject.AddComponent<BoxCollider2D>();
        
        SortingGroup sortingGroup = GetComponent<SortingGroup>();

        if (sortingGroup == null)
            sortingGroup = gameObject.AddComponent<SortingGroup>();

        sortingGroup.sortingLayerName = "Upgrade";

        // 트리거 활성화
        collider.isTrigger = true;
    }

    public void SetTarget(Transform target)
    {
        if (target == null)
        {
            MoveToDirection(transform.right);
            return;
        }

        Vector2 direction =
            (target.position - transform.position).normalized;

        MoveToDirection(direction);
    }

    protected void MoveToDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;

        RotateToDirection();

        float speed =
            DataManager.Instance.UpgradeDatasDic[id].speed;

        rb.linearVelocity = moveDirection * speed;
    }

    protected void RotateToDirection()
    {
        float angle =
            Mathf.Atan2(
                moveDirection.y,
                moveDirection.x
            ) * Mathf.Rad2Deg;

        transform.rotation =
            Quaternion.Euler(0f, 0f, angle);
    }

    protected int GetDamage()
    {
        int level =
            DataManager.Instance.UpgradeDatasDic[id].upgradeLevel;

        return DataManager.Instance.UpgradeDatasDic[id].damage[level];
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            EnemyCombat enemyCombat =
                other.GetComponent<EnemyCombat>();

            if (enemyCombat != null)
            {
                enemyCombat.TakeDamage(GetDamage());
            }

            if(DataManager.Instance.UpgradeDatasDic[id].hp <= 0)
                pooledObject.DestroyObj();
        }

        if (other.CompareTag("Wall"))
        {
            pooledObject.DestroyObj();
        }
    }

    protected virtual void OnDisable()
    {
        rb.linearVelocity = Vector2.zero;
    }
}