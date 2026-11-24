using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UpgradeCooldownHud : MonoBehaviour
{
    private const float EntrySize = 76f;

    private readonly Dictionary<int, CooldownEntry> entries =
        new Dictionary<int, CooldownEntry>();

    private UpgradeManager manager;
    private RectTransform root;
    private CanvasGroup canvasGroup;

    [SerializeField] private RectTransform dynamicOverlay;

    [Header("Level Outline")]
    [SerializeField] private Color levelOutlineColor = Color.black;

    [SerializeField, Range(0f, 1f)]
    private float levelOutlineWidth = 0.2f;

    [SerializeField, Range(-1f, 1f)]
    private float levelFaceDilate = 0.2f;

    private bool layoutDirty;
    private UpgradeManager subscribedManager;

    private void OnEnable()
    {
        UpgradeManager.InstanceChanged += BindManager;
        BindManager(manager != null ? manager : UpgradeManager.Instance);
    }

    private void OnDisable()
    {
        UpgradeManager.InstanceChanged -= BindManager;

        if (subscribedManager != null)
            subscribedManager.UpgradesChanged -= SynchronizeInventory;

        subscribedManager = null;
    }

    private void BindManager(UpgradeManager owner)
    {
        if (subscribedManager != null)
            subscribedManager.UpgradesChanged -= SynchronizeInventory;

        manager = owner;
        subscribedManager = isActiveAndEnabled ? owner : null;

        if (subscribedManager != null)
            subscribedManager.UpgradesChanged += SynchronizeInventory;

        SynchronizeInventory();
    }

    private void OnRectTransformDimensionsChange()
    {
        layoutDirty = true;
    }

    public static UpgradeCooldownHud FindInScene()
    {
        return FindFirstObjectByType<UpgradeCooldownHud>(
            FindObjectsInactive.Include);
    }

    public void Initialize(UpgradeManager owner)
    {
        root = transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();

        if (root == null)
            Debug.LogError(
                "[UpgradeCooldownHud] 씬 오브젝트에 RectTransform이 필요합니다.",
                this);

        if (canvasGroup == null)
            Debug.LogError(
                "[UpgradeCooldownHud] 씬 오브젝트에 CanvasGroup이 필요합니다.",
                this);

        EnsureRoot();

        if (manager != owner ||
            subscribedManager != (isActiveAndEnabled ? owner : null))
        {
            BindManager(owner);
        }
    }

    public void Track(int upgradeId)
    {
        if (upgradeId <= 0)
            return;

        EnsureRoot();

        if (root == null)
            return;

        if (!entries.TryGetValue(upgradeId, out CooldownEntry entry) ||
            entry.Root == null)
        {
            entry = CreateEntry(upgradeId);
            entries[upgradeId] = entry;
            layoutDirty = true;
        }

        MatchCollectedIcon(upgradeId, entry);
        RefreshEntry(upgradeId, entry);
    }

    public void ClearAll()
    {
        foreach (CooldownEntry entry in entries.Values)
        {
            if (entry.Root != null)
                Destroy(entry.Root);

            if (entry.DynamicRoot != null)
                Destroy(entry.DynamicRoot.gameObject);
        }

        entries.Clear();
        layoutDirty = true;

        SetVisible(false);
    }

    private void Update()
    {
        if (manager == null || root == null)
            return;

        foreach (KeyValuePair<int, CooldownEntry> pair in entries)
            RefreshEntry(pair.Key, pair.Value);

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

    private void SynchronizeInventory()
    {
        if (manager == null)
        {
            ClearAll();
            return;
        }

        EnsureRoot();

        if (root == null)
            return;

        for (int i = 0; i < manager.skillUpgrades.Count; i++)
        {
            int id = manager.skillUpgrades[i];
            Track(id);
        }

        List<int> removed = null;

        foreach (KeyValuePair<int, CooldownEntry> pair in entries)
        {
            if (!manager.skillUpgrades.Contains(pair.Key))
            {
                if (pair.Value.Root != null)
                    Destroy(pair.Value.Root);

                if (pair.Value.DynamicRoot != null)
                    Destroy(pair.Value.DynamicRoot.gameObject);

                layoutDirty = true;

                (removed ??= new List<int>()).Add(pair.Key);
            }
        }

        if (removed != null)
        {
            foreach (int id in removed)
                entries.Remove(id);
        }
    }

    private void LateUpdate()
    {
        if (!layoutDirty || root == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(root);

        foreach (CooldownEntry entry in entries.Values)
        {
            if (entry.Root == null || entry.DynamicRoot == null)
                continue;

            RectTransform source =
                (RectTransform)entry.Root.transform;

            RectTransform target = entry.DynamicRoot;

            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.sizeDelta = source.sizeDelta;
            target.anchoredPosition = source.anchoredPosition;
            target.localScale = source.localScale;
            target.localRotation = source.localRotation;
        }

        layoutDirty = false;
    }

    private void SetEntryVisible(CooldownEntry entry, bool visible)
    {
        if (entry.Root.activeSelf == visible)
            return;

        entry.Root.SetActive(visible);
        entry.DynamicRoot.gameObject.SetActive(visible);

        layoutDirty = true;
    }

    private void RefreshEntry(int upgradeId, CooldownEntry entry)
    {
        if (entry.Root == null)
            return;

        int level = manager.GetUpgradeDisplayLevel(upgradeId);
        entry.SetLevel(level);

        if (manager.IsPersistentUpgrade(upgradeId))
        {
            SetEntryVisible(entry, true);

            SetActive(entry.CooldownShade.gameObject, false);
            SetActive(entry.TimeText.gameObject, false);

            return;
        }

        bool coolingDown = manager.TryGetUpgradeCooldown(
            upgradeId,
            out float remaining,
            out float duration);

        SetEntryVisible(entry, coolingDown);

        if (!coolingDown)
            return;

        SetActive(entry.CooldownShade.gameObject, true);
        SetActive(entry.TimeText.gameObject, true);

        entry.CooldownShade.fillAmount = duration > 0f
            ? Mathf.Clamp01(remaining / duration)
            : 0f;

        entry.SetRemainingTime(remaining);
    }

    private void MatchCollectedIcon(int upgradeId, CooldownEntry entry)
    {
        int index = manager.skillUpgrades.IndexOf(upgradeId);

        var icons = manager.skillUpgradeIcons;

        var source = icons != null &&
                     index >= 0 &&
                     index < icons.Length
            ? icons[index]
            : null;

        entry.SetIcon(
            source != null
                ? source.sprite
                : manager.GetUpgradeIcon(upgradeId));

        if (source == null)
            return;

        entry.Icon.material = source.material;
        entry.Icon.color = source.color;
        entry.Icon.preserveAspect = source.preserveAspect;
    }

    private void EnsureRoot()
    {
        if (root == null)
            root = transform as RectTransform;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (dynamicOverlay == null && root != null)
        {
            dynamicOverlay =
                root.Find("Dynamic Cooldown Overlay") as RectTransform;
        }
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            return;

        float alpha = visible ? 1f : 0f;

        if (canvasGroup.alpha != alpha)
            canvasGroup.alpha = alpha;

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

        RectTransform entryRect =
            (RectTransform)entryObject.transform;

        entryRect.sizeDelta = new Vector2(EntrySize, EntrySize);

        UnityEngine.UI.Image background =
            entryObject.GetComponent<UnityEngine.UI.Image>();

        background.color = Color.black;
        background.raycastTarget = false;

        Outline outline = entryObject.GetComponent<Outline>();

        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(2f, 2f);
        outline.useGraphicAlpha = true;

        LayoutElement layout =
            entryObject.GetComponent<LayoutElement>();

        layout.preferredWidth = EntrySize;
        layout.preferredHeight = EntrySize;

        UnityEngine.UI.Image icon = CreateImage(
            "Icon",
            entryRect,
            new Vector2(5f, 5f),
            new Vector2(-5f, -5f));

        icon.preserveAspect = true;
        icon.color = Color.white;

        UpgradeHudRendering.ApplyCollectedIconStyle(icon, upgradeId);

        GameObject dynamicObject = new GameObject(
            $"Cooldown_{upgradeId}_Dynamic",
            typeof(RectTransform));

        dynamicObject.layer = root.gameObject.layer;

        RectTransform dynamicRect =
            (RectTransform)dynamicObject.transform;

        dynamicRect.SetParent(dynamicOverlay, false);
        dynamicRect.sizeDelta = new Vector2(EntrySize, EntrySize);

        dynamicOverlay.SetAsLastSibling();

        UnityEngine.UI.Image shade = CreateImage(
            "CooldownShade",
            dynamicRect,
            new Vector2(5f, 5f),
            new Vector2(-5f, -5f));

        shade.color = new Color(0f, 0f, 0f, 0.8f);
        shade.type = UnityEngine.UI.Image.Type.Filled;
        shade.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
        shade.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
        shade.fillClockwise = false;
        shade.preserveAspect = true;

        TextMeshProUGUI timeText = CreateText(dynamicRect);
        TextMeshProUGUI levelText = CreateLevelText(dynamicRect);

        CooldownEntry entry = new CooldownEntry(
            entryObject,
            dynamicRect,
            icon,
            shade,
            timeText,
            levelText);

        MatchCollectedIcon(upgradeId, entry);

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

        RectTransform rect =
            (RectTransform)imageObject.transform;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        UnityEngine.UI.Image image =
            imageObject.GetComponent<UnityEngine.UI.Image>();

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

        RectTransform rect =
            (RectTransform)textObject.transform;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text =
            textObject.GetComponent<TextMeshProUGUI>();

        text.fontSize = 26f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        UpgradeHudRendering.ApplyTextStyle(
            text,
            UpgradeHudRendering.TextStyle.CooldownTime);

        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        return text;
    }

    private TextMeshProUGUI CreateLevelText(RectTransform parent)
    {
        GameObject textObject = new GameObject(
            "UpgradeLevel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));

        textObject.layer = parent.gameObject.layer;
        textObject.transform.SetParent(parent, false);

        RectTransform rect =
            (RectTransform)textObject.transform;

        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-4f, 3f);
        rect.sizeDelta = new Vector2(54f, 22f);

        TextMeshProUGUI text =
            textObject.GetComponent<TextMeshProUGUI>();

        text.fontSize = 17f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.BottomRight;
        text.color = Color.white;

        UpgradeHudRendering.ApplyTextStyle(
            text,
            UpgradeHudRendering.TextStyle.CooldownLevel);

        // 레벨 텍스트 머티리얼 설정
        ApplyLevelOutline(text);

        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        return text;
    }

    private void ApplyLevelOutline(TextMeshProUGUI text)
    {
        Material sharedMaterial = text.fontSharedMaterial;

        if (sharedMaterial == null)
            return;

        // 윤곽선과 Dilate를 지원하는 셰이더인지 확인
        if (!sharedMaterial.HasProperty(ShaderUtilities.ID_OutlineWidth) ||
            !sharedMaterial.HasProperty(ShaderUtilities.ID_OutlineColor) ||
            !sharedMaterial.HasProperty(ShaderUtilities.ID_FaceDilate))
        {
            return;
        }

        // 원본 머티리얼에 영향을 주지 않도록 개별 머티리얼 사용
        text.outlineColor = levelOutlineColor;
        text.outlineWidth = levelOutlineWidth;

        Material material = text.fontMaterial;

        // 글자 자체의 두께 설정
        material.SetFloat(
            ShaderUtilities.ID_FaceDilate,
            levelFaceDilate);

        // 윤곽선 영역이 잘리지 않도록 패딩 갱신
        text.UpdateMeshPadding();

        text.SetVerticesDirty();
        text.SetMaterialDirty();
    }

    private sealed class CooldownEntry
    {
        public readonly GameObject Root;
        public readonly RectTransform DynamicRoot;
        public readonly UnityEngine.UI.Image Icon;
        public readonly UnityEngine.UI.Image CooldownShade;
        public readonly TextMeshProUGUI TimeText;
        public readonly TextMeshProUGUI LevelText;

        private int displayedLevel = -1;
        private int displayedTime = -1;
        private bool decimalTime;

        public CooldownEntry(
            GameObject rootObject,
            RectTransform dynamicRoot,
            UnityEngine.UI.Image icon,
            UnityEngine.UI.Image cooldownShade,
            TextMeshProUGUI timeText,
            TextMeshProUGUI levelText)
        {
            Root = rootObject;
            DynamicRoot = dynamicRoot;
            Icon = icon;
            CooldownShade = cooldownShade;
            TimeText = timeText;
            LevelText = levelText;
        }

        public void SetIcon(Sprite sprite)
        {
            if (Icon.sprite != sprite)
                Icon.sprite = sprite;

            if (CooldownShade.sprite != sprite)
                CooldownShade.sprite = sprite;
        }

        public void SetLevel(int level)
        {
            if (displayedLevel == level)
                return;

            displayedLevel = level;

            SetActive(LevelText.gameObject, level > 0);

            LevelText.text = level > 0
                ? $"Lv.{level}"
                : string.Empty;
        }

        public void SetRemainingTime(float remaining)
        {
            bool useDecimal = remaining <= 2f;

            int value = useDecimal
                ? Mathf.RoundToInt(remaining * 10f)
                : Mathf.CeilToInt(remaining);

            if (value == displayedTime && decimalTime == useDecimal)
                return;

            displayedTime = value;
            decimalTime = useDecimal;

            TimeText.text = useDecimal
                ? remaining.ToString("0.0")
                : value.ToString();
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target.activeSelf != active)
            target.SetActive(active);
    }
}