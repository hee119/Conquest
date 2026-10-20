using UnityEngine;

[RequireComponent(typeof(PooledObject), typeof(Collider2D))]
public sealed class MagnetPickup : MonoBehaviour
{
    public const int PoolId = 7001;
    public int id = PoolId;

    private PooledObject pooledObject;

    private void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerCombat combat = other.GetComponent<PlayerCombat>();
        if (combat == null || combat.isDead)
            return;

        foreach (Exp orb in FindObjectsByType<Exp>(FindObjectsSortMode.None))
            orb.ActivateMagnet(other.transform);

        pooledObject.DestroyObj();
    }
}
