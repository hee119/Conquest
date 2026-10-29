using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    private Dictionary<int, IObjectPool<PooledObject>> _pools = new();
    private readonly HashSet<PooledObject> activeObjects = new();
    private readonly HashSet<PooledObject> pendingReparents = new();

    [SerializeField] private int maxSize = 50;
    [SerializeField] private int enemyPoolMaxSize = 200;

    public List<GameObject> prefabs = new List<GameObject>();

    public Dictionary<int, GameObject> prefabsDic = new Dictionary<int, GameObject>();

    public bool prefabIsLoaded = false;

    private GameObject player;
    private Transform stagingParent;

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
        prefabs.AddRange(Resources.LoadAll<GameObject>("Prefabs/Item"));

        

        for (int i = 0; i < prefabs.Count; i++)
        {
            SetPool(i);
        }

        prefabIsLoaded = true;
        player = GameObject.FindGameObjectWithTag("Player");
    }

    private void OnEnable()
    {
        if (Instance == this)
            GameSceneManager.SceneChanging += ResetActiveObjects;
    }

    private void OnDisable()
    {
        GameSceneManager.SceneChanging -= ResetActiveObjects;
    }

    private void LateUpdate()
    {
        FlushPendingReparents();
    }

    private void FlushPendingReparents()
    {
        foreach (PooledObject pooledObject in pendingReparents)
        {
            if (pooledObject != null && !pooledObject.IsSpawned)
                pooledObject.transform.SetParent(transform, false);
        }
        pendingReparents.Clear();
    }

    private void OnDestroy()
    {
        GameSceneManager.SceneChanging -= ResetActiveObjects;
        if (Instance == this)
            Instance = null;
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
        var healthPickup = prefab.GetComponent<HealthPickup>();
        var magnetPickup = prefab.GetComponent<MagnetPickup>();
        

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
        else if (healthPickup != null)
            id = healthPickup.id;
        else if (magnetPickup != null)
            id = magnetPickup.id;
        else
            return;

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
            maxSize: enemyInfo != null ? enemyPoolMaxSize : maxSize
        );

        _pools.Add(id, pool);
    }

    private PooledObject CreatePool(GameObject prefab, IObjectPool<PooledObject> pool)
    {
        // Keep the prefab inactive in the hierarchy until its pool is assigned.
        // Otherwise OnEnable runs once during Instantiate, before Spawn can set
        // the position or the upgrade level.
        if (stagingParent == null)
        {
            GameObject staging = new GameObject(prefab.name + "Mom");
            staging.transform.SetParent(transform, false);
            staging.SetActive(false);
            stagingParent = staging.transform;
        }

        GameObject instance = Instantiate(prefab, stagingParent);
        instance.SetActive(false);
        instance.transform.SetParent(transform, false);
        PooledObject pooledObject = instance.GetComponent<PooledObject>();
        
        if(pooledObject == null) Debug.LogWarning("pooled object is null");
        
        pooledObject.SetManagedPool(pool);
        pooledObject.gameObject.SetActive(false);
        return pooledObject;
    }

    private void OnGet(PooledObject pooledObject)
    {
        // A scene unload or an older double release may leave a destroyed
        // Unity object in the underlying pool. Spawn will discard that entry.
        if (pooledObject == null)
            return;

        if (pendingReparents.Remove(pooledObject))
            pooledObject.transform.SetParent(transform, false);
        pooledObject.MarkSpawned();
        activeObjects.Add(pooledObject);
    }

    public void OnRelease(PooledObject pooledObject)
    {
        if (pooledObject == null)
            return;

        activeObjects.Remove(pooledObject);
        pooledObject.gameObject.SetActive(false);

        // UI objects are temporarily parented to a scene canvas by SpawnUI.
        // Move every released object back under the persistent pool so a scene
        // reload cannot destroy a pooled reference that will be reused later.
        Transform parent = pooledObject.transform.parent;
        if (parent != null && parent != transform && !parent.gameObject.activeInHierarchy)
        {
            // ExpBubbleFlow can return children from a parent's OnDisable.
            // Unity forbids reparenting until that hierarchy change finishes.
            pendingReparents.Add(pooledObject);
        }
        else
        {
            pooledObject.transform.SetParent(transform, false);
        }
    }

    /// <summary>
    /// Clears objects left active by the previous stage before a retry.
    /// The pool itself stays alive, but no old enemies, skills, or pickups do.
    /// </summary>
    public void ResetActiveObjects()
    {
        // Move already returned UI children before the old scene is unloaded.
        FlushPendingReparents();
        List<PooledObject> snapshot = new List<PooledObject>(activeObjects);
        // Detach pooled children before disabling their former parents.
        // This also saves scene-owned UI objects before that scene unloads.
        foreach (PooledObject pooledObject in snapshot)
        {
            if (pooledObject != null)
                pooledObject.transform.SetParent(transform, true);
        }

        foreach (PooledObject pooledObject in snapshot)
        {
            if (pooledObject != null)
                pooledObject.DestroyObj();
        }

        activeObjects.Clear();
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
        PooledObject obj = GetPooledObject(id);
        if (obj == null)
            return null;

        obj.transform.position = targetPos;
        obj.gameObject.SetActive(true);

        return obj.gameObject;
    }
    
    public GameObject SpawnUI(
        int id,
        Transform parent
    )
    {
        PooledObject obj = GetPooledObject(id);
        if (obj == null)
            return null;


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

    private PooledObject GetPooledObject(int id)
    {
        if (!_pools.TryGetValue(id, out IObjectPool<PooledObject> pool))
        {
            Debug.LogWarning($"[PoolManager] Pool ID {id} is not registered.", this);
            return null;
        }

        // Drain invalid cached entries, then allow one fresh creation. Keep
        // this bounded so a broken prefab cannot cause an infinite loop.
        int attempts = pool.CountInactive + 1;
        while (attempts-- > 0)
        {
            PooledObject obj = pool.Get();
            if (obj != null)
                return obj;
            activeObjects.Remove(obj);
        }

        Debug.LogError($"[PoolManager] Could not create an object for pool ID {id}.", this);
        return null;
    }

    public void SpawnTest(int id)
    {
        var countInputObject = GameObject.Find("SpawnIdCountInput");
        var countInput = countInputObject != null
            ? countInputObject.GetComponent<TMPro.TMP_InputField>()
            : null;

        // 씬에 입력칸이 없는 경우 기존 동작대로 한 마리를 생성합니다.
        if (countInput == null)
        {
            Spawn(id, Vector2.zero);
            return;
        }

        if (!int.TryParse(countInput.text, out int spawnCount) || spawnCount < 0)
            return;

        for (int i = 0; i < spawnCount; i++)
            Spawn(id, Vector2.zero);
    }
    
    
}
