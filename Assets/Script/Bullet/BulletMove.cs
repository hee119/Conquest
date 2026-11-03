using System;
using UnityEngine;

public class BulletMove : MonoBehaviour
{
    protected BulletInfo bulletInfo;
    protected PooledObject pooledObject;
    public GameObject target;
    protected Rigidbody2D rb;

   
    public void Awake()
    {
        bulletInfo = GetComponent<BulletInfo>();
        pooledObject = GetComponent<PooledObject>();
        rb = GetComponent<Rigidbody2D>();
    }

   

    public virtual void Move()
    {
        Vector2 direction = target.transform.position - transform.position;
        rb.linearVelocity = direction.normalized * bulletInfo.speed;
    }

    public void MoveInDirection(Vector2 direction)
    {
        if (rb == null || bulletInfo == null) return;
        rb.linearVelocity = direction.normalized * bulletInfo.speed;
    }

    public virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy")) return;

        if (target != null && other.CompareTag(target.tag))
        {
            if (target.CompareTag("Player"))
            {
                other.GetComponent<PlayerCombat>().TakeDamage(bulletInfo.damage);
            }
            pooledObject.DestroyObj();
        }
    }
}
