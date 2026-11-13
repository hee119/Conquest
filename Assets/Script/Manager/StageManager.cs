using System.Collections;
using System.Collections.Generic;
using NavMeshPlus.Components;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;

    public Tilemap backgroundTilemap;
    public TileBase[] tiles;

    [Header("장식 타일 자동 배치")]
    [Tooltip("비워두면 현재 스테이지의 DecorationGrid 아래 타일맵을 사용합니다.")]
    public Tilemap decorationTilemap;
    public TileBase[] decorationTiles = System.Array.Empty<TileBase>();
    [Min(0)] public int decorationCount = 50;
    [Tooltip("장식 타일 중심 사이의 최소 거리입니다. 월드 단위이며 0이면 간격을 제한하지 않습니다.")]
    [Min(0f)] public float decorationMinimumSpacing = 2f;

    private Tilemap generatedDecorationTilemap;
    private readonly Dictionary<Vector3Int, TileBase> generatedDecorations = new();
    private string decorationTilemapPath;
    private Tilemap preparedTerrain;
    private Vector3Int preparedMinCell, preparedMaxCell;

    public int waveRow;

    // Called on the inactive scene component: generate scenery without Awake,
    // spawning enemies, starting a run, or activating scene roots.
    public void PrepareTilesForLoading()
    {
        var scene = gameObject.scene;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var map in root.GetComponentsInChildren<Tilemap>(true))
                if (map.CompareTag("BackGround")) backgroundTilemap = map;
            foreach (var candidate in root.GetComponentsInChildren<Camera>(true))
                if (candidate.CompareTag("MainCamera")) camera = candidate;
        }
        if (tiles == null || tiles.Length == 0)
        {
            int stage = GameManager.Instance != null ? GameManager.Instance.currentStage : Mathf.Max(1, currentStage);
            var allTiles = Resources.LoadAll<TileBase>("Tiles");
            var choices = new List<TileBase>();
            for (int i = (stage - 1) * 5; i < Mathf.Min(stage * 5, allTiles.Length); i++)
                choices.Add(allTiles[i]);
            tiles = choices.ToArray();
        }
        FillCameraArea(true);
    }

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

    [SerializeField, Range(0f, 1f)]
    private float forwardSpawnChance = 0.75f;

    [SerializeField, Min(0.01f)]
    private float spawnViewportMargin = 0.06f;

    private PlayerMove playerMovement;

    [SerializeField, Min(1)]
    private int maxLivingEnemies = 120;

    [SerializeField, Min(0.05f)]
    private float enemyRefillInterval = 0.25f;

    private int reachedRewardWave;
    private bool canSkipToNextWave = false;
    private bool requestNextWave = false;

    private Coroutine stageRoutine;
    private Coroutine healthPickupRoutine;
    private Coroutine magnetPickupRoutine;
    private string stageSceneName;


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
            playerMovement = playerObject.GetComponent<PlayerMove>();
        }
    }


    public void StageInit()
    {
        StopRun();
        CharacterProgression.BeginRun();
        reachedRewardWave = 0;

        // Retry reloads the scene while this manager and its pools persist.
        // Release every object left from the failed run before rebuilding the stage.
        if (PoolManager.Instance != null)
            PoolManager.Instance.ResetActiveObjects();

        camera = Camera.main;
        stageSceneName = SceneManager.GetActiveScene().name;

        // StageManager survives scene reloads, while the NavMeshSurface belongs
        // to the new Stage1 scene. Rebind it before FillCameraArea rebuilds data.
        if (navMeshSurface == null)
        {
            GameObject navObject = GameObject.FindGameObjectWithTag("Nav");
            navMeshSurface = navObject != null
                ? navObject.GetComponent<NavMeshSurface>()
                : null;
        }

        currentStage =
            GameManager.Instance.currentStage;

        if (DataManager.Instance != null)
            DataManager.Instance.GetWaveBadgeDatas();

        if (DataManager.Instance != null && DataManager.Instance.TryGetWaveGold(currentStage, 0, out int initialGold))
            CharacterProgression.RecordWaveGold(initialGold);

        if (tiles == null || tiles.Length == 0)
        {
            tiles =
                GameManager.Instance.tiles[currentStage].tiles;
        }
        
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
            playerMovement = playerObject.GetComponent<PlayerMove>();
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

        canSkipToNextWave = false;
        requestNextWave = false;

        FillCameraArea();
        ConfigureCameraBounds();

        SoundManager.Instance?.Play(SoundIds.BattleMusic, loop: true, bgm: true);
        StageStart();
        StartPickupCoroutines();
    }

    public void StopRun()
    {
        if (stageRoutine != null)
            StopCoroutine(stageRoutine);
        stageRoutine = null;
        StopPickupCoroutines();
        canSkipToNextWave = false;
        requestNextWave = false;
    }

    private void StartPickupCoroutines()
    {
        if (DataManager.Instance == null ||
            !DataManager.Instance.pickupSpawnDatas.TryGetValue(currentStage, out PickupSpawnData data))
            return;

        healthPickupRoutine = StartCoroutine(
            SpawnPickupLoop(HealthPickup.PoolId, data.healthSpawnIntervalSeconds, data.healAmount));
        magnetPickupRoutine = StartCoroutine(
            SpawnPickupLoop(MagnetPickup.PoolId, data.magnetSpawnIntervalSeconds, 0));
    }

    private void StopPickupCoroutines()
    {
        if (healthPickupRoutine != null)
            StopCoroutine(healthPickupRoutine);
        if (magnetPickupRoutine != null)
            StopCoroutine(magnetPickupRoutine);
        healthPickupRoutine = null;
        magnetPickupRoutine = null;
    }

    private IEnumerator SpawnPickupLoop(int pickupId, float interval, int healAmount)
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);

            if (SceneManager.GetActiveScene().name != stageSceneName ||
                player == null || !player.gameObject.activeInHierarchy)
                yield break;

            int activeCount = pickupId == HealthPickup.PoolId
                ? FindObjectsByType<HealthPickup>(FindObjectsSortMode.None).Length
                : FindObjectsByType<MagnetPickup>(FindObjectsSortMode.None).Length;
            int maxActive = pickupId == HealthPickup.PoolId ? 3 : 1;
            if (activeCount >= maxActive || !TryGetPickupSpawnPosition(out Vector2 position))
                continue;

            GameObject pickup = PoolManager.Instance != null
                ? PoolManager.Instance.Spawn(pickupId, position)
                : null;
            if (pickupId == HealthPickup.PoolId && pickup != null)
                pickup.GetComponent<HealthPickup>().Configure(healAmount);
        }
    }

    private bool TryGetPickupSpawnPosition(out Vector2 position)
    {
        position = default;
        if (camera == null || backgroundTilemap == null)
            return false;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector3 point = camera.ViewportToWorldPoint(new Vector3(
                Random.Range(0.1f, 0.9f), Random.Range(0.1f, 0.9f),
                camera.nearClipPlane));
            Vector2 candidate = point;
            if (Vector2.Distance(candidate, player.position) < 2.5f ||
                !backgroundTilemap.HasTile(backgroundTilemap.WorldToCell(point)))
                continue;

            position = candidate;
            return true;
        }

        return false;
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


    private static bool IsUpgradeChoiceOpen =>
        LevelManager.Instance != null &&
        LevelManager.Instance.upgradeUI != null &&
        LevelManager.Instance.upgradeUI.activeInHierarchy;

    public void GoNextWave()
    {
        if (IsUpgradeChoiceOpen)
            return;

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


    public void StageStart()
    {
        if (stageRoutine != null)
            StopCoroutine(stageRoutine);

        stageRoutine = StartCoroutine(StageStartCol());
    }


    private void FillCameraArea(bool terrainOnly = false)
    {
        Camera cam = camera != null ? camera : Camera.main;

        if (cam == null ||
            backgroundTilemap == null || tiles == null || tiles.Length == 0)
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

        // Include the whole painted map as well as the camera padding, so holes
        // outside the initial camera view are filled without replacing painted cells.
        if (backgroundTilemap.GetUsedTilesCount() > 0)
        {
            BoundsInt paintedBounds = backgroundTilemap.cellBounds;
            minCell.x = Mathf.Min(minCell.x, paintedBounds.xMin);
            minCell.y = Mathf.Min(minCell.y, paintedBounds.yMin);
            maxCell.x = Mathf.Max(maxCell.x, paintedBounds.xMax - 1);
            maxCell.y = Mathf.Max(maxCell.y, paintedBounds.yMax - 1);
        }


        if (preparedTerrain == backgroundTilemap)
        {
            minCell = preparedMinCell;
            maxCell = preparedMaxCell;
        }
        else
        {
        for (int x = minCell.x;
             x <= maxCell.x;
             x++)
        {
            for (int y = minCell.y;
                 y <= maxCell.y;
                 y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (backgroundTilemap.HasTile(cell))
                    continue;

                TileBase randomTile =
                    tiles[
                        Random.Range(
                            0,
                            tiles.Length
                        )
                    ];

                backgroundTilemap.SetTile(
                    cell,
                    randomTile
                );
            }
        }


        ScatterGroundTiles(minCell, maxCell);
        PlaceDecorationTiles(minCell, maxCell);
        preparedTerrain = backgroundTilemap;
        preparedMinCell = minCell;
        preparedMaxCell = maxCell;
        }

        if (terrainOnly) return;

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


    private void ScatterGroundTiles(Vector3Int minCell, Vector3Int maxCell)
    {
        // Redistribute the existing grass/flower mix over the whole prepared map.
        // Keep the same tile counts and leave the obstacle (tree) tilemap alone.
        var cells = new List<Vector3Int>();
        var groundTiles = new List<TileBase>();
        for (int x = minCell.x; x <= maxCell.x; x++)
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                var tile = backgroundTilemap.GetTile(cell);
                if (tile == null) continue;
                cells.Add(cell);
                groundTiles.Add(tile);
            }
        for (int i = groundTiles.Count - 1; i > 0; i--)
        {
            int other = Random.Range(0, i + 1);
            (groundTiles[i], groundTiles[other]) = (groundTiles[other], groundTiles[i]);
        }
        backgroundTilemap.SetTiles(cells.ToArray(), groundTiles.ToArray());
    }

    private void PlaceDecorationTiles(Vector3Int minCell, Vector3Int maxCell)
    {
        // Remove only tiles this generator placed, preserving hand-painted tiles.
        if (generatedDecorationTilemap != null)
            foreach (var pair in generatedDecorations)
                if (generatedDecorationTilemap.GetTile(pair.Key) == pair.Value)
                    generatedDecorationTilemap.SetTile(pair.Key, null);
        generatedDecorations.Clear();
        generatedDecorationTilemap = null;

        if (backgroundTilemap == null || decorationCount <= 0 || decorationTiles == null)
            return;

        var choices = new List<TileBase>();
        foreach (var tile in decorationTiles)
            if (tile != null)
                choices.Add(tile);
        if (choices.Count == 0)
            return;

        // The manager survives retries, so scene-owned references must be rebound.
        
        if (decorationTilemap == null ||
            decorationTilemap.gameObject.scene != backgroundTilemap.gameObject.scene ||
            !decorationTilemap.CompareTag("Decoration"))
        {
            decorationTilemap = null;

            foreach (var root in backgroundTilemap.gameObject.scene.GetRootGameObjects())
            {
                foreach (var map in root.GetComponentsInChildren<Tilemap>(true))
                {
                    if (!map.CompareTag("Decoration"))
                        continue;

                    decorationTilemap = map;
                    break;
                }

                if (decorationTilemap != null)
                    break;
            }
        }


        if (decorationTilemap == null || decorationTilemap == backgroundTilemap)
        {
            Debug.LogWarning("[StageManager] 바닥과 별도의 장식 Tilemap을 지정하세요.");
            return;
        }
        decorationTilemapPath = GetTilemapPath(decorationTilemap);
        generatedDecorationTilemap = decorationTilemap;
        // Authored small stones participate in the random scatter too.
        // Tiles outside this configured set (including trees) remain untouched.
        foreach (var cell in decorationTilemap.cellBounds.allPositionsWithin)
            if (choices.Contains(decorationTilemap.GetTile(cell)))
                decorationTilemap.SetTile(cell, null);


        float spacing = Mathf.Max(0f, decorationMinimumSpacing);
        float bucketSize = Mathf.Max(0.01f, spacing);
        var occupied = new Dictionary<Vector2Int, List<Vector2>>();
        Vector2Int Bucket(Vector2 point) => new Vector2Int(
            Mathf.FloorToInt(point.x / bucketSize), Mathf.FloorToInt(point.y / bucketSize));
        void Remember(Vector2 point)
        {
            var key = Bucket(point);
            if (!occupied.TryGetValue(key, out var points))
                occupied[key] = points = new List<Vector2>();
            points.Add(point);
        }
        bool HasSpace(Vector2 point)
        {
            if (spacing <= 0f)
                return true;
            var key = Bucket(point);
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    if (occupied.TryGetValue(key + new Vector2Int(x, y), out var points))
                        foreach (var other in points)
                            if ((other - point).sqrMagnitude < spacing * spacing)
                                return false;
            return true;
        }

        if (spacing > 0f)
            foreach (var cell in decorationTilemap.cellBounds.allPositionsWithin)
                if (decorationTilemap.HasTile(cell))
                    Remember(decorationTilemap.GetCellCenterWorld(cell));

        var candidates = new List<Vector3Int>();
        var seen = new HashSet<Vector3Int>();
        for (int x = minCell.x; x <= maxCell.x; x++)
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                var groundCell = new Vector3Int(x, y, 0);
                if (!backgroundTilemap.HasTile(groundCell))
                    continue;
                var cell = decorationTilemap.WorldToCell(backgroundTilemap.GetCellCenterWorld(groundCell));
                if (seen.Add(cell) && !decorationTilemap.HasTile(cell))
                    candidates.Add(cell);
            }

        // Stratify the map, then give every region a turn before placing more
        // in the same region. Randomize within regions so it does not look tiled.
        float width = Mathf.Max(1, maxCell.x - minCell.x + 1);
        float height = Mathf.Max(1, maxCell.y - minCell.y + 1);
        float worldWidth = Vector3.Distance(backgroundTilemap.GetCellCenterWorld(minCell),
            backgroundTilemap.GetCellCenterWorld(minCell + new Vector3Int((int)width, 0, 0)));
        float worldHeight = Vector3.Distance(backgroundTilemap.GetCellCenterWorld(minCell),
            backgroundTilemap.GetCellCenterWorld(minCell + new Vector3Int(0, (int)height, 0)));
        // An impossible requested density must not produce hundreds of tiny regions.
        int regionCount = spacing > 0f
            ? Mathf.Min(decorationCount, Mathf.Max(1, Mathf.FloorToInt(worldWidth * worldHeight / (spacing * spacing * 1.5f))))
            : decorationCount;
        int columns = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(regionCount * width / height)), 1, (int)width);
        int rows = Mathf.Clamp(Mathf.CeilToInt((float)regionCount / columns), 1, (int)height);
        var regions = new List<Vector3Int>[columns * rows];
        for (int i = 0; i < regions.Length; i++) regions[i] = new List<Vector3Int>();
        // Shuffle all candidates once; each region inherits a random order.
        for (int i = 0; i < candidates.Count; i++)
        {
            int swap = Random.Range(i, candidates.Count);
            (candidates[i], candidates[swap]) = (candidates[swap], candidates[i]);
            Vector3Int ground = backgroundTilemap.WorldToCell(decorationTilemap.GetCellCenterWorld(candidates[i]));
            int x = Mathf.Clamp((int)((ground.x - minCell.x) / width * columns), 0, columns - 1);
            int y = Mathf.Clamp((int)((ground.y - minCell.y) / height * rows), 0, rows - 1);
            regions[y * columns + x].Add(candidates[i]);
        }
        var order = new List<int>();
        for (int i = 0; i < regions.Length; i++) if (regions[i].Count > 0) order.Add(i);
        for (int i = 0; i < order.Count; i++)
        {
            int swap = Random.Range(i, order.Count);
            (order[i], order[swap]) = (order[swap], order[i]);
        }
        bool placed;
        do
        {
            placed = false;
            foreach (int region in order)
            {
                var cells = regions[region];
                while (cells.Count > 0 && generatedDecorations.Count < decorationCount)
                {
                    var cell = cells[cells.Count - 1];
                    cells.RemoveAt(cells.Count - 1);
                    Vector2 position = decorationTilemap.GetCellCenterWorld(cell);
                    if (!HasSpace(position)) continue;
                    var tile = choices[Random.Range(0, choices.Count)];
                    decorationTilemap.SetTile(cell, tile);
                    decorationTilemap.SetColliderType(cell, Tile.ColliderType.None);
                    generatedDecorations.Add(cell, tile);
                    if (spacing > 0f) Remember(position);
                    placed = true;
                    break;
                }
            }
        } while (placed && generatedDecorations.Count < decorationCount);

        if (generatedDecorations.Count < decorationCount)
            Debug.LogWarning($"[StageManager] 장식 타일 {generatedDecorations.Count}/{decorationCount}개 배치: 빈 공간이나 최소 간격 조건을 확인하세요.");
    }

    private static string GetTilemapPath(Tilemap map)
    {
        string path = map.name;
        for (Transform parent = map.transform.parent; parent != null; parent = parent.parent)
            path = parent.name + "/" + path;
        return path;
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


    
    private bool TryGetSpawnPosition(out Vector3 position)
    {
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
                playerMovement = playerObject.GetComponent<PlayerMove>();
            }
        }

        if (player != null && camera != null && camera.orthographic && backgroundTilemap != null)
        {
            Vector2 direction = playerMovement != null ? playerMovement.MoveDir : Vector2.zero;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                int side = Random.Range(0, 4); // top, bottom, right, left
                if (direction.sqrMagnitude > 0.001f && Random.value < forwardSpawnChance)
                    side = Mathf.Abs(direction.y) >= Mathf.Abs(direction.x)
                        ? (direction.y > 0f ? 0 : 1)
                        : (direction.x > 0f ? 2 : 3);

                float along = Random.Range(0f, 1f);
                float margin = Mathf.Max(0.01f, spawnViewportMargin);
                Vector3 viewport = side == 0 ? new Vector3(along, 1f + margin, 0f)
                    : side == 1 ? new Vector3(along, -margin, 0f)
                    : side == 2 ? new Vector3(1f + margin, along, 0f)
                    : new Vector3(-margin, along, 0f);
                viewport.z = Mathf.Abs(camera.transform.position.z - player.position.z);
                Vector3 candidate = camera.ViewportToWorldPoint(viewport);
                candidate.z = player.position.z;
                if (!backgroundTilemap.HasTile(backgroundTilemap.WorldToCell(candidate)) ||
                    ((Vector2)(candidate - player.position)).sqrMagnitude < minSpawnDistance * minSpawnDistance)
                    continue;

                // Avoid obstacles and unwalkable cells on the 2D navigation surface.
                if (navMeshSurface != null)
                {
                    if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 0.75f, NavMesh.AllAreas))
                        continue;
                    candidate = hit.position;
                    candidate.z = player.position.z;
                    Vector3 sampledViewport = camera.WorldToViewportPoint(candidate);
                    if (sampledViewport.x >= 0f && sampledViewport.x <= 1f &&
                        sampledViewport.y >= 0f && sampledViewport.y <= 1f)
                        continue;
                    if (((Vector2)(candidate - player.position)).sqrMagnitude < minSpawnDistance * minSpawnDistance)
                        continue;
                }

                position = candidate;
                return true;
            }
        }

        Transform randomSafePoint = null;
        Transform farthestPoint = null;

        float farthestDistance = -1f;
        int safeCount = 0;

        foreach (Transform spawnPoint in monsterSpawnPoints)
        {
            if (spawnPoint == null)
                continue;

            if (player == null)
            {
                position = spawnPoint.position;
                return true;
            }

            float sqrDistance =
                (player.position - spawnPoint.position).sqrMagnitude;

            if (sqrDistance > farthestDistance)
            {
                farthestDistance = sqrDistance;
                farthestPoint = spawnPoint;
            }

            float minDistanceSqr =
                minSpawnDistance * minSpawnDistance;

            if (sqrDistance >= minDistanceSqr)
            {
                safeCount++;

                // Reservoir sampling: 추가 리스트 없이 무작위 선택
                if (Random.Range(0, safeCount) == 0)
                    randomSafePoint = spawnPoint;
            }
        }

        Transform fallback = randomSafePoint != null ? randomSafePoint : farthestPoint;
        position = fallback != null ? fallback.position : Vector3.zero;
        return fallback != null;
    }



    public IEnumerator StageStartCol()
    {
        waveRow = 0;
        
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

        bool bossDefeated = false;


        for (int i = 0;
             i < stageInfo.enemyId.Count;
             i++)
        {
            int enemyId =
                stageInfo.enemyId[i];

            string enemyType =
                stageInfo.type[i];

            waveRow = i;
            float spawnInterval = stageInfo.spawnInterval[i];
            int enemyPerSpawn = stageInfo.enemyPerSpawn[i];
            reachedRewardWave++;
            if (DataManager.Instance != null && DataManager.Instance.TryGetWaveGold(currentStage, reachedRewardWave, out int waveGold))
                CharacterProgression.RecordWaveGold(waveGold);

            if (SoundManager.Instance != null)
                SoundManager.Instance.Play(SoundIds.EnemyWaveStart);

            Debug.Log(
                $"[Wave Start] " +
                $"enemyId={enemyId} / " +
                $"wave={reachedRewardWave}/{stageInfo.enemyId.Count} / " +
                $"spawnCount={enemyPerSpawn}"
            );


            GameObject boss = null;
            SlimeKing.SplitEncounter splitEncounter = null;


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
                    spawnedBoss.GetComponent<BossHealthSource>()?.RegisterBoss();
                    if (spawnedBoss.TryGetComponent(out SlimeKing slimeBoss))
                        splitEncounter = slimeBoss.StartSplitEncounter();
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

            int regularWave = i;
            if (enemyType == "Boss")
            {
                regularWave = i - 1;
                while (regularWave >= 0 && stageInfo.type[regularWave] == "Boss")
                    regularWave--;
            }

            if (enemyType != "Boss" || (boss != null && regularWave >= 0))
            {
                // Counts represent percentages; legacy data uses them as weights.
                bool isBossWave = enemyType == "Boss";
                bool hasBossMinions = isBossWave && stageInfo.bossWaveEnemyIds[i] > 0;
                var groups = hasBossMinions
                    ? new List<WaveEnemyGroup> { new WaveEnemyGroup(stageInfo.bossWaveEnemyIds[i], 100) }
                    : stageInfo.enemyGroups[regularWave];
                float totalWeight = 0f;
                foreach (WaveEnemyGroup group in groups)
                    totalWeight += group.count;

                int requested = hasBossMinions ? stageInfo.bossWavePopulations[i] : stageInfo.enemyPopulation[regularWave] > 0
                    ? stageInfo.enemyPopulation[regularWave] : Mathf.CeilToInt(totalWeight);
                int population = Mathf.Min(Mathf.Max(1, requested), Mathf.Max(1, maxLivingEnemies));
                var quotas = new int[groups.Count];
                var fractions = new float[groups.Count];
                int assigned = 0;
                for (int g = 0; g < groups.Count; g++)
                {
                    float exact = population * groups[g].count / totalWeight;
                    quotas[g] = Mathf.FloorToInt(exact);
                    fractions[g] = exact - quotas[g];
                    assigned += quotas[g];
                }
                while (assigned < population)
                {
                    int best = 0;
                    for (int g = 1; g < fractions.Length; g++)
                        if (fractions[g] > fractions[best]) best = g;
                    quotas[best]++;
                    fractions[best] = -1f;
                    assigned++;
                }

                float duration = Mathf.Max(0.1f, spawnInterval);
                float elapsed = 0f;
                float nextRefill = 0f;
                float refillInterval = stageInfo.enemyRefillIntervals[i] > 0f
                    ? stageInfo.enemyRefillIntervals[i] : Mathf.Max(0.05f, enemyRefillInterval);
                var livingById = new Dictionary<int, int>();
                canSkipToNextWave = !isBossWave;
                requestNextWave = false;

                while (IsUpgradeChoiceOpen || (isBossWave
                    ? splitEncounter != null ? splitEncounter.HasLivingMembers : boss != null && boss.activeInHierarchy
                    : elapsed < duration && !requestNextWave))
                {
                    if (IsUpgradeChoiceOpen || Time.timeScale <= 0f)
                    {
                        yield return null;
                        continue;
                    }

                    float step = isBossWave ? Time.deltaTime : Mathf.Min(Time.deltaTime, duration - elapsed);
                    elapsed += step;
                    nextRefill -= step;
                    if (nextRefill > 0f)
                    {
                        yield return null;
                        continue;
                    }
                    nextRefill = refillInterval;
                    int living = EnemyCombat.CountLivingEnemiesById(livingById);
                    if (hasBossMinions)
                    {
                        livingById.TryGetValue(stageInfo.bossWaveEnemyIds[i], out living);
                    }
                    int available = Mathf.Max(0, population - living);
                    if (available > 0)
                    {
                        int selectedIndex = -1;
                        float lowestFill = float.PositiveInfinity;
                        for (int g = 0; g < groups.Count; g++)
                        {
                            livingById.TryGetValue(groups[g].enemyId, out int existing);
                            if (quotas[g] <= 0 || existing >= quotas[g]) continue;
                            float fill = (float)existing / quotas[g];
                            if (fill < lowestFill) { lowestFill = fill; selectedIndex = g; }
                        }
                        if (selectedIndex >= 0 && PoolManager.Instance != null &&
                            TryGetSpawnPosition(out Vector3 spawnPosition))
                        {
                            GameObject spawned = PoolManager.Instance.Spawn(groups[selectedIndex].enemyId, spawnPosition);
                            if (spawned != null)
                            {
                                EnemyCombat spawnedCombat = spawned.GetComponent<EnemyCombat>();
                                if (spawnedCombat != null) spawnedCombat.IsWaveAutoSpawned = true;
                                monsterCount++;
                            }
                        }
                    }
                    yield return null;
                }
                canSkipToNextWave = false;
                requestNextWave = false;
            }


            // 보스 죽을 때까지 대기
            if (enemyType == "Boss")
            {
                if (boss != null)
                {
                    EnemyCombat defeatedBoss = boss.GetComponent<EnemyCombat>();
                    yield return new WaitUntil(
                        () =>
                            splitEncounter != null ? !splitEncounter.HasLivingMembers :
                            boss == null || (defeatedBoss != null && defeatedBoss.isDead)
                    );

                    if (boss == null)
                    {
                        Debug.LogWarning("[Boss] 보스가 처치 전에 사라져 웨이브를 종료합니다.");
                        yield break;
                    }
                    bool defeated = splitEncounter != null ? splitEncounter.WasCleared :
                        defeatedBoss != null && defeatedBoss.isDead;
                    if (!defeated)
                    {
                        Debug.LogWarning("[Boss] 보스전이 처치 전에 중단되어 웨이브를 종료합니다.");
                        yield break;
                    }
                    if (i == stageInfo.enemyId.Count - 1)
                        bossDefeated = defeated;
                    if (defeated && LevelManager.Instance != null)
                    {
                        LevelManager.Instance.GrantBossLevel();
                        yield return new WaitUntil(() => LevelManager.Instance == null ||
                            (!LevelManager.Instance.HasPendingBossLevel && !IsUpgradeChoiceOpen));
                    }
                }
            }


            canSkipToNextWave = true;
            requestNextWave = false;


            Debug.Log(
                $"[Wave Wait] " +
                $"자동 진행 대기 중 / " +
                $"enemyId={enemyId} / " +
                $"wave={reachedRewardWave}/{stageInfo.enemyId.Count}"
            );


            if (enemyType == "Boss" && spawnInterval > 0)
            {
                float timer = 0f;

                while (IsUpgradeChoiceOpen || (timer < spawnInterval && !requestNextWave))
                {
                    if (!IsUpgradeChoiceOpen)
                        timer += Time.deltaTime;
                    yield return null;
                }
            }


            canSkipToNextWave = false;
            requestNextWave = false;

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

        stageRoutine = null;
        StopPickupCoroutines();

        if (currentStage == 1 && bossDefeated)
            GameEndScreenController.ShowDemoClear();
    }
}
