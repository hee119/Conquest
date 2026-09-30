using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CodexPlaytestAgent : MonoBehaviour
{
    private const string MarkerFile = "Temp/CodexPlaytestAgent.enabled";
    private const float MaxRunSeconds = 420f;
    private const float MemorySampleInterval = 0.75f;
    private const int MaxStoredMemorySamples = 900;
    private const int MaxStoredEpisodes = 40;
    private const string MemoryFileName = "CodexPlaytestSurvivalMemory.json";

    private readonly int[] upgradePriority =
    {
        400, 405, 406, 401, 403, 404, 407, 402,
        700, 702, 704, 703, 701, 705, 706
    };

    private GameObject player;
    private Rigidbody2D playerRb;
    private PlayerMove playerMove;
    private PlayerCombat playerCombat;
    private SkillSpawner skillSpawner;
    private Tilemap boundsTilemap;
    private float startedAt;
    private float lastAttackAt;
    private int lastMinuteCount;
    private bool ending;
    private bool selectingUpgrade;
    private bool memorySaved;
    private float lastSampleAt;
    private Vector2 lastMoveDirection;
    private SurvivalMemory memory = new SurvivalMemory();
    private SurvivalEpisode currentEpisode;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // This run uses an explicit scene object so only one agent controls the player.
    }

    private void Start()
    {
        startedAt = Time.realtimeSinceStartup;
        memory = LoadMemory();
        currentEpisode = new SurvivalEpisode
        {
            startedAt = DateTime.UtcNow.ToString("O")
        };

        Debug.Log("[CodexPlaytest] START 자동 플레이 측정 시작");
        Debug.Log($"[CodexPlaytest] MEMORY runs={memory.totalRuns}, best={memory.bestSurvivalSeconds:0.0}s");
    }

    private void Update()
    {
        if (ending)
            return;

        if (!TryBindPlayer())
            return;

        AutoAttackNearestEnemy();
        AutoSelectUpgradeIfPaused();
        AutoCombineIfPossible();
        LogNewMinuteResults();
        RecordSurvivalSample();

        if (playerCombat != null && playerCombat.isDead)
        {
            Finish("player died");
            return;
        }

        if (Time.realtimeSinceStartup - startedAt >= MaxRunSeconds)
        {
            Finish("time limit reached");
        }
    }

    private void FixedUpdate()
    {
        if (ending || Time.timeScale <= 0f || playerRb == null || player == null || !player.activeInHierarchy)
            return;

        Vector2 move = ChooseMoveDirection();
        lastMoveDirection = move;
        float speed = playerMove != null ? Mathf.Max(playerMove.speed, 5f) : 5f;

        playerRb.linearVelocity = move * speed;
    }

    private bool TryBindPlayer()
    {
        if (player != null && player.activeInHierarchy)
            return true;

        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || !player.activeInHierarchy)
            return false;

        playerRb = player.GetComponent<Rigidbody2D>();
        playerMove = player.GetComponent<PlayerMove>();
        playerCombat = player.GetComponent<PlayerCombat>();
        skillSpawner = player.GetComponent<SkillSpawner>();
        boundsTilemap = StageManager.Instance != null ? StageManager.Instance.backgroundTilemap : boundsTilemap;
        EnsurePlayerStats();
        return playerRb != null;
    }

    private void EnsurePlayerStats()
    {
        if (DataManager.Instance == null || CharacterManager.Instance == null || player == null)
            return;

        if (!DataManager.Instance.playerDatasDic.TryGetValue(CharacterManager.Instance.currentCharacter, out PlayerData data))
            return;

        PlayerInfo info = player.GetComponent<PlayerInfo>();
        if (info != null && info.hp <= 0)
        {
            info.hp = data.hp;
            info.hpRegenRate = data.hpRegenRate;
            info.speed = data.speed;
            info.damage = data.damage;
            info.defense = data.defense;
            info.criticalDamage = data.criticalDamage;
            info.criticalChance = data.criticalChance;
        }

        if (playerMove != null && playerMove.speed <= 0f)
            playerMove.speed = Mathf.Max(3f, data.speed);

        if (playerCombat != null && playerCombat.maxHp <= 0f)
        {
            playerCombat.maxHp = data.hp;
            playerCombat.hp = data.hp;
            playerCombat.isDead = false;
            player.SetActive(true);
            playerCombat.UpdateHpUI();
        }
    }

    private Vector2 ChooseMoveDirection()
    {
        Vector2 position = player.transform.position;
        Vector2 away = Vector2.zero;
        Vector2 towardExp = Vector2.zero;
        Vector2 towardCenter = Vector2.zero;
        float nearestEnemySqr = float.MaxValue;
        float nearestExpSqr = float.MaxValue;

        foreach (EnemyCombat enemy in FindObjectsByType<EnemyCombat>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (enemy == null || enemy.isDead || !enemy.gameObject.activeInHierarchy)
                continue;

            Vector2 delta = position - (Vector2)enemy.transform.position;
            float sqr = delta.sqrMagnitude;
            nearestEnemySqr = Mathf.Min(nearestEnemySqr, sqr);

            if (sqr < 100f && sqr > 0.01f)
                away += delta.normalized * (100f / Mathf.Max(1f, sqr));
        }

        foreach (Exp exp in FindObjectsByType<Exp>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (exp == null || !exp.gameObject.activeInHierarchy)
                continue;

            Vector2 delta = (Vector2)exp.transform.position - position;
            float sqr = delta.sqrMagnitude;
            if (sqr < nearestExpSqr)
            {
                nearestExpSqr = sqr;
                towardExp = delta.sqrMagnitude > 0.01f ? delta.normalized : Vector2.zero;
            }
        }

        SurvivalState state = BuildSurvivalState(
            position,
            nearestEnemySqr,
            nearestExpSqr,
            out towardCenter
        );

        Vector2 orbit = new Vector2(Mathf.Sin(Time.time * 0.9f), Mathf.Cos(Time.time * 0.7f));
        Vector2 learned = GetLearnedDirection(state);
        float lowHp = 1f - state.hpRatio;
        float enemyWeight = memory.weights.enemyAvoidWeight * Mathf.Lerp(1f, 1.65f, lowHp);
        float expWeight = memory.weights.expSeekWeight * Mathf.Lerp(1f, 0.35f, state.enemyThreat);
        float centerWeight = memory.weights.edgeAvoidWeight * state.edgeDanger;

        Vector2 desired =
            orbit * memory.weights.exploreWeight +
            away.normalized * enemyWeight * (0.35f + state.enemyThreat * 1.8f) +
            towardExp * expWeight * state.expDesire +
            towardCenter * centerWeight +
            learned * memory.weights.learnedDirectionWeight;

        desired = KeepInsideTilemap(position, desired);

        if (desired.sqrMagnitude < 0.01f)
            desired = orbit;

        return desired.normalized;
    }

    private SurvivalState BuildSurvivalState(
        Vector2 position,
        float nearestEnemySqr,
        float nearestExpSqr,
        out Vector2 towardCenter
    )
    {
        SurvivalState state = new SurvivalState();
        float nearestEnemyDistance = Mathf.Sqrt(nearestEnemySqr);
        float nearestExpDistance = Mathf.Sqrt(nearestExpSqr);

        state.elapsedSeconds = Time.realtimeSinceStartup - startedAt;
        state.hpRatio =
            playerCombat != null && playerCombat.maxHp > 0f
                ? Mathf.Clamp01(playerCombat.hp / playerCombat.maxHp)
                : 1f;
        state.nearestEnemyDistance =
            float.IsInfinity(nearestEnemyDistance) ? 99f : Mathf.Min(nearestEnemyDistance, 99f);
        state.nearestExpDistance =
            float.IsInfinity(nearestExpDistance) ? 99f : Mathf.Min(nearestExpDistance, 99f);
        state.enemyThreat = Mathf.Clamp01((10f - state.nearestEnemyDistance) / 10f);
        state.expDesire = Mathf.Clamp01((15f - state.nearestExpDistance) / 15f);
        state.level = LevelManager.Instance != null ? LevelManager.Instance.currentLevel : 0;
        state.currentExp = LevelManager.Instance != null ? LevelManager.Instance.currentExp : 0;

        towardCenter = Vector2.zero;
        state.edgeDanger = CalculateEdgeDanger(position, out towardCenter);

        return state;
    }

    private float CalculateEdgeDanger(Vector2 position, out Vector2 towardCenter)
    {
        towardCenter = Vector2.zero;

        if (boundsTilemap == null)
        {
            boundsTilemap = StageManager.Instance != null ? StageManager.Instance.backgroundTilemap : null;
        }

        if (boundsTilemap == null)
            return 0f;

        Bounds localBounds = boundsTilemap.localBounds;
        Vector3 min = boundsTilemap.transform.TransformPoint(localBounds.min);
        Vector3 max = boundsTilemap.transform.TransformPoint(localBounds.max);
        Vector2 center = new Vector2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);

        towardCenter = (center - position).sqrMagnitude > 0.01f
            ? (center - position).normalized
            : Vector2.zero;

        float distanceToEdge = Mathf.Min(
            position.x - min.x,
            max.x - position.x,
            position.y - min.y,
            max.y - position.y
        );

        return Mathf.Clamp01((4f - distanceToEdge) / 4f);
    }

    private Vector2 GetLearnedDirection(SurvivalState state)
    {
        if (memory.samples == null || memory.samples.Count == 0)
            return Vector2.zero;

        Vector2 direction = Vector2.zero;
        float totalWeight = 0f;
        int checkedSamples = 0;

        foreach (SurvivalSample sample in memory.samples)
        {
            if (checkedSamples >= 80)
                break;

            checkedSamples++;
            float similarity = CalculateStateSimilarity(state, sample);
            if (similarity <= 0.25f)
                continue;

            float quality = Mathf.Max(1f, sample.quality);
            float weight = similarity * quality;
            direction += new Vector2(sample.moveX, sample.moveY) * weight;
            totalWeight += weight;
        }

        if (totalWeight <= 0.01f || direction.sqrMagnitude <= 0.01f)
            return Vector2.zero;

        return direction.normalized;
    }

    private float CalculateStateSimilarity(SurvivalState state, SurvivalSample sample)
    {
        float hpDiff = Mathf.Abs(state.hpRatio - sample.hpRatio);
        float enemyDiff = Mathf.Abs(state.enemyThreat - sample.enemyThreat);
        float expDiff = Mathf.Abs(state.expDesire - sample.expDesire);
        float edgeDiff = Mathf.Abs(state.edgeDanger - sample.edgeDanger);
        float levelDiff = Mathf.Abs(Mathf.Clamp01(state.level / 20f) - Mathf.Clamp01(sample.level / 20f));

        float distance =
            hpDiff * 0.7f +
            enemyDiff * 1.25f +
            expDiff * 0.75f +
            edgeDiff * 0.9f +
            levelDiff * 0.35f;

        return Mathf.Clamp01(1f - distance / 2.2f);
    }

    private void AutoAttackNearestEnemy()
    {
        if (Time.timeScale <= 0f || player == null || skillSpawner == null)
            return;

        const int basicSkillId = 200;
        if (!DataManager.Instance.skillDatasDic.TryGetValue(basicSkillId, out SkillData skill))
            return;

        float cooldown = Mathf.Max(0.15f, skill.skillCoolTime);
        if (Time.time - lastAttackAt < cooldown)
            return;

        EnemyCombat nearest = null;
        float nearestSqr = float.MaxValue;
        Vector2 playerPos = player.transform.position;

        foreach (EnemyCombat enemy in FindObjectsByType<EnemyCombat>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (enemy == null || enemy.isDead || !enemy.gameObject.activeInHierarchy)
                continue;

            float sqr = ((Vector2)enemy.transform.position - playerPos).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = enemy;
            }
        }

        if (nearest != null)
        {
            float dir = Mathf.Sign(nearest.transform.position.x - player.transform.position.x);
            if (!Mathf.Approximately(dir, 0f))
            {
                Vector3 scale = player.transform.localScale;
                player.transform.localScale = new Vector3(dir * Mathf.Abs(scale.x), scale.y, scale.z);
            }
        }

        skillSpawner.SkillSpawn(basicSkillId);
        skillSpawner.SkillUse(basicSkillId);
        lastAttackAt = Time.time;
    }

    private Vector2 KeepInsideTilemap(Vector2 position, Vector2 desired)
    {
        if (boundsTilemap == null)
        {
            boundsTilemap = StageManager.Instance != null ? StageManager.Instance.backgroundTilemap : null;
        }

        if (boundsTilemap == null)
            return desired;

        Bounds localBounds = boundsTilemap.localBounds;
        Vector3 min = boundsTilemap.transform.TransformPoint(localBounds.min);
        Vector3 max = boundsTilemap.transform.TransformPoint(localBounds.max);
        const float padding = 1.25f;

        Vector2 clampedTarget = new Vector2(
            Mathf.Clamp(position.x + desired.x * 2f, min.x + padding, max.x - padding),
            Mathf.Clamp(position.y + desired.y * 2f, min.y + padding, max.y - padding));

        bool nearEdge =
            position.x < min.x + padding ||
            position.x > max.x - padding ||
            position.y < min.y + padding ||
            position.y > max.y - padding;

        if (nearEdge)
        {
            Vector2 center = new Vector2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);
            return (center - position).normalized;
        }

        return (clampedTarget - position).normalized;
    }

    private void AutoSelectUpgradeIfPaused()
    {
        if (Time.timeScale > 0f)
        {
            selectingUpgrade = false;
            return;
        }

        if (selectingUpgrade)
            return;

        SelectUpgrade best = FindObjectsByType<SelectUpgrade>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(x => x != null && x.gameObject.activeInHierarchy && x.upgradeUiInfo != null && x.upgradeUiInfo.upgradeData.id > 0)
            .OrderBy(x => ScoreUpgrade(x.upgradeUiInfo.upgradeData.id))
            .FirstOrDefault();

        if (best == null)
            return;

        UpgradeData selectedData = best.upgradeUiInfo.upgradeData;
        Debug.Log($"[CodexPlaytest] SELECT id={selectedData.id}");
        RecordUpgradeSelection(selectedData);
        selectingUpgrade = true;
        best.UpgradeIsSelected();
        StartCoroutine(WaitForUpgradeResume());
    }

    private IEnumerator WaitForUpgradeResume()
    {
        float deadline = Time.realtimeSinceStartup + 2f;
        yield return new WaitUntil(() => Time.timeScale > 0f || Time.realtimeSinceStartup >= deadline);
        selectingUpgrade = false;
        if (Time.timeScale <= 0f)
        {
            Time.timeScale = 1f;
        }
    }

    private int ScoreUpgrade(int id)
    {
        int index = System.Array.IndexOf(upgradePriority, id);
        int baseScore = index >= 0 ? index : 1000 + id;
        UpgradeMemory upgradeMemory = FindUpgradeMemory(id);
        if (upgradeMemory == null || upgradeMemory.count <= 0)
            return baseScore;

        int learnedBonus = Mathf.RoundToInt(Mathf.Clamp(upgradeMemory.averageSurvivalSeconds, 0f, 420f) * 0.75f);
        return baseScore - learnedBonus;
    }

    private void RecordUpgradeSelection(UpgradeData selectedData)
    {
        if (currentEpisode == null || selectedData == null)
            return;

        List<int> offeredIds = FindObjectsByType<SelectUpgrade>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(x => x != null && x.gameObject.activeInHierarchy && x.upgradeUiInfo != null && x.upgradeUiInfo.upgradeData.id > 0)
            .Select(x => x.upgradeUiInfo.upgradeData.id)
            .Distinct()
            .ToList();

        currentEpisode.selectedUpgrades.Add(selectedData.id);
        currentEpisode.upgradeEvents.Add(new UpgradeEvent
        {
            elapsedSeconds = Time.realtimeSinceStartup - startedAt,
            selectedId = selectedData.id,
            selectedType = selectedData.type,
            selectedLevelBefore = selectedData.upgradeLevel,
            hpRatio = GetHpRatio(),
            currentLevel = LevelManager.Instance != null ? LevelManager.Instance.currentLevel : 0,
            currentExp = LevelManager.Instance != null ? LevelManager.Instance.currentExp : 0,
            offeredIds = offeredIds
        });
    }

    private float ScoreCombination(int firstId, int secondId, int resultId)
    {
        CombineMemory combination = FindCombineMemory(firstId, secondId, resultId);
        if (combination == null || combination.count <= 0)
            return 0f;

        return combination.averageSurvivalSeconds + combination.bestSurvivalSeconds * 0.25f;
    }

    private void AutoCombineIfPossible()
    {
        UpgradeManager manager = UpgradeManager.Instance;
        DataManager data = DataManager.Instance;
        if (manager == null || data == null || manager.maxLevelUpgrades.Count < 2)
            return;

        CombineCandidate best = null;

        foreach (int firstId in manager.maxLevelUpgrades.ToArray())
        {
            if (!data.upgradeDatasDic.TryGetValue(firstId, out UpgradeData first))
                continue;

            if (first.combinationList == null || first.result == null)
                continue;

            int pairCount = Mathf.Min(first.combinationList.Length, first.result.Length);
            for (int i = 0; i < pairCount; i++)
            {
                int secondId = first.combinationList[i];
                if (!manager.maxLevelUpgrades.Contains(secondId))
                    continue;

                int resultId = first.result[i];
                if (!HasCombineResultData(data, resultId))
                    continue;

                float score = ScoreCombination(firstId, secondId, resultId);
                if (best == null || score > best.score)
                {
                    best = new CombineCandidate
                    {
                        firstId = firstId,
                        secondId = secondId,
                        resultId = resultId,
                        score = score
                    };
                }
            }
        }

        if (best != null)
        {
            ApplyCombine(best.firstId, best.secondId, best.resultId);
        }
    }

    private void ApplyCombine(int firstId, int secondId, int resultId)
    {
        UpgradeManager manager = UpgradeManager.Instance;
        DataManager data = DataManager.Instance;
        if (manager == null || data == null || !HasCombineResultData(data, resultId))
            return;

        Debug.Log($"[CodexPlaytest] COMBINE {firstId}+{secondId} => {resultId}");
        RecordCombine(firstId, secondId, resultId);

        StopUpgradeCoroutine(firstId);
        StopUpgradeCoroutine(secondId);

        manager.skillUpgrades.Remove(firstId);
        manager.skillUpgrades.Remove(secondId);
        manager.statUpgrades.Remove(firstId);
        manager.statUpgrades.Remove(secondId);
        manager.maxLevelUpgrades.Remove(firstId);
        manager.maxLevelUpgrades.Remove(secondId);

        if (data.upgradeDatasDic.TryGetValue(resultId, out UpgradeData result) &&
            result.type == "Skill")
        {
            if (!manager.skillUpgrades.Contains(resultId))
                manager.SkillUpgradesAdd(resultId);
        }
        else if (data.statUpgradeDatasDic.TryGetValue(resultId, out _))
        {
            if (!manager.statUpgrades.Contains(resultId))
                manager.StatUpgradesAdd(resultId);
        }

        manager.upgrades.AddRange(manager.renewableUpgrades);
        manager.renewableUpgrades.Clear();
        Time.timeScale = 1f;
    }

    private bool HasCombineResultData(DataManager data, int resultId)
    {
        if (data == null || resultId <= 0)
            return false;

        return data.upgradeDatasDic.ContainsKey(resultId) ||
               data.statUpgradeDatasDic.ContainsKey(resultId);
    }

    private void RecordCombine(int firstId, int secondId, int resultId)
    {
        if (currentEpisode == null)
            return;

        currentEpisode.combinedUpgrades.Add(GetCombineKey(firstId, secondId, resultId));
        currentEpisode.combineEvents.Add(new CombineEvent
        {
            elapsedSeconds = Time.realtimeSinceStartup - startedAt,
            firstId = firstId,
            secondId = secondId,
            resultId = resultId,
            key = GetCombineKey(firstId, secondId, resultId),
            hpRatio = GetHpRatio(),
            currentLevel = LevelManager.Instance != null ? LevelManager.Instance.currentLevel : 0,
            currentExp = LevelManager.Instance != null ? LevelManager.Instance.currentExp : 0
        });
    }

    private void StopUpgradeCoroutine(int upgradeId)
    {
        UpgradeManager manager = UpgradeManager.Instance;
        if (!manager.upgradeCor.TryGetValue(upgradeId, out IEnumerator coroutine))
            return;

        manager.StopCoroutine(coroutine);
        manager.upgradeCor.Remove(upgradeId);
    }

    private void LogNewMinuteResults()
    {
        LevelManager level = LevelManager.Instance;
        if (level == null)
            return;

        if (level.expPerMinute.Count == lastMinuteCount)
            return;

        for (int i = lastMinuteCount; i < level.expPerMinute.Count; i++)
        {
            Debug.Log($"[CodexPlaytest] MINUTE {i + 1}: exp={level.expPerMinute[i]}, levels={level.levelPerMinute[i]}");
        }

        lastMinuteCount = level.expPerMinute.Count;
    }

    private void Finish(string reason)
    {
        ending = true;
        SaveCurrentEpisode(reason);
        LogNewMinuteResults();

        LevelManager level = LevelManager.Instance;
        int totalExp = level != null ? level.expPerMinute.Sum() : 0;
        int totalLevels = level != null ? level.levelPerMinute.Sum() : 0;
        int currentLevel = level != null ? level.currentLevel : -1;
        int currentExp = level != null ? level.currentExp : -1;

        Debug.Log(
            $"[CodexPlaytest] END reason={reason}, minutes={lastMinuteCount}, " +
            $"totalExp={totalExp}, totalLevelUps={totalLevels}, currentLevel={currentLevel}, currentExp={currentExp}");
    }

    private void OnDisable()
    {
        if (Application.isPlaying && !memorySaved)
        {
            SaveCurrentEpisode("agent disabled");
        }
    }

    private void OnApplicationQuit()
    {
        if (!memorySaved)
        {
            SaveCurrentEpisode("application quit");
        }
    }

    private void RecordSurvivalSample()
    {
        if (currentEpisode == null || player == null || Time.realtimeSinceStartup - lastSampleAt < MemorySampleInterval)
            return;

        lastSampleAt = Time.realtimeSinceStartup;

        Vector2 position = player.transform.position;
        Vector2 towardCenter;
        float nearestEnemySqr = FindNearestEnemySqr(position);
        float nearestExpSqr = FindNearestExpSqr(position);
        SurvivalState state = BuildSurvivalState(position, nearestEnemySqr, nearestExpSqr, out towardCenter);

        currentEpisode.samples.Add(new SurvivalSample
        {
            elapsedSeconds = state.elapsedSeconds,
            x = position.x,
            y = position.y,
            hpRatio = state.hpRatio,
            nearestEnemyDistance = state.nearestEnemyDistance,
            nearestExpDistance = state.nearestExpDistance,
            enemyThreat = state.enemyThreat,
            expDesire = state.expDesire,
            edgeDanger = state.edgeDanger,
            level = state.level,
            currentExp = state.currentExp,
            moveX = lastMoveDirection.x,
            moveY = lastMoveDirection.y,
            quality = 1f
        });
    }

    private float FindNearestEnemySqr(Vector2 position)
    {
        float nearestSqr = float.MaxValue;

        foreach (EnemyCombat enemy in FindObjectsByType<EnemyCombat>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (enemy == null || enemy.isDead || !enemy.gameObject.activeInHierarchy)
                continue;

            float sqr = ((Vector2)enemy.transform.position - position).sqrMagnitude;
            nearestSqr = Mathf.Min(nearestSqr, sqr);
        }

        return nearestSqr;
    }

    private float FindNearestExpSqr(Vector2 position)
    {
        float nearestSqr = float.MaxValue;

        foreach (Exp exp in FindObjectsByType<Exp>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (exp == null || !exp.gameObject.activeInHierarchy)
                continue;

            float sqr = ((Vector2)exp.transform.position - position).sqrMagnitude;
            nearestSqr = Mathf.Min(nearestSqr, sqr);
        }

        return nearestSqr;
    }

    private void SaveCurrentEpisode(string reason)
    {
        if (memorySaved || currentEpisode == null)
            return;

        memorySaved = true;

        float survivalSeconds = Mathf.Max(0f, Time.realtimeSinceStartup - startedAt);
        LevelManager level = LevelManager.Instance;
        currentEpisode.reason = reason;
        currentEpisode.survivalSeconds = survivalSeconds;
        currentEpisode.finalLevel = level != null ? level.currentLevel : 0;
        currentEpisode.finalExp = level != null ? level.currentExp : 0;
        currentEpisode.totalExp = level != null ? level.expPerMinute.Sum() : 0;
        currentEpisode.totalLevelUps = level != null ? level.levelPerMinute.Sum() : 0;

        if (level != null)
        {
            currentEpisode.expPerMinute = new List<int>(level.expPerMinute);
            currentEpisode.levelPerMinute = new List<int>(level.levelPerMinute);
        }

        AppendEpisodeToMemory(currentEpisode);
        SaveMemory(memory);
        Debug.Log($"[CodexPlaytest] MEMORY SAVE reason={reason}, survival={survivalSeconds:0.0}s, samples={currentEpisode.samples.Count}");
    }

    private void AppendEpisodeToMemory(SurvivalEpisode episode)
    {
        if (memory.episodes == null)
            memory.episodes = new List<SurvivalEpisode>();
        if (memory.samples == null)
            memory.samples = new List<SurvivalSample>();
        if (memory.upgrades == null)
            memory.upgrades = new List<UpgradeMemory>();
        if (memory.combinations == null)
            memory.combinations = new List<CombineMemory>();

        memory.totalRuns++;
        memory.bestSurvivalSeconds = Mathf.Max(memory.bestSurvivalSeconds, episode.survivalSeconds);
        memory.episodes.Add(episode);
        memory.episodes = memory.episodes
            .OrderByDescending(x => x.survivalSeconds)
            .Take(MaxStoredEpisodes)
            .ToList();

        foreach (SurvivalSample sample in episode.samples)
        {
            sample.quality = Mathf.Max(1f, episode.survivalSeconds + sample.elapsedSeconds * 0.1f);
            memory.samples.Add(sample);
        }

        memory.samples = memory.samples
            .OrderByDescending(x => x.quality)
            .Take(MaxStoredMemorySamples)
            .ToList();

        for (int i = 0; i < episode.selectedUpgrades.Count; i++)
        {
            int id = episode.selectedUpgrades[i];
            UpgradeMemory upgrade = FindOrCreateUpgradeMemory(id);
            float positionalBonus = 1f + Mathf.Max(0f, 4f - i) * 0.05f;
            float value = episode.survivalSeconds * positionalBonus;
            upgrade.averageSurvivalSeconds =
                (upgrade.averageSurvivalSeconds * upgrade.count + value) /
                Mathf.Max(1, upgrade.count + 1);
            upgrade.bestSurvivalSeconds = Mathf.Max(upgrade.bestSurvivalSeconds, episode.survivalSeconds);
            upgrade.bestPickIndex = upgrade.bestSurvivalSeconds <= episode.survivalSeconds ? i : upgrade.bestPickIndex;
            upgrade.count++;
        }

        for (int i = 0; i < episode.combineEvents.Count; i++)
        {
            CombineEvent combineEvent = episode.combineEvents[i];
            CombineMemory combination = FindOrCreateCombineMemory(
                combineEvent.firstId,
                combineEvent.secondId,
                combineEvent.resultId
            );

            float positionalBonus = 1f + Mathf.Max(0f, 3f - i) * 0.05f;
            float value = episode.survivalSeconds * positionalBonus;
            combination.averageSurvivalSeconds =
                (combination.averageSurvivalSeconds * combination.count + value) /
                Mathf.Max(1, combination.count + 1);
            combination.bestSurvivalSeconds = Mathf.Max(combination.bestSurvivalSeconds, episode.survivalSeconds);
            combination.bestCombineIndex = combination.bestSurvivalSeconds <= episode.survivalSeconds
                ? i
                : combination.bestCombineIndex;
            combination.count++;
        }

        memory.upgrades = memory.upgrades
            .OrderByDescending(x => x.averageSurvivalSeconds)
            .ToList();
        memory.combinations = memory.combinations
            .OrderByDescending(x => x.averageSurvivalSeconds)
            .ToList();
        memory.bestUpgradeBuild = memory.episodes
            .OrderByDescending(x => x.survivalSeconds)
            .Select(x => new UpgradeBuildMemory
            {
                survivalSeconds = x.survivalSeconds,
                selectedUpgrades = new List<int>(x.selectedUpgrades ?? new List<int>()),
                combinedUpgrades = new List<string>(x.combinedUpgrades ?? new List<string>())
            })
            .FirstOrDefault();

        RecalculateWeightsFromMemory();
    }

    private void RecalculateWeightsFromMemory()
    {
        if (memory.samples == null || memory.samples.Count == 0)
        {
            memory.weights = PolicyWeights.Default();
            return;
        }

        int takeCount = Mathf.Min(120, memory.samples.Count);
        List<SurvivalSample> bestSamples = memory.samples.Take(takeCount).ToList();
        float enemyThreat = bestSamples.Average(x => x.enemyThreat);
        float expDesire = bestSamples.Average(x => x.expDesire);
        float edgeDanger = bestSamples.Average(x => x.edgeDanger);
        float hpRatio = bestSamples.Average(x => x.hpRatio);

        memory.weights.enemyAvoidWeight = Mathf.Lerp(1.35f, 2.6f, enemyThreat);
        memory.weights.expSeekWeight = Mathf.Lerp(0.65f, 1.45f, expDesire) * Mathf.Lerp(0.75f, 1.15f, hpRatio);
        memory.weights.edgeAvoidWeight = Mathf.Lerp(1.1f, 2.1f, edgeDanger);
        memory.weights.exploreWeight = Mathf.Lerp(0.15f, 0.45f, hpRatio);
        memory.weights.learnedDirectionWeight = Mathf.Clamp(0.25f + memory.totalRuns * 0.08f, 0.25f, 1.35f);
    }

    private SurvivalMemory LoadMemory()
    {
        string path = GetMemoryPath();
        if (!File.Exists(path))
            return SurvivalMemory.CreateDefault();

        try
        {
            SurvivalMemory loaded = JsonUtility.FromJson<SurvivalMemory>(File.ReadAllText(path));
            if (loaded == null)
                return SurvivalMemory.CreateDefault();

            loaded.EnsureDefaults();
            return loaded;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[CodexPlaytest] MEMORY LOAD 실패: {ex.Message}");
            return SurvivalMemory.CreateDefault();
        }
    }

    private void SaveMemory(SurvivalMemory data)
    {
        try
        {
            string path = GetMemoryPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[CodexPlaytest] MEMORY SAVE 실패: {ex.Message}");
        }
    }

    private string GetMemoryPath()
    {
        return Path.Combine(Application.persistentDataPath, MemoryFileName);
    }

    private float GetHpRatio()
    {
        return playerCombat != null && playerCombat.maxHp > 0f
            ? Mathf.Clamp01(playerCombat.hp / playerCombat.maxHp)
            : 1f;
    }

    private UpgradeMemory FindUpgradeMemory(int id)
    {
        if (memory.upgrades == null)
            return null;

        return memory.upgrades.FirstOrDefault(x => x.id == id);
    }

    private UpgradeMemory FindOrCreateUpgradeMemory(int id)
    {
        UpgradeMemory upgrade = FindUpgradeMemory(id);
        if (upgrade != null)
            return upgrade;

        upgrade = new UpgradeMemory { id = id };
        memory.upgrades.Add(upgrade);
        return upgrade;
    }

    private CombineMemory FindCombineMemory(int firstId, int secondId, int resultId)
    {
        if (memory.combinations == null)
            return null;

        string key = GetCombineKey(firstId, secondId, resultId);
        return memory.combinations.FirstOrDefault(x => x.key == key);
    }

    private CombineMemory FindOrCreateCombineMemory(int firstId, int secondId, int resultId)
    {
        CombineMemory combination = FindCombineMemory(firstId, secondId, resultId);
        if (combination != null)
            return combination;

        int min = Mathf.Min(firstId, secondId);
        int max = Mathf.Max(firstId, secondId);
        combination = new CombineMemory
        {
            key = GetCombineKey(firstId, secondId, resultId),
            firstId = min,
            secondId = max,
            resultId = resultId
        };
        memory.combinations.Add(combination);
        return combination;
    }

    private string GetCombineKey(int firstId, int secondId, int resultId)
    {
        int min = Mathf.Min(firstId, secondId);
        int max = Mathf.Max(firstId, secondId);
        return $"{min}+{max}->{resultId}";
    }

    private struct SurvivalState
    {
        public float elapsedSeconds;
        public float hpRatio;
        public float nearestEnemyDistance;
        public float nearestExpDistance;
        public float enemyThreat;
        public float expDesire;
        public float edgeDanger;
        public int level;
        public int currentExp;
    }

    [Serializable]
    private class SurvivalMemory
    {
        public int totalRuns;
        public float bestSurvivalSeconds;
        public PolicyWeights weights = PolicyWeights.Default();
        public List<SurvivalEpisode> episodes = new List<SurvivalEpisode>();
        public List<SurvivalSample> samples = new List<SurvivalSample>();
        public List<UpgradeMemory> upgrades = new List<UpgradeMemory>();
        public List<CombineMemory> combinations = new List<CombineMemory>();
        public UpgradeBuildMemory bestUpgradeBuild = new UpgradeBuildMemory();

        public static SurvivalMemory CreateDefault()
        {
            SurvivalMemory data = new SurvivalMemory();
            data.EnsureDefaults();
            return data;
        }

        public void EnsureDefaults()
        {
            if (weights == null)
                weights = PolicyWeights.Default();
            if (episodes == null)
                episodes = new List<SurvivalEpisode>();
            if (samples == null)
                samples = new List<SurvivalSample>();
            if (upgrades == null)
                upgrades = new List<UpgradeMemory>();
            if (combinations == null)
                combinations = new List<CombineMemory>();
            if (bestUpgradeBuild == null)
                bestUpgradeBuild = new UpgradeBuildMemory();
        }
    }

    [Serializable]
    private class PolicyWeights
    {
        public float enemyAvoidWeight = 1.8f;
        public float expSeekWeight = 1.1f;
        public float edgeAvoidWeight = 1.4f;
        public float exploreWeight = 0.25f;
        public float learnedDirectionWeight = 0.25f;

        public static PolicyWeights Default()
        {
            return new PolicyWeights();
        }
    }

    [Serializable]
    private class SurvivalEpisode
    {
        public string startedAt;
        public string reason;
        public float survivalSeconds;
        public int finalLevel;
        public int finalExp;
        public int totalExp;
        public int totalLevelUps;
        public List<int> selectedUpgrades = new List<int>();
        public List<string> combinedUpgrades = new List<string>();
        public List<UpgradeEvent> upgradeEvents = new List<UpgradeEvent>();
        public List<CombineEvent> combineEvents = new List<CombineEvent>();
        public List<int> expPerMinute = new List<int>();
        public List<int> levelPerMinute = new List<int>();
        public List<SurvivalSample> samples = new List<SurvivalSample>();
    }

    [Serializable]
    private class SurvivalSample
    {
        public float elapsedSeconds;
        public float x;
        public float y;
        public float hpRatio;
        public float nearestEnemyDistance;
        public float nearestExpDistance;
        public float enemyThreat;
        public float expDesire;
        public float edgeDanger;
        public int level;
        public int currentExp;
        public float moveX;
        public float moveY;
        public float quality;
    }

    [Serializable]
    private class UpgradeMemory
    {
        public int id;
        public int count;
        public float averageSurvivalSeconds;
        public float bestSurvivalSeconds;
        public int bestPickIndex;
    }

    [Serializable]
    private class CombineCandidate
    {
        public int firstId;
        public int secondId;
        public int resultId;
        public float score;
    }

    [Serializable]
    private class UpgradeEvent
    {
        public float elapsedSeconds;
        public int selectedId;
        public string selectedType;
        public int selectedLevelBefore;
        public float hpRatio;
        public int currentLevel;
        public int currentExp;
        public List<int> offeredIds = new List<int>();
    }

    [Serializable]
    private class CombineEvent
    {
        public float elapsedSeconds;
        public int firstId;
        public int secondId;
        public int resultId;
        public string key;
        public float hpRatio;
        public int currentLevel;
        public int currentExp;
    }

    [Serializable]
    private class CombineMemory
    {
        public string key;
        public int firstId;
        public int secondId;
        public int resultId;
        public int count;
        public float averageSurvivalSeconds;
        public float bestSurvivalSeconds;
        public int bestCombineIndex;
    }

    [Serializable]
    private class UpgradeBuildMemory
    {
        public float survivalSeconds;
        public List<int> selectedUpgrades = new List<int>();
        public List<string> combinedUpgrades = new List<string>();
    }
}
