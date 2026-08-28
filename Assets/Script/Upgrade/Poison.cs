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
        if (stopAfterSeconds != null)
            StopCoroutine(stopAfterSeconds);

        if (dotDamage != null)
            StopCoroutine(dotDamage);

        stopAfterSeconds = null;
        dotDamage = null;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }
    
    private void OnEnable()
    {
        animator.SetBool("Idle", true);
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

            if(gameObject.activeInHierarchy)
                dotDamage = StartCoroutine(PoisonDamage(other.gameObject));
            return;
        }

        if (other.CompareTag("Wall"))
        {
            pooledObject.DestroyObj();
        }
    }

    IEnumerator PoisonDamage(GameObject target = null)
    {
        if (target != null)
        {
            gameObject.transform.SetParent(target.transform);
            rb.linearVelocity = Vector3.zero;
            transform.position = target.transform.position;
        }
        
        if(gameObject.activeInHierarchy)
            stopAfterSeconds = StartCoroutine(StopAfterSeconds(DataManager.Instance.UpgradeDatasDic[id].dotDuration[DataManager.Instance.UpgradeDatasDic[id].upgradeLevel], dotDamage));
        
        animator.SetBool("Idle", false);

        while (gameObject.activeInHierarchy)
        {
            animator.SetTrigger("Dot");

            enemyCombat.TakeDamage(
                DataManager.Instance.UpgradeDatasDic[id].dotDamage[DataManager.Instance.UpgradeDatasDic[id].upgradeLevel]
            );
            
            if (enemyCombat.isDead)
            {
                gameObject.transform.SetParent(null);

                // Dot 애니메이션 끝날 때까지 대기
                yield return new WaitUntil(() =>
                    animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f
                );
                
                pooledObject.DestroyObj();
                yield break;
            }

            yield return new WaitForSeconds(
                DataManager.Instance.UpgradeDatasDic[id].dotInterval
            );
        }
    }
    
    private IEnumerator StopAfterSeconds(float time, Coroutine coroutine)
    {
        yield return new WaitForSeconds(time);
        if(coroutine != null) StopCoroutine(coroutine);
        stopAfterSeconds = null;
        dotDamage = null;
        pooledObject.DestroyObj();
    }
}
