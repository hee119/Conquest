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

    public static UpgradeCooldownHud Attach(UpgradeManager owner)
    {
        UpgradeCooldownHud hud = owner.GetComponent<UpgradeCooldownHud>();
        if (hud == null)
            hud = owner.gameObject.AddComponent<UpgradeCooldownHud>();

        hud.manager = owner;
        hud.EnsureRoot();
        return hud;
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
        if (root != null)
            root.gameObject.SetActive(false);
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

        root.gameObject.SetActive(hasVisibleEntry);
    }

    private void RefreshEntry(int upgradeId, CooldownEntry entry)
    {
        if (entry.Root == null)
            return;

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

        entries.Clear();
        RectTransform targetParent = FindTargetParent();
        if (targetParent == null)
            return;

        GameObject rootObject = new GameObject(
            "UpgradeCooldownHUD",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(HorizontalLayoutGroup));
        rootObject.layer = LayerMask.NameToLayer("UI");
        rootObject.transform.SetParent(targetParent, false);

        root = (RectTransform)rootObject.transform;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.zero;
        root.pivot = Vector2.zero;
        root.anchoredPosition = new Vector2(24f, 54f);
        root.sizeDelta = new Vector2(620f, EntrySize);

        CanvasGroup group = rootObject.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        HorizontalLayoutGroup layout = rootObject.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.LowerLeft;
        layout.spacing = 10f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        rootObject.SetActive(false);
    }

    private RectTransform FindTargetParent()
    {
        if (manager.skillUpgradeIcons != null)
        {
            foreach (UnityEngine.UI.Image image in manager.skillUpgradeIcons)
            {
                if (image == null)
                    continue;

                Transform current = image.transform.parent;
                while (current != null)
                {
                    if (current.name == "Safe Area" && current is RectTransform safeArea)
                        return safeArea;

                    current = current.parent;
                }

                if (image.canvas != null)
                    return image.canvas.transform as RectTransform;
            }
        }

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null && canvas.isRootCanvas)
                return canvas.transform as RectTransform;
        }

        return null;
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
        CooldownEntry entry = new CooldownEntry(entryObject, icon, shade, timeText);
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

    private sealed class CooldownEntry
    {
        public readonly GameObject Root;
        public readonly UnityEngine.UI.Image Icon;
        public readonly UnityEngine.UI.Image CooldownShade;
        public readonly TextMeshProUGUI TimeText;

        public CooldownEntry(
            GameObject rootObject,
            UnityEngine.UI.Image icon,
            UnityEngine.UI.Image cooldownShade,
            TextMeshProUGUI timeText)
        {
            Root = rootObject;
            Icon = icon;
            CooldownShade = cooldownShade;
            TimeText = timeText;
        }

        public void SetIcon(Sprite sprite)
        {
            Icon.sprite = sprite;
            CooldownShade.sprite = sprite;
        }
    }
}
