using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public struct CharacterEnhancementBonus
{
    public float hp;
    public int hpRegenRate;
    public float speed;
    public int attack;
    public int defense;
    public float criticalDamage;
    public int criticalChance;

    public void Add(CharacterEnhancementBonus value)
    {
        hp += value.hp;
        hpRegenRate += value.hpRegenRate;
        speed += value.speed;
        attack += value.attack;
        defense += value.defense;
        criticalDamage += value.criticalDamage;
        criticalChance += value.criticalChance;
    }
}

// Seven comma-separated arrays in PlayerData, one additive value per enhancement level.
public static class CharacterEnhancementTable
{
    private static Dictionary<int, SortedDictionary<int, CharacterEnhancementBonus>> curves;
    private static readonly string[] Columns = { "EnhancementHp", "EnhancementHpRegenRate", "EnhancementSpeed",
        "EnhancementDamage", "EnhancementDefense", "EnhancementCriticalDamage", "EnhancementCriticalChance" };

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
        if (string.IsNullOrWhiteSpace(csv)) return result;
        string[] lines = csv.Split('\n');
        string[] headers = DataManager.ParseCsvLine(lines[0].Trim());
        int[] columns = Array.ConvertAll(Columns, name => Array.FindIndex(headers, h => h.Trim() == name));
        if (Array.Exists(columns, c => c < 0)) return result;
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var row = DataManager.ParseCsvLine(lines[i].Trim());
            if (!int.TryParse(row[0], out int id) || id <= 0) continue;
            var values = new float[Columns.Length][];
            bool valid = true;
            int count = -1;
            for (int c = 0; c < columns.Length; c++)
            {
                if (columns[c] >= row.Length) { valid = false; break; }
                var parts = row[columns[c]].Split(',');
                if (count < 0) count = parts.Length;
                if (count != parts.Length) { valid = false; break; }
                values[c] = new float[count];
                for (int n = 0; n < count; n++)
                {
                    if (!float.TryParse(parts[n].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
                        || v < 0 || float.IsInfinity(v) || float.IsNaN(v)
                        || ((c == 0 || c == 1 || c == 3 || c == 4 || c == 6) && (v != Math.Floor(v) || v >= int.MaxValue)))
                    { valid = false; break; }
                    values[c][n] = v;
                }
                if (!valid) break;
            }
            if (!valid || result.ContainsKey(id))
            {
                Debug.LogWarning($"캐릭터 {id}: 강화 배열의 길이와 숫자 형식을 확인하세요.");
                continue;
            }
            var steps = new SortedDictionary<int, CharacterEnhancementBonus>();
            for (int n = 0; n < count; n++) steps.Add(n + 1, new CharacterEnhancementBonus {
                hp = values[0][n], hpRegenRate = (int)values[1][n], speed = values[2][n],
                attack = (int)values[3][n], defense = (int)values[4][n], criticalDamage = values[5][n],
                criticalChance = (int)values[6][n] });
            result.Add(id, steps);
        }
        return result;
    }
}
