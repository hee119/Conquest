using System.Globalization;
using TMPro;
using UnityEngine;

public sealed class CharacterSummaryStats : MonoBehaviour
{
    [SerializeField] private TMP_Text hpValue;
    [SerializeField] private TMP_Text attackValue;
    [SerializeField] private TMP_Text cooldownValue;
    private int characterId = 1;
    private LocalizationManager localization;
    private static readonly System.Collections.Generic.Dictionary<int, float> BaseCooldowns = new();

    private void OnEnable()
    {
        RefreshLayout();
        CharacterProgression.Changed += Refresh;
        LocalizationManager.InstanceChanged += BindLanguage;
        BindLanguage(LocalizationManager.Instance);
    }
    private void OnDisable()
    {
        CharacterProgression.Changed -= Refresh;
        LocalizationManager.InstanceChanged -= BindLanguage;
        BindLanguage(null);
    }
    private void Start() => RefreshLayout();
    private void RefreshLayout()
    {
        RefreshIconLayout();
    }
    private void RefreshIconLayout()
    {
        foreach (Transform row in transform)
        {
            var tile = row.Find("Icon Tile") as RectTransform;
            var label = row.Find("Label") as RectTransform;
            var value = row.Find("Value") as RectTransform;
            var rect = row as RectTransform;
            if (label == null || value == null || rect == null) continue;
            if (tile != null) tile.gameObject.SetActive(false);
            foreach (var text in row.GetComponentsInChildren<TMP_Text>(true))
            {
                text.enableAutoSizing = true;
                text.fontSizeMin = 11f;
                text.fontSizeMax = 18f;
            }
            float left = rect.rect.xMin + 13f;
            label.pivot = new Vector2(0f, 0.5f);
            Vector3 position = label.localPosition; position.x = left; label.localPosition = position;
            value.pivot = new Vector2(1f, 0.5f);
            position = value.localPosition; position.x = rect.rect.xMax - 13f; value.localPosition = position;
            value.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 225f);
            float available = position.x - value.rect.width * Mathf.Abs(value.localScale.x) - left - 12f;
            label.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                Mathf.Max(80f, available / Mathf.Max(0.01f, Mathf.Abs(label.localScale.x))));
        }
    }

    private void BindLanguage(LocalizationManager manager)
    {
        if (localization != null) localization.OnLanguageChanged -= Refresh;
        localization = manager;
        if (localization != null) localization.OnLanguageChanged += Refresh;
        Refresh();
    }
    public void SetCharacter(int id) { characterId = id; Refresh(); }

    public static float BaseCooldown(int id)
    {
        if (BaseCooldowns.Count == 0)
        {
            var csv = Resources.Load<TextAsset>("TextData/SkillData");
            if (csv != null) foreach (var line in csv.text.Split('\n'))
            {
                var fields = line.Split(',');
                if (fields.Length > 3 && int.TryParse(fields[0], out int skill) &&
                    float.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float cooldown))
                    BaseCooldowns[skill] = cooldown;
            }
        }
        return BaseCooldowns.TryGetValue(id == 2 ? 201 : 200, out float value) ? value : 0f;
    }
    public void Refresh()
    {
        int level = CharacterProgression.Level(characterId);
        if (!CharacterEnhancementTable.TryGetStats(characterId, level, out var current)) return;
        CharacterEnhancementTable.TryGetStats(characterId, level + 1, out var next);
        bool max = level >= CharacterProgression.Settings.maxLevel;
        string N(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        string Value(float currentValue, float nextValue, string suffix = "")
        {
            float delta = nextValue - currentValue;
            string result = "<b>" + N(currentValue) + suffix + "</b>";
            if (!max && Mathf.Abs(delta) > 0.0001f)
                result += "  <color=#75F58A>" + (delta > 0 ? "+" : "") + N(delta) + suffix + "</color>";
            return result;
        }
        if (hpValue != null) hpValue.text = Value(current.hp, next.hp);
        if (attackValue != null) attackValue.text = Value(current.attack, next.attack);
        string seconds = localization != null ? localization.GetTranslation("character_stat_seconds") : "초";
        float basicCooldown = BaseCooldown(characterId);
        if (cooldownValue != null) cooldownValue.text = Value(
            basicCooldown / Mathf.Max(.1f, current.attackSpeed),
            basicCooldown / Mathf.Max(.1f, next.attackSpeed), seconds);
    }
}
