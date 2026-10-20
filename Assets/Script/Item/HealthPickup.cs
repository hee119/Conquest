using UnityEngine;

[RequireComponent(typeof(PooledObject), typeof(Collider2D))]
public sealed class HealthPickup : MonoBehaviour
{
    public const int PoolId = 7000;
    public int id = PoolId;

    private int healAmount;
    private PooledObject pooledObject;

    private void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
    }

    public void Configure(int amount)
    {
        healAmount = Mathf.Max(0, amount);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerCombat combat = other.GetComponent<PlayerCombat>();
        if (combat == null || combat.isDead || combat.hp >= combat.maxHp)
            return;

        combat.Heal(healAmount);
        pooledObject.DestroyObj();
    }
}
