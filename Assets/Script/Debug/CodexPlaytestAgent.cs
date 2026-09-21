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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // This run uses an explicit scene object so only one agent controls the player.
    }

    private void Start()
    {
        startedAt = Time.realtimeSinceStartup;
        Debug.Log("[CodexPlaytest] START 자동 플레이 측정 시작");
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

        Vector2 orbit = new Vector2(Mathf.Sin(Time.time * 0.9f), Mathf.Cos(Time.time * 0.7f));
        Vector2 desired = orbit * 0.25f;

        if (nearestEnemySqr < 49f)
            desired += away * 1.8f;

        if (nearestExpSqr < 225f)
            desired += towardExp * (nearestEnemySqr < 25f ? 0.25f : 1.15f);

        desired = KeepInsideTilemap(position, desired);

        if (desired.sqrMagnitude < 0.01f)
            desired = orbit;

        return desired.normalized;
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

        Debug.Log($"[CodexPlaytest] SELECT id={best.upgradeUiInfo.upgradeData.id}");
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
        if (index >= 0)
            return index;

        return 1000 + id;
    }

    private void AutoCombineIfPossible()
    {
        UpgradeManager manager = UpgradeManager.Instance;
        DataManager data = DataManager.Instance;
        if (manager == null || data == null || manager.maxLevelUpgrades.Count < 2)
            return;

        foreach (int firstId in manager.maxLevelUpgrades.ToArray())
        {
            if (!data.upgradeDatasDic.TryGetValue(firstId, out UpgradeData first))
                continue;

            for (int i = 0; i < first.combinationList.Length; i++)
            {
                int secondId = first.combinationList[i];
                if (!manager.maxLevelUpgrades.Contains(secondId))
                    continue;

                int resultId = first.result[i];
                if (!data.upgradeDatasDic.ContainsKey(resultId))
                    continue;

                ApplyCombine(firstId, secondId, resultId);
                return;
            }
        }
    }

    private void ApplyCombine(int firstId, int secondId, int resultId)
    {
        UpgradeManager manager = UpgradeManager.Instance;
        Debug.Log($"[CodexPlaytest] COMBINE {firstId}+{secondId} => {resultId}");

        StopUpgradeCoroutine(firstId);
        StopUpgradeCoroutine(secondId);

        manager.skillUpgrades.Remove(firstId);
        manager.skillUpgrades.Remove(secondId);
        manager.statUpgrades.Remove(firstId);
        manager.statUpgrades.Remove(secondId);
        manager.maxLevelUpgrades.Remove(firstId);
        manager.maxLevelUpgrades.Remove(secondId);

        if (DataManager.Instance.upgradeDatasDic.TryGetValue(resultId, out UpgradeData result) &&
            result.type == "Skill")
        {
            if (!manager.skillUpgrades.Contains(resultId))
                manager.SkillUpgradesAdd(resultId);
        }
        else if (DataManager.Instance.statUpgradeDatasDic.TryGetValue(resultId, out _))
        {
            if (!manager.statUpgrades.Contains(resultId))
                manager.StatUpgradesAdd(resultId);
        }

        manager.upgrades.AddRange(manager.renewableUpgrades);
        manager.renewableUpgrades.Clear();
        Time.timeScale = 1f;
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
}
