using UnityEngine;

public static class UpgradeFusionRules
{
    public static bool TryGetResult(DataManager data, int first, int second, out int result)
    {
        result = 0;
        if (data == null || first == second ||
            !data.upgradeDatasDic.ContainsKey(first) || !data.upgradeDatasDic.ContainsKey(second))
            return false;
        return Find(data, first, second, out result) || Find(data, second, first, out result);
    }

    private static bool Find(DataManager data, int first, int second, out int result)
    {
        result = 0;
        UpgradeData recipe = data.upgradeDatasDic[first];
        if (recipe.combinationList == null || recipe.result == null)
            return false;
        int count = Mathf.Min(recipe.combinationList.Length, recipe.result.Length);
        for (int i = 0; i < count; i++)
        {
            int candidate = recipe.result[i];
            if (recipe.combinationList[i] == second && candidate != first && candidate != second &&
                data.upgradeDatasDic.TryGetValue(candidate, out var output) && output.type == "Skill")
            {
                result = candidate;
                return true;
            }
        }
        return false;
    }

    public static bool CanCombine(UpgradeManager manager, int first, int second, out int result)
    {
        result = 0;
        return manager != null && IsMaxed(manager, first) && IsMaxed(manager, second) &&
            TryGetResult(DataManager.Instance, first, second, out result) &&
            !manager.skillUpgrades.Contains(result);
    }

    public static bool IsMaxed(UpgradeManager manager, int id)
    {
        return manager != null && DataManager.Instance != null && manager.skillUpgrades.Contains(id) &&
            DataManager.Instance.upgradeDatasDic.TryGetValue(id, out var data) && data.UpgradeLevel >= 4;
    }

    public static bool HasAvailablePair(UpgradeManager manager)
    {
        if (manager == null || manager.skillUpgrades.Count < 2)
            return false;
        for (int i = 0; i < manager.skillUpgrades.Count; i++)
            for (int j = i + 1; j < manager.skillUpgrades.Count; j++)
                if (CanCombine(manager, manager.skillUpgrades[i], manager.skillUpgrades[j], out _))
                    return true;
        return false;
    }
}
