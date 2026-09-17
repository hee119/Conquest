using UnityEngine;

public class SplitEnemy : EnemyCombat
{
    public int splitCount;
    public int splitSpawnCount = 2;
    public override void Die()
    {
        EnemySplit();
        base.Die();
    }

    public void EnemySplit()
    {
        if (splitCount > 0)
        {
            for (int i = 0; i < splitSpawnCount; i++)
            { 
                GameObject obj = PoolManager.Instance.Spawn(unitInfo.id, transform.position);
                SplitEnemy splitEnemy = obj.GetComponent<SplitEnemy>();
                splitEnemy.unitInfo.damage = (int)(splitEnemy.unitInfo.damage / 2f);
                splitEnemy.unitInfo.hp = (int)(splitEnemy.unitInfo.hp / 1.4f);
                splitEnemy.unitInfo.speed = (int)(splitEnemy.unitInfo.speed * 2f);
                splitEnemy.enemyMove.moveSpeed = splitEnemy.unitInfo.speed;
                splitEnemy.enemyMove.MoveSpeedRenewal();
                obj.transform.localScale = transform.localScale;
                obj.transform.localScale = new Vector3(obj.transform.localScale.x / 1.3f, obj.transform.localScale.y / 1.3f, 1);
                
                splitEnemy.splitCount = splitCount - 1;
            }
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        transform.localScale = Vector3.one;
    }

    protected void OnDisable()
    {
        unitInfo.damage = DataManager.Instance.playerAndEnemyInfosDic[unitInfo.id].damage;
        unitInfo.speed = DataManager.Instance.playerAndEnemyInfosDic[unitInfo.id].speed;
        unitInfo.hp = DataManager.Instance.playerAndEnemyInfosDic[unitInfo.id].hp;
        enemyMove.moveSpeed = unitInfo.speed;
        enemyMove.MoveSpeedRenewal();
    }

    public void BaseDie()
    {
        base.Die();
    }
}
