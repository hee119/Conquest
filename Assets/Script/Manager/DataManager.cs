using System.Collections;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
public class DataManager : MonoBehaviour
{
    public static DataManager Instance;
    public string stageInfoText;
    public StageInfo[] stageInfos;
    

    
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

        AllDataDownload();
    }
    
    [ContextMenu("All Data Download")]
    public void AllDataDownload()
    {
        GetStageInfos();
    }
    

    public void GetStageInfos()
    {
        stageInfoText = Resources.Load<TextAsset>("TextData/StageData").text;

        string[] lines = stageInfoText.Split('\n');
        stageInfos = new StageInfo[lines.Length];

        for (int i = 0; i < stageInfos.Length; i++)
        {
            stageInfos[i] = new StageInfo();
        }
        
        for (int i = 1; i < lines.Length; i++)
        {
            int[] stageData = lines[i].Split(",").Select(int.Parse).ToArray();
            for (int j = 0; j < stageData.Length; j++)
            {
                if (i == stageData[0])
                {
                    stageInfos[i].enemyId.Add(stageData[1]);
                    stageInfos[i].enemySpawnCount.Add(stageData[2]);
                    stageInfos[i].spawnInterval.Add(stageData[3]);
                }
            }
        }
        
        Debug.Log("<color=green>Stage Data Download Complete</color>");
    }
}

public class StageInfo
{
    public List<int> enemyId = new List<int>();
    public List<int> enemySpawnCount = new List<int>();
    public List<float> spawnInterval = new List<float>();
}
