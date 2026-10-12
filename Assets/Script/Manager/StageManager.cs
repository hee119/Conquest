using System.Collections;
using System.Collections.Generic;
using NavMeshPlus.Components;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Tilemaps;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;

    public Tilemap backgroundTilemap;
    public TileBase[] tiles;

    public GameObject monsterSpawnPointPrefab;

    private List<Transform> monsterSpawnPoints =
        new List<Transform>();

    // 보스 전용 중앙 스폰포인트
    private Transform bossSpawnPoint;

    public int currentStage;
    public NavMeshSurface navMeshSurface;
    public int monsterCount = 0;

    private Camera camera;
    private Transform player;
    private GameObject runtimeCameraBounds;

    [SerializeField]
    private float minSpawnDistance = 5f;

    [SerializeField]
    private int spawnBatchSize = 10;

    private int currentWave;
    private bool canSkipToNextWave = false;
    private bool requestNextWave = false;

    private Dictionary<int, int> currentEnemyPerSpawn =
        new Dictionary<int, int>();


    private void Awake()
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

        if (navMeshSurface == null)
        {
            GameObject navObject =
                GameObject.FindGameObjectWithTag("Nav");

            if (navObject != null)
            {
                navMeshSurface =
                    navObject.GetComponent<NavMeshSurface>();
            }
        }

        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }


    public void StageInit()
    {
        camera = Camera.main;

        currentStage =
            GameManager.Instance.currentStage;

        tiles =
            GameManager.Instance.tiles[currentStage].tiles;

        if (backgroundTilemap == null)
        {
            GameObject background =
                GameObject.FindGameObjectWithTag("BackGround");

            if (background != null)
            {
                backgroundTilemap =
                    background.GetComponent<Tilemap>();
            }
        }

        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }


        // 기존 일반 스폰포인트 제거
        foreach (Transform spawnPoint in monsterSpawnPoints)
        {
            if (spawnPoint != null)
            {
                Destroy(spawnPoint.gameObject);
            }
        }

        monsterSpawnPoints.Clear();


        // 기존 보스 스폰포인트 제거
        if (bossSpawnPoint != null)
        {
            Destroy(bossSpawnPoint.gameObject);
            bossSpawnPoint = null;
        }


        monsterCount = 0;
        currentWave = 0;

        canSkipToNextWave = false;
        requestNextWave = false;

        InitializeEnemySpawnCounts();

        FillCameraArea();
        ConfigureCameraBounds();

        StageStart();
    }

    private void ConfigureCameraBounds()
    {
        if (backgroundTilemap == null)
            return;

        CinemachineCamera virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        if (virtualCamera == null)
            return;

        if (runtimeCameraBounds != null)
            Destroy(runtimeCameraBounds);

        Bounds mapBounds = backgroundTilemap.localBounds;
        if (mapBounds.size.x <= 0f || mapBounds.size.y <= 0f)
            return;

        runtimeCameraBounds = new GameObject("Runtime Camera Bounds");
        runtimeCameraBounds.hideFlags = HideFlags.HideAndDontSave;
        runtimeCameraBounds.transform.SetParent(backgroundTilemap.transform, false);

        BoxCollider2D boundsCollider = runtimeCameraBounds.AddComponent<BoxCollider2D>();
        boundsCollider.offset = mapBounds.center;
        boundsCollider.size = mapBounds.size;
        boundsCollider.isTrigger = true;

        CinemachineConfiner2D confiner = virtualCamera.GetComponent<CinemachineConfiner2D>();
        if (confiner == null)
            confiner = virtualCamera.gameObject.AddComponent<CinemachineConfiner2D>();

        confiner.BoundingShape2D = boundsCollider;
        confiner.Damping = 0f;
        confiner.SlowingDistance = 0f;
        confiner.InvalidateBoundingShapeCache();
        confiner.InvalidateLensCache();
    }


    public void GoNextWave()
    {
        if (!canSkipToNextWave)
        {
            Debug.Log(
                "[Next Wave] 지금은 넘길 수 있는 웨이브 대기 상태가 아닙니다."
            );

            return;
        }

        requestNextWave = true;

        Debug.Log(
            "[Next Wave] 다음 웨이브 진행 요청"
        );
    }


    private void InitializeEnemySpawnCounts()
    {
        currentEnemyPerSpawn.Clear();

        if (DataManager.Instance == null)
        {
            return;
        }

        StageInfo[] stageInfos =
            DataManager.Instance.stageInfos;

        if (stageInfos == null)
        {
            Debug.LogWarning(
                "DataManager.stageInfos가 null입니다."
            );

            return;
        }

        if (currentStage < 0 ||
            currentStage >= stageInfos.Length)
        {
            Debug.LogWarning(
                $"존재하지 않는 Stage입니다. currentStage = {currentStage}"
            );

            return;
        }

        StageInfo stageInfo =
            stageInfos[currentStage];

        if (stageInfo == null)
        {
            Debug.LogWarning(
                $"StageInfo가 null입니다. currentStage = {currentStage}"
            );

            return;
        }

        for (int i = 0;
             i < stageInfo.enemyId.Count;
             i++)
        {
            int enemyId =
                stageInfo.enemyId[i];

            if (!currentEnemyPerSpawn.ContainsKey(enemyId))
            {
                currentEnemyPerSpawn.Add(
                    enemyId,
                    stageInfo.enemyPerSpawn[i]
                );
            }
        }
    }


    public void StageStart()
    {
        StartCoroutine(StageStartCol());
    }


    private void FillCameraArea()
    {
        Camera cam = Camera.main;

        if (cam == null ||
            backgroundTilemap == null)
        {
            return;
        }

        Vector3 bottomLeft =
            cam.ViewportToWorldPoint(
                new Vector3(
                    0,
                    0,
                    -cam.transform.position.z
                )
            );

        Vector3 topRight =
            cam.ViewportToWorldPoint(
                new Vector3(
                    1,
                    1,
                    -cam.transform.position.z
                )
            );

        int padding = 7;

        Vector3Int minCell =
            backgroundTilemap.WorldToCell(bottomLeft)
            - new Vector3Int(
                padding,
                padding,
                0
            );

        Vector3Int maxCell =
            backgroundTilemap.WorldToCell(topRight)
            + new Vector3Int(
                padding,
                padding,
                0
            );


        for (int x = minCell.x;
             x <= maxCell.x;
             x++)
        {
            for (int y = minCell.y;
                 y <= maxCell.y;
                 y++)
            {
                TileBase randomTile =
                    tiles[
                        Random.Range(
                            0,
                            tiles.Length
                        )
                    ];

                backgroundTilemap.SetTile(
                    new Vector3Int(x, y, 0),
                    randomTile
                );
            }
        }


        if (navMeshSurface != null)
        {
            navMeshSurface.RemoveData();
            navMeshSurface.BuildNavMesh();
        }


        // 일반 몬스터용 가장자리 스폰포인트
        CreateMonsterSpawnPoints(
            minCell,
            maxCell
        );


        // 보스 전용 중앙 스폰포인트
        CreateBossSpawnPoint(
            minCell,
            maxCell
        );
    }


    private void CreateMonsterSpawnPoints(
        Vector3Int minCell,
        Vector3Int maxCell)
    {
        int spawnCountPerSide = 3;

        // 위 / 아래
        for (int i = 0;
             i < spawnCountPerSide;
             i++)
        {
            float t =
                (i + 1f) /
                (spawnCountPerSide + 1f);

            int x =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        minCell.x,
                        maxCell.x,
                        t
                    )
                );

            CreateSpawnPoint(
                new Vector3Int(
                    x,
                    minCell.y,
                    0
                )
            );

            CreateSpawnPoint(
                new Vector3Int(
                    x,
                    maxCell.y,
                    0
                )
            );
        }


        // 왼쪽 / 오른쪽
        for (int i = 0;
             i < spawnCountPerSide;
             i++)
        {
            float t =
                (i + 1f) /
                (spawnCountPerSide + 1f);

            int y =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        minCell.y,
                        maxCell.y,
                        t
                    )
                );

            CreateSpawnPoint(
                new Vector3Int(
                    minCell.x,
                    y,
                    0
                )
            );

            CreateSpawnPoint(
                new Vector3Int(
                    maxCell.x,
                    y,
                    0
                )
            );
        }
    }


    private void CreateSpawnPoint(
        Vector3Int cell)
    {
        if (monsterSpawnPointPrefab == null)
        {
            return;
        }

        Vector3 position =
            backgroundTilemap.GetCellCenterWorld(cell);

        GameObject spawnPoint =
            Instantiate(
                monsterSpawnPointPrefab,
                position,
                Quaternion.identity
            );

        monsterSpawnPoints.Add(
            spawnPoint.transform
        );
    }


    // ==============================
    // 보스 전용 중앙 스폰포인트
    // ==============================

    private void CreateBossSpawnPoint(
        Vector3Int minCell,
        Vector3Int maxCell)
    {
        if (monsterSpawnPointPrefab == null)
        {
            return;
        }

        int centerX =
            Mathf.RoundToInt(
                (minCell.x + maxCell.x) / 2f
            );

        int centerY =
            Mathf.RoundToInt(
                (minCell.y + maxCell.y) / 2f
            );

        Vector3Int centerCell =
            new Vector3Int(
                centerX,
                centerY,
                0
            );

        Vector3 centerPosition =
            backgroundTilemap.GetCellCenterWorld(
                centerCell
            );

        GameObject spawnPoint =
            Instantiate(
                monsterSpawnPointPrefab,
                centerPosition,
                Quaternion.identity
            );

        spawnPoint.name =
            "BossSpawnPoint";

        bossSpawnPoint =
            spawnPoint.transform;
    }


    private Transform GetSafeSpawnPoint()
    {
        List<Transform> safeSpawnPoints =
            new List<Transform>();

        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player =
                    playerObject.transform;
            }
        }

        foreach (Transform spawnPoint in monsterSpawnPoints)
        {
            if (spawnPoint == null)
            {
                continue;
            }

            if (player == null)
            {
                return spawnPoint;
            }

            float distance =
                Vector3.Distance(
                    player.position,
                    spawnPoint.position
                );

            if (distance >= minSpawnDistance)
            {
                safeSpawnPoints.Add(
                    spawnPoint
                );
            }
        }


        if (safeSpawnPoints.Count > 0)
        {
            return safeSpawnPoints[
                Random.Range(
                    0,
                    safeSpawnPoints.Count
                )
            ];
        }


        Transform farthestSpawnPoint = null;

        float farthestDistance = -1f;

        foreach (Transform spawnPoint in monsterSpawnPoints)
        {
            if (spawnPoint == null)
            {
                continue;
            }

            if (player == null)
            {
                return spawnPoint;
            }

            float distance =
                Vector3.Distance(
                    player.position,
                    spawnPoint.position
                );

            if (distance > farthestDistance)
            {
                farthestDistance =
                    distance;

                farthestSpawnPoint =
                    spawnPoint;
            }
        }

        return farthestSpawnPoint;
    }


    public IEnumerator StageStartCol()
    {
        yield return new WaitUntil(
            () => DataManager.Instance != null
        );

        StageInfo[] stageInfos =
            DataManager.Instance.stageInfos;

        if (stageInfos == null)
        {
            Debug.LogWarning(
                "StageInfo 배열이 null입니다."
            );

            yield break;
        }

        if (currentStage < 0 ||
            currentStage >= stageInfos.Length)
        {
            Debug.LogWarning(
                $"존재하지 않는 Stage입니다. currentStage = {currentStage}"
            );

            yield break;
        }

        StageInfo stageInfo =
            stageInfos[currentStage];

        if (stageInfo == null)
        {
            Debug.LogWarning(
                $"StageInfo가 null입니다. currentStage = {currentStage}"
            );

            yield break;
        }


        Debug.Log(
            $"[Stage Start] Stage = {currentStage}"
        );

        Debug.Log(
            $"[Stage Start] Enemy 종류 = {stageInfo.enemyId.Count}"
        );


        for (int i = 0;
             i < stageInfo.enemyId.Count;
             i++)
        {
            int enemyId =
                stageInfo.enemyId[i];

            string enemyType =
                stageInfo.type[i];

            int waveCount =
                stageInfo.waveCount[i];

            float spawnInterval =
                stageInfo.spawnInterval[i];

            float increaseInterval =
                stageInfo.waveIncreaseInterval[i];

            float spawnMultiplier =
                stageInfo.enemySpawnMultiplier[i];

            int enemyPerSpawn =
                currentEnemyPerSpawn[enemyId];


            Debug.Log(
                $"[Enemy Start] " +
                $"enemyId={enemyId} / " +
                $"type={enemyType} / " +
                $"waveCount={waveCount} / " +
                $"enemyPerSpawn={enemyPerSpawn}"
            );


            currentWave = 0;


            for (int j = 0;
                 j < waveCount;
                 j++)
            {
                currentWave++;

                Debug.Log(
                    $"[Wave Start] " +
                    $"enemyId={enemyId} / " +
                    $"wave={currentWave}/{waveCount} / " +
                    $"spawnCount={enemyPerSpawn}"
                );


                GameObject boss = null;


                // ============================
                // 보스 스폰
                // ============================

                if (enemyType == "Boss")
                {
                    if (bossSpawnPoint == null)
                    {
                        Debug.LogWarning(
                            "BossSpawnPoint가 없습니다."
                        );

                        continue;
                    }

                    GameObject spawnedBoss =
                        PoolManager.Instance.Spawn(
                            enemyId,
                            bossSpawnPoint.position
                        );

                    if (spawnedBoss != null)
                    {
                        boss = spawnedBoss;
                        monsterCount++;

                        Debug.Log(
                            $"[Boss Spawn] " +
                            $"enemyId={enemyId} / " +
                            $"position={bossSpawnPoint.position}"
                        );
                    }
                }

                // ============================
                // 일반 몬스터 스폰
                // ============================

                else
                {
                    for (int k = 0;
                         k < enemyPerSpawn;
                         k++)
                    {
                        Transform spawnPoint =
                            GetSafeSpawnPoint();

                        if (spawnPoint == null)
                        {
                            Debug.LogWarning(
                                "사용할 수 있는 몬스터 스폰포인트가 없습니다."
                            );

                            continue;
                        }

                        GameObject spawnedEnemy =
                            PoolManager.Instance.Spawn(
                                enemyId,
                                spawnPoint.position
                            );

                        if (spawnedEnemy == null)
                        {
                            Debug.LogWarning(
                                $"Spawn 실패: enemyId={enemyId}"
                            );

                            continue;
                        }

                        monsterCount++;

                        // Smooth initial spawn spikes without changing wave totals.
                        if (spawnBatchSize > 0 && (k + 1) % spawnBatchSize == 0)
                            yield return null;
                    }
                }


                // 보스 죽을 때까지 대기
                if (enemyType == "Boss")
                {
                    if (boss != null)
                    {
                        yield return new WaitUntil(
                            () =>
                                boss == null ||
                                !boss.activeInHierarchy
                        );
                    }
                }


                canSkipToNextWave = true;
                requestNextWave = false;


                Debug.Log(
                    $"[Wave Wait] " +
                    $"자동 진행 대기 중 / " +
                    $"enemyId={enemyId} / " +
                    $"wave={currentWave}/{waveCount}"
                );


                if (spawnInterval > 0)
                {
                    float timer = 0f;

                    while (
                        timer < spawnInterval &&
                        !requestNextWave
                    )
                    {
                        timer += Time.deltaTime;
                        yield return null;
                    }
                }


                canSkipToNextWave = false;
                requestNextWave = false;


                int increaseWave =
                    Mathf.RoundToInt(
                        increaseInterval
                    );


                // 일반 몬스터만 소환 수 증가
                if (enemyType != "Boss" &&
                    increaseWave > 0 &&
                    currentWave % increaseWave == 0)
                {
                    enemyPerSpawn =
                        Mathf.FloorToInt(
                            enemyPerSpawn *
                            spawnMultiplier
                        );

                    currentEnemyPerSpawn[enemyId] =
                        enemyPerSpawn;


                    Debug.Log(
                        $"[Spawn Increase] " +
                        $"enemyId={enemyId} / " +
                        $"다음 웨이브 소환수={enemyPerSpawn}"
                    );
                }
            }


            monsterCount = 0;


            Debug.Log(
                $"[Enemy End] enemyId={enemyId} 종료"
            );
        }


        canSkipToNextWave = false;
        requestNextWave = false;


        Debug.Log(
            "[Stage Complete] 모든 적 종류의 웨이브 종료"
        );

        GameEndScreenController.ShowDemoClear();
    }
}
