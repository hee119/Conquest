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
    public PooledObject pooledObject;
    protected virtual float ChainAttackInterval => 0.05f;

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
        pooledObject = GetComponent<PooledObject>();
    }

    public virtual IEnumerator DetectUpgradeCor(int id, bool chain = false, GameObject target = null)
    {
        this.id = id;

        Debug.Log(
            $"[DetectUpgrade] Coroutine 시작 | ID={id} | Chain={chain} | Target={(target != null ? target.name : "NULL")}"
        );

        // =========================================
        // 일반 공격
        // =========================================
        while (true)
        {
            SetUpgradeData(false);

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
                Transform closestEnemy =
                    FindClosestEnemy(enemies);

                if (closestEnemy == null)
                    break;

                Vector3 targetPos =
                    closestEnemy.position;

                Attack(
                    targetPos,
                    closestEnemy.gameObject,
                    false
                );

                Collider2D collider =
                    closestEnemy.GetComponent<Collider2D>();

                if (collider != null)
                {
                    enemies.Remove(collider);
                }
            }

            yield return new WaitForSeconds(coolTime);
        }
    }

    // =========================================
    // 업그레이드 데이터
    // =========================================

    protected virtual void SetUpgradeData(bool chain)
    {
        int level =
            DataManager.Instance
                .UpgradeDatasDic[id]
                .upgradeLevel;

        count =
            DataManager.Instance
                .UpgradeDatasDic[id]
                .count[level];

        if (chain)
        {
            count =
                DataManager.Instance
                    .UpgradeDatasDic[id]
                    .chainCount;
        }

        coolTime =
            DataManager.Instance
                .UpgradeDatasDic[id]
                .coolTime[level];

        detectionRange =
            DataManager.Instance
                .UpgradeDatasDic[id]
                .detectionRange[level];

        if (chain)
        {
            detectionRange =
                DataManager.Instance
                    .UpgradeDatasDic[id]
                    .chainRange;
        }
    }

    // =========================================
    // 플레이어 기준 적 탐색
    // =========================================

    public virtual List<Collider2D> DetectEnemies(float detectionRange)
    {
        int enemyLayer =
            LayerMask.GetMask("Enemy");

        Collider2D[] detected =
            Physics2D.OverlapCircleAll(
                player.transform.position,
                detectionRange,
                enemyLayer
            );

        return new List<Collider2D>(detected);
    }

    // =========================================
    // 특정 위치 기준 적 탐색
    // 연쇄 번개용
    // =========================================

    protected virtual List<Collider2D> DetectEnemiesFromPosition(Vector3 position, float range)
    {
        int enemyLayer =
            LayerMask.GetMask("Enemy");

        Collider2D[] detected =
            Physics2D.OverlapCircleAll(
                position,
                range,
                enemyLayer
            );

        return new List<Collider2D>(detected);
    }

    // =========================================
    // 플레이어 기준 가장 가까운 적
    // =========================================

    public virtual Transform FindClosestEnemy(List<Collider2D> enemies)
    {
        Transform closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider2D enemy in enemies)
        {
            if (enemy == null)
                continue;

            if (!enemy.gameObject.activeInHierarchy)
                continue;

            float distance =
                (
                    enemy.transform.position -
                    player.transform.position
                ).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }

        return closestEnemy;
    }

    // =========================================
    // 특정 위치 기준 가장 가까운 적
    // 연쇄 번개용
    // =========================================

    protected virtual Transform FindClosestEnemyFromPosition(List<Collider2D> enemies, Vector3 position)
    {
        Transform closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider2D enemy in enemies)
        {
            if (enemy == null)
                continue;

            if (!enemy.gameObject.activeInHierarchy)
                continue;

            EnemyCombat enemyCombat =
                enemy.GetComponent<EnemyCombat>();

            if (enemyCombat == null ||
                enemyCombat.isDead)
            {
                continue;
            }

            float distance =
                (
                    enemy.transform.position -
                    position
                ).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }

        return closestEnemy;
    }

    // =========================================
    // 공격
    // =========================================

    public virtual void Attack(Vector3 targetPos, GameObject target = null, bool isChain = false, Vector3? startPosition = null, bool destroyAfter = true)
    {
        Vector3 spawnPos = startPosition ?? player.transform.position;

        GameObject obj = PoolManager.Instance.Spawn(id, spawnPos);

        if (obj == null)
            return;

        HomingProjectile projectile = obj.GetComponent<HomingProjectile>();

        if (projectile == null)
            return;

        projectile.SetTarget(targetPos);
    }

    // =========================================
    // 범위 표시
    // =========================================

    protected virtual void OnDrawGizmosSelected()
    {
        if (player == null)
            return;

        Gizmos.DrawWireSphere(
            player.transform.position,
            detectionRange
        );
    }

    // =========================================
    // 범위 내 적 존재 여부
    // =========================================

    public virtual bool HasEnemyInRange(
        float detectionRange)
    {
        return DetectEnemies(
            detectionRange
        ).Count > 0;
    }
    
    public List<GameObject> GetChainAttackList(GameObject target = null)
{
    SetUpgradeData(true);

    Debug.Log(
        $"[Chain 진단] GetChainAttackList 시작 | id={id} | " +
        $"count(chainCount)={count} | detectionRange(chainRange)={detectionRange} | " +
        $"전달받은 target={(target != null ? target.name : "NULL")}"
    );

    GameObject firstTarget = target;

    // 첫 타겟이 없으면 플레이어 기준으로 찾기
    if (firstTarget == null)
    {
        List<Collider2D> initialEnemies =
            DetectEnemies(detectionRange);

        Debug.Log(
            $"[Chain 진단] 플레이어 기준 재탐색 | " +
            $"플레이어 위치={player.transform.position} | range={detectionRange} | " +
            $"발견된 적 수={initialEnemies.Count}"
        );

        Transform closest = FindClosestEnemy(initialEnemies);

        if (closest == null)
        {
            Debug.LogWarning("[Chain 진단] firstTarget을 찾지 못함 → 빈 리스트 반환");
            return new List<GameObject>();
        }

        firstTarget = closest.gameObject;
        Debug.Log($"[Chain 진단] firstTarget 확정 = {firstTarget.name}");
    }

    List<GameObject> attackList = new List<GameObject>();
    List<GameObject> hitEnemies = new List<GameObject>();

    GameObject searchOrigin = firstTarget;

    for (int i = 0; i < count; i++)
    {
        if (searchOrigin == null || !searchOrigin.activeInHierarchy)
        {
            Debug.LogWarning($"[Chain 진단] i={i} searchOrigin이 null이거나 비활성 → break");
            break;
        }

        EnemyCombat originEnemy = searchOrigin.GetComponent<EnemyCombat>();

        if (originEnemy == null || originEnemy.isDead)
        {
            Debug.LogWarning(
                $"[Chain 진단] i={i} EnemyCombat 없음 또는 사망 상태 → break | " +
                $"컴포넌트 존재={originEnemy != null} | isDead={(originEnemy != null ? originEnemy.isDead.ToString() : "N/A")}"
            );
            break;
        }

        attackList.Add(searchOrigin);
        hitEnemies.Add(searchOrigin);

        Debug.Log($"[Chain 진단] i={i} 리스트에 추가됨: {searchOrigin.name}");

        Vector3 searchPos = searchOrigin.transform.position;

        List<Collider2D> enemies =
            DetectEnemiesFromPosition(searchPos, detectionRange);

        enemies.RemoveAll(
            x => x == null || hitEnemies.Contains(x.gameObject)
        );

        Debug.Log(
            $"[Chain 진단] i={i} 다음 후보 탐색 | 기준위치={searchPos} | " +
            $"range={detectionRange} | 후보 수(중복제외)={enemies.Count}"
        );

        Transform nextEnemy =
            FindClosestEnemyFromPosition(enemies, searchPos);

        if (nextEnemy == null)
        {
            Debug.Log($"[Chain 진단] i={i} 다음 타겟 없음 → break");
            break;
        }

        searchOrigin = nextEnemy.gameObject;
    }

    Debug.Log($"[Chain 진단] GetChainAttackList 종료 | 최종 attackList.Count={attackList.Count}");

    return attackList;
}
}