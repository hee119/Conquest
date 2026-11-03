using UnityEngine;
using UnityEngine.UI;

/// <summary>Repairs missing debug button targets when scene managers are recreated.</summary>
public sealed class DebugButtonAutoBinding : MonoBehaviour
{
    [SerializeField] private Button skillUpgrade;
    [SerializeField] private Button waveSkip;
    [SerializeField] private Button spawnId;
    [SerializeField] private int[] skillIds = { 400, 401, 402, 403, 404 };
    [SerializeField] private int spawnPoolId = 1;
    private UpgradeManager upgradeManager;
    private StageManager stageManager;
    private PoolManager poolManager;

    private void OnEnable()
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            if (skillUpgrade == null && button.name == "SkillUpgrade") skillUpgrade = button;
            if (waveSkip == null && button.name == "WaveSkip") waveSkip = button;
            if (spawnId == null && button.name == "SpawnId") spawnId = button;
        }
        FindTargets();
        if (skillUpgrade != null) skillUpgrade.onClick.AddListener(Upgrade);
        if (waveSkip != null) waveSkip.onClick.AddListener(Skip);
        if (spawnId != null) spawnId.onClick.AddListener(Spawn);
    }

    private void OnDisable()
    {
        if (skillUpgrade != null) skillUpgrade.onClick.RemoveListener(Upgrade);
        if (waveSkip != null) waveSkip.onClick.RemoveListener(Skip);
        if (spawnId != null) spawnId.onClick.RemoveListener(Spawn);
    }

    private void FindTargets()
    {
        upgradeManager = UpgradeManager.Instance != null ? UpgradeManager.Instance : FindFirstObjectByType<UpgradeManager>();
        stageManager = StageManager.Instance != null ? StageManager.Instance : FindFirstObjectByType<StageManager>();
        poolManager = PoolManager.Instance != null ? PoolManager.Instance : FindFirstObjectByType<PoolManager>();
    }

    private static bool MissingTarget(Button button, int index)
    {
        return index >= button.onClick.GetPersistentEventCount() ||
            button.onClick.GetPersistentTarget(index) == null;
    }

    private void Upgrade()
    {
        FindTargets();
        if (upgradeManager == null) return;
        for (int i = 0; i < skillIds.Length; i++)
            if (MissingTarget(skillUpgrade, i)) upgradeManager.SkillUpgradesAdd(skillIds[i]);
    }

    private void Skip()
    {
        FindTargets();
        if (stageManager != null && MissingTarget(waveSkip, 0)) stageManager.GoNextWave();
    }

    private void Spawn()
    {
        FindTargets();
        if (poolManager != null && MissingTarget(spawnId, 0)) poolManager.SpawnTest(spawnPoolId);
    }
}
