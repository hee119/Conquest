using UnityEngine;

/// <summary>LavaField 증강 관련 동작과 상태를 관리한다.</summary>
public class LavaField : Burn
{
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy"))
            return;
        Debug.Log("적이다");
        HandleEnemyContact(other);
    }
    /// <summary>다른 콜라이더가 진입했을 때 적중 효과를 처리한다.</summary>
    public void HandleEnemyContact(Collider2D other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        // 이미 불타고 있으면 무시
        EnemyCombat enemy = other.GetComponent<EnemyCombat>();

        if (enemy == null)
        {
            Debug.Log("적이없다");
            return;
        }
        
        if (enemy.isBurn)
        {
            Debug.Log("불타고 있다");
            return;
        }

        // Burn 오브젝트 생성
        GameObject burn = PoolManager.Instance.Spawn(
            GameManager.Instance.burnPrefabId,
            other.transform.position
        );
        
        if (burn == null)
        {
            Debug.Log("id가 다르다");
            return;
        }

        Burn burnComponent = burn.GetComponent<Burn>();

        if (burnComponent != null)
        {
            burn.SetActive(true);
            burnComponent.TargetBurn(other.gameObject);

            Debug.Log(
                $"[LavaField] BurnID={GameManager.Instance.burnPrefabId}, " +
                $"isBurn={enemy.isBurn}, " +
                $"BurnActive={burn.activeInHierarchy}"
            );        }
    }
}