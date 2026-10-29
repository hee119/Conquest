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
    public string bulletDataText;
    public string bossPatternData;
    public string statUpgradeDataText;
    public string characterSkillUpgradeDataText;

    public StageInfo[] stageInfos;
    public Dictionary<int, PlayerData> playerDatasDic = new();
    public Dictionary<int, EnemyData> enemyDatasDic = new();
    public Dictionary<int, SkillData> skillDatasDic = new();
    public int[] levelDatas;
    public Dictionary<int, ExpData> expDatasDic = new();
    public Dictionary<int, UpgradeData> upgradeDatasDic = new();
    public Dictionary<int, StatUpgradeData> statUpgradeDatasDic = new();
    public Dictionary<int, CharacterSkillUpgradeData> characterSkillUpgradeDatasDic = new();
    public Dictionary <int, BulletData> bulletDatasDic = new();
    public Dictionary <int, BossPatternData> bossPatternDatasDic = new();
    public Dictionary<int, PickupSpawnData> pickupSpawnDatas = new();
    public Dictionary<int, SortedDictionary<int, int>> waveGoldDatas = new();
    public Dictionary <int, Sprite> upgradeImagesDic = new();
    public Dictionary <int, Sprite> skillUpgradeIconDic = new();
    public Dictionary <int, Sprite> statUpgradeIconDic = new();
    public ImageInfo[] upgradeImages;
    public ImageInfo[] skillUpgradeIcons;
    public ImageInfo[] statUpgradeIcons;

    
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
            return;
        }

        AllDataDownload();
    }
    
    [ContextMenu("All Data Download")]
    public void AllDataDownload()
    {
        GetStageDatas();
        GetPlayerDatas();
        GetEnemyDatas();
        GetSkillDatas();
        GetLevelDatas();
        GetExpDatas();
        GetUpgradeDatas();
        GetBulletData();
        GetBossPatternData();
        GetStatUpgradeData();
        GetCharacterSkillUpgradeData();
        GetPickupSpawnDatas();
    }

    public void GetWaveGoldDatas()
    {
        waveGoldDatas.Clear();
        if (stageInfos == null) return;
        for (int stage = 0; stage < stageInfos.Length; stage++)
        {
            var info = stageInfos[stage];
            if (info == null) continue;
            int waveOffset = 0;
            var rewards = new SortedDictionary<int, int>();
            for (int row = 0; row < info.waveCount.Count; row++)
            {
                if (row < info.waveCumulativeGold.Count)
                {
                    var values = info.waveCumulativeGold[row];
                    for (int wave = 0; wave < values.Length && wave < info.waveCount[row]; wave++)
                        rewards[waveOffset + wave + 1] = values[wave];
                }
                waveOffset += info.waveCount[row];
            }
            if (rewards.Count > 0) waveGoldDatas[stage] = rewards;
        }
    }

    public bool TryGetWaveGold(int stage, int reachedWave, out int gold)
    {
        gold = 0;
        if (!waveGoldDatas.TryGetValue(stage, out var rewards) || rewards.Count == 0)
            return false;
        foreach (var pair in rewards)
        {
            if (pair.Key > reachedWave) break;
            // A missing wave keeps the last reward; cumulative rewards never shrink.
            gold = Mathf.Max(gold, pair.Value);
        }
        return true;
    }

    public void GetPickupSpawnDatas()
    {
        pickupSpawnDatas.Clear();
        TextAsset textAsset = Resources.Load<TextAsset>("TextData/HealthPickupData");
        if (textAsset == null)
        {
            Debug.LogWarning("TextData/HealthPickupData를 찾을 수 없습니다.");
            return;
        }

        string[] lines = textAsset.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] columns = line.Split(',');
            if (columns.Length < 4 ||
                !int.TryParse(columns[0].Trim(), out int stage) ||
                !float.TryParse(columns[1].Trim(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float healthInterval) ||
                !int.TryParse(columns[2].Trim(), out int healAmount) ||
                !float.TryParse(columns[3].Trim(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float magnetInterval) ||
                stage < 1 || healthInterval <= 0f || healAmount <= 0 || magnetInterval <= 0f)
            {
                Debug.LogWarning($"HealthPickupData 행을 건너뜁니다: {i + 1} ({line})");
                continue;
            }

            pickupSpawnDatas[stage] = new PickupSpawnData
            {
                healthSpawnIntervalSeconds = healthInterval,
                healAmount = healAmount,
                magnetSpawnIntervalSeconds = magnetInterval
            };
        }
    }
    

    public void GetStageDatas()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("TextData/StageData");
        if (textAsset == null)
        {
            Debug.LogWarning("StageData를 찾을 수 없습니다.");
            return;
        }

        stageDataText = textAsset.text;
        stageInfos = ParseStageData(stageDataText);
        GetWaveGoldDatas();
    }

    private static StageInfo[] ParseStageData(string csv)
    {
        // Keep physical line numbers so a malformed row can be located in the CSV.
        string[] lines = csv.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var stages = new Dictionary<int, StageInfo>();
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        const System.Globalization.NumberStyles integer = System.Globalization.NumberStyles.Integer;
        int maxStage = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] row = ParseCsvLine(lines[i]);
            if (row.Length < 8 ||
                !int.TryParse(row[0], integer, culture, out int stage) || stage < 0 ||
                !int.TryParse(row[1], integer, culture, out int enemyId) ||
                !int.TryParse(row[2], integer, culture, out int waveCount) ||
                !TryParseStageFloat(row[3], out float spawnInterval) ||
                !int.TryParse(row[4], integer, culture, out int enemyPerSpawn) ||
                !TryParseStageFloat(row[5], out float enemySpawnMultiplier) ||
                !TryParseStageFloat(row[6], out float waveIncreaseInterval) ||
                string.IsNullOrWhiteSpace(row[7]))
            {
                Debug.LogWarning($"[StageData] {i + 1}행의 필수 값이 비어 있거나 잘못되어 해당 행을 건너뜁니다: {lines[i]}");
                continue;
            }

            if (!stages.TryGetValue(stage, out StageInfo info))
            {
                info = new StageInfo();
                stages.Add(stage, info);
            }

            maxStage = Mathf.Max(maxStage, stage);
            info.enemyId.Add(enemyId);
            info.waveCount.Add(waveCount);
            info.spawnInterval.Add(spawnInterval);
            info.enemyPerSpawn.Add(enemyPerSpawn);
            info.enemySpawnMultiplier.Add(enemySpawnMultiplier);
            info.waveIncreaseInterval.Add(waveIncreaseInterval);
            info.type.Add(row[7].Trim());
            var goldValues = new List<int>();
            if (row.Length > 9 && !string.IsNullOrWhiteSpace(row[9]))
                foreach (string value in row[9].Split(','))
                {
                    if (!int.TryParse(value.Trim(), integer, culture, out int gold) || gold < 0)
                    {
                        Debug.LogWarning($"[StageData] {i + 1}행의 누적 골드 목록이 잘못되어 해당 행의 골드 설정을 건너뜁니다.");
                        goldValues.Clear();
                        break;
                    }
                    goldValues.Add(gold);
                }
            info.waveCumulativeGold.Add(goldValues.ToArray());
        }

        var result = new StageInfo[maxStage + 1];
        for (int stage = 0; stage < result.Length; stage++)
            result[stage] = stages.TryGetValue(stage, out StageInfo info) ? info : new StageInfo();
        return result;
    }

    private static bool TryParseStageFloat(string value, out float result)
    {
        return float.TryParse(value, System.Globalization.NumberStyles.Float,
                   System.Globalization.CultureInfo.InvariantCulture, out result) &&
               !float.IsNaN(result) && !float.IsInfinity(result);
    }

    public void GetPlayerDatas()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("TextData/PlayerData");
        if (textAsset == null)
        {
            Debug.LogError("TextData/PlayerData를 찾을 수 없습니다.");
            return;
        }

        playerAndEnemyDataText = textAsset.text;
        playerDatasDic.Clear();

        string[] lines = playerAndEnemyDataText.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] data = ParseCsvLine(line);
            if (data.Length < 9 ||
                !int.TryParse(data[0].Trim(), out int id) ||
                !int.TryParse(data[2].Split(',')[0].Trim(), out int hp) ||
                !int.TryParse(data[3].Split(',')[0].Trim(), out int hpRegenRate) ||
                !float.TryParse(data[4].Split(',')[0].Trim(), out float speed) ||
                !int.TryParse(data[5].Split(',')[0].Trim(), out int damage) ||
                !int.TryParse(data[6].Split(',')[0].Trim(), out int defense) ||
                !float.TryParse(data[7].Split(',')[0].Trim(), out float criticalDamage) ||
                !int.TryParse(data[8].Split(',')[0].Trim(), out int criticalChance))
            {
                Debug.LogWarning($"PlayerData 행을 건너뜁니다. line={i + 1}, value=\"{line}\"");
                continue;
            }

            PlayerData playerData = new PlayerData
            {
                id = id,
                name = data[1].Trim(),
                hp = hp,
                hpRegenRate = hpRegenRate,
                speed = speed,
                damage = damage,
                defense = defense,
                criticalDamage = criticalDamage,
                criticalChance = criticalChance
            };

            playerDatasDic[id] = playerData;
        }

        GameObject[] agentPrefabs = Resources.LoadAll<GameObject>("Prefabs/Agent");
        foreach (GameObject prefab in agentPrefabs)
        {
            PlayerInfo playerInfo = prefab.GetComponent<PlayerInfo>();
            if (playerInfo == null)
                continue;

            if (playerDatasDic.TryGetValue(playerInfo.id, out PlayerData data))
            {
                playerInfo.hp = data.hp;
                playerInfo.hpRegenRate = data.hpRegenRate;
                playerInfo.speed = data.speed;
                playerInfo.damage = data.damage;
                playerInfo.defense = data.defense;
                playerInfo.criticalDamage = data.criticalDamage;
                playerInfo.criticalChance = data.criticalChance;
            }
            else
            {
                Debug.LogWarning($"ID {playerInfo.id}에 해당하는 플레이어 데이터를 찾을 수 없습니다.");
            }
        }

        Debug.Log($"<color=green>Player Data Download Complete ({playerDatasDic.Count}개)</color>");
    }
    public void GetEnemyDatas()
{
    TextAsset textAsset =
        Resources.Load<TextAsset>("TextData/EnemyData");

    playerAndEnemyDataText = textAsset.text;

    string[] lines =
        playerAndEnemyDataText.Split('\n');

    for (int i = 1; i < lines.Length; i++)
    {
        if (string.IsNullOrWhiteSpace(lines[i]))
            continue;

        string[] data =
            lines[i].Split(",");

        EnemyData enemyData =
            new EnemyData();

        enemyData.id =
            int.Parse(data[0]);

        enemyData.name =
            data[1];

        enemyData.hp =
            int.Parse(data[2]);

        enemyData.hpRegenRate =
            int.Parse(data[3]);

        enemyData.speed =
            float.Parse(data[4]);

        enemyData.damage =
            int.Parse(data[5]);

        enemyData.defense =
            int.Parse(data[6]);

        enemyData.dropExpOrbId =
            int.Parse(data[7]);

        enemyData.explosionDamage =
            int.Parse(data[8]);

        enemyData.explosionRange =
            int.Parse(data[9]);

        enemyDatasDic.Add(
            enemyData.id,
            enemyData
        );
    }

    GameObject[] enemyPrefabs =
        Resources.LoadAll<GameObject>(
            "Prefabs/Enemy"
        );

    GameObject[] bossPrefabs =
        Resources.LoadAll<GameObject>(
            "Prefabs/Boss"
        );

    GameObject[] allEnemyPrefabs =
        enemyPrefabs
            .Concat(bossPrefabs)
            .ToArray();

    foreach (GameObject prefab in allEnemyPrefabs)
    {
        EnemyInfo enemyInfo =
            prefab.GetComponent<EnemyInfo>();

        EnemyCombat enemyCombat =
            prefab.GetComponent<EnemyCombat>();

        EnemyMove enemyMove =
            prefab.GetComponent<EnemyMove>();

        if (enemyInfo == null)
            continue;

        if (enemyDatasDic.TryGetValue(
                enemyInfo.id,
                out EnemyData data))
        {
            enemyInfo.hp =
                data.hp;

            enemyInfo.hpRegenRate =
                data.hpRegenRate;

            enemyInfo.speed =
                data.speed;

            enemyInfo.damage =
                data.damage;

            enemyInfo.defense =
                data.defense;

            enemyInfo.dropExpOrbId =
                data.dropExpOrbId;

            enemyInfo.explosionDamage =
                data.explosionDamage;

            enemyInfo.explosionRange =
                data.explosionRange;

            if (enemyCombat != null)
            {
                enemyCombat.maxHp =
                    (int)enemyInfo.hp;
            }

            if (enemyMove != null)
            {
                enemyMove.moveSpeed =
                    enemyInfo.speed;
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning(
                $"ID {enemyInfo.id}에 해당하는 에너미 데이터를 찾을 수 없습니다."
            );
        }
    }

    UnityEngine.Debug.Log(
        "<color=green>Enemy Data Download Complete</color>"
    );
}
    
    public void GetSkillDatas()
    {
        TextAsset skillAsset = Resources.Load<TextAsset>("TextData/SkillData");
        if (skillAsset == null)
        {
            Debug.LogError("TextData/SkillData를 찾을 수 없습니다.");
            return;
        }

        skillDataText = skillAsset.text;
        skillDatasDic.Clear();

        string[] lines = skillDataText.Split('\n');
        
        
        for (int i = 1; i < lines.Length; i++)
        {
            string[] skillData = lines[i].Split(",");
            if (skillData.Length < 8 || !int.TryParse(skillData[0].Trim(), out int skillId))
                continue;

            SkillData skillDataTemp = new SkillData();
            
            skillDataTemp.skillId = skillId;
            skillDataTemp.skillName = skillData[1].Trim();
            int.TryParse(skillData[2].Trim(), out skillDataTemp.skillHp);
            float.TryParse(skillData[3].Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out skillDataTemp.skillCoolTime);
            float.TryParse(skillData[4].Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out skillDataTemp.skillDamage);
            float.TryParse(skillData[5].Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out skillDataTemp.skillCritDamage);
            float.TryParse(skillData[6].Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out skillDataTemp.lifetime);
            float.TryParse(skillData[7].Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out skillDataTemp.movementLockTime);

            // Mage projectile settings are optional columns so existing skills remain compatible.
            if (skillData.Length > 8)
                float.TryParse(skillData[8].Trim(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out skillDataTemp.projectileSpeed);
            if (skillData.Length > 9)
                float.TryParse(skillData[9].Trim(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out skillDataTemp.projectileLifetime);
            if (skillData.Length > 10)
                int.TryParse(skillData[10].Trim(), out skillDataTemp.maxProjectiles);
            if (skillData.Length > 11)
                float.TryParse(skillData[11].Trim(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out skillDataTemp.spawnOffset);

            skillDatasDic[skillDataTemp.skillId] = skillDataTemp;
        }
        
        GameObject[] skillPrefabs =
            Resources.LoadAll<GameObject>("Prefabs/SkillEffect");
        GameObject mageProjectilePrefab =
            Resources.Load<GameObject>("Prefabs/SkillEffect/MagicBolt");

        if (mageProjectilePrefab != null)
            skillPrefabs = skillPrefabs.Append(mageProjectilePrefab).ToArray();

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
            skillInfo.skillDamage = (int)data.skillDamage;
            skillInfo.skillCritDamage = (int)data.skillCritDamage;
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
        TextAsset expAsset = Resources.Load<TextAsset>("TextData/ExpData");
        if (expAsset == null)
        {
            Debug.LogError("TextData/ExpData를 찾을 수 없습니다.");
            return;
        }

        expDataText = expAsset.text;
        expDatasDic.Clear();

        string[] lines = expDataText.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] expData = line.Split(',');
            if (expData.Length < 3 ||
                !int.TryParse(expData[0].Trim(), out int id) ||
                !int.TryParse(expData[2].Trim(), out int exp))
            {
                Debug.LogWarning($"ExpData 행을 건너뜁니다. line={i + 1}, value=\"{line}\"");
                continue;
            }

            ExpData expDataTemp = new ExpData
            {
                id = id,
                name = expData[1].Trim(),
                exp = exp
            };

            expDatasDic[id] = expDataTemp;
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
    
    public void GetStatUpgradeData()
    {
        StartCoroutine(GetStatUpgradeDatasCor());
    }

    public void GetCharacterSkillUpgradeData()
    {
        StartCoroutine(GetCharacterSkillUpgradeDatasCor());
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
    upgradeImages = Resources.LoadAll<ImageInfo>("UpgradeImageScrip");

    foreach (ImageInfo imageInfo in upgradeImages)
    {
        if (!upgradeImagesDic.ContainsKey(imageInfo.id))
        {
            upgradeImagesDic.Add(imageInfo.id, imageInfo.image);
        }
    }
    
    skillUpgradeIcons = Resources.LoadAll<ImageInfo>("SkillUpgradeIcon");

    foreach (ImageInfo imageInfo in skillUpgradeIcons)
    {
        if (!skillUpgradeIconDic.ContainsKey(imageInfo.id))
        {
            skillUpgradeIconDic.Add(imageInfo.id, imageInfo.image);
        }
    }
    
    statUpgradeIcons = Resources.LoadAll<ImageInfo>("StatUpgradeIcon");

    foreach (ImageInfo imageInfo in statUpgradeIcons)
    {
        if (!statUpgradeIconDic.ContainsKey(imageInfo.id))
        {
            statUpgradeIconDic.Add(imageInfo.id, imageInfo.image);
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
            .Select(x => float.Parse(x.Trim()))
            .ToArray();

    // Speed
    upgradeDataTemp.speed =
        float.Parse(upgradeData[8].Trim());

    // Dot Damage
    upgradeDataTemp.dotDamage =
        upgradeData[9]
            .Split(',')
            .Select(x => float.Parse(x.Trim()))
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
    
    upgradeDataTemp.description = upgradeData[16].Trim();
    
    upgradeDataTemp.combinationList =
        string.IsNullOrWhiteSpace(upgradeData[17])
            ? Array.Empty<int>()
            : upgradeData[17]
                .Split(',')
                .Select(x => int.Parse(x.Trim()))
                .ToArray();
    
    upgradeDataTemp.result =
        string.IsNullOrWhiteSpace(upgradeData[18])
            ? Array.Empty<int>()
            : upgradeData[18]
                .Split(',')
                .Select(x => int.Parse(x.Trim()))
                .ToArray();
    
    upgradeDataTemp.chainCount =
        string.IsNullOrWhiteSpace(upgradeData[19])
            ? 0
            : int.Parse(upgradeData[19].Trim()); 
    
    upgradeDataTemp.chainRange =
        string.IsNullOrWhiteSpace(upgradeData[20])
            ? 0
            : float.Parse(upgradeData[20].Trim()); 
    
    upgradeDataTemp.boomDamage = string.IsNullOrWhiteSpace(upgradeData[21])
        ? 0
        : int.Parse(upgradeData[21].Trim()); 
    
    upgradeDataTemp.explosionRange = string.IsNullOrWhiteSpace(upgradeData[22])
        ? 0
        : float.Parse(upgradeData[22].Trim()); 
    
    upgradeDataTemp.tier = string.IsNullOrWhiteSpace(upgradeData[23])
        ? 0
        : int.Parse(upgradeData[23].Trim()); 

    // 이미지
    if (upgradeImagesDic.TryGetValue(
        upgradeDataTemp.id,
        out Sprite sprite))
    {
        upgradeDataTemp.sprite = sprite;
    }
    if (upgradeDataTemp.tier <= 0)
    {
        UpgradeManager.Instance.upgrades.Add(
            upgradeDataTemp.id
        );
    }

    upgradeDatasDic.Add(
        upgradeDataTemp.id,
        upgradeDataTemp
    );
}

    // 업그레이드 함수 연결
    UpgradeManager.Instance.UpgradesInit();
}
    
    public IEnumerator GetStatUpgradeDatasCor()
    {
        yield return new WaitUntil(() => UpgradeManager.Instance != null);

        TextAsset textAsset = Resources.Load<TextAsset>("TextData/StatUpgradeData");
        if (textAsset == null)
        {
            Debug.LogError("StatUpgradeData.csv를 찾을 수 없습니다.");
            yield break;
        }

        statUpgradeDataText = textAsset.text;
        statUpgradeDatasDic.Clear();

        string[] lines = statUpgradeDataText.Split(
            new[] { '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries);
        string[] headers = ParseCsvLine(lines[0]);
        int tierColumn = FindColumnIndex(headers, "Tier");

        for (int i = 1; i < lines.Length; i++)
        {
            string[] statData = ParseCsvLine(lines[i]);
            if (statData.Length < 11 || !int.TryParse(statData[0].Trim(), out int id))
            {
                Debug.LogWarning($"StatUpgradeData 행을 건너뜁니다. line={i + 1}, value=\"{lines[i]}\"");
                continue;
            }

            StatUpgradeData data = new StatUpgradeData
            {
                id = id,
                name = !string.IsNullOrWhiteSpace(statData[1])
                    ? statData[1].Trim()
                    : "upgread_name_" + statData[3].Trim(),
                type = statData[2].Trim(),
                function = statData[3].Trim(),
                critChance = ParseOptionalFloatArray(statData, 4),
                critDamage = ParseOptionalFloatArray(statData, 5),
                attackPower = ParseOptionalFloatArray(statData, 6),
                maxHp = ParseOptionalFloatArray(statData, 7),
                skillCooldown = ParseOptionalFloatArray(statData, 8),
                passiveCooldown = ParseOptionalFloatArray(statData, 9),
                defense = ParseOptionalFloatArray(statData, 10),
                description = statData.Length > 11 && !string.IsNullOrWhiteSpace(statData[11])
                    ? statData[11].Trim()
                    : "upgread_des_" + statData[3].Trim(),
                tier = ParseOptionalInt(statData, tierColumn, 0)
            };

            if (upgradeImagesDic.TryGetValue(data.id, out Sprite sprite))
                data.sprite = sprite;

            statUpgradeDatasDic[data.id] = data;
            if (data.tier <= 0 && !UpgradeManager.Instance.upgrades.Contains(data.id))
                UpgradeManager.Instance.upgrades.Add(data.id);
        }

        UpgradeManager.Instance.UpgradesInit();
    }

    public IEnumerator GetCharacterSkillUpgradeDatasCor()
    {
        yield return new WaitUntil(() => UpgradeManager.Instance != null);

        TextAsset textAsset = Resources.Load<TextAsset>("TextData/CharacterSkillUpgradeData");
        if (textAsset == null)
        {
            Debug.LogError("CharacterSkillUpgradeData.csv를 찾을 수 없습니다.");
            yield break;
        }

        characterSkillUpgradeDataText = textAsset.text;
        characterSkillUpgradeDatasDic.Clear();

        if (skillUpgradeIconDic.Count == 0)
        {
            skillUpgradeIcons = Resources.LoadAll<ImageInfo>("SkillUpgradeIcon");
            foreach (ImageInfo imageInfo in skillUpgradeIcons)
            {
                if (!skillUpgradeIconDic.ContainsKey(imageInfo.id))
                    skillUpgradeIconDic.Add(imageInfo.id, imageInfo.image);
            }
        }

        string[] lines = characterSkillUpgradeDataText.Split(
            new[] { '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries);
        string[] headers = ParseCsvLine(lines[0]);
        int idColumn = FindColumnIndex(headers, "Id");
        int nameColumn = FindColumnIndex(headers, "NameId");
        int typeColumn = FindColumnIndex(headers, "Type");
        int functionColumn = FindColumnIndex(headers, "Function");
        int characterIdColumn = FindColumnIndex(headers, "CharacterId");
        int rangeColumn = FindColumnIndex(headers, "RangePercent");
        int countColumn = FindColumnIndex(headers, "AttackCount");
        int pierceColumn = FindColumnIndex(headers, "Pierce");
        int chainCountColumn = FindColumnIndex(headers, "ChainCount");
        int chainRangeColumn = FindColumnIndex(headers, "ChainRange");
        int damageColumn = FindColumnIndex(headers, "DamagePercent");
        int tierColumn = FindColumnIndex(headers, "Tier");
        int descriptionColumn = FindColumnIndex(headers, "DesIds", "DesId");

        for (int i = 1; i < lines.Length; i++)
        {
            string[] columns = ParseCsvLine(lines[i]);
            int id = ParseOptionalInt(columns, idColumn, -1);
            if (id < 0)
            {
                Debug.LogWarning($"CharacterSkillUpgradeData 행을 건너뜁니다. line={i + 1}, value=\"{lines[i]}\"");
                continue;
            }

            CharacterSkillUpgradeData data = new CharacterSkillUpgradeData
            {
                id = id,
                name = GetOptionalString(columns, nameColumn),
                type = GetOptionalString(columns, typeColumn),
                function = GetOptionalString(columns, functionColumn),
                characterId = ParseOptionalInt(columns, characterIdColumn, 0),
                rangePercent = ParseOptionalFloatArray(columns, rangeColumn),
                attackCount = ParseOptionalIntArray(columns, countColumn),
                pierce = ParseOptionalIntArray(columns, pierceColumn),
                chainCount = ParseOptionalIntArray(columns, chainCountColumn),
                chainRange = ParseOptionalFloatArray(columns, chainRangeColumn),
                damagePercent = ParseOptionalFloatArray(columns, damageColumn),
                tier = ParseOptionalInt(columns, tierColumn, 0),
                descriptions = ParseOptionalStringArray(columns, descriptionColumn)
            };

            if (upgradeImagesDic.TryGetValue(data.id, out Sprite upgradeSprite))
                data.sprite = upgradeSprite;
            else if (skillUpgradeIconDic.TryGetValue(data.id, out Sprite skillIcon))
                data.sprite = skillIcon;

            characterSkillUpgradeDatasDic[data.id] = data;
            if (data.tier <= 0 && !UpgradeManager.Instance.upgrades.Contains(data.id))
                UpgradeManager.Instance.upgrades.Add(data.id);
        }
    }
    public void GetBulletData()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("TextData/BulletData");
        bulletDataText = textAsset.text;

        string[] lines = textAsset.text.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] bulletData = ParseCsvLine(lines[i]);

            BulletData bulletDataTemp = new BulletData();

            bulletDataTemp.id = int.Parse(bulletData[0]);
            bulletDataTemp.damage = int.Parse(bulletData[1]);
            bulletDataTemp.range = int.Parse(bulletData[2]);
            bulletDataTemp.speed = int.Parse(bulletData[3]);

            bulletDatasDic.Add(bulletDataTemp.id, bulletDataTemp);
        }
        
        GameObject[] bulletPrefabs = Resources.LoadAll<GameObject>("Prefabs/Bullet");

        foreach (GameObject prefab in bulletPrefabs)
        {
            BulletInfo bulletInfo = prefab.GetComponent<BulletInfo>();
            if (bulletInfo != null)
            {
                bulletInfo.damage = bulletDatasDic[bulletInfo.id].damage;
                bulletInfo.range = bulletDatasDic[bulletInfo.id].range;
                bulletInfo.speed = bulletDatasDic[bulletInfo.id].speed;
            }
        }
    }
    
    public void GetBossPatternData()
    {
        bossPatternData = Resources.Load<TextAsset>("TextData/PatternData").text;
        
        string[] lines = bossPatternData.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            string[] bossPatternData = lines[i].Split(",");
            
            BossPatternData bossPatternDataTemp = new BossPatternData();
            
            bossPatternDataTemp.id = int.Parse(bossPatternData[0]);
            bossPatternDataTemp.name = bossPatternData[1];
            bossPatternDataTemp.damage = int.Parse(bossPatternData[2]);
            
            bossPatternDatasDic.Add(bossPatternDataTemp.id, bossPatternDataTemp);
        }
        
        GameObject[] bossPatternPrefab = Resources.LoadAll<GameObject>("Prefabs/BossPattern");

        foreach (GameObject prefab in bossPatternPrefab)
        {
            BossPatternInfo patternData = prefab.GetComponent<BossPatternInfo>();
            if (patternData != null)
            {
                patternData.name = bossPatternDatasDic[patternData.id].name;
                patternData.damage = bossPatternDatasDic[patternData.id].damage;
            }
        }
    }


/// <summary>
/// CSV 한 줄을 파싱한다.
/// "5, 4.5, 4, 3.5, 3"처럼
/// 따옴표 안에 있는 쉼표는 무시한다.
/// </summary>
private static int ParseOptionalInt(string[] columns, int index, int fallback)
{
    return index >= 0 && index < columns.Length &&
           int.TryParse(columns[index].Trim(), out int value)
        ? value
        : fallback;
}

private static string GetOptionalString(string[] columns, int index)
{
    return index >= 0 && index < columns.Length
        ? columns[index].Trim()
        : string.Empty;
}

private static int FindColumnIndex(string[] headers, params string[] acceptedNames)
{
    if (headers == null || acceptedNames == null)
        return -1;

    for (int i = 0; i < headers.Length; i++)
    {
        string header = headers[i].Trim();
        foreach (string acceptedName in acceptedNames)
        {
            if (string.Equals(
                    header,
                    acceptedName,
                    StringComparison.OrdinalIgnoreCase))
                return i;
        }
    }

    return -1;
}

private static int[] ParseOptionalIntArray(string[] columns, int index)
{
    if (index >= columns.Length || string.IsNullOrWhiteSpace(columns[index]))
        return Array.Empty<int>();

    return columns[index]
        .Split(',')
        .Select(value => int.TryParse(value.Trim(), out int parsed) ? parsed : 0)
        .ToArray();
}

private static float[] ParseOptionalFloatArray(string[] columns, int index)
{
    if (index >= columns.Length || string.IsNullOrWhiteSpace(columns[index]))
        return Array.Empty<float>();

    return columns[index]
        .Split(',')
        .Select(value => float.TryParse(
            value.Trim(),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float parsed) ? parsed : 0f)
        .ToArray();
}

private static string[] ParseOptionalStringArray(string[] columns, int index)
{
    if (index < 0 || index >= columns.Length || string.IsNullOrWhiteSpace(columns[index]))
        return Array.Empty<string>();

    return columns[index]
        .Split(',')
        .Select(value => value.Trim())
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .ToArray();
}

internal static string[] ParseCsvLine(string line)
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
    public List<int> waveCount = new List<int>();
    public List<int[]> waveCumulativeGold = new List<int[]>();
    public List<float> spawnInterval = new List<float>();
    public List<int> enemyPerSpawn = new List<int>();
    public List<float> enemySpawnMultiplier = new List<float>();
    public List<float> waveIncreaseInterval = new List<float>();
    public List<string> type = new List<string>();
}

[System.Serializable]
public class PlayerData
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

[System.Serializable]
public class EnemyData
{
    public int id;
    public string name;
    public float hp;
    public int hpRegenRate;
    public float speed;
    public int damage;
    public int defense;
    public int dropExpOrbId;
    public int explosionDamage;
    public int explosionRange;
}

public class SkillData
{
    public int skillId;
    public string skillName;
    public int skillHp;
    public float skillCoolTime;
    public float skillDamage;
    public float skillCritDamage;
    public float lifetime;
    public float movementLockTime;
    public float projectileSpeed;
    public float projectileLifetime;
    public int maxProjectiles;
    public float spawnOffset;
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
    public float[] damage;
    public float[] dotDamage;
    public int[] dotCount;
    public int hp;
    public float[] slow;
    public float lifetime;
    public int[] debuffChance;
    public int[] orbitTime;
    public string description;
    public int[] combinationList;
    public int[] result;
    public int chainCount;
    public float chainRange;
    public int boomDamage;
    public float explosionRange;
    public int tier;

    public Sprite sprite;
    public int upgradeLevel;
    public int UpgradeLevel
    {
        get
        {
            return upgradeLevel - 1;
        }
        set
        {
            upgradeLevel = value; 
        }
    }
}

[System.Serializable]
public class BulletData{
    public int id;
    public int damage;
    public int range;
    public int speed;
    public string name;
}

[System.Serializable]
public class BossPatternData
{
    public int id;
    public string name;
    public int damage;
}

[System.Serializable]
public class StatUpgradeData
{
    public int id;
    public string name;
    public string type;
    public string function;

    public float[] critChance;
    public float[] critDamage;
    public float[] attackPower;
    public float[] maxHp;
    public float[] skillCooldown;
    public float[] passiveCooldown;
    public float[] defense;

    public int tier;
    public Sprite sprite;
    public int upgradeLevel;
    public string description;

    public int UpgradeLevel
    {
        get
        {
            return upgradeLevel - 1;
        }
        set
        {
            upgradeLevel = value;
        }
    }
}

[System.Serializable]
public class CharacterSkillUpgradeData
{
    public int id;
    public string name;
    public string type;
    public string function;
    public int characterId;
    public float[] rangePercent = Array.Empty<float>();
    public int[] attackCount = Array.Empty<int>();
    public int[] pierce = Array.Empty<int>();
    public int[] chainCount = Array.Empty<int>();
    public float[] chainRange = Array.Empty<float>();
    public float[] damagePercent = Array.Empty<float>();
    public int tier;
    public string[] descriptions = Array.Empty<string>();
    public Sprite sprite;
    public int upgradeLevel;

    public int UpgradeLevel
    {
        get { return upgradeLevel - 1; }
        set { upgradeLevel = value; }
    }
}
