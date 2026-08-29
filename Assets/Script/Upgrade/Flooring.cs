using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class Flooring : MonoBehaviour
{
    private int id;
    
    protected UpgradeInfo upgradeInfo;
    protected List<EnemyMove> enemies = new List<EnemyMove>();
    protected SpriteRenderer spriteRenderer;
    protected PooledObject pooledObject;

    public void Awake()
    {
        upgradeInfo = GetComponent<UpgradeInfo>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        pooledObject = GetComponent<PooledObject>();
        id = upgradeInfo.id;
    }

    protected void OnEnable()
    {
        StartCoroutine(DestoryFlooring());
    }

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
                enemies[i].MoveSpeedRenewal();
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
        enemyMove.MoveSpeedRenewal();
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
            enemy.MoveSpeedRenewal();
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

    protected virtual IEnumerator DestoryFlooring()
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = new Vector3(0,0,0);
        float timer = 0;
        float duration = DataManager.Instance.UpgradeDatasDic[id].lifetime;
        float alpha = 1;
        Color color = spriteRenderer.color;
        
        while (timer < duration)
        {
            float t = timer / duration;
            timer += Time.deltaTime;
            
            transform.localScale = Vector3.Lerp(startScale,endScale , t);
            spriteRenderer.color = new Color(color.r ,color.g,color.b, Mathf.Lerp(alpha, 0, t));
            yield return null;
        }
    }
}
