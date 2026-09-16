using System.Collections;
using System.Collections.Generic;
using NavMeshPlus.Components;
using UnityEngine;
using UnityEngine.Tilemaps;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;

    public Tilemap backgroundTilemap;
    public TileBase[] tiles;
    public GameObject monsterSpawnPointPrefab;

    private List<Transform> monsterSpawnPoints = new List<Transform>();

    public int currentStage;
    public NavMeshSurface navMeshSurface;
    public int monsterCount = 0;

    private Camera camera;

    private Transform player;
    [SerializeField] private float minSpawnDistance = 5f;
    
    int currentWave;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
        }

        if (navMeshSurface == null)
        {
            navMeshSurface = GameObject.FindGameObjectWithTag("Nav")
                .GetComponent<NavMeshSurface>();
        }
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }


    public void StageInit()
    {
        camera = Camera.main;

        currentStage = GameManager.Instance.currentStage;
        tiles = GameManager.Instance.tiles[currentStage].tiles;

        if (backgroundTilemap == null)
        {
            backgroundTilemap = GameObject.FindGameObjectWithTag("BackGround")
                .GetComponent<Tilemap>();
        }

        // 기존 스폰포인트 제거
        foreach (Transform spawnPoint in monsterSpawnPoints)
        {
            if (spawnPoint != null)
            {
                Destroy(spawnPoint.gameObject);
            }
        }

        monsterSpawnPoints.Clear();
        monsterCount = 0;

        FillCameraArea();
        StageStart();
    }


    public void StageStart()
    {
        StartCoroutine(StageStartCol());
    }


    void FillCameraArea()
    {
        Camera cam = Camera.main;

        Vector3 bottomLeft = cam.ViewportToWorldPoint(
            new Vector3(0, 0, -cam.transform.position.z)
        );

        Vector3 topRight = cam.ViewportToWorldPoint(
            new Vector3(1, 1, -cam.transform.position.z)
        );

        int padding = 7;

        Vector3Int minCell =
            backgroundTilemap.WorldToCell(bottomLeft)
            - new Vector3Int(padding, padding, 0);

        Vector3Int maxCell =
            backgroundTilemap.WorldToCell(topRight)
            + new Vector3Int(padding, padding, 0);


        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                TileBase randomTile =
                    tiles[Random.Range(0, tiles.Length)];

                backgroundTilemap.SetTile(
                    new Vector3Int(x, y, 0),
                    randomTile
                );
            }
        }


        navMeshSurface.RemoveData();
        navMeshSurface.BuildNavMesh();

        CreateMonsterSpawnPoints(minCell, maxCell);
    }


    void CreateMonsterSpawnPoints(Vector3Int minCell, Vector3Int maxCell)
    {
        int spawnCountPerSide = 3;


        // 아래 / 위
        for (int i = 0; i < spawnCountPerSide; i++)
        {
            float t = (i + 1f) / (spawnCountPerSide + 1f);

            int x = Mathf.RoundToInt(
                Mathf.Lerp(minCell.x, maxCell.x, t)
            );

            // 아래
            CreateSpawnPoint(
                new Vector3Int(x, minCell.y, 0)
            );

            // 위
            CreateSpawnPoint(
                new Vector3Int(x, maxCell.y, 0)
            );
        }


        // 왼쪽 / 오른쪽
        for (int i = 0; i < spawnCountPerSide; i++)
        {
            float t = (i + 1f) / (spawnCountPerSide + 1f);

            int y = Mathf.RoundToInt(
                Mathf.Lerp(minCell.y, maxCell.y, t)
            );

            // 왼쪽
            CreateSpawnPoint(
                new Vector3Int(minCell.x, y, 0)
            );

            // 오른쪽
            CreateSpawnPoint(
                new Vector3Int(maxCell.x, y, 0)
            );
        }
    }


    void CreateSpawnPoint(Vector3Int cell)
    {
        Vector3 position =
            backgroundTilemap.GetCellCenterWorld(cell);

        GameObject spawnPoint = Instantiate(
            monsterSpawnPointPrefab,
            position,
            Quaternion.identity
        );

        monsterSpawnPoints.Add(spawnPoint.transform);
    }


    Transform GetSafeSpawnPoint()
    {
        List<Transform> safeSpawnPoints =
            new List<Transform>();


        // 플레이어와 충분히 떨어진 스폰포인트만 추가
        foreach (Transform spawnPoint in monsterSpawnPoints)
        {
            if (spawnPoint == null)
                continue;

            float distance =
                Vector3.Distance(
                    player.position,
                    spawnPoint.position
                );

            if (distance >= minSpawnDistance)
            {
                safeSpawnPoints.Add(spawnPoint);
            }
        }


        // 안전한 스폰포인트가 있으면 랜덤 선택
        if (safeSpawnPoints.Count > 0)
        {
            return safeSpawnPoints[
                Random.Range(0, safeSpawnPoints.Count)
            ];
        }


        // 모든 스폰포인트가 가까우면
        // 플레이어와 가장 먼 곳 선택
        Transform farthestSpawnPoint = null;
        float farthestDistance = -1f;

        foreach (Transform spawnPoint in monsterSpawnPoints)
        {
            if (spawnPoint == null)
                continue;

            float distance =
                Vector3.Distance(
                    player.position,
                    spawnPoint.position
                );

            if (distance > farthestDistance)
            {
                farthestDistance = distance;
                farthestSpawnPoint = spawnPoint;
            }
        }

        return farthestSpawnPoint;
    }


    public IEnumerator StageStartCol()
    {
        yield return new WaitUntil(
            () => DataManager.Instance != null
        );


        Debug.Log(DataManager.Instance.stageInfos);
        Debug.Log(DataManager.Instance.stageInfos[currentStage]);
        Debug.Log(
            DataManager.Instance
                .stageInfos[currentStage]
                .enemyId.Count
        );


        for (int i = 0; i < DataManager.Instance.stageInfos[currentStage].enemyId.Count; i++)
        {
            for (int j = 0; j < DataManager.Instance.stageInfos[currentStage].waveCount[i]; j++)
            {

                GameObject boss = null;
                for (int k = 0; k < DataManager.Instance.stageInfos[currentStage].enemyPerSpawn[i]; k++) {
                    
                    Transform spawnPoint = GetSafeSpawnPoint();
                    
                    if (spawnPoint == null)
                    {
                        Debug.LogWarning(
                            "사용할 수 있는 몬스터 스폰포인트가 없습니다."
                        );

                        continue;
                    }

                    if (DataManager.Instance.stageInfos[currentStage].type[i] == "Boss")
                    {
                        boss = PoolManager.Instance.Spawn(
                            DataManager.Instance
                                .stageInfos[currentStage]
                                .enemyId[i],
                            spawnPoint.position
                        );
                    }
                    else
                    {
                        PoolManager.Instance.Spawn(
                            DataManager.Instance
                                .stageInfos[currentStage]
                                .enemyId[i],
                            spawnPoint.position
                        );
                    }


                    monsterCount++;
                }

                if (DataManager.Instance.stageInfos[currentStage].type[i] == "Boss")
                {
                    yield return new WaitUntil(() => !boss.activeInHierarchy);
                }
                
                yield return new WaitForSeconds(
                    DataManager.Instance
                        .stageInfos[currentStage]
                        .spawnInterval[i]
                );

                currentWave++;

                if (
                    currentWave >=
                    DataManager.Instance
                        .stageInfos[currentStage]
                        .waveCount[i]
                )
                {
                    monsterCount = 0;
                    currentWave = 0;
                    break;
                }
                
                if (currentWave % DataManager.Instance.stageInfos[currentStage].waveIncreaseInterval[i] == 0)
                {
                    DataManager.Instance.stageInfos[currentStage].enemyPerSpawn[i] =
                        (int)(DataManager.Instance.stageInfos[currentStage].enemyPerSpawn[i] *
                              DataManager.Instance.stageInfos[currentStage].enemySpawnMultiplier[i]);                
                }
                
            }
        }
    }
}