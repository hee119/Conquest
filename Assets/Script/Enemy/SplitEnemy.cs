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
                GameObject obj = PoolManager.Instance.Spawn(enemyInfo.id, transform.position);
                SplitEnemy splitEnemy = obj.GetComponent<SplitEnemy>();
                splitEnemy.enemyInfo.damage = (int)(splitEnemy.enemyInfo.damage / 2f);
                splitEnemy.enemyInfo.hp = (int)(splitEnemy.enemyInfo.hp / 1.4f);
                splitEnemy.enemyInfo.speed = (int)(splitEnemy.enemyInfo.speed * 2f);
                splitEnemy.enemyMove.moveSpeed = splitEnemy.enemyInfo.speed;
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
        enemyInfo.damage = DataManager.Instance.enemyDatasDic[enemyInfo.id].damage;
        enemyInfo.speed = DataManager.Instance.enemyDatasDic[enemyInfo.id].speed;
        enemyInfo.hp = (int)DataManager.Instance.enemyDatasDic[enemyInfo.id].hp;
        enemyMove.moveSpeed = enemyInfo.speed;
        enemyMove.MoveSpeedRenewal();
    }

    public void BaseDie()
    {
        base.Die();
    }
}
