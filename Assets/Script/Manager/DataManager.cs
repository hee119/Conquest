using System;
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
    public string skillDataText;
    public string levelDataText;
    public string expDataText;

    public StageInfo[] stageInfos;
    public Dictionary<int, PlayerAndEnemyInfos> playerAndEnemyInfos = new();
    public Dictionary<int, SkillData> skillDatas = new();
    public int[] levelDatas;
    public Dictionary<int, ExpData> expDatas = new();
    

    
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
        GetSkillDatas();
        GetLevelDatas();
        GetExpDatas();
        Debug.Log(skillDatas[200].skillHp);
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
                playerAndEnemyInfosTemp.dropExpOrbId = int.Parse(playerAndEnemyData[9]);
            playerAndEnemyInfos.Add(playerAndEnemyInfosTemp.id, playerAndEnemyInfosTemp);
        }
        
        
        GameObject[] enemyPrefabs =
            Resources.LoadAll<GameObject>("Prefabs/Enemy");

        foreach (GameObject prefab in enemyPrefabs)
        {
            UnitInfo unitInfo = prefab.GetComponent<UnitInfo>();
            EnemyCombat enemyCombat = prefab.GetComponent<EnemyCombat>();

            if (unitInfo == null)
                continue;

            if (playerAndEnemyInfos.TryGetValue(unitInfo.id, out PlayerAndEnemyInfos data))
            {
                unitInfo.hp = data.hp;
                unitInfo.hpRegenRate = data.hpRegenRate;
                unitInfo.speed = data.speed;
                unitInfo.damage = data.damage;
                unitInfo.defense = data.defense;
                unitInfo.criticalDamage = data.criticalDamage;
                unitInfo.criticalChance = data.criticalChance;
                unitInfo.dropExpOrbId = data.dropExpOrbId;
                if(enemyCombat !=null) enemyCombat.maxHp = unitInfo.hp;
            }
            else
            {
                Debug.LogWarning(
                    $"ID {unitInfo.id}에 해당하는 데이터를 찾을 수 없습니다."
                );
            }
        }
        
        GameObject[] agentPrefabs =
            Resources.LoadAll<GameObject>("Prefabs/Agent");

        foreach (GameObject prefab in agentPrefabs)
        {
            UnitInfo unitInfo = prefab.GetComponent<UnitInfo>();

            if (unitInfo == null)
                continue;

            if (playerAndEnemyInfos.TryGetValue(unitInfo.id, out PlayerAndEnemyInfos data))
            {
                unitInfo.hp = data.hp;
                unitInfo.hpRegenRate = data.hpRegenRate;
                unitInfo.speed = data.speed;
                unitInfo.damage = data.damage;
                unitInfo.defense = data.defense;
                unitInfo.criticalDamage = data.criticalDamage;
                unitInfo.criticalChance = data.criticalChance;
                unitInfo.dropExpOrbId = data.dropExpOrbId;
            }
            else
            {
                Debug.LogWarning(
                    $"ID {unitInfo.id}에 해당하는 데이터를 찾을 수 없습니다."
                );
            }
        }


        Debug.Log("<color=green>GetPlayerAndEnemy Data Download Complete</color>");
    }
    
    public void GetSkillDatas()
    {
        skillDataText = Resources.Load<TextAsset>("TextData/SkillData").text;

        string[] lines = skillDataText.Split('\n');
        
        
        for (int i = 1; i < lines.Length; i++)
        {
            string[] skillData = lines[i].Split(",");
            SkillData skillDataTemp = new SkillData();
            
            skillDataTemp.skillId = int.Parse(skillData[0]);
            skillDataTemp.skillName = skillData[1];
            skillDataTemp.skillHp = int.Parse(skillData[2]);
            skillDataTemp.skillCoolTime = float.Parse(skillData[3]);
            skillDataTemp.skillDamage = int.Parse(skillData[4]);
            skillDataTemp.skillCritDamage = int.Parse(skillData[5]);
            skillDataTemp.lifetime = float.Parse(skillData[6]);
            skillDataTemp.movementLockTime = float.Parse(skillData[7]);
            skillDatas.Add(skillDataTemp.skillId, skillDataTemp);
        }
        
        GameObject[] skillPrefabs =
            Resources.LoadAll<GameObject>("Prefabs/SkillEffect");

        // 프리팹에 데이터 넣기
        foreach (GameObject prefab in skillPrefabs)
        {
            SkillInfo skillInfo = prefab.GetComponent<SkillInfo>();

            if (skillInfo == null)
                continue;

            if (!skillDatas.TryGetValue(
                    skillInfo.skillId,
                    out SkillData data))
            {
                Debug.LogWarning(
                    $"Skill ID {skillInfo.skillId}에 해당하는 데이터를 찾을 수 없습니다."
                );

                continue;
            }

            skillInfo.skillName = data.skillName;
            skillInfo.skillHp = data.skillHp;
            skillInfo.skillCoolTime = data.skillCoolTime;
            skillInfo.skillDamage = data.skillDamage;
            skillInfo.skillCritDamage = data.skillCritDamage;
            skillInfo.lifetime = data.lifetime;
            skillInfo.movementLockTime = data.movementLockTime;
        }

        
        
        Debug.Log("<color=green>Skill Data Download Complete</color>");
    }

    public void GetLevelDatas()
    {
        levelDataText = Resources.Load<TextAsset>("TextData/LevelData").text;
        
        int[] result = Array.ConvertAll(levelDataText.Split('\n').Skip(1).ToArray(), int.Parse);
        
        levelDatas = result;
    }

    public void GetExpDatas()
    {
        expDataText = Resources.Load<TextAsset>("TextData/ExpData").text;
        
        string[] lines = expDataText.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            string[] expData = lines[i].Split(",");
            
            ExpData expDataTemp = new ExpData();
            
            expDataTemp.id = int.Parse(expData[0]);
            expDataTemp.name = expData[1];
            expDataTemp.exp = int.Parse(expData[2]);
            
            expDatas.Add(expDataTemp.id, expDataTemp);
        }
        
        GameObject[] expPrefabs = Resources.LoadAll<GameObject>("Prefabs/Exp");

        foreach (GameObject prefab in expPrefabs)
        {
            ExpInfo expData = prefab.GetComponent<ExpInfo>();
            if (expData != null)
            {
                expData.name = expDatas[expData.id].name;
                expData.exp = expDatas[expData.id].exp;
            }
        }
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
    public int dropExpOrbId;
}

public class SkillData
{
    public int skillId;
    public string skillName;
    public int skillHp;
    public float skillCoolTime;
    public int skillDamage;
    public int skillCritDamage;
    public float lifetime;
    public float movementLockTime;
}

public class ExpData
{
    public int id;
    public string name;
    public int exp;
}
