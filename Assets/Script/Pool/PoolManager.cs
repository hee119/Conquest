using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    private Dictionary<int, IObjectPool<PooledObject>> _pools = new();

    [SerializeField] private int maxSize = 50;

    public List<GameObject> prefabs = new List<GameObject>();

    public Dictionary<int, GameObject> prefabsDic = new Dictionary<int, GameObject>();

    public bool prefabIsLoaded = false;

    private GameObject player;

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        prefabs.AddRange(Resources.LoadAll<GameObject>("Prefabs/Enemy"));
        prefabs.AddRange(Resources.LoadAll<GameObject>("Prefabs/SkillEffect"));
        prefabs.AddRange(Resources.LoadAll<GameObject>("Prefabs/Exp"));
        prefabs.AddRange(Resources.LoadAll<GameObject>("Prefabs/Upgrade"));

        

        for (int i = 0; i < prefabs.Count; i++)
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
        var skillInfo = prefab.GetComponent<SkillInfo>();
        var expInfo = prefab.GetComponent<ExpInfo>();
        var upgradeInfo = prefab.GetComponent<UpgradeInfo>();
        

        int id;

        if (unitInfo != null)
            id = unitInfo.id;
        else if (skillInfo != null)
            id = skillInfo.skillId;
        else if (expInfo != null)
            id = expInfo.id;
        else if (upgradeInfo != null)
            id = upgradeInfo.id;
        else
            return;

        Debug.Log(id);
        if (_pools.ContainsKey(id))
        {
            Debug.LogWarning("pool already contains id: " + id);
            return;
        }
        
        prefabsDic.Add(id, prefab);
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
        if (!_pools.ContainsKey(id))
        {
            Debug.LogWarning(id + "is not found");
            Debug.LogWarning("Pool id not found");
            return null;
        }

        PooledObject obj = _pools[id].Get();
        
        if (obj == null)
        {
            Debug.LogWarning("obj is null");
        }

        obj.transform.position = targetPos;

        return obj.gameObject;
    }
}