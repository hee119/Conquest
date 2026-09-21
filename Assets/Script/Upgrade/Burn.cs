using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Burn : DotDamage
{
    private List<EnemyCombat> enemyCombatList = new();

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

        // =========================================
        // Burn 시작
        // =========================================

        if (gameObject.activeInHierarchy)
        {
            dotDamage = StartCoroutine(BurnCoroutine());
        }
    }

    private IEnumerator BurnCoroutine()
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

        float damage =
            DataManager.Instance
                .upgradeDatasDic[id]
                .dotDamage[level];

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
                yield return PlayDotAnimation();

                pooledObject.DestroyObj();
                yield break;
            }
        }
        else
        {
            // 이미 죽어있는 적이면
            // Dot 1번만 재생하고 종료
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
                    yield return PlayDotAnimation();
                }

                break;
            }

            // -------------------------------------------------
            // Dot 애니메이션
            // -------------------------------------------------

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

            level =
                DataManager.Instance
                    .upgradeDatasDic[id]
                    .upgradeLevel;

            dotCount =
                DataManager.Instance
                    .upgradeDatasDic[id]
                    .dotCount[level];

            damage =
                DataManager.Instance
                    .upgradeDatasDic[id]
                    .dotDamage[level];
        }

        pooledObject.DestroyObj();
    }

    // =========================================================
    // Burn 데미지
    // =========================================================

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
            targetCol.ClosestPoint(
                transform.position
            );

        enemyCombat.TakeDamage(
            finalDamage,
            hitPoint
        );
    }

    // =========================================================
    // Dot 애니메이션
    // =========================================================

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

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wall"))
        {
            pooledObject.DestroyObj();
        }
    }

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