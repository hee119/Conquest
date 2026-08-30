using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class DotDamage : MonoBehaviour
{
    protected int id;
    protected UpgradeInfo upgradeInfo;
    protected Animator animator;
    protected Coroutine dotDamage;
    protected Coroutine stopAfterSeconds;
    protected EnemyCombat enemyCombat;
    protected Collider2D col;
    protected PooledObject pooledObject;
    protected Rigidbody2D rb;
    protected Collider2D collider2D;

    protected void Awake()
    {
        upgradeInfo = GetComponent<UpgradeInfo>();
        animator = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        pooledObject = GetComponent<PooledObject>();
        rb = GetComponent<Rigidbody2D>();
        collider2D = GetComponent<Collider2D>();
        id = upgradeInfo.id;
    }

    protected virtual void OnDisable()
    {
        if (stopAfterSeconds != null)
            StopCoroutine(stopAfterSeconds);

        if (dotDamage != null)
            StopCoroutine(dotDamage);

        stopAfterSeconds = null;
        dotDamage = null;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        col.enabled = true;
    }

    protected void OnEnable()
    {
        col.enabled = true;
        animator.SetBool("Idle", true);
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            col.enabled = false;
            enemyCombat = other.GetComponent<EnemyCombat>();
            Collider2D targetCollider = other.GetComponent<Collider2D>();
            Vector2 hitPoint = targetCollider.ClosestPoint(transform.position);
            if (enemyCombat != null)
            {
                enemyCombat.TakeDamage(GetDamage(), hitPoint);

                if (enemyCombat.isDead)
                {
                    pooledObject.DestroyObj();
                }
            }

            if (gameObject.activeInHierarchy)
                dotDamage = StartCoroutine(PoisonDamage(other.gameObject));

            return;
        }

        if (other.CompareTag("Wall"))
        {
            pooledObject.DestroyObj();
        }
    }

    protected int GetDamage()
    {
        int level =
            DataManager.Instance.UpgradeDatasDic[id].upgradeLevel;

        return DataManager.Instance.UpgradeDatasDic[id].damage[level];
    }

    protected IEnumerator PoisonDamage(GameObject target = null)
    {
        if (target != null)
        {
            gameObject.transform.SetParent(target.transform);
            rb.linearVelocity = Vector3.zero;
            gameObject.transform.localPosition = Vector3.zero;
        }

        if (gameObject.activeInHierarchy)
            stopAfterSeconds = StartCoroutine(
                StopAfterSeconds(
                    DataManager.Instance.UpgradeDatasDic[id]
                        .dotDuration[
                            DataManager.Instance.UpgradeDatasDic[id].upgradeLevel
                        ],
                    dotDamage
                )
            );

        animator.SetBool("Idle", false);

        while (gameObject.activeInHierarchy)
        {
            animator.SetTrigger("Dot");
            Vector2 hitPoint = target.GetComponent<Collider2D>().ClosestPoint(transform.position);
            
            enemyCombat.TakeDamage(
                DataManager.Instance.UpgradeDatasDic[id]
                    .dotDamage[
                        DataManager.Instance.UpgradeDatasDic[id].upgradeLevel
                    ], hitPoint
            );

            if (enemyCombat.isDead)
            {
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

    protected IEnumerator StopAfterSeconds(float time, Coroutine coroutine)
    {
        yield return new WaitForSeconds(time);

        if (coroutine != null)
            StopCoroutine(coroutine);

        stopAfterSeconds = null;
        dotDamage = null;
        pooledObject.DestroyObj();
    }
}