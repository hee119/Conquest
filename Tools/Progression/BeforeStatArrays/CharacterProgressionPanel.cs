using TMPro;
using UnityEngine;

public sealed class CharacterProgressionPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text bonusText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private UnityEngine.UI.Button upgradeButton;
    private int characterId = 1;
    private LocalizationManager localization;

    private void OnEnable()
    {
        CharacterProgression.Changed += Refresh;
        localization = LocalizationManager.Instance;
        if (localization != null) localization.OnLanguageChanged += Refresh;
        if (upgradeButton != null) upgradeButton.onClick.AddListener(Upgrade);
        Refresh();
    }

    private void OnDisable()
    {
        CharacterProgression.Changed -= Refresh;
        if (localization != null) localization.OnLanguageChanged -= Refresh;
        if (upgradeButton != null) upgradeButton.onClick.RemoveListener(Upgrade);
    }

    public void SetCharacter(int id) { characterId = id; Refresh(); }
    public bool IsUpgradeButton(UnityEngine.UI.Button button) => button == upgradeButton;

    public void Refresh()
    {
        var c = CharacterProgression.State.Character(characterId);
        if (c == null) return;
        var config = CharacterProgression.Settings;
        bool maxed = c.level >= config.maxLevel;
        string Copy(string ko, string en, string jp) => CharacterProgression.Copy(ko, en, jp);
        if (levelText != null) levelText.text = Copy("강화", "Enhancement", "強化") + $"  +{c.level} / {config.maxLevel}";
        if (bonusText != null)
        {
            bonusText.enableAutoSizing = true;
            bonusText.fontSizeMin = 13;
            bonusText.fontSizeMax = 22;
            bonusText.text = Describe(CharacterProgression.Bonuses(characterId, c.level));
        }
        if (costText != null) costText.text = maxed ? Copy("최대 강화 완료", "Fully enhanced", "最大強化済み") : Copy(
            $"골드 {CharacterProgression.State.gold:N0} / {config.GoldCost(c.level):N0}    파편 {c.fragments} / {config.FragmentCost(c.level)}",
            $"Gold {CharacterProgression.State.gold:N0} / {config.GoldCost(c.level):N0}    Shards {c.fragments} / {config.FragmentCost(c.level)}",
            $"ゴールド {CharacterProgression.State.gold:N0} / {config.GoldCost(c.level):N0}    欠片 {c.fragments} / {config.FragmentCost(c.level)}");
        if (statusText != null)
        {
            statusText.enableAutoSizing = true;
            statusText.fontSizeMin = 12;
            statusText.fontSizeMax = 19;
            statusText.text = maxed ? string.Empty : Copy("다음 강화: ", "Next: ", "次の強化: ") +
                Describe(CharacterProgression.Bonuses(characterId, c.level + 1, false));
        }
        if (buttonText != null) buttonText.text = maxed ? Copy("강화 완료", "MAX", "強化済み") : Copy("강화하기", "Enhance", "強化する");
        if (upgradeButton != null) upgradeButton.interactable = CharacterProgression.CanUpgrade(characterId);
    }

    private static string Describe(CharacterEnhancementBonus bonus)
    {
        var parts = new System.Collections.Generic.List<string>();
        void Add(float value, string ko, string en, string jp, string unit = "")
        {
            if (value > 0) parts.Add(CharacterProgression.Copy(ko, en, jp) + $" +{value:0.#}{unit}");
        }
        Add(bonus.hpPercent, "체력", "HP", "HP", "%");
        Add(bonus.attack, "공격력", "Attack", "攻撃力");
        Add(bonus.defense, "방어력", "Defense", "防御力");
        Add(bonus.criticalChance, "치명타", "Crit", "会心率", "%p");
        Add(bonus.rangePercent, "평타 범위", "Attack range", "通常範囲", "%");
        Add(bonus.basicDamagePercent, "평타 피해", "Basic damage", "通常ダメージ", "%");
        Add(bonus.attackCount, "평타 개수", "Attack count", "通常攻撃数");
        Add(bonus.pierce, "관통", "Pierce", "貫通");
        return parts.Count > 0 ? string.Join(" · ", parts) : CharacterProgression.Copy("추가 보너스 없음", "No bonus", "追加効果なし");
    }

    public void Upgrade()
    {
        if (!CharacterProgression.TryUpgrade(characterId)) { Refresh(); return; }
        SoundManager.Instance?.PlayOneShotIgnoringPause(SoundIds.UpgradeSelect);
        if (UpgradeManager.Instance != null) UpgradeManager.Instance.RefreshPermanentStats();
        Refresh();
    }
}
