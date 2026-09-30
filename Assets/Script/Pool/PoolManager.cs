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
        prefabs.AddRange(Resources.LoadAll<GameObject>("Prefabs/Bullet"));
        prefabs.AddRange(Resources.LoadAll<GameObject>("Prefabs/Boss"));
        prefabs.AddRange(Resources.LoadAll<GameObject>("Prefabs/BossPattern"));
        prefabs.AddRange(Resources.LoadAll<GameObject>("Prefabs/UI"));

        

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

        var enemyInfo = prefab.GetComponent<EnemyInfo>();
        var playerInfo = prefab.GetComponent<PlayerInfo>();
        var skillInfo = prefab.GetComponent<SkillInfo>();
        var expInfo = prefab.GetComponent<ExpInfo>();
        var upgradeInfo = prefab.GetComponent<UpgradeInfo>();
        var bulletInfo = prefab.GetComponent<BulletInfo>();
        var bossPatternInfo = prefab.GetComponent<BossPatternInfo>();
        

        int id;

        if (enemyInfo != null)
            id = enemyInfo.id;
        else if (playerInfo != null)
            id = playerInfo.id;
        else if (skillInfo != null)
            id = skillInfo.skillId;
        else if (expInfo != null)
            id = expInfo.id;
        else if (upgradeInfo != null)
            id = upgradeInfo.id;
        else if (bulletInfo != null)
            id = bulletInfo.id; 
        else if (bossPatternInfo != null)
            id = bossPatternInfo.id; 
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
        
        if(pooledObject == null) Debug.LogWarning("pooled object is null");
        
        pooledObject.SetManagedPool(pool);
        pooledObject.gameObject.SetActive(false);
        return pooledObject;
    }

    private void OnGet(PooledObject pooledObject)
    {
        pooledObject.gameObject.SetActive(true);
    }

    public void OnRelease(PooledObject pooledObject)
    {
        pooledObject.gameObject.SetActive(false);
    }

    private void OnDestroyPool(PooledObject pooledObject)
    {
        if(pooledObject != null)
            Destroy(pooledObject.gameObject);
    }

    // =========================
    // 기존 함수 유지
    // =========================

    public GameObject Spawn(int id, Vector2 targetPos)
    {
        if (!_pools.ContainsKey(id))
        {
            Debug.LogWarning(id + " is not found");
            return null;
        }

        PooledObject obj = _pools[id].Get();

        if (obj == null)
        {
            Debug.LogWarning("obj is null");
            return null;
        }

        obj.gameObject.SetActive(false);
        obj.transform.position = targetPos;
        obj.gameObject.SetActive(true);

        return obj.gameObject;
    }
    
    public GameObject SpawnUI(
        int id,
        Transform parent
    )
    {
        if (!_pools.ContainsKey(id))
        {
            Debug.LogWarning(
                "[PoolManager] UI Pool ID 없음 : " +
                id
            );

            return null;
        }


        PooledObject obj =
            _pools[id].Get();


        if (obj == null)
        {
            Debug.LogWarning(
                "[PoolManager] UI Pool Object가 null"
            );

            return null;
        }


        // OnGet에서 이미 켜졌기 때문에
        // UI Transform 세팅 전에 잠깐 꺼줌
        obj.gameObject.SetActive(
            false
        );


        obj.transform.SetParent(
            parent,
            false
        );


        obj.transform.localPosition =
            Vector3.zero;


        obj.transform.localRotation =
            Quaternion.identity;


        obj.transform.localScale =
            Vector3.one;


        obj.gameObject.SetActive(
            true
        );


        return obj.gameObject;
    }

    public void SpawnTest(int id)
    {
        Spawn(id, Vector2.zero);
    }
}