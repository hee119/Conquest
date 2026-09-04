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

        Debug.Log(
            $"[DetectUpgrade] Awake | " +
            $"Name={gameObject.name} | " +
            $"Player={(player != null ? player.name : "NULL")}"
        );
    }

    public virtual IEnumerator DetectUpgradeCor(int id)
    {
        this.id = id;

        Debug.Log(
            $"[DetectUpgrade] Coroutine 시작 | ID={id}"
        );

        while (true)
        {
            SetUpgradeData();

            Debug.Log(
                $"[DetectUpgrade] 데이터 갱신 | " +
                $"ID={id} | Count={count} | " +
                $"CoolTime={coolTime} | " +
                $"Range={detectionRange}"
            );

            List<Collider2D> enemies =
                DetectEnemies(detectionRange);

            Debug.Log(
                $"[DetectUpgrade] 적 탐색 결과 | " +
                $"Count={enemies.Count}"
            );

            if (enemies.Count == 0)
            {
                yield return null;
                continue;
            }

            for (int i = 0; i < count; i++)
            {
                Debug.Log(
                    $"[DetectUpgrade] 공격 대상 탐색 | " +
                    $"Index={i + 1}/{count} | " +
                    $"EnemyListCount={enemies.Count}"
                );

                Transform closestEnemy =
                    FindClosestEnemy(enemies);

                if (closestEnemy == null)
                {
                    Debug.LogWarning(
                        $"[DetectUpgrade] FindClosestEnemy 결과 NULL"
                    );

                    break;
                }

                Vector3 targetPos =
                    closestEnemy.position;

                Debug.Log(
                    $"[DetectUpgrade] 타겟 발견 | " +
                    $"Target={closestEnemy.name} | " +
                    $"Position={targetPos} | " +
                    $"Active={closestEnemy.gameObject.activeInHierarchy}"
                );

                Attack(
                    targetPos,
                    closestEnemy.gameObject
                );

                Collider2D collider =
                    closestEnemy.GetComponent<Collider2D>();

                if (collider != null)
                {
                    enemies.Remove(collider);

                    Debug.Log(
                        $"[DetectUpgrade] 타겟 제거 | " +
                        $"Target={closestEnemy.name} | " +
                        $"남은 적={enemies.Count}"
                    );
                }
                else
                {
                    Debug.LogWarning(
                        $"[DetectUpgrade] Collider2D 없음 | " +
                        $"Target={closestEnemy.name}"
                    );
                }
            }

            Debug.Log(
                $"[DetectUpgrade] 공격 완료 → 쿨타임 {coolTime}초"
            );

            yield return new WaitForSeconds(coolTime);
        }
    }

    protected virtual void SetUpgradeData()
    {
        int level =
            DataManager.Instance.UpgradeDatasDic[id].upgradeLevel;

        count =
            DataManager.Instance.UpgradeDatasDic[id]
                .count[level];

        coolTime =
            DataManager.Instance.UpgradeDatasDic[id]
                .coolTime[level];

        detectionRange =
            DataManager.Instance.UpgradeDatasDic[id]
                .detectionRange[level];

        Debug.Log(
            $"[DetectUpgrade] SetUpgradeData | " +
            $"ID={id} | Level={level} | " +
            $"Count={count} | CoolTime={coolTime} | " +
            $"Range={detectionRange}"
        );
    }

    public virtual List<Collider2D> DetectEnemies(float detectionRange)
    {
        int enemyLayer = LayerMask.GetMask("Enemy");

        Collider2D[] detected =
            Physics2D.OverlapCircleAll(
                player.transform.position,
                detectionRange,
                enemyLayer
            );

        Debug.Log(
            $"[DetectUpgrade] DetectEnemies | " +
            $"PlayerPos={player.transform.position} | " +
            $"Range={detectionRange} | " +
            $"Found={detected.Length}"
        );

        return new List<Collider2D>(detected);
    }

    public virtual Transform FindClosestEnemy(
        List<Collider2D> enemies)
    {
        Transform closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider2D enemy in enemies)
        {
            if (enemy == null)
            {
                Debug.LogWarning(
                    "[DetectUpgrade] 적 목록에 NULL Collider 존재"
                );

                continue;
            }

            if (!enemy.gameObject.activeInHierarchy)
            {
                Debug.LogWarning(
                    $"[DetectUpgrade] 비활성 적 발견 | " +
                    $"Name={enemy.name}"
                );

                continue;
            }

            float distance =
                (enemy.transform.position -
                 player.transform.position).sqrMagnitude;

            Debug.Log(
                $"[DetectUpgrade] 적 거리 확인 | " +
                $"Enemy={enemy.name} | " +
                $"DistanceSqr={distance}"
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }

        if (closestEnemy != null)
        {
            Debug.Log(
                $"[DetectUpgrade] 가장 가까운 적 | " +
                $"Target={closestEnemy.name} | " +
                $"DistanceSqr={closestDistance}"
            );
        }
        else
        {
            Debug.LogWarning(
                "[DetectUpgrade] 가장 가까운 적을 찾지 못함"
            );
        }

        return closestEnemy;
    }

    public virtual void Attack(
        Vector3 targetPos,
        GameObject target = null)
    {
        Debug.Log(
            $"[DetectUpgrade] Attack 시작 | " +
            $"ID={id} | " +
            $"TargetPos={targetPos} | " +
            $"Target={(target != null ? target.name : "NULL")}"
        );

        GameObject obj =
            PoolManager.Instance.Spawn(
                id,
                player.transform.position
            );

        if (obj == null)
        {
            Debug.LogError(
                $"[DetectUpgrade] Attack Spawn 실패 | ID={id}"
            );

            return;
        }

        HomingProjectile projectile =
            obj.GetComponent<HomingProjectile>();

        if (projectile == null)
        {
            Debug.LogError(
                $"[DetectUpgrade] HomingProjectile 없음 | " +
                $"Obj={obj.name}"
            );

            return;
        }

        Debug.Log(
            $"[DetectUpgrade] SetTarget 호출 | " +
            $"Obj={obj.name} | " +
            $"TargetPos={targetPos}"
        );

        projectile.SetTarget(targetPos);

        Debug.Log(
            $"[DetectUpgrade] SetTarget 완료 | " +
            $"Obj={obj.name}"
        );
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
        bool result =
            DetectEnemies(detectionRange).Count > 0;

        Debug.Log(
            $"[DetectUpgrade] HasEnemyInRange | " +
            $"Range={detectionRange} | Result={result}"
        );

        return result;
    }
}