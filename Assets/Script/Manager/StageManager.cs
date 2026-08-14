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
    private Camera camera;

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
            navMeshSurface = GameObject.FindGameObjectWithTag("Nav").GetComponent<NavMeshSurface>();
        }
    }

    public void StageInit()
    {
        camera = Camera.main;
        currentStage = GameManager.Instance.currentStage;
        tiles = GameManager.Instance.tiles[currentStage].tiles;
        if (backgroundTilemap == null)
        {
            backgroundTilemap = GameObject.FindGameObjectWithTag("BackGround").GetComponent<Tilemap>();
        }

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

        int padding = 5;

        Vector3Int minCell = backgroundTilemap.WorldToCell(bottomLeft) - new Vector3Int(padding, padding, 0);
        Vector3Int maxCell = backgroundTilemap.WorldToCell(topRight) + new Vector3Int(padding, padding, 0);

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                TileBase randomTile =
                    tiles[Random.Range(0, tiles.Length)];

                backgroundTilemap.SetTile(new Vector3Int(x, y, 0), randomTile);
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
            CreateSpawnPoint(new Vector3Int(x, minCell.y, 0));

            // 위
            CreateSpawnPoint(new Vector3Int(x, maxCell.y, 0));
        }

        // 왼쪽 / 오른쪽
        for (int i = 0; i < spawnCountPerSide; i++)
        {
            float t = (i + 1f) / (spawnCountPerSide + 1f);

            int y = Mathf.RoundToInt(
                Mathf.Lerp(minCell.y, maxCell.y, t)
            );

            // 왼쪽
            CreateSpawnPoint(new Vector3Int(minCell.x, y, 0));

            // 오른쪽
            CreateSpawnPoint(new Vector3Int(maxCell.x, y, 0));
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

    public IEnumerator StageStartCol()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);
        Debug.Log(DataManager.Instance.stageInfos);
        Debug.Log(DataManager.Instance.stageInfos[currentStage]);
        Debug.Log(DataManager.Instance.stageInfos[currentStage].enemyId.Count);
        for (int i = 0; i < DataManager.Instance.stageInfos[currentStage].enemyId.Count; i++)
        {
            int monsterCount = 0;

            for (int j = 0; j < monsterSpawnPoints.Count; j++)
            {
                PoolManager.Instance.Spawn(
                    DataManager.Instance.stageInfos[currentStage].enemyId[i],
                    monsterSpawnPoints[j].position
                );

                monsterCount++;

                if (monsterCount >= DataManager.Instance.stageInfos[currentStage].enemySpawnCount[i])
                {
                    break;
                }

                yield return new WaitForSeconds(
                    DataManager.Instance.stageInfos[currentStage].spawnInterval[i]
                );
            }
        }
    }
}
