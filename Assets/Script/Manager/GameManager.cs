using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int currentStage = 1;
    public TileBase[] tileBases;
    public Tiles[] tiles = new Tiles[6];
    StageInfo[] stageInfos;
    public int burnChance;
    public int burnPrefabId;
    [FormerlySerializedAs("mushroomChance")] public int moldChance;
    [FormerlySerializedAs("mushroomPrefabId")] public int moldPrefabId;
    public string currentLanguage = "ko";
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
        FillTiles();
    }

    public void FillTiles()
    {
        tileBases = Resources.LoadAll<TileBase>("Tiles");
        for (int i = 1; i < tiles.Length; i++)
        {
            tiles[i] = new Tiles();
            tiles[i].tiles = tileBases
                .Skip((i - 1) * 5)
                .Take(5)
                .ToArray();
        }

        StartCoroutine(WaitStageManager());
    }

    IEnumerator WaitStageManager()
    {
        yield return new WaitUntil(() => StageManager.Instance != null);        
        StageManager.Instance.StageInit();
    }

    
}

public class Tiles
{
    public TileBase[] tiles;
}
