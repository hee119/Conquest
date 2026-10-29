using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Conquest/Character Progression Settings")]
public sealed class CharacterProgressionSettings : ScriptableObject
{
    [Header("결과 보상")]
    [Min(0)] public int victoryGold = 300;
    [Min(0)] public int defeatGold = 100;
    [Min(0)] public int victoryFragments = 6;
    [Min(0)] public int defeatFragments = 3;
    [Header("캐릭터 강화")]
    [Min(1)] public int maxLevel = 5;
    [Min(1)] public int firstGoldCost = 200;
    [Min(0)] public int goldCostPerLevel = 100;
    [Min(1)] public int firstFragmentCost = 3;
    [Min(0)] public int fragmentCostPerLevel = 2;
    [Min(0)] public int attackBonusPerLevel = 2;
    [Min(0f)] public float hpPercentPerLevel = 5f;
    public CharacterDefinition[] characters =
    {
        new CharacterDefinition { id = 1, nameKey = "lobby_character_swordman", initiallyOwned = true },
        new CharacterDefinition { id = 2, nameKey = "lobby_character_mage", initiallyOwned = true }
    };

    public int GoldCost(int level) => (int)Math.Min(int.MaxValue,
        Math.Max(1L, (long)firstGoldCost + (long)Math.Max(0, level) * goldCostPerLevel));
    public int FragmentCost(int level) => (int)Math.Min(int.MaxValue,
        Math.Max(1L, (long)firstFragmentCost + (long)Math.Max(0, level) * fragmentCostPerLevel));

    [Serializable]
    public sealed class CharacterDefinition
    {
        public int id;
        public string nameKey;
        public Sprite portrait;
        public bool initiallyOwned;
    }
}
