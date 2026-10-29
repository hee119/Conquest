using UnityEngine;
using UnityEngine.Pool;
public class PooledObject : MonoBehaviour
{
    private IObjectPool<PooledObject> _ManagedPool;
    private bool isSpawned;
    private Vector3 initialLocalScale;
    private Quaternion initialLocalRotation;

    public bool IsSpawned => isSpawned;
    public uint SpawnVersion { get; private set; }
    
    public void SetManagedPool(IObjectPool<PooledObject> pool)
    {
        _ManagedPool = pool;
        isSpawned = false;
        initialLocalScale = transform.localScale;
        initialLocalRotation = transform.localRotation;
    }

    internal void MarkSpawned()
    {
        // Attached effects can inherit inverse scales from small enemies.
        // Restore the prefab transform before any OnEnable callback runs.
        transform.localScale = initialLocalScale;
        transform.localRotation = initialLocalRotation;
        unchecked { SpawnVersion++; }
        isSpawned = true;
    }

    public void DestroyObj()
    {
        if (_ManagedPool == null || !isSpawned)
            return;

        // Release can disable components that also request a return. Close
        // this spawn first so collision/disable callbacks cannot release twice.
        isSpawned = false;
        _ManagedPool.Release(this);
    }
}
