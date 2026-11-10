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
        UiSoundEvents.UseDedicatedClick(upgradeButton);
        CharacterProgression.Changed += Refresh;
        localization = LocalizationManager.Instance;
        if (localization != null) localization.OnLanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        CharacterProgression.Changed -= Refresh;
        if (localization != null) localization.OnLanguageChanged -= Refresh;
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
            bonusText.text = Describe(CharacterProgression.Bonuses(characterId, c.level));
        }
        if (costText != null)
        {
            string Cost(string label, int owned, int required)
            {
                string value = $"{label} {required:N0}";
                return owned < required ? $"<color=#FF4545>{value}</color>" : value;
            }
            costText.richText = true;
            costText.text = maxed ? Copy("최대 강화 완료", "Fully enhanced", "最大強化済み") :
                Cost(Copy("증표", "Tokens", "証"), c.fragments, config.FragmentCost(c.level)) +
                "  +  " + Cost(Copy("골드", "Gold", "ゴールド"), CharacterProgression.State.gold, config.GoldCost(c.level));
        }
        if (statusText != null)
        {
            statusText.text = maxed ? string.Empty : Copy("다음 강화: ", "Next: ", "次の強化: ") +
                Describe(CharacterProgression.Bonuses(characterId, c.level + 1, false));
        }
        if (buttonText != null) buttonText.text = maxed ? Copy("완료", "MAX", "完了") : Copy("강화", "Enhance", "強化");
        if (upgradeButton != null) upgradeButton.interactable = CharacterProgression.CanUpgrade(characterId);
    }

    private static string Describe(CharacterEnhancementBonus bonus)
    {
        var parts = new System.Collections.Generic.List<string>();
        void Add(float value, string ko, string en, string jp, string unit = "")
        {
            if (value > 0) parts.Add(CharacterProgression.Copy(ko, en, jp) + $" +{value:0.#}{unit}");
        }
        Add(bonus.hp, "체력", "HP", "HP");
        Add(bonus.hpRegenRate, "체력 재생", "HP regen", "HP回復");
        Add(bonus.speed, "이동 속도", "Speed", "移動速度");
        Add(bonus.attack, "공격력", "Attack", "攻撃力");
        Add(bonus.defense, "방어력", "Defense", "防御力");
        Add(bonus.criticalDamage, "치명타 피해", "Critical damage", "会心ダメージ");
        Add(bonus.criticalChance, "치명타 확률", "Critical chance", "会心率");
        Add(bonus.pierce, "관통 수", "Pierce", "貫通数");
        Add(bonus.attackRange, "공격 범위 배율", "Attack range multiplier", "攻撃範囲倍率");
        Add(bonus.attackSpeed, "공격 속도 배율", "Attack speed multiplier", "攻撃速度倍率");
        return parts.Count > 0 ? string.Join(" · ", parts) : CharacterProgression.Copy("추가 보너스 없음", "No bonus", "追加効果なし");
    }

    public void Upgrade()
    {
        if (!CharacterProgression.TryUpgrade(characterId)) { Refresh(); return; }
        UiSoundEvents.Request(UiSoundCue.Upgrade);
        if (UpgradeManager.Instance != null) UpgradeManager.Instance.RefreshPermanentStats();
        Refresh();
    }
}
