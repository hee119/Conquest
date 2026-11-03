using System.Collections.Generic;
using UnityEngine;

public class BoomSlime : SplitEnemy
{
    [Header("Boom Setting")]
    [SerializeField]
    private string idleStateName = "Idle";

    private bool canBoom = false;

    protected override void OnEnable()
    {
        base.OnEnable();

        canBoom = false;

        if (anim != null) anim.Play(idleStateName);
    }

    public override void Die()
    {
        if (isDead || canBoom)
            return;

        // 죽는 중 추가 피격 방지
        isDying = true;

        // 실제로 죽었을 때만 Animation Event 허용
        canBoom = true;

        // 먼저 분열
        EnemySplit();

        // 그 다음 폭발 애니
        BoomAnim();
    }

    private void BoomAnim()
    {
        if (anim == null)
        {
            Boom();
            return;
        }

        if (!anim.Play("Boom")) Boom();
    }

    // Boom 애니메이션 Animation Event에서 실행
    public void Boom()
    {
        // 풀에서 재활성화하면서
        // 잘못된 Animation Event가 호출돼도 무시
        if (!canBoom)
            return;

        canBoom = false;

        var data =
            DataManager.Instance.enemyDatasDic[enemyInfo.id];

        float explosionRange =
            data.explosionRange;

        float explosionDamage =
            data.explosionDamage;

        Collider2D[] colliders =
            Physics2D.OverlapCircleAll(
                transform.position,
                explosionRange
            );

        HashSet<EnemyCombat> damagedEnemies =
            new HashSet<EnemyCombat>();

        foreach (Collider2D col in colliders)
        {
            EnemyCombat target =
                col.GetComponent<EnemyCombat>();

            if (target == null)
                continue;

            // 자기 자신 제외
            if (target == this)
                continue;

            // 내가 분열로 만든 자식 제외
            if (IsSpawnedChild(target))
                continue;

            // Collider 여러 개면 중복 데미지 방지
            if (!damagedEnemies.Add(target))
                continue;

            target.TakeRawDamage(
                explosionDamage,
                target.transform.position
            );
        }

        BaseDie();
    }
}