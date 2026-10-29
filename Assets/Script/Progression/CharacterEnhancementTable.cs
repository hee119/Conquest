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
    public int pierce;
    public float attackRange;
    public float attackSpeed;

    public void Add(CharacterEnhancementBonus value)
    {
        hp += value.hp;
        hpRegenRate += value.hpRegenRate;
        speed += value.speed;
        attack += value.attack;
        defense += value.defense;
        criticalDamage += value.criticalDamage;
        criticalChance += value.criticalChance;
        pierce += value.pierce;
        attackRange += value.attackRange;
        attackSpeed += value.attackSpeed;
    }
}

// Final stat arrays: index 0 is base, indexes 1..5 are enhanced values.
public static class CharacterEnhancementTable
{
    private static Dictionary<int, SortedDictionary<int, CharacterEnhancementBonus>> curves;
    private static readonly string[] Columns = { "Hp", "HpRegenRate", "Speed",
        "Damage", "Defense", "Critical Damage", "Critical Chance", "Pierce", "AttackRange", "AttackSpeed" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reload() => curves = null;

    public static bool TryGet(int characterId, int level, bool cumulative, out CharacterEnhancementBonus bonus)
    {
        EnsureLoaded();
        bonus = default;
        if (!curves.TryGetValue(characterId, out var steps)) return false;
        foreach (var step in steps)
        {
            if (step.Key == 0) continue;
            if (step.Key > level) break;
            if (cumulative || step.Key == level) bonus.Add(step.Value);
        }
        return true;
    }

    public static BasicAttackModifiers BasicAttack(int characterId, int level, out float attackSpeed)
    {
        EnsureLoaded();
        var result = BasicAttackModifiers.Default;
        attackSpeed = 1f;
        if (!curves.TryGetValue(characterId, out var steps) || !steps.TryGetValue(0, out var basic)) return result;
        foreach (var step in steps)
        {
            if (step.Key == 0) continue;
            if (step.Key > Mathf.Clamp(level, 0, 5)) break;
            basic.Add(step.Value);
        }
        result.pierceCount = Mathf.Max(0, basic.pierce);
        result.rangeMultiplier = Mathf.Max(0.1f, basic.attackRange);
        attackSpeed = Mathf.Max(0.1f, basic.attackSpeed);
        return result;
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
                        || v < 0 || ((c == 8 || c == 9) && v < 0.1f) || float.IsInfinity(v) || float.IsNaN(v)
                        || ((c == 0 || c == 1 || c == 3 || c == 4 || c == 6 || c == 7) && (v != Math.Floor(v) || v >= int.MaxValue)))
                    { valid = false; break; }
                    values[c][n] = v;
                }
                if (!valid) break;
            }
            if (!valid || count != 6 || result.ContainsKey(id))
            {
                Debug.LogWarning($"캐릭터 {id}: 강화 배열의 길이와 숫자 형식을 확인하세요.");
                continue;
            }
            var steps = new SortedDictionary<int, CharacterEnhancementBonus>();
            steps.Add(0, new CharacterEnhancementBonus { pierce = (int)values[7][0], attackRange = values[8][0], attackSpeed = values[9][0] });
            for (int n = 1; n < count; n++) steps.Add(n, new CharacterEnhancementBonus {
                hp = values[0][n] - values[0][n - 1],
                hpRegenRate = (int)(values[1][n] - values[1][n - 1]),
                speed = values[2][n] - values[2][n - 1],
                attack = (int)(values[3][n] - values[3][n - 1]),
                defense = (int)(values[4][n] - values[4][n - 1]),
                criticalDamage = values[5][n] - values[5][n - 1],
                criticalChance = (int)(values[6][n] - values[6][n - 1]),
                pierce = (int)(values[7][n] - values[7][n - 1]),
                attackRange = values[8][n] - values[8][n - 1],
                attackSpeed = values[9][n] - values[9][n - 1] });
            result.Add(id, steps);
        }
        return result;
    }
}
