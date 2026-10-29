using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public struct CharacterEnhancementBonus
{
    public float hpPercent;
    public int attack;
    public int defense;
    public int criticalChance;
    public float rangePercent;
    public float basicDamagePercent;
    public int attackCount;
    public int pierce;

    public void Add(CharacterEnhancementBonus value)
    {
        hpPercent += value.hpPercent;
        attack += value.attack;
        defense += value.defense;
        criticalChance += value.criticalChance;
        rangePercent += value.rangePercent;
        basicDamagePercent += value.basicDamagePercent;
        attackCount += value.attackCount;
        pierce += value.pierce;
    }
}

// The published PlayerData CSV carries the editable enhancement tab in its
// EnhancementSteps column. No authenticated Sheets request is needed in a build.
public static class CharacterEnhancementTable
{
    private static Dictionary<int, SortedDictionary<int, CharacterEnhancementBonus>> curves;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reload() => curves = null;

    public static bool TryGet(int characterId, int level, bool cumulative, out CharacterEnhancementBonus bonus)
    {
        EnsureLoaded();
        bonus = default;
        if (!curves.TryGetValue(characterId, out var steps)) return false;
        foreach (var step in steps)
        {
            if (step.Key > level) break;
            if (cumulative || step.Key == level) bonus.Add(step.Value);
        }
        return true;
    }

    private static void EnsureLoaded()
    {
        if (curves != null) return;
        var asset = Resources.Load<TextAsset>("TextData/PlayerData");
        curves = Parse(asset != null ? asset.text : string.Empty);
    }

    public static Dictionary<int, SortedDictionary<int, CharacterEnhancementBonus>> Parse(string csv)
    {
        var result = new Dictionary<int, SortedDictionary<int, CharacterEnhancementBonus>>();
        string[] lines = csv.Split('\n');
        string[] headers = DataManager.ParseCsvLine(lines[0].Trim());
        int column = Array.FindIndex(headers, h => h.Trim() == "EnhancementSteps");
        if (column < 0) return result;
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var row = DataManager.ParseCsvLine(lines[i].Trim());
            if (row.Length <= column || !int.TryParse(row[0], out int id) || id <= 0 || string.IsNullOrWhiteSpace(row[column])) continue;
            var steps = new SortedDictionary<int, CharacterEnhancementBonus>();
            bool valid = true;
            foreach (string packed in row[column].Split(';'))
            {
                string[] cells = packed.Split('|');
                if (cells.Length != 9 || !Integer(cells[0], out int level) || level < 1 || steps.ContainsKey(level) ||
                    !Number(cells[1], out float hp) || !Integer(cells[2], out int attack) ||
                    !Integer(cells[3], out int defense) || !Integer(cells[4], out int crit) ||
                    !Number(cells[5], out float range) || !Number(cells[6], out float damage) ||
                    !Integer(cells[7], out int count) || !Integer(cells[8], out int pierce))
                {
                    valid = false;
                    break;
                }
                steps.Add(level, new CharacterEnhancementBonus { hpPercent = hp, attack = attack,
                    defense = defense, criticalChance = crit, rangePercent = range,
                    basicDamagePercent = damage, attackCount = count, pierce = pierce });
            }
            if (valid && !result.ContainsKey(id)) result.Add(id, steps);
            else Debug.LogWarning($"캐릭터 {id} 강화 데이터 오류: 중복 레벨 및 음수/정수가 아닌 증가량을 확인하세요. 기본 강화를 사용합니다.");
        }
        return result;
    }

    private static bool Integer(string value, out int number) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number) && number >= 0;
    private static bool Number(string value, out float number) =>
        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) &&
        number >= 0 && !float.IsInfinity(number) && !float.IsNaN(number);
}
