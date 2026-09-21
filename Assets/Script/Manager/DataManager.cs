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

    public StageInfo[] stageInfos;
    public Dictionary<int, PlayerData> playerDatasDic = new();
    public Dictionary<int, EnemyData> enemyDatasDic = new();
    public Dictionary<int, SkillData> skillDatasDic = new();
    public int[] levelDatas;
    public Dictionary<int, ExpData> expDatasDic = new();
    public Dictionary<int, UpgradeData> upgradeDatasDic = new();
    public Dictionary<int, StatUpgradeData> statUpgradeDatasDic = new();
    public Dictionary <int, BulletData> bulletDatasDic = new();
    public Dictionary <int, BossPatternData> bossPatternDatasDic = new();
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
    }
    

    public void GetStageDatas()
{
    TextAsset textAsset =
        Resources.Load<TextAsset>("TextData/StageData");

    if (textAsset == null)
    {
        UnityEngine.Debug.LogWarning(
            "StageData를 찾을 수 없습니다."
        );

        return;
    }

    stageDataText = textAsset.text;

    string[] lines =
        stageDataText.Split(
            new[] { '\r', '\n' },
            System.StringSplitOptions.RemoveEmptyEntries
        );


    int maxStage = 0;


    // 최대 스테이지 번호 찾기
    for (int i = 1; i < lines.Length; i++)
    {
        string[] stageData =
            lines[i].Split(',');

        if (stageData.Length < 8)
            continue;

        if (int.TryParse(
                stageData[0].Trim(),
                out int stage))
        {
            maxStage =
                Mathf.Max(
                    maxStage,
                    stage
                );
        }
    }


    // StageInfo 배열 생성
    stageInfos =
        new StageInfo[maxStage + 1];


    for (int i = 0;
         i < stageInfos.Length;
         i++)
    {
        stageInfos[i] =
            new StageInfo();
    }


    // 실제 데이터 저장
    for (int i = 1;
         i < lines.Length;
         i++)
    {
        string[] stageData =
            lines[i].Split(',');

        if (stageData.Length < 8)
            continue;


        int stage =
            int.Parse(
                stageData[0].Trim()
            );

        int enemyId =
            int.Parse(
                stageData[1].Trim()
            );

        int waveCount =
            int.Parse(
                stageData[2].Trim()
            );

        float spawnInterval =
            float.Parse(
                stageData[3].Trim()
            );

        int enemyPerSpawn =
            int.Parse(
                stageData[4].Trim()
            );

        float enemySpawnMultiplier =
            float.Parse(
                stageData[5].Trim()
            );

        float waveIncreaseInterval =
            float.Parse(
                stageData[6].Trim()
            );

        string type =
            stageData[7].Trim();


        // ★ 핵심
        // i가 아니라 stage를 사용해야 함
        stageInfos[stage].enemyId.Add(
            enemyId
        );

        stageInfos[stage].waveCount.Add(
            waveCount
        );

        stageInfos[stage].spawnInterval.Add(
            spawnInterval
        );

        stageInfos[stage].enemyPerSpawn.Add(
            enemyPerSpawn
        );

        stageInfos[stage].enemySpawnMultiplier.Add(
            enemySpawnMultiplier
        );

        stageInfos[stage].waveIncreaseInterval.Add(
            waveIncreaseInterval
        );

        stageInfos[stage].type.Add(
            type
        );
    }


    // 확인용 로그
    for (int stage = 0;
         stage < stageInfos.Length;
         stage++)
    {
        if (stageInfos[stage] == null)
            continue;

        UnityEngine.Debug.Log(
            $"[StageData] Stage {stage} / " +
            $"Enemy Count = {stageInfos[stage].enemyId.Count}"
        );

        for (int i = 0;
             i < stageInfos[stage].enemyId.Count;
             i++)
        {
            UnityEngine.Debug.Log(
                $"[StageData] " +
                $"Stage={stage} / " +
                $"Index={i} / " +
                $"EnemyId={stageInfos[stage].enemyId[i]} / " +
                $"Wave={stageInfos[stage].waveCount[i]} / " +
                $"Spawn={stageInfos[stage].enemyPerSpawn[i]}"
            );
        }
    }
}
    
    public void GetPlayerDatas()
{
    TextAsset textAsset =
        Resources.Load<TextAsset>("TextData/PlayerData");

    playerAndEnemyDataText = textAsset.text;

    string[] lines = playerAndEnemyDataText.Split('\n');

    for (int i = 1; i < lines.Length; i++)
    {
        if (string.IsNullOrWhiteSpace(lines[i]))
            continue;

        string[] data = lines[i].Split(",");

        PlayerData playerData = new PlayerData();

        playerData.id = int.Parse(data[0]);
        playerData.name = data[1];
        playerData.hp = int.Parse(data[2]);
        playerData.hpRegenRate = int.Parse(data[3]);
        playerData.speed = float.Parse(data[4]);
        playerData.damage = int.Parse(data[5]);
        playerData.defense = int.Parse(data[6]);
        playerData.criticalDamage = float.Parse(data[7]);
        playerData.criticalChance = int.Parse(data[8]);

        playerDatasDic.Add(playerData.id, playerData);
    }

    GameObject[] agentPrefabs =
        Resources.LoadAll<GameObject>("Prefabs/Agent");

    foreach (GameObject prefab in agentPrefabs)
    {
        PlayerInfo playerInfo = prefab.GetComponent<PlayerInfo>();

        if (playerInfo == null)
            continue;

        if (playerDatasDic.TryGetValue(
                playerInfo.id,
                out PlayerData data))
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
            Debug.LogWarning(
                $"ID {playerInfo.id}에 해당하는 플레이어 데이터를 찾을 수 없습니다."
            );
        }
    }

    Debug.Log("<color=green>Player Data Download Complete</color>");
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
    
    public void GetStatUpgradeData()
    {
        StartCoroutine(GetStatUpgradeDatasCor());
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

    // StatUpgradeData CSV 불러오기
    TextAsset textAsset =
        Resources.Load<TextAsset>("TextData/StatUpgradeData");

    if (textAsset == null)
    {
        UnityEngine.Debug.LogError(
            "StatUpgradeData.csv를 찾을 수 없습니다."
        );

        yield break;
    }

    statUpgradeDataText =
        textAsset.text;

    // 줄 단위로 분리
    string[] lines =
        statUpgradeDataText.Split(
            new[] { '\r', '\n' },
            System.StringSplitOptions.RemoveEmptyEntries
        );

    for (int i = 1; i < lines.Length; i++)
    {
        if (string.IsNullOrWhiteSpace(lines[i]))
            continue;

        string[] statData =
            ParseCsvLine(lines[i]);

        // Id ~ Defense = 11개
        if (statData.Length < 11)
        {
            UnityEngine.Debug.LogError(
                $"StatUpgradeData CSV 데이터 부족\n" +
                $"Line: {i + 1}\n" +
                $"내용: {lines[i]}"
            );

            continue;
        }

        UnityEngine.Debug.Log(
            $"StatUpgradeData CSV Line {i + 1} ID = [{statData[0]}]"
        );

        // ID
        if (!int.TryParse(
                statData[0].Trim(),
                out int id))
        {
            UnityEngine.Debug.LogError(
                $"StatUpgradeData ID 파싱 실패!\n" +
                $"Line: {i + 1}\n" +
                $"ID 값: [{statData[0]}]\n" +
                $"전체 줄: {lines[i]}"
            );

            continue;
        }

        StatUpgradeData statUpgradeDataTemp =
            new StatUpgradeData();

        // ID
        statUpgradeDataTemp.id =
            id;

        // 타입
        statUpgradeDataTemp.type =
            statData[2].Trim();

        // 함수
        statUpgradeDataTemp.function =
            statData[3].Trim();

        // 이름
        // Localization CSV의 key 사용
        statUpgradeDataTemp.name =
            "upgread_name_" +
            statUpgradeDataTemp.function;

        // 설명
        // Localization CSV의 key 사용
        statUpgradeDataTemp.description =
            "upgread_des_" +
            statUpgradeDataTemp.function;

        // Critical Chance
        statUpgradeDataTemp.critChance =
            string.IsNullOrWhiteSpace(statData[4])
                ? Array.Empty<float>()
                : statData[4]
                    .Split(',')
                    .Select(x => float.Parse(x.Trim()))
                    .ToArray();

        // Critical Damage
        statUpgradeDataTemp.critDamage =
            string.IsNullOrWhiteSpace(statData[5])
                ? Array.Empty<float>()
                : statData[5]
                    .Split(',')
                    .Select(x => float.Parse(x.Trim()))
                    .ToArray();

        // Attack Power
        statUpgradeDataTemp.attackPower =
            string.IsNullOrWhiteSpace(statData[6])
                ? Array.Empty<float>()
                : statData[6]
                    .Split(',')
                    .Select(x => float.Parse(x.Trim()))
                    .ToArray();

        // Max HP
        statUpgradeDataTemp.maxHp =
            string.IsNullOrWhiteSpace(statData[7])
                ? Array.Empty<float>()
                : statData[7]
                    .Split(',')
                    .Select(x => float.Parse(x.Trim()))
                    .ToArray();

        // Skill Cooldown
        statUpgradeDataTemp.skillCooldown =
            string.IsNullOrWhiteSpace(statData[8])
                ? Array.Empty<float>()
                : statData[8]
                    .Split(',')
                    .Select(x => float.Parse(x.Trim()))
                    .ToArray();

        // Passive Cooldown
        statUpgradeDataTemp.passiveCooldown =
            string.IsNullOrWhiteSpace(statData[9])
                ? Array.Empty<float>()
                : statData[9]
                    .Split(',')
                    .Select(x => float.Parse(x.Trim()))
                    .ToArray();

        // Defense
        statUpgradeDataTemp.defense =
            string.IsNullOrWhiteSpace(statData[10])
                ? Array.Empty<float>()
                : statData[10]
                    .Split(',')
                    .Select(x => float.Parse(x.Trim()))
                    .ToArray();

        // Tier
        // 현재 CSV에는 Tier 컬럼이 없으므로 기본값 0
        statUpgradeDataTemp.tier = 0;

        // 이미지
        if (upgradeImagesDic.TryGetValue(
                statUpgradeDataTemp.id,
                out Sprite sprite))
        {
            statUpgradeDataTemp.sprite =
                sprite;
        }

        // Dictionary 등록
        statUpgradeDatasDic.Add(
            statUpgradeDataTemp.id,
            statUpgradeDataTemp
        );

        // Tier가 0 이하이면 업그레이드 목록에 추가
        if (statUpgradeDataTemp.tier <= 0)
        {
            UpgradeManager.Instance.upgrades.Add(
                statUpgradeDataTemp.id
            );
        }

        UpgradeManager.Instance.UpgradesInit();
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
    public List<int> waveCount = new List<int>();
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