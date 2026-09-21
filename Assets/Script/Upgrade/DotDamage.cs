using System.Collections;
using UnityEngine;

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

    protected virtual void Awake()
    {
        upgradeInfo = GetComponent<UpgradeInfo>();
        animator = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        pooledObject = GetComponent<PooledObject>();
        rb = GetComponent<Rigidbody2D>();

        id = upgradeInfo.id;
    }
    
    

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
                    yield return PlayDotAnimation();
                }

                break;
            }

            // =========================================
            // Dot 애니메이션
            // =========================================

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
        yield return new WaitUntil(() =>
        {
            if (animator == null)
                return true;

            AnimatorStateInfo state =
                animator.GetCurrentAnimatorStateInfo(0);

            return state.IsName("Dot");
        });

        // Dot 애니메이션 종료 대기
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
        // 현재 캐릭터 스탯
        // =====================================

        int characterId =
            CharacterManager.Instance.currentCharacter;

        PlayerData playerData =
            DataManager.Instance.playerDatasDic[characterId];

        int playerDamage =
            playerData.damage;

        int critChance =
            playerData.criticalChance;

        float critDamage =
            playerData.criticalDamage;

        // =====================================
        // 공격력 적용
        // =====================================

        int finalDamage =
            (int)(
                damage *
                (1f + playerDamage / 100f)
            );

        // =====================================
        // 크리티컬 판정
        // DOT 1틱마다 한 번 판정
        // =====================================

        bool isCritical =
            Random.Range(0, 100) < critChance;

        if (isCritical)
        {
            finalDamage =
                (int)(
                    finalDamage *
                    (2f + critDamage / 100f)
                );
        }

        // =====================================
        // 피격 위치
        // =====================================

        Vector2 hitPoint =
            targetCollider.ClosestPoint(
                transform.position
            );

        enemyCombat.TakeDamage(
            finalDamage,
            hitPoint
        );
    }

    protected virtual void OnTriggerEnter2D(
        Collider2D other
    )
    {
        if (other.CompareTag("Wall"))
        {
            pooledObject.DestroyObj();
        }
    }

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