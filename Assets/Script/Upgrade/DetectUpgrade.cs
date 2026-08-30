using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectUpgrade : MonoBehaviour
{
    [SerializeField]
    protected GameObject player;

    protected int id;
    protected int count;
    protected float coolTime;
    public float detectionRange;

    protected virtual void Awake()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
    }

    public virtual IEnumerator DetectUpgradeCor(int id)
    {
        this.id = id;

        while (true)
        {
            SetUpgradeData();

            List<Collider2D> enemies = DetectEnemies(detectionRange);

            // 적이 없으면 쿨타임 진행 X
            if (enemies.Count == 0)
            {
                yield return null;
                continue;
            }

            // count만큼 공격
            for (int i = 0; i < count; i++)
            {
                Transform closestEnemy = FindClosestEnemy(enemies);

                if (closestEnemy == null)
                    break;

                Attack(closestEnemy);

                // 같은 적을 다시 선택하지 않도록 제거
                Collider2D collider =
                    closestEnemy.GetComponent<Collider2D>();

                if (collider != null)
                {
                    enemies.Remove(collider);
                }
            }

            // 공격했을 때만 쿨타임
            yield return new WaitForSeconds(coolTime);
        }
    }

    protected virtual void SetUpgradeData()
    {
        int level =
            DataManager.Instance.UpgradeDatasDic[id].upgradeLevel;

        count =
            DataManager.Instance.UpgradeDatasDic[id].count[level];

        coolTime =
            DataManager.Instance.UpgradeDatasDic[id].coolTime[level];

        detectionRange =
            DataManager.Instance.UpgradeDatasDic[id].detectionRange[level];
    }

    public virtual List<Collider2D> DetectEnemies(float detectionRange)
    {
        int enemyLayer = LayerMask.GetMask("Enemy");

        return new List<Collider2D>(
            Physics2D.OverlapCircleAll(
                player.transform.position,
                detectionRange,
                enemyLayer
            )
        );
    }

    public virtual Transform FindClosestEnemy(
        List<Collider2D> enemies)
    {
        Transform closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider2D enemy in enemies)
        {
            if (enemy == null)
                continue;

            float distance =
                (enemy.transform.position - player.transform.position)
                .sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }

        return closestEnemy;
    }

    // 실제 공격
    public virtual void Attack(Transform target)
    {
        GameObject obj = PoolManager.Instance.Spawn(
            id,
            player.transform.position
        );

        HomingProjectile projectile =
            obj.GetComponent<HomingProjectile>();

        if (projectile != null)
        {
            projectile.SetTarget(target.position);
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (player == null)
            return;

        Gizmos.DrawWireSphere(
            player.transform.position,
            detectionRange
        );
    }
    
    public virtual bool HasEnemyInRange(float detectionRange)
    {
        return DetectEnemies(detectionRange).Count > 0;
    }
}