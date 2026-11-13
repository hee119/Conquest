using System.Collections.Generic;
using UnityEngine;

public class SplitEnemy : EnemyCombat
{
    public int splitCount;
    public int splitSpawnCount = 2;
    public float defaultScale = 1f;
    private int initialSplitCount;
    private bool hasInitialSplitCount;

    protected readonly List<EnemyCombat> spawnedChildren =
        new List<EnemyCombat>();



    public override void Die()
    {
        EnemySplit();
        base.Die();
    }

    public virtual void EnemySplit()
    {
        if (splitCount <= 0 || splitSpawnCount <= 0)
            return;

        SoundManager.Instance?.Play(SoundIds.SlimeSplit);

        for (int i = 0; i < splitSpawnCount; i++)
        {
            GameObject obj =
                PoolManager.Instance.Spawn(
                    enemyInfo.id,
                    transform.position
                );

            SplitEnemy splitEnemy =
                obj.GetComponent<SplitEnemy>();

            if (splitEnemy == null)
                continue;

            // 내가 생성한 자식 기록
            spawnedChildren.Add(splitEnemy);

            splitEnemy.enemyInfo.damage =
                (int)(splitEnemy.enemyInfo.damage / 2f);

            splitEnemy.enemyInfo.hp =
                (int)(splitEnemy.enemyInfo.hp / 1.4f);
            splitEnemy.ResetHealth(splitEnemy.enemyInfo.hp);

            splitEnemy.enemyInfo.speed =
                (int)(splitEnemy.enemyInfo.speed * 2f);

            splitEnemy.enemyMove.moveSpeed =
                splitEnemy.enemyInfo.speed;

            splitEnemy.enemyMove.MoveSpeedRenewal();

            obj.transform.localScale =
                transform.localScale;

            obj.transform.localScale =
                new Vector3(
                    obj.transform.localScale.x / 1.3f,
                    obj.transform.localScale.y / 1.3f,
                    1f
                );

            splitEnemy.splitCount =
                splitCount - 1;
        }
    }

    protected bool IsSpawnedChild(EnemyCombat target)
    {
        return spawnedChildren.Contains(target);
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (!hasInitialSplitCount)
        {
            initialSplitCount = splitCount;
            hasInitialSplitCount = true;
        }
        splitCount = initialSplitCount;

        spawnedChildren.Clear();

        transform.localScale =
            new Vector3(
                defaultScale,
                defaultScale,
                defaultScale
            );
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        spawnedChildren.Clear();

        var data =
            DataManager.Instance.enemyDatasDic[enemyInfo.id];

        enemyInfo.damage = data.damage;
        enemyInfo.speed = data.speed;
        enemyInfo.hp = data.hp;
        enemyInfo.defense = data.defense;
        enemyInfo.hpRegenRate = data.hpRegenRate;
        enemyInfo.explosionDamage = data.explosionDamage;
        enemyInfo.explosionRange = data.explosionRange;

        enemyMove.moveSpeed =
            enemyInfo.speed;

        enemyMove.MoveSpeedRenewal();
    }

    public void BaseDie()
    {
        base.Die();
    }
}
