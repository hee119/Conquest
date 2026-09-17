using UnityEngine;

public class SplitEnemy : EnemyCombat
{
    public int splitCount;
    public int splitSpawnCount = 2;
    public override void Die()
    {
        if (!isDead)
        {
            isDead = true;

            PoolManager.Instance.Spawn(
                unitInfo.dropExpOrbId,
                transform.position
            );

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);

                if (child.TryGetComponent<DotDamage>(out var dot))
                {
                    child.SetParent(null, true);
                }

                if (child.TryGetComponent<Mold>(out var mus))
                {
                    child.SetParent(null, true);

                    if (mus.gameObject.activeInHierarchy)
                    {
                        mus.animator.enabled = true;
                        mus.animator.SetTrigger("Boom");
                    }
                }
            }
            
            EnemySplit();

            pooledObject.DestroyObj();
        }
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
}
