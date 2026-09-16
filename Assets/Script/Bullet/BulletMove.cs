using System;
using UnityEngine;

public class BulletMove : MonoBehaviour
{
    BulletInfo bulletInfo;
    PooledObject pooledObject;
    private GameObject target;
    Rigidbody2D rb;
    public GameObject Target
    {
        get
        {
            return target;
        }
        set
        {
            target = value;
            Move();
        }
    }

   
    public void Awake()
    {
        bulletInfo = GetComponent<BulletInfo>();
        pooledObject = GetComponent<PooledObject>();
        rb = GetComponent<Rigidbody2D>();
    }

   

    public void Move()
    {
        Vector2 direction = target.transform.position - transform.position;
        rb.linearVelocity = direction.normalized * bulletInfo.speed;
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        Vector2 hitPoint = other.ClosestPoint(transform.position);

        if (other.CompareTag(target.tag))
        {
            if (target.CompareTag("Player"))
            {
                other.GetComponent<PlayerCombat>().TakeDamage(bulletInfo.damage);
            }
            else if (other.CompareTag("Enemy"))
            {
                other.GetComponent<EnemyCombat>().TakeDamage(bulletInfo.damage, hitPoint);
            }
            
            pooledObject.DestroyObj();
        }
    }
}
