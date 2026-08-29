using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectUpgrade : MonoBehaviour
{
    [SerializeField]
    protected GameObject player;

    public void Awake()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
    }

    public IEnumerator DetectUpgradeCor(int id)
    {
        int level = DataManager.Instance.UpgradeDatasDic[id].upgradeLevel;

        int count =
            DataManager.Instance.UpgradeDatasDic[id].count[level];

        float coolTime =
            DataManager.Instance.UpgradeDatasDic[id].coolTime[level];

        float detectionRange =
            DataManager.Instance.UpgradeDatasDic[id].detectionRange[level];

        int enemyLayer = LayerMask.GetMask("Enemy");

        while (true)
        {
            // 주변 Enemy 전부 탐지
            List<Collider2D> enemies = new (Physics2D.OverlapCircleAll(
                player.transform.position,
                detectionRange,
                enemyLayer
            ));

            // 적이 없으면 쿨타임 진행 X
            if (enemies.Count == 0)
            {
                yield return null;
                continue;
            }

            for (int i = 0; i < count; i++)
            {
                // 가장 가까운 적 찾기
                Transform closestEnemy = null;
                float closestDistance = Mathf.Infinity;

                foreach (Collider2D enemy in enemies)
                {
                    float distance =
                        (enemy.transform.position - player.transform.position)
                        .sqrMagnitude;

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = enemy.transform;
                    }
                }

                // 투사체 생성
                if (closestEnemy != null)
                {
                    GameObject obj = PoolManager.Instance.Spawn(
                        id,
                        player.transform.position
                    );


                    Debug.Log(obj);

                    // 생성된 투사체에 타겟 전달
                    HomingProjectile projectile =
                        obj.GetComponent<HomingProjectile>();

                    Debug.Log(DataManager.Instance.UpgradeDatasDic[id].speed);

                    if (projectile != null)
                    {
                        projectile.SetTarget(closestEnemy);
                        enemies.Remove(closestEnemy.GetComponent<Collider2D>());
                    }
                }
            }

            // 발사했을 때만 쿨타임
            yield return new WaitForSeconds(coolTime);
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (player == null)
            return;

        Gizmos.DrawWireSphere(
            player.transform.position,
            1f
        );
    }
}