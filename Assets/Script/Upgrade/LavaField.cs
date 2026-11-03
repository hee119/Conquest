using UnityEngine;

/// <summary>LavaField 증강 관련 동작과 상태를 관리한다.</summary>
public class LavaField : Burn
{
    /// <summary>다른 콜라이더가 진입했을 때 적중 효과를 처리한다.</summary>
    public void HandleEnemyContact(Collider2D other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        // 이미 불타고 있으면 무시
        EnemyCombat enemy = other.GetComponent<EnemyCombat>();

        if (enemy == null || enemy.isBurn)
            return;

        // Burn 오브젝트 생성
        GameObject burn = PoolManager.Instance.Spawn(
            GameManager.Instance.burnPrefabId,
            other.transform.position
        );

        if (burn == null)
            return;

        Burn burnComponent = burn.GetComponent<Burn>();

        if (burnComponent != null)
        {
            burnComponent.TargetBurn(other.gameObject);
        }
    }
}