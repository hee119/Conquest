using System.Collections;
using UnityEngine;

/// <summary>DotDamage 증강 관련 동작과 상태를 관리한다.</summary>
public class DotDamage : MonoBehaviour
{
    protected int id;

    protected UpgradeInfo upgradeInfo;
    protected Animator animator;
    protected Coroutine dotDamage;

    protected EnemyCombat enemyCombat;

    protected Collider2D col;
    protected PooledObject pooledObject;
    protected Rigidbody2D rb;

    /// <summary>필요한 컴포넌트와 초기 상태를 준비한다.</summary>
    protected virtual void Awake()
    {
        upgradeInfo = GetComponent<UpgradeInfo>();
        animator = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        pooledObject = GetComponent<PooledObject>();
        rb = GetComponent<Rigidbody2D>();

        id = upgradeInfo.id;
    }
    
    

    /// <summary>활성화될 때 필요한 이벤트와 동작을 시작한다.</summary>
    protected virtual void OnEnable()
    {
        if (col != null)
            col.enabled = true;

        if (animator != null)
        {
            animator.ResetTrigger("Dot");
            animator.SetBool("Idle", true);
        }

        enemyCombat = null;
        dotDamage = null;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
    }

    /// <summary>
    /// Poison에서 타겟을 전달받음.
    /// 거리 검사는 Poison에서 담당.
    /// 여기서는 실제 DOT만 시작한다.
    /// </summary>
    public virtual void SetTarget(GameObject target)
    {
        if (target == null)
            return;

        if (target.layer != LayerMask.NameToLayer("Enemy"))
            return;

        EnemyCombat enemy =
            target.GetComponent<EnemyCombat>();

        if (enemy == null)
            return;

        enemyCombat = enemy;

        // =========================================
        // 이동 정지
        // =========================================

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // =========================================
        // Collider 비활성화
        // =========================================

        if (col != null)
            col.enabled = false;

        // =========================================
        // 적이 이미 죽어있음
        //
        // Dot 1회만 재생하고 종료
        // =========================================

        if (enemyCombat.isDead)
        {
            if (gameObject.activeInHierarchy)
            {
                dotDamage =
                    StartCoroutine(
                        PlayDeadTargetDot()
                    );
            }

            return;
        }
        
        if (gameObject.activeInHierarchy)
        {
            dotDamage =
                StartCoroutine(
                    PoisonDamage()
                );
        }
        
        transform.SetParent(
            enemy.transform
        );

        transform.localPosition =
            Vector3.zero;
    }

    /// <summary>
    /// 실제 DOT 처리
    /// </summary>
    protected IEnumerator PoisonDamage()
    {
        if (enemyCombat == null)
        {
            pooledObject.DestroyObj();
            yield break;
        }

        int level =
            DataManager.Instance
                .upgradeDatasDic[id]
                .UpgradeLevel;

        int dotCount =
            DataManager.Instance
                .upgradeDatasDic[id]
                .dotCount[level];

        float dotDamageValue =
            DataManager.Instance
                .upgradeDatasDic[id]
                .dotDamage[level];

        int playedCount = 0;

        // =========================================
        // DOT 반복
        // =========================================

        while (playedCount < dotCount)
        {
            // 타겟 자체가 사라짐
            if (enemyCombat == null)
                break;

            // =========================================
            // 적이 죽어있음
            //
            // 지금까지 Dot을 하나도 안 했다면
            // Dot 1회 재생
            //
            // 이미 하나라도 했다면
            // 추가 Dot 없이 종료
            // =========================================

            if (enemyCombat.isDead)
            {
                if (playedCount == 0)
                {
                    /// <summary>PlayDotAnimation 동작을 처리한다.</summary>
                    yield return PlayDotAnimation();
                }

                break;
            }

            // =========================================
            // Dot 애니메이션
            // =========================================

            /// <summary>PlayDotAnimation 동작을 처리한다.</summary>
            yield return PlayDotAnimation();

            playedCount++;

            // =========================================
            // 애니메이션 도중 적이 죽었음
            //
            // 현재 애니메이션은 이미 끝났으므로
            // 추가 데미지 없이 종료
            // =========================================

            if (enemyCombat == null)
                break;

            if (enemyCombat.isDead)
                break;

            // =========================================
            // DOT 데미지
            // =========================================

            DealDamage(dotDamageValue);

            // =========================================
            // 이번 데미지로 적 사망
            // =========================================

            if (enemyCombat == null)
                break;

            if (enemyCombat.isDead)
                break;

            // =========================================
            // 다음 DOT 데이터 갱신
            // =========================================

            level =
                DataManager.Instance
                    .upgradeDatasDic[id]
                    .UpgradeLevel;

            dotCount =
                DataManager.Instance
                    .upgradeDatasDic[id]
                    .dotCount[level];

            dotDamageValue =
                DataManager.Instance
                    .upgradeDatasDic[id]
                    .dotDamage[level];
        }

        pooledObject.DestroyObj();
    }

    /// <summary>
    /// 타겟이 이미 죽어있는 경우
    /// Dot 1회만 재생하고 종료
    /// </summary>
    protected IEnumerator PlayDeadTargetDot()
    {
        /// <summary>PlayDotAnimation 동작을 처리한다.</summary>
        yield return PlayDotAnimation();

        pooledObject.DestroyObj();
    }

    /// <summary>
    /// Dot 애니메이션을 끝까지 재생
    /// </summary>
    protected IEnumerator PlayDotAnimation()
    {
        if (animator == null)
            yield break;

        animator.SetBool("Idle", false);

        animator.ResetTrigger("Dot");
        animator.SetTrigger("Dot");

        // Animator 상태 변경 대기
        yield return null;

        // Dot 상태 진입 대기
        /// <summary>WaitUntil 동작을 처리한다.</summary>
        yield return new WaitUntil(() =>
        {
            if (animator == null)
                return true;

            AnimatorStateInfo state =
                animator.GetCurrentAnimatorStateInfo(0);

            return state.IsName("Dot");
        });

        // Dot 애니메이션 종료 대기
        /// <summary>WaitUntil 동작을 처리한다.</summary>
        yield return new WaitUntil(() =>
        {
            if (animator == null)
                return true;

            AnimatorStateInfo state =
                animator.GetCurrentAnimatorStateInfo(0);

            return state.IsName("Dot") &&
                   state.normalizedTime >= 1f;
        });
    }

    /// <summary>
    /// 현재 타겟에게 DOT 데미지
    /// </summary>
    protected void DealDamage(float damage)
    {
        if (enemyCombat == null)
            return;

        if (enemyCombat.isDead)
            return;

        Collider2D targetCollider =
            enemyCombat.GetComponent<Collider2D>();

        if (targetCollider == null)
            return;

        // =====================================
        // 피격 위치
        // =====================================

        Vector2 hitPoint =
            targetCollider.ClosestPoint(
                transform.position
            );

        enemyCombat.TakeDamage(id, damage,
            hitPoint
        );
    }

    /// <summary>다른 콜라이더가 진입했을 때 적중 효과를 처리한다.</summary>
    protected virtual void OnTriggerEnter2D(
        Collider2D other
    )
    {
        if (other.CompareTag("Wall"))
        {
            pooledObject.DestroyObj();
        }
    }

    /// <summary>비활성화될 때 이벤트와 진행 중인 동작을 정리한다.</summary>
    protected virtual void OnDisable()
    {
        // =========================================
        // 코루틴 정리
        // =========================================

        if (dotDamage != null)
        {
            StopCoroutine(dotDamage);
            dotDamage = null;
        }


        enemyCombat = null;

        // Rigidbody 초기화
        // =========================================

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Dynamic;
        }

        // =========================================
        // Collider 초기화
        // =========================================

        if (col != null)
            col.enabled = true;
    }

    /// <summary>Damage 관련 값을 조회한다.</summary>
    protected float GetDamage()
    {
        int level =
            DataManager.Instance
                .upgradeDatasDic[id]
                .UpgradeLevel;

        return DataManager.Instance
            .upgradeDatasDic[id]
            .damage[level];
    }
}