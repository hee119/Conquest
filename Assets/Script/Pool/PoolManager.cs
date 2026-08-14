using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    private Dictionary<int, IObjectPool<PooledObject>> _pools = new();

    [SerializeField] private int maxSize = 50;

    public GameObject[] prefabs;

    public bool prefabIsLoaded = false;

    private GameObject player;

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        prefabs = Resources.LoadAll<GameObject>("Prefabs/Enemy");

        foreach (var enemy in prefabs)
        {
            Debug.Log(enemy.name);
        }

        for (int i = 0; i < prefabs.Length; i++)
        {
            SetPool(i);
        }

        prefabIsLoaded = true;
        player = GameObject.FindGameObjectWithTag("Player");
    }
    // =========================
    // POOL SETUP
    // =========================

    void SetPool(int i)
    {
        GameObject prefab = prefabs[i];

        var unitInfo = prefab.GetComponent<UnitInfo>();

        int id;

        if (unitInfo != null)
            id = unitInfo.id;
        else
            return;

        if (_pools.ContainsKey(id))
            return;

        ObjectPool<PooledObject> pool = null;

        pool = new ObjectPool<PooledObject>(
            () => CreatePool(prefab, pool),
            OnGet,
            OnRelease,
            OnDestroyPool,
            collectionCheck: false,
            defaultCapacity: 10,
            maxSize: maxSize
        );

        _pools.Add(id, pool);
    }

    private PooledObject CreatePool(GameObject prefab, IObjectPool<PooledObject> pool)
    {
        PooledObject pooledObject = Instantiate(prefab).GetComponent<PooledObject>();
        pooledObject.SetManagedPool(pool);
        pooledObject.gameObject.SetActive(false);
        return pooledObject;
    }

    private void OnGet(PooledObject pooledObject)
    {
        pooledObject.gameObject.SetActive(true);
    }

    private void OnRelease(PooledObject pooledObject)
    {
        pooledObject.gameObject.SetActive(false);
    }

    private void OnDestroyPool(PooledObject pooledObject)
    {
        Destroy(pooledObject.gameObject);
    }

    // =========================
    // 기존 함수 유지
    // =========================

    public GameObject Spawn(int id, Vector2 targetPos)
    {
        if (!_pools.ContainsKey(id)) return null;

        PooledObject obj = _pools[id].Get();

        obj.transform.position = targetPos;

        return obj.gameObject;
    }
}