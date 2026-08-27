using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Poison : HomingProjectile
{
    Animator animator;
    private Coroutine dotDamage;
    private Coroutine stopAfterSeconds;
    private EnemyCombat enemyCombat;
    protected override void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        pooledObject = GetComponent<PooledObject>();
        animator = GetComponent<Animator>();
    }

    protected override void OnDisable()
    {
        animator.SetBool("Poison", false);
        StopCoroutine(stopAfterSeconds);
        StopCoroutine(dotDamage);
    }
    
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            enemyCombat = other.GetComponent<EnemyCombat>();

            if (enemyCombat != null)
            {
                enemyCombat.TakeDamage(GetDamage());
            }

            dotDamage = StartCoroutine(PoisonDamage());
            return;
        }

        if (other.CompareTag("Wall"))
        {
            pooledObject.DestroyObj();
        }
    }

    IEnumerator PoisonDamage()
    {
        animator.SetBool("Poison", true);
        stopAfterSeconds = StartCoroutine(StopAfterSeconds(DataManager.Instance.UpgradeDatasDic[id].dotDuration[DataManager.Instance.UpgradeDatasDic[id].upgradeLevel], dotDamage));
        
        while (true)
        {
            enemyCombat.TakeDamage(
                DataManager.Instance.UpgradeDatasDic[id].dotDamage[DataManager.Instance.UpgradeDatasDic[id].upgradeLevel]
            );

            yield return new WaitForSeconds(
                DataManager.Instance.UpgradeDatasDic[id].dotInterval
            );
        }
    }
    
    private IEnumerator StopAfterSeconds(float time, Coroutine coroutine)
    {
        yield return new WaitForSeconds(time);
        StopCoroutine(coroutine);
        stopAfterSeconds = null;
        dotDamage = null;
        pooledObject.DestroyObj();
    }
}
