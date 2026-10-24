using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UpgradeCooldownHud : MonoBehaviour
{
    private const float EntrySize = 76f;
    private readonly Dictionary<int, CooldownEntry> entries = new Dictionary<int, CooldownEntry>();

    private UpgradeManager manager;
    private RectTransform root;
    private CanvasGroup canvasGroup;

    public static UpgradeCooldownHud FindInScene()
    {
        return FindFirstObjectByType<UpgradeCooldownHud>(FindObjectsInactive.Include);
    }

    public void Initialize(UpgradeManager owner)
    {
        manager = owner;
        root = transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
        if (root == null)
            Debug.LogError("[UpgradeCooldownHud] 씬 오브젝트에 RectTransform이 필요합니다.", this);
        if (canvasGroup == null)
            Debug.LogError("[UpgradeCooldownHud] 씬 오브젝트에 CanvasGroup이 필요합니다.", this);
    }

    public void Track(int upgradeId)
    {
        if (upgradeId <= 0)
            return;

        EnsureRoot();
        if (root == null)
            return;

        if (!entries.TryGetValue(upgradeId, out CooldownEntry entry) || entry.Root == null)
        {
            entry = CreateEntry(upgradeId);
            entries[upgradeId] = entry;
        }

        entry.SetIcon(manager.GetUpgradeIcon(upgradeId));
        RefreshEntry(upgradeId, entry);
    }

    public void ClearAll()
    {
        foreach (CooldownEntry entry in entries.Values)
        {
            if (entry.Root != null)
                Destroy(entry.Root);
        }

        entries.Clear();
        SetVisible(false);
    }

    private void Update()
    {
        if (manager == null)
            manager = UpgradeManager.Instance;

        if (manager == null)
            return;

        EnsureRoot();
        if (root == null)
            return;

        // 조합으로 기존 스킬이 제거되거나 씬이 다시 로드된 경우에도 목록을 자동 동기화한다.
        for (int i = 0; i < manager.skillUpgrades.Count; i++)
            Track(manager.skillUpgrades[i]);

        List<int> removed = null;
        foreach (KeyValuePair<int, CooldownEntry> pair in entries)
        {
            if (!manager.skillUpgrades.Contains(pair.Key))
            {
                if (pair.Value.Root != null)
                    Destroy(pair.Value.Root);
                (removed ??= new List<int>()).Add(pair.Key);
                continue;
            }

            RefreshEntry(pair.Key, pair.Value);
        }

        if (removed != null)
        {
            foreach (int id in removed)
                entries.Remove(id);
        }

        bool hasVisibleEntry = false;
        foreach (CooldownEntry entry in entries.Values)
        {
            if (entry.Root != null && entry.Root.activeSelf)
            {
                hasVisibleEntry = true;
                break;
            }
        }

        SetVisible(hasVisibleEntry);
    }

    private void RefreshEntry(int upgradeId, CooldownEntry entry)
    {
        if (entry.Root == null)
            return;

        int level = manager.GetUpgradeDisplayLevel(upgradeId);
        entry.LevelText.gameObject.SetActive(level > 0);
        entry.LevelText.text = level > 0 ? $"Lv.{level}" : string.Empty;

        if (manager.IsPersistentUpgrade(upgradeId))
        {
            entry.Root.SetActive(true);
            entry.CooldownShade.gameObject.SetActive(false);
            entry.TimeText.gameObject.SetActive(false);
            return;
        }

        bool coolingDown = manager.TryGetUpgradeCooldown(
            upgradeId,
            out float remaining,
            out float duration);

        entry.Root.SetActive(coolingDown);
        if (!coolingDown)
            return;

        entry.CooldownShade.gameObject.SetActive(true);
        entry.TimeText.gameObject.SetActive(true);
        entry.CooldownShade.fillAmount = duration > 0f
            ? Mathf.Clamp01(remaining / duration)
            : 0f;
        entry.TimeText.text = remaining > 2f
            ? Mathf.CeilToInt(remaining).ToString()
            : remaining.ToString("0.0");
    }

    private void EnsureRoot()
    {
        if (root != null)
            return;

        root = transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private CooldownEntry CreateEntry(int upgradeId)
    {
        GameObject entryObject = new GameObject(
            $"Cooldown_{upgradeId}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(UnityEngine.UI.Image),
            typeof(Outline),
            typeof(LayoutElement));
        entryObject.layer = root.gameObject.layer;
        entryObject.transform.SetParent(root, false);

        RectTransform entryRect = (RectTransform)entryObject.transform;
        entryRect.sizeDelta = new Vector2(EntrySize, EntrySize);

        UnityEngine.UI.Image background = entryObject.GetComponent<UnityEngine.UI.Image>();
        background.color = new Color(0f, 0f, 0f, 0.82f);
        background.raycastTarget = false;

        Outline outline = entryObject.GetComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;

        LayoutElement layout = entryObject.GetComponent<LayoutElement>();
        layout.preferredWidth = EntrySize;
        layout.preferredHeight = EntrySize;

        UnityEngine.UI.Image icon = CreateImage("Icon", entryRect, new Vector2(5f, 5f), new Vector2(-5f, -5f));
        icon.preserveAspect = true;
        icon.color = Color.white;

        UnityEngine.UI.Image shade = CreateImage("CooldownShade", entryRect, new Vector2(5f, 5f), new Vector2(-5f, -5f));
        shade.color = new Color(0f, 0f, 0f, 0.72f);
        shade.type = UnityEngine.UI.Image.Type.Filled;
        shade.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
        shade.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
        shade.fillClockwise = false;
        shade.preserveAspect = true;

        TextMeshProUGUI timeText = CreateText(entryRect);
        TextMeshProUGUI levelText = CreateLevelText(entryRect);
        CooldownEntry entry = new CooldownEntry(
            entryObject,
            icon,
            shade,
            timeText,
            levelText);
        entry.SetIcon(manager.GetUpgradeIcon(upgradeId));
        return entry;
    }

    private static UnityEngine.UI.Image CreateImage(
        string name,
        RectTransform parent,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        GameObject imageObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(UnityEngine.UI.Image));
        imageObject.layer = parent.gameObject.layer;
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)imageObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        UnityEngine.UI.Image image = imageObject.GetComponent<UnityEngine.UI.Image>();
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI CreateText(RectTransform parent)
    {
        GameObject textObject = new GameObject(
            "CooldownTime",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = parent.gameObject.layer;
        textObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)textObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = 26f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.outlineColor = Color.black;
        text.outlineWidth = 0.3f;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private static TextMeshProUGUI CreateLevelText(RectTransform parent)
    {
        GameObject textObject = new GameObject(
            "UpgradeLevel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = parent.gameObject.layer;
        textObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)textObject.transform;
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-4f, 3f);
        rect.sizeDelta = new Vector2(54f, 22f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = 17f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.BottomRight;
        text.color = Color.white;
        text.outlineColor = Color.black;
        text.outlineWidth = 0.35f;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private sealed class CooldownEntry
    {
        public readonly GameObject Root;
        public readonly UnityEngine.UI.Image Icon;
        public readonly UnityEngine.UI.Image CooldownShade;
        public readonly TextMeshProUGUI TimeText;
        public readonly TextMeshProUGUI LevelText;

        public CooldownEntry(
            GameObject rootObject,
            UnityEngine.UI.Image icon,
            UnityEngine.UI.Image cooldownShade,
            TextMeshProUGUI timeText,
            TextMeshProUGUI levelText)
        {
            Root = rootObject;
            Icon = icon;
            CooldownShade = cooldownShade;
            TimeText = timeText;
            LevelText = levelText;
        }

        public void SetIcon(Sprite sprite)
        {
            Icon.sprite = sprite;
            CooldownShade.sprite = sprite;
        }
    }
}
