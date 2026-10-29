using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class CharacterProgress
{
    public int id;
    public bool owned;
    public int level;
    public int fragments;
}

[Serializable]
public sealed class RunReward
{
    public string runId;
    public bool victory;
    public int gold;
    public int characterId;
    public int fragments;
}

[Serializable]
public sealed class CharacterProgressionSave
{
    public int version = 1;
    public int gold;
    public string activeRunId;
    public bool runUsesWaveGold;
    public int cumulativeRunGold;
    public RunReward lastReward;
    public List<CharacterProgress> characters = new();

    public void EnsureCharacters(CharacterProgressionSettings settings)
    {
        characters ??= new List<CharacterProgress>();
        gold = Math.Max(0, gold);
        foreach (var definition in settings.characters)
        {
            if (definition == null || definition.id <= 0)
                continue;
            var character = characters.Find(c => c != null && c.id == definition.id);
            if (character == null)
            {
                character = new CharacterProgress { id = definition.id, owned = definition.initiallyOwned };
                characters.Add(character);
            }
            character.level = Mathf.Clamp(character.level, 0, settings.maxLevel);
            character.fragments = Math.Max(0, character.fragments);
        }
    }

    public CharacterProgress Character(int id) => characters.Find(c => c != null && c.id == id);

    public bool TryGrantReward(CharacterProgressionSettings settings, bool victory, int recipientId, out RunReward reward)
    {
        reward = lastReward;
        if (string.IsNullOrEmpty(activeRunId) || (lastReward != null && lastReward.runId == activeRunId))
            return false;
        var recipient = Character(recipientId);
        if (recipient == null || !recipient.owned)
            return false;
        reward = new RunReward
        {
            runId = activeRunId, victory = victory, characterId = recipientId,
            gold = Math.Max(0, runUsesWaveGold ? cumulativeRunGold : victory ? settings.victoryGold : settings.defeatGold),
            fragments = Math.Max(0, victory ? settings.victoryFragments : settings.defeatFragments)
        };
        gold = (int)Math.Min(int.MaxValue, (long)gold + reward.gold);
        recipient.fragments = (int)Math.Min(int.MaxValue, (long)recipient.fragments + reward.fragments);
        lastReward = reward;
        return true;
    }

    public bool TryUpgrade(CharacterProgressionSettings settings, int id)
    {
        var character = Character(id);
        if (character == null || !character.owned || character.level >= settings.maxLevel)
            return false;
        int goldCost = settings.GoldCost(character.level);
        int fragmentCost = settings.FragmentCost(character.level);
        if (gold < goldCost || character.fragments < fragmentCost)
            return false;
        gold -= goldCost;
        character.fragments -= fragmentCost;
        character.level++;
        return true;
    }
}

public static class CharacterProgression
{
    private const string SaveKey = "Conquest.CharacterProgression.v1";
    private static CharacterProgressionSave save;
    private static CharacterProgressionSettings settings;
    public static event Action Changed;
    public static CharacterProgressionSettings Settings
    {
        get
        {
            if (settings == null)
                settings = Resources.Load<CharacterProgressionSettings>("CharacterProgressionSettings");
            if (settings == null)
                throw new InvalidOperationException("CharacterProgressionSettings 에셋이 없습니다.");
            return settings;
        }
    }
    public static CharacterProgressionSave State
    {
        get
        {
            if (save != null) return save;
            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            try { save = string.IsNullOrEmpty(json) ? new CharacterProgressionSave() : JsonUtility.FromJson<CharacterProgressionSave>(json); }
            catch (ArgumentException) { Debug.LogWarning("캐릭터 진행도 저장 데이터를 읽지 못했습니다."); }
            save ??= new CharacterProgressionSave();
            save.EnsureCharacters(Settings);
            return save;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() { save = null; settings = null; Changed = null; }

    private static void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(State));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static void BeginRun()
    {
        State.activeRunId = Guid.NewGuid().ToString("N");
        State.runUsesWaveGold = false;
        State.cumulativeRunGold = 0;
        Save();
    }

    public static void RecordWaveGold(int cumulativeGold)
    {
        if (string.IsNullOrEmpty(State.activeRunId) || State.lastReward?.runId == State.activeRunId)
            return;
        State.runUsesWaveGold = true;
        State.cumulativeRunGold = Math.Max(State.cumulativeRunGold, Math.Max(0, cumulativeGold));
    }

    public static RunReward GrantResult(bool victory)
    {
        if (State.lastReward != null && State.lastReward.runId == State.activeRunId)
            return State.lastReward;
        var owned = Settings.characters.Where(d => d != null && State.Character(d.id)?.owned == true).ToArray();
        if (owned.Length == 0) return null;
        int recipient = owned[UnityEngine.Random.Range(0, owned.Length)].id;
        if (!State.TryGrantReward(Settings, victory, recipient, out var reward)) return null;
        Save();
        return reward;
    }

    public static bool CanUpgrade(int id)
    {
        var c = State.Character(id);
        return c != null && c.owned && c.level < Settings.maxLevel &&
            State.gold >= Settings.GoldCost(c.level) && c.fragments >= Settings.FragmentCost(c.level);
    }

    public static bool TryUpgrade(int id)
    {
        if (!State.TryUpgrade(Settings, id)) return false;
        Save();
        return true;
    }

    public static int Level(int id) => State.Character(id)?.level ?? 0;
    public static CharacterEnhancementBonus Bonuses(int id, int level, bool cumulative = true)
    {
        level = Mathf.Clamp(level, 0, Settings.maxLevel);
        if (CharacterEnhancementTable.TryGet(id, level, cumulative, out var bonus)) return bonus;
        int count = cumulative ? level : level > 0 ? 1 : 0;
        return new CharacterEnhancementBonus { attack = count * Settings.attackBonusPerLevel,
            hpPercent = count * Settings.hpPercentPerLevel };
    }
    public static int AttackBonus(int id) => Bonuses(id, Level(id)).attack;
    public static float HpMultiplier(int id) => 1f + Bonuses(id, Level(id)).hpPercent / 100f;
    public static BasicAttackModifiers BasicAttackBonus(int id)
    {
        var bonus = Bonuses(id, Level(id));
        var result = BasicAttackModifiers.Default;
        result.rangeMultiplier += bonus.rangePercent / 100f;
        result.damageMultiplier += bonus.basicDamagePercent / 100f;
        result.attackCount += bonus.attackCount;
        result.pierceCount += bonus.pierce;
        return result;
    }
    public static void ReloadEnhancements()
    {
        CharacterEnhancementTable.Reload();
        Changed?.Invoke();
    }
    public static CharacterProgressionSettings.CharacterDefinition Definition(int id) => Settings.characters.FirstOrDefault(c => c != null && c.id == id);
    public static string Copy(string ko, string en, string jp) => LocalizationManager.Instance?.currentLanguage switch { "en" => en, "jp" => jp, _ => ko };
    public static string CharacterName(int id)
    {
        string key = Definition(id)?.nameKey;
        return !string.IsNullOrEmpty(key) && LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetTranslation(key) : id == 2 ? Copy("마법사", "Mage", "魔法使い") : Copy("검사", "Swordsman", "剣士");
    }
}
