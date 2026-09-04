using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using UnityEngine.UI;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;
    public string stageDataText;
    public string playerAndEnemyDataText;
    public string skillDataText;
    public string levelDataText;
    public string expDataText;
    public string upgradeDataText;

    public StageInfo[] stageInfos;
    public Dictionary<int, PlayerAndEnemyInfos> playerAndEnemyInfosDic = new();
    public Dictionary<int, SkillData> skillDatasDic = new();
    public int[] levelDatas;
    public Dictionary<int, ExpData> expDatasDic = new();
    public Dictionary<int, UpgradeData> UpgradeDatasDic = new();
    public Dictionary <int, Image> upgradeImagesDic = new();

    
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
        GetUpgradeDatas();
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
            string[] stageData = lines[i].Split(",");
            int stage = int.Parse(stageData[0]);
            int enemyId = int.Parse(stageData[1]);
            int enemySpawnCount = int.Parse(stageData[2]);
            float spawnInterval = float.Parse(stageData[3]);
            int monstersPerSpawn = int.Parse(stageData[4]);
            for (int j = 0; j < stageData.Length; j++)
            {
                if (i == stage)
                {
                    stageInfos[i].enemyId.Add(enemyId);
                    stageInfos[i].enemySpawnCount.Add(enemySpawnCount);
                    stageInfos[i].spawnInterval.Add(spawnInterval);
                    stageInfos[i].enemysPerSpawn.Add(monstersPerSpawn);
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
            playerAndEnemyInfosDic.Add(playerAndEnemyInfosTemp.id, playerAndEnemyInfosTemp);
        }
        
        
        GameObject[] enemyPrefabs =
            Resources.LoadAll<GameObject>("Prefabs/Enemy");

        foreach (GameObject prefab in enemyPrefabs)
        {
            UnitInfo unitInfo = prefab.GetComponent<UnitInfo>();
            EnemyCombat enemyCombat = prefab.GetComponent<EnemyCombat>();
            EnemyMove enemyMove = prefab.GetComponent<EnemyMove>();

            if (unitInfo == null)
                continue;

            if (playerAndEnemyInfosDic.TryGetValue(unitInfo.id, out PlayerAndEnemyInfos data))
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
                if(enemyMove !=null) enemyMove.moveSpeed = unitInfo.speed;
                
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

            if (playerAndEnemyInfosDic.TryGetValue(unitInfo.id, out PlayerAndEnemyInfos data))
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
            skillDatasDic.Add(skillDataTemp.skillId, skillDataTemp);
        }
        
        GameObject[] skillPrefabs =
            Resources.LoadAll<GameObject>("Prefabs/SkillEffect");

        // 프리팹에 데이터 넣기
        foreach (GameObject prefab in skillPrefabs)
        {
            SkillInfo skillInfo = prefab.GetComponent<SkillInfo>();

            if (skillInfo == null)
                continue;

            if (!skillDatasDic.TryGetValue(
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
            
            expDatasDic.Add(expDataTemp.id, expDataTemp);
        }
        
        GameObject[] expPrefabs = Resources.LoadAll<GameObject>("Prefabs/Exp");

        foreach (GameObject prefab in expPrefabs)
        {
            ExpInfo expData = prefab.GetComponent<ExpInfo>();
            if (expData != null)
            {
                expData.name = expDatasDic[expData.id].name;
                expData.exp = expDatasDic[expData.id].exp;
            }
        }
    }

    public void GetUpgradeDatas()
    {
        StartCoroutine(GetUpgradeDatasCor());
    }
    
    public IEnumerator GetUpgradeDatasCor()
{
    yield return new WaitUntil(() => UpgradeManager.Instance != null);

    // UpgradeData CSV 불러오기
    TextAsset textAsset = Resources.Load<TextAsset>("TextData/UpgradeData");

    if (textAsset == null)
    {
        Debug.LogError("UpgradeData.csv를 찾을 수 없습니다.");
        yield break;
    }

    upgradeDataText = textAsset.text;

    // 이미지 데이터 불러오기
    ImageInfo[] upgradeImages = Resources.LoadAll<ImageInfo>("Image");

    foreach (ImageInfo imageInfo in upgradeImages)
    {
        if (!upgradeImagesDic.ContainsKey(imageInfo.id))
        {
            upgradeImagesDic.Add(imageInfo.id, imageInfo.image);
        }
    }

    // 줄 단위로 분리
    string[] lines = upgradeDataText.Split(
        new[] { '\r', '\n' },
        System.StringSplitOptions.RemoveEmptyEntries
    );

    for (int i = 1; i < lines.Length; i++)
{
    if (string.IsNullOrWhiteSpace(lines[i]))
        continue;

    string[] upgradeData = ParseCsvLine(lines[i]);

    if (upgradeData.Length < 12)
    {
        Debug.LogError(
            $"CSV 데이터 부족\n" +
            $"Line: {i + 1}\n" +
            $"내용: {lines[i]}"
        );
        continue;
    }

    Debug.Log($"CSV Line {i + 1} ID = [{upgradeData[0]}]");

    if (!int.TryParse(upgradeData[0].Trim(), out int id))
    {
        Debug.LogError(
            $"ID 파싱 실패!\n" +
            $"Line: {i + 1}\n" +
            $"ID 값: [{upgradeData[0]}]\n" +
            $"전체 줄: {lines[i]}"
        );

        continue;
    }

    UpgradeData upgradeDataTemp = new UpgradeData();

    // ID
    upgradeDataTemp.id = id;

    // 이름
    upgradeDataTemp.name =
        upgradeData[1].Trim();

    // 타입
    upgradeDataTemp.type =
        upgradeData[2].Trim();

    // 함수
    upgradeDataTemp.function =
        upgradeData[3].Trim();

    // CoolTime
    upgradeDataTemp.coolTime =
        upgradeData[4]
            .Split(',')
            .Select(x => float.Parse(x.Trim()))
            .ToArray();

    // Count
    upgradeDataTemp.count =
        upgradeData[5]
            .Split(',')
            .Select(x => int.Parse(x.Trim()))
            .ToArray();

    // Detection Range
    upgradeDataTemp.detectionRange =
        upgradeData[6]
            .Split(',')
            .Select(x => float.Parse(x.Trim()))
            .ToArray();

    // Damage
    upgradeDataTemp.damage =
        upgradeData[7]
            .Split(',')
            .Select(x => int.Parse(x.Trim()))
            .ToArray();

    // Speed
    upgradeDataTemp.speed =
        float.Parse(upgradeData[8].Trim());

    // Dot Damage
    upgradeDataTemp.dotDamage =
        upgradeData[9]
            .Split(',')
            .Select(x => int.Parse(x.Trim()))
            .ToArray();

    // Dot Duration
    upgradeDataTemp.dotCount =
        upgradeData[10]
            .Split(',')
            .Select(x => int.Parse(x.Trim()))
            .ToArray();

    upgradeDataTemp.hp =
        int.Parse(upgradeData[11].Trim());
    
    upgradeDataTemp.slow =
        upgradeData[12]
            .Split(',')
            .Select(x => float.Parse(x.Trim()))
            .ToArray();
    
    upgradeDataTemp.lifetime =         
        int.Parse(upgradeData[13].Trim());
    
    upgradeDataTemp.debuffChance =
        upgradeData[14]
            .Split(',')
            .Select(x => int.Parse(x.Trim()))
            .ToArray();
    
    upgradeDataTemp.orbitTime =
        upgradeData[15]
            .Split(',')
            .Select(x => int.Parse(x.Trim()))
            .ToArray();

    
    
    // 이미지
    if (upgradeImagesDic.TryGetValue(
        upgradeDataTemp.id,
        out Image sprite))
    {
        upgradeDataTemp.sprite = sprite;
    }

    UpgradeManager.Instance.upgrades.Add(
        upgradeDataTemp.id
    );

    UpgradeDatasDic.Add(
        upgradeDataTemp.id,
        upgradeDataTemp
    );
}

    // 업그레이드 함수 연결
    UpgradeManager.Instance.UpgradesInit();
}


/// <summary>
/// CSV 한 줄을 파싱한다.
/// "5, 4.5, 4, 3.5, 3"처럼
/// 따옴표 안에 있는 쉼표는 무시한다.
/// </summary>
private string[] ParseCsvLine(string line)
{
    List<string> result = new List<string>();

    bool insideQuotes = false;
    string current = "";

    for (int i = 0; i < line.Length; i++)
    {
        char c = line[i];

        // 따옴표
        if (c == '"')
        {
            // CSV에서 ""는 따옴표 문자 하나
            if (insideQuotes &&
                i + 1 < line.Length &&
                line[i + 1] == '"')
            {
                current += '"';
                i++;
            }
            else
            {
                insideQuotes = !insideQuotes;
            }

            continue;
        }

        // 따옴표 밖의 쉼표 = 컬럼 구분
        if (c == ',' && !insideQuotes)
        {
            result.Add(current);
            current = "";
            continue;
        }

        current += c;
    }

    // 마지막 데이터
    result.Add(current);

    return result.ToArray();
}
}

public class StageInfo
{
    public List<int> enemyId = new List<int>();
    public List<int> enemySpawnCount = new List<int>();
    public List<float> spawnInterval = new List<float>();
    public List<int> enemysPerSpawn = new List<int>();
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

[System.Serializable]
public class UpgradeData
{
    public int id;
    public string name;
    public string type;
    public string function;
    public float[] coolTime;
    public int[] count;
    public float[] detectionRange;
    public float speed;
    public int[] damage;
    public int[] dotDamage;
    public int[] dotCount;
    public int hp;
    public float[] slow;
    public float lifetime;
    public int[] debuffChance;
    public int[] orbitTime;
    public Image sprite;
    public int upgradeLevel;
    public string description;
    
}
