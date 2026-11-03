using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>화상 상태와 지속 피해를 관리한다.</summary>
public class Burn : DotDamage
{
    private List<EnemyCombat> enemyCombatList = new();

    /// <summary>TargetBurn 동작을 처리한다.</summary>
    public void TargetBurn(GameObject target)
    {
        Debug.Log(
            $"[Burn] ===== TargetBurn START =====\n" +
            $"target={(target != null ? target.name : "NULL")}"
        );

        if (target == null)
            return;

        if (target.layer != LayerMask.NameToLayer("Enemy"))
            return;

        EnemyCombat targetEnemy =
            target.GetComponent<EnemyCombat>();

        if (targetEnemy == null)
            return;

        // 이미 Burn 중이면 무시
        if (targetEnemy.isBurn)
            return;

        // 리스트에 추가
        if (!enemyCombatList.Contains(targetEnemy))
            enemyCombatList.Add(targetEnemy);

        // 타겟 저장
        enemyCombat = targetEnemy;

        Collider2D targetCol =
            target.GetComponent<Collider2D>();

        if (targetCol == null)
            return;

        // =========================================
        // 타겟에게 붙기
        // =========================================

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (col != null)
            col.enabled = false;

        if (enemyCombat.isDead)
        {
            pooledObject.DestroyObj();
            return;
        }
        
        transform.SetParent(target.transform);
        transform.localPosition = Vector3.zero;

        // Burn 상태
        

            enemyCombat.isBurn = true;

            SoundManager.Instance?.Play(SoundIds.EnemyBurn, enemy: enemyCombat);

        // =========================================
        // Burn 시작
        // =========================================

        if (gameObject.activeInHierarchy)
        {
            dotDamage = StartCoroutine(BurnCoroutine());
        }
    }

    /// <summary>BurnCoroutine 동작을 처리한다.</summary>
    private IEnumerator BurnCoroutine()
    {
        if (enemyCombat == null)
        {
            pooledObject.DestroyObj();
            yield break;
        }

        if (!TryGetBurnValues(out int dotCount, out float damage))
        {
            pooledObject.DestroyObj();
            yield break;
        }

        // =====================================================
        // 1. 최초 데미지
        // =====================================================

        if (!enemyCombat.isDead)
        {
            DealBurnDamage(damage);

            // 최초 데미지로 죽었다면
            // Dot 애니메이션 딱 1번 재생 후 종료
            if (enemyCombat.isDead)
            {
                /// <summary>PlayDotAnimation 동작을 처리한다.</summary>
                yield return PlayDotAnimation();

                pooledObject.DestroyObj();
                yield break;
            }
        }
        else
        {
            // 이미 죽어있는 적이면
            // Dot 1번만 재생하고 종료
            /// <summary>PlayDotAnimation 동작을 처리한다.</summary>
            yield return PlayDotAnimation();

            pooledObject.DestroyObj();
            yield break;
        }

        // =====================================================
        // 2. DOT 반복
        // =====================================================

        int playedCount = 0;

        while (playedCount < dotCount)
        {
            // 타겟 자체가 없어졌으면 종료
            if (enemyCombat == null)
                break;

            // -------------------------------------------------
            // 적이 죽었다면
            //
            // 아직 Dot을 하나도 재생하지 않았다면
            // 딱 1번만 재생하고 종료
            //
            // 이미 재생 중이었다면 PlayDotAnimation()이
            // 끝난 상태이므로 그냥 종료
            // -------------------------------------------------

            if (enemyCombat.isDead)
            {
                if (playedCount == 0)
                {
                    /// <summary>PlayDotAnimation 동작을 처리한다.</summary>
                    yield return PlayDotAnimation();
                }

                break;
            }

            // -------------------------------------------------
            // Dot 애니메이션
            // -------------------------------------------------

            /// <summary>PlayDotAnimation 동작을 처리한다.</summary>
            yield return PlayDotAnimation();

            playedCount++;

            // -------------------------------------------------
            // 애니메이션 재생 중 적이 죽었다면
            //
            // 현재 Dot 애니메이션은 이미 끝까지 재생했으므로
            // 추가 Dot 없이 종료
            // -------------------------------------------------

            if (enemyCombat == null)
                break;

            if (enemyCombat.isDead)
                break;

            // -------------------------------------------------
            // Dot 데미지
            // -------------------------------------------------

            DealBurnDamage(damage);

            // 이번 데미지로 적이 죽었다면 종료
            if (enemyCombat == null)
                break;

            if (enemyCombat.isDead)
                break;

            // -------------------------------------------------
            // 다음 DOT 데이터 갱신
            // -------------------------------------------------

            if (!TryGetBurnValues(out dotCount, out damage))
                break;
        }

        pooledObject.DestroyObj();
    }

    /// <summary>GetBurnValues 동작의 가능 여부를 확인하고 처리한다.</summary>
    private bool TryGetBurnValues(out int dotCount, out float damage)
    {
        dotCount = 0;
        damage = 0f;
        if (DataManager.Instance == null ||
            !DataManager.Instance.upgradeDatasDic.TryGetValue(id, out var data) ||
            data == null || data.upgradeLevel <= 0 ||
            data.dotCount == null || data.dotCount.Length == 0 ||
            data.dotDamage == null || data.dotDamage.Length == 0)
        {
            return false;
        }

        // Owned levels are 1-based; all level arrays use the 0-based accessor.
        // Check each array's bounds independently when refreshing an active burn.
        int level = data.UpgradeLevel;
        dotCount = data.dotCount[Mathf.Clamp(level, 0, data.dotCount.Length - 1)];
        damage = data.dotDamage[Mathf.Clamp(level, 0, data.dotDamage.Length - 1)];
        return true;
    }

    // =========================================================
    // Burn 데미지
    // =========================================================

    /// <summary>DealBurnDamage 동작을 처리한다.</summary>
    private void DealBurnDamage(float damage)
    {
        if (enemyCombat == null)
            return;

        if (enemyCombat.isDead)
            return;

        Collider2D targetCol =
            enemyCombat.GetComponent<Collider2D>();

        if (targetCol == null)
            return;

        // =====================================
        // 피격 위치
        // =====================================

        Vector2 hitPoint =
            targetCol.ClosestPoint(
                transform.position
            );

        enemyCombat.TakeDamage(id, damage,
            hitPoint
        );
    }

    // =========================================================
    // Dot 애니메이션
    // =========================================================

    /// <summary>PlayDotAnimation 동작을 처리한다.</summary>
    private IEnumerator PlayDotAnimation()
    {
        if (animator == null)
            yield break;

        animator.SetBool("Idle", false);

        animator.ResetTrigger("Dot");
        animator.SetTrigger("Dot");

        // Animator가 Dot 상태로 넘어갈 시간
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

    /// <summary>다른 콜라이더가 진입했을 때 적중 효과를 처리한다.</summary>
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wall"))
        {
            pooledObject.DestroyObj();
        }
    }

    /// <summary>비활성화될 때 이벤트와 진행 중인 동작을 정리한다.</summary>
    protected override void OnDisable()
    {
        // Burn 걸려있던 적들의 isBurn 해제
        foreach (EnemyCombat enemy in enemyCombatList)
        {
            if (enemy != null)
            {
                enemy.isBurn = false;
            }
        }

        enemyCombatList.Clear();

        if (dotDamage != null)
        {
            StopCoroutine(dotDamage);
            dotDamage = null;
        }

        enemyCombat = null;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Dynamic;
        }

        if (col != null)
            col.enabled = true;
    }
}
