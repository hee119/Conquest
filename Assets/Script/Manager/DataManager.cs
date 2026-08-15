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
    public string stageDataText;
    public string playerAndEnemyDataText;

    public StageInfo[] stageInfos;
    public Dictionary<int, PlayerAndEnemyInfos> playerAndEnemyInfos = new();
    

    
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
        GetStageDatas();
        GetPlayerAndEnemyDatas();
    }
    

    public void GetStageDatas()
    {
        stageDataText = Resources.Load<TextAsset>("TextData/StageData").text;

        string[] lines = stageDataText.Split('\n');
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
    
    public void GetPlayerAndEnemyDatas()
    {
        playerAndEnemyDataText = Resources.Load<TextAsset>("TextData/PlayerAndEnemyData").text;

        string[] lines = playerAndEnemyDataText.Split('\n');
        
        
        for (int i = 1; i < lines.Length; i++)
        {
            string[] playerAndEnemyData = lines[i].Split(",");
            PlayerAndEnemyInfos playerAndEnemyInfosTemp = new PlayerAndEnemyInfos();
                playerAndEnemyInfosTemp.id = int.Parse(playerAndEnemyData[0]);
                playerAndEnemyInfosTemp.name = playerAndEnemyData[1];
                playerAndEnemyInfosTemp.hp = int.Parse(playerAndEnemyData[2]);
                playerAndEnemyInfosTemp.hpRegenRate = int.Parse(playerAndEnemyData[3]);
                playerAndEnemyInfosTemp.speed = float.Parse(playerAndEnemyData[4]);
                playerAndEnemyInfosTemp.damage = int.Parse(playerAndEnemyData[5]);
                playerAndEnemyInfosTemp.defense = int.Parse(playerAndEnemyData[6]);
                playerAndEnemyInfosTemp.criticalDamage = float.Parse(playerAndEnemyData[7]);
                playerAndEnemyInfosTemp.criticalChance = int.Parse(playerAndEnemyData[8]);
            playerAndEnemyInfos.Add(playerAndEnemyInfosTemp.id, playerAndEnemyInfosTemp);
        }
        
        Debug.Log("<color=green>GetPlayerAndEnemy Data Download Complete</color>");
    }
}

public class StageInfo
{
    public List<int> enemyId = new List<int>();
    public List<int> enemySpawnCount = new List<int>();
    public List<float> spawnInterval = new List<float>();
}

public class PlayerAndEnemyInfos
{
    public int id;
    public string name;
    public int hp;
    public int hpRegenRate;
    public float speed;
    public int damage;
    public int defense;
    public float criticalDamage;
    public int criticalChance;
}
