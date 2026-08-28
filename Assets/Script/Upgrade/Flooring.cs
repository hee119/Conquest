using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class Flooring : MonoBehaviour
{
    [SerializeField]
    public int id;
    
    protected List<EnemyMove> enemies = new List<EnemyMove>();

    protected virtual void OnDisable()
    {
        for (int i = enemies.Count - 1; i >= 0; i++)
        {
            UnitInfo unitInfo = enemies[i].GetComponent<UnitInfo>();
            enemies[i].slowCount -= 1;

            if (enemies[i].slowCount <= 0)
            {
                enemies[i].isSlowDown = false;
                enemies[i].moveSpeed = DataManager.Instance.playerAndEnemyInfosDic[unitInfo.id].speed;
            }
        }
        enemies.Clear();
    }
    protected void SlowDown(GameObject target)
    {
        EnemyMove enemyMove = target.GetComponent<EnemyMove>();
        UnitInfo unitInfo = target.GetComponent<UnitInfo>();
        enemyMove.slowCount += 1;
        enemyMove.isSlowDown = true;
        enemies.Add(enemyMove);
        enemyMove.moveSpeed = DataManager.Instance.playerAndEnemyInfosDic[unitInfo.id].speed * DataManager.Instance.UpgradeDatasDic[id].slow[DataManager.Instance.UpgradeDatasDic[id].upgradeLevel];
    }

    protected void SlowDownClear(EnemyMove enemy)
    {
        enemy.slowCount -= 1;
        enemies.Remove(enemy);
        UnitInfo unitInfo = enemy.GetComponent<UnitInfo>();

        if (enemy.slowCount <= 0)
        {
            enemy.isSlowDown = false;
            enemy.moveSpeed = DataManager.Instance.playerAndEnemyInfosDic[unitInfo.id].speed;
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            SlowDown(other.gameObject);
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            SlowDownClear(other.gameObject.GetComponent<EnemyMove>());
        }
    }
}
