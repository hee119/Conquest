using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>바닥 장판의 지속 시간과 적 감속 효과를 관리한다.</summary>
public abstract class Flooring : MonoBehaviour
{
    private int id;
    
    protected UpgradeInfo upgradeInfo;
    protected List<EnemyMove> enemies = new List<EnemyMove>();
    protected SpriteRenderer spriteRenderer;
    protected PooledObject pooledObject;
    private Color initialColor;

    /// <summary>필요한 컴포넌트와 초기 상태를 준비한다.</summary>
    public void Awake()
    {
        upgradeInfo = GetComponent<UpgradeInfo>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        pooledObject = GetComponent<PooledObject>();
        id = upgradeInfo.id;
        initialColor = spriteRenderer.color;
    }

    /// <summary>활성화될 때 필요한 이벤트와 동작을 시작한다.</summary>
    protected void OnEnable()
    {
        spriteRenderer.color = initialColor;
        StartCoroutine(DestoryFlooring());
    }

    /// <summary>비활성화될 때 이벤트와 진행 중인 동작을 정리한다.</summary>
    protected virtual void OnDisable()
    {
        StopAllCoroutines();
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            EnemyMove enemy = enemies[i];

            if (enemy == null)
                continue;

            EnemyInfo enemyInfo = enemy.GetComponent<EnemyInfo>();

            if (enemyInfo == null)
                continue;

            enemy.slowCount -= 1;

            if (enemy.slowCount <= 0)
            {
                enemy.isSlowDown = false;
                enemy.moveSpeed =
                    DataManager.Instance.enemyDatasDic[enemyInfo.id].speed;

                enemy.MoveSpeedRenewal();
            }
        }

        enemies.Clear();
    }
    /// <summary>대상 적에게 장판의 감속 효과를 적용한다.</summary>
    protected void SlowDown(GameObject target)
    {
        EnemyMove enemyMove = target.GetComponent<EnemyMove>();
        EnemyInfo enemyInfo = target.GetComponent<EnemyInfo>();
        if (enemyMove == null || enemyInfo == null || enemies.Contains(enemyMove)) return;
        enemyMove.slowCount += 1;
        enemyMove.isSlowDown = true;
        enemies.Add(enemyMove);
        enemyMove.moveSpeed = DataManager.Instance.enemyDatasDic[enemyInfo.id].speed * DataManager.Instance.upgradeDatasDic[id].slow[DataManager.Instance.upgradeDatasDic[id].UpgradeLevel];
        enemyMove.MoveSpeedRenewal();
    }

    /// <summary>장판에서 벗어난 적의 감속 상태를 정리한다.</summary>
    protected void SlowDownClear(EnemyMove enemy)
    {
        if (enemy == null || !enemies.Remove(enemy)) return;
        enemy.slowCount = Mathf.Max(0, enemy.slowCount - 1);
        EnemyInfo enemyInfo = enemy.GetComponent<EnemyInfo>();

        if (enemy.slowCount <= 0)
        {
            enemy.isSlowDown = false;
            enemy.moveSpeed = DataManager.Instance.enemyDatasDic[enemyInfo.id].speed;
            enemy.MoveSpeedRenewal();
        }
    }

    /// <summary>다른 콜라이더가 진입했을 때 적중 효과를 처리한다.</summary>
    public virtual void HandleEnemyContact(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            SlowDown(other.gameObject);
        }
    }

    /// <summary>다른 콜라이더가 벗어났을 때 적용된 효과를 정리한다.</summary>
    public virtual void HandleEnemyExit(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            SlowDownClear(other.gameObject.GetComponent<EnemyMove>());
        }
    }

    /// <summary>장판을 시간에 따라 축소하고 수명이 끝나면 풀에 반환한다.</summary>
    protected virtual IEnumerator DestoryFlooring()
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = new Vector3(0,0,0);
        float timer = 0;
        float duration = DataManager.Instance.upgradeDatasDic[id].lifetime;
        float alpha = initialColor.a;
        Color color = spriteRenderer.color;
        
        while (timer < duration)
        {
            float t = timer / duration;
            timer += Time.deltaTime;
            
            transform.localScale = Vector3.Lerp(startScale,endScale , t);
            spriteRenderer.color = new Color(color.r ,color.g,color.b, Mathf.Lerp(alpha, 0, t));
            yield return null;
        }
        // Transparent renderers and their triggers must not accumulate forever.
        pooledObject.DestroyObj();
    }
}
