using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;
public class PooledObject : MonoBehaviour
{
    private IObjectPool<PooledObject> _ManagedPool;
    
    public void SetManagedPool(IObjectPool<PooledObject> pool)
    {
        _ManagedPool = pool;
    }

    public void DestroyObj()
    {
        if (_ManagedPool != null)
        {
            _ManagedPool.Release(this);
        }
    }
    

    public void EnableObj()
    {
        _ManagedPool.Get();
    }
    
}