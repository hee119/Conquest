using System.Collections;
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
            Collider2D[] enemies = Physics2D.OverlapCircleAll(
                player.transform.position,
                detectionRange,
                enemyLayer
            );

            // 적이 없으면 쿨타임 진행 X
            if (enemies.Length == 0)
            {
                yield return null;
                continue;
            }

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
            for (int i = 0; i < count; i++)
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