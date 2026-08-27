using UnityEngine;

public abstract class HomingProjectile : MonoBehaviour
{
    public int id;

    protected Rigidbody2D rb;
    protected PooledObject pooledObject;

    protected Vector2 moveDirection;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        pooledObject = GetComponent<PooledObject>();
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