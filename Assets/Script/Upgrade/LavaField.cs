using UnityEngine;

public class LavaField : Burn
{
    protected override void OnTriggerEnter2D(Collider2D other)
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