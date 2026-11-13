using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Chain 증강 관련 동작과 상태를 관리한다.</summary>
public class Chain : DetectUpgrade
{
    public UpgradeInfo upgradeInfo;
    public int chainId;

    [SerializeField] private float chainAttackInterval = 0.15f;

    /// <summary>필요한 컴포넌트와 초기 상태를 준비한다.</summary>
    protected override void Awake()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
        
        upgradeInfo = GetComponent<UpgradeInfo>();
        pooledObject = GetComponent<PooledObject>();
    }
    
    /// <summary>다른 콜라이더가 진입했을 때 적중 효과를 처리한다.</summary>
    public void HandleEnemyContact(Collider2D collision)
{
    Debug.Log(
        $"[Chain 진단] OnTriggerEnter2D 호출 | 대상={collision.gameObject.name} | " +
        $"Tag={collision.tag} | Layer={LayerMask.LayerToName(collision.gameObject.layer)}"
    );

    if (collision.CompareTag("Enemy"))
    {
        // The projectile's upgrade determines the chain limits; chainId selects
        // the separate lightning prefab and its damage data.
        List<GameObject> target = GetChainAttackList(upgradeInfo.id, collision.gameObject);

        Debug.Log($"[Chain 진단] 반환된 target 리스트 개수={target.Count}");
        for (int i = 0; i < target.Count; i++)
        {
            Debug.Log($"[Chain 진단]   [{i}] = {(target[i] != null ? target[i].name : "NULL")}");
        }

        if (target.Count <= 1)
        {
            Debug.LogWarning("[Chain 진단] 리스트 길이가 1 이하 → 번개가 하나도 생성되지 않을 것으로 예상됨");
        }

        StartCoroutine(ChainAttack(target));
    }
    else
    {
        Debug.Log($"[Chain 진단] Enemy 태그 아님 → 무시됨 (실제 Tag={collision.tag})");
    }
}

/// <summary>ChainAttack 동작을 처리한다.</summary>
private IEnumerator ChainAttack(List<GameObject> target)
{
    Debug.Log($"[Chain 진단] ChainAttack 코루틴 시작 | target.Count={target.Count}");

    Vector3 previousPosition = Vector3.zero;
    var targetPools = new PooledObject[target.Count];
    var targetVersions = new uint[target.Count];
    for (int i = 0; i < target.Count; i++)
    {
        if (target[i] == null) continue;
        targetPools[i] = target[i].GetComponent<PooledObject>();
        if (targetPools[i] != null)
            targetVersions[i] = targetPools[i].SpawnVersion;
    }

    for (int i = 0; i < target.Count; i++)
    {
        GameObject currentTarget = target[i];

        if (currentTarget == null || !currentTarget.activeInHierarchy ||
            (targetPools[i] != null && targetPools[i].SpawnVersion != targetVersions[i]))
        {
            Debug.LogWarning($"[Chain 진단] i={i} currentTarget NULL → continue");
            continue;
        }

        Vector3 currentPosition = currentTarget.transform.position;

        EnemyCombat currentEnemy = currentTarget.GetComponent<EnemyCombat>();

        if (currentEnemy == null || currentEnemy.isDead)
        {
            Debug.LogWarning($"[Chain 진단] i={i} EnemyCombat 없음 ({currentTarget.name}) → continue");
            continue;
        }

        if (i == 0)
        {
            Debug.Log($"[Chain 진단] i=0 (최초 타겟, 번개 생성 안 함) | pos={currentPosition}");
            previousPosition = currentPosition;
            continue;
        }

        if (currentEnemy.isDead || !currentTarget.activeInHierarchy)
        {
            Debug.LogWarning(
                $"[Chain 진단] i={i} 타겟 무효 → continue | isDead={currentEnemy.isDead} | " +
                $"active={currentTarget.activeInHierarchy}"
            );
            continue;
        }

        GameObject lightningObj = PoolManager.Instance.Spawn(chainId, currentPosition);

        if (lightningObj == null)
        {
            Debug.LogWarning($"[Chain 진단] i={i} PoolManager.Spawn 실패 (null) | chainId={chainId}");
            continue;
        }

        Lightning lightning = lightningObj.GetComponent<Lightning>();

        if (lightning == null)
        {
            lightningObj.GetComponent<PooledObject>()?.DestroyObj();
            Debug.LogWarning($"[Chain 진단] i={i} Lightning 컴포넌트 없음 (스폰된 오브젝트: {lightningObj.name})");
            continue;
        }

        Debug.Log($"[Chain 진단] i={i} 번개 발사 {previousPosition} → {currentPosition} (타겟: {currentTarget.name})");

        lightning.Attack(chainId, currentPosition, currentTarget, true, previousPosition);

        previousPosition = currentPosition;

        /// <summary>WaitForSeconds 동작을 처리한다.</summary>
        yield return new WaitForSeconds(chainAttackInterval);
    }

    Debug.Log("[Chain 진단] ChainAttack 코루틴 종료");
}

    /// <summary>비활성화될 때 이벤트와 진행 중인 동작을 정리한다.</summary>
    protected virtual void OnDisable()
    {
        StopAllCoroutines();
    }
}
