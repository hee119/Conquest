using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum InterfaceScaleCategory { Buttons, Sliders, Text, Icons, Vitals, Experience }

/// <summary>Scales individual UI elements while retaining authored transforms.</summary>
public sealed class InterfaceScaleManager : MonoBehaviour
{
    public const float MinScale = 0.5f;
    public const float MaxScale = 2f;
    private const string PreferencePrefix = "Settings.InterfaceScale.";
    private static InterfaceScaleManager instance;
    public static InterfaceScaleManager Current => instance;
    public static InterfaceScaleManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<InterfaceScaleManager>(FindObjectsInactive.Include);
            if (instance == null)
                Debug.LogError("씬에 InterfaceScaleManager를 배치해야 합니다.");
            return instance;
        }
    }

    private sealed class Target
    {
        public RectTransform Rect;
        public InterfaceScaleCategory Category;
        public Vector3 BaseScale;
        public float AppliedScale = 1f;
        public bool WasActive;
        public int Depth;
    }

    private readonly float[] scales = new float[6];
    private readonly Dictionary<RectTransform, Target> targets = new Dictionary<RectTransform, Target>();
    private readonly List<Target> orderedTargets = new List<Target>();
    private readonly List<RectTransform> removedTargets = new List<RectTransform>();
    private readonly List<UnityEngine.UI.Graphic> graphics = new List<UnityEngine.UI.Graphic>();
    private readonly Vector3[] corners = new Vector3[4];
    private bool dirty = true;
    private float nextScan;
    private int screenWidth, screenHeight;
    private Rect safeArea;
    private LocalizationManager localization;
    public event Action<InterfaceScaleCategory, float> ScaleChanged;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        for (int i = 0; i < scales.Length; i++)
            scales[i] = Mathf.Clamp(PlayerPrefs.GetFloat(PreferencePrefix + (InterfaceScaleCategory)i, 1f), MinScale, MaxScale);
        SceneManager.sceneLoaded += OnSceneLoaded;
        Canvas.willRenderCanvases += ApplyPending;
    }

    public float GetScale(InterfaceScaleCategory category) => scales[(int)category];

    public void SetScale(InterfaceScaleCategory category, float value)
    {
        value = Mathf.Clamp(value, MinScale, MaxScale);
        if (Mathf.Approximately(scales[(int)category], value)) return;
        scales[(int)category] = value;
        PlayerPrefs.SetFloat(PreferencePrefix + category, value);
        ScanTargets();
        dirty = true;
        ScaleChanged?.Invoke(category, value);
    }

    public void RefreshTargets() { ScanTargets(); dirty = true; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RefreshTargets();

    private void LateUpdate()
    {
        if (localization != LocalizationManager.Instance)
        {
            if (localization != null) localization.OnLanguageChanged -= OnLanguageChanged;
            localization = LocalizationManager.Instance;
            if (localization != null) localization.OnLanguageChanged += OnLanguageChanged;
        }
        if (Screen.width != screenWidth || Screen.height != screenHeight || Screen.safeArea != safeArea)
        {
            screenWidth = Screen.width; screenHeight = Screen.height; safeArea = Screen.safeArea;
            dirty = true;
        }
        // Pick up newly generated cooldown icons and pooled UI without scanning every frame.
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + 1f;
            ScanTargets();
        }
        foreach (Target target in orderedTargets)
        {
            if (target.Rect == null) continue;
            bool active = target.Rect.gameObject.activeInHierarchy;
            if (active != target.WasActive) { target.WasActive = active; dirty = true; }
        }
    }

    private void OnLanguageChanged() => dirty = true;

    private void ScanTargets()
    {
        removedTargets.Clear();
        foreach (var pair in targets) if (pair.Key == null) removedTargets.Add(pair.Key);
        foreach (RectTransform rect in removedTargets) targets.Remove(rect);
        bool changed = removedTargets.Count > 0;
        foreach (RectTransform rect in FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (targets.ContainsKey(rect) || !rect.gameObject.scene.IsValid() ||
                rect.GetComponentInParent<Canvas>(true) == null ||
                rect.GetComponentInParent<InterfaceScaleControl>(true) != null ||
                !TryGetCategory(rect, out InterfaceScaleCategory category)) continue;
            int depth = 0;
            for (Transform parent = rect.parent; parent != null; parent = parent.parent) depth++;
            targets.Add(rect, new Target { Rect = rect, Category = category, BaseScale = rect.localScale, Depth = depth });
            changed = true;
        }
        if (!changed) return;
        orderedTargets.Clear(); orderedTargets.AddRange(targets.Values);
        orderedTargets.Sort((a, b) => a.Depth.CompareTo(b.Depth));
        dirty = true;
    }

    private static bool TryGetCategory(RectTransform rect, out InterfaceScaleCategory category)
    {
        category = InterfaceScaleCategory.Buttons;
        if (rect.GetComponent<HpBar>() != null || rect.GetComponent<DashBar>() != null)
        { category = InterfaceScaleCategory.Vitals; return true; }
        if (rect.GetComponent<ExpBar>() != null)
        { category = InterfaceScaleCategory.Experience; return true; }
        if (rect.GetComponent<UnityEngine.UI.Slider>() != null)
        {
            // HP's ghost fill is an internal slider, not a separate interface element.
            if (rect.parent != null && rect.parent.GetComponentInParent<UnityEngine.UI.Slider>(true) != null) return false;
            category = InterfaceScaleCategory.Sliders; return true;
        }
        if (rect.GetComponent<UnityEngine.UI.Button>() != null || rect.GetComponent<TMP_Dropdown>() != null ||
            rect.GetComponent<UnityEngine.UI.Toggle>() != null) return true;
        if (rect.GetComponent<TMP_Text>() != null)
        { category = InterfaceScaleCategory.Text; return true; }
        var image = rect.GetComponent<UnityEngine.UI.Image>();
        if (image == null || image.sprite == null || image.type == UnityEngine.UI.Image.Type.Sliced ||
            image.sprite.border != Vector4.zero || rect.GetComponentInParent<UnityEngine.UI.Slider>(true) != null ||
            rect.GetComponentInParent<TMP_Dropdown>(true) != null || rect.GetComponentInParent<TMP_Text>(true) != null) return false;
        // Large decorative panels are not icons.
        if (rect.rect.width > 512f || rect.rect.height > 512f) return false;
        string name = rect.name.ToLowerInvariant();
        if (name.Contains("background") || name.Contains("frame") || name.Contains("outline") ||
            name.Contains("glow") || name.Contains("mask") || name.Contains("line") || name.Contains("fill")) return false;
        if (rect.GetComponentInParent<UnityEngine.UI.Button>(true) != null && !name.Contains("icon")) return false;
        category = InterfaceScaleCategory.Icons;
        return true;
    }

    private void ApplyPending()
    {
        bool applyAll = dirty;
        dirty = false;
        foreach (Target target in orderedTargets)
        {
            // World-space HP/dash bars move with their owner, so check just these
            // between setting changes instead of rebuilding the whole HUD every frame.
            if (!applyAll && target.Category != InterfaceScaleCategory.Vitals) continue;
            if (target.Rect == null || !target.Rect.gameObject.activeInHierarchy) continue;
            float requested = scales[(int)target.Category];
            if (!applyAll && requested <= 1f && Mathf.Approximately(target.AppliedScale, requested)) continue;
            float parentScale = GetAncestorScale(target.Rect);
            // Text/icons inside a button use their own setting rather than
            // multiplying the parent and child settings (200% must not become 400%).
            target.Rect.localScale = Scaled(target.BaseScale, requested / parentScale);
            if (requested > 1f && !FitsScreen(target.Rect))
            {
                // Only this element rejects the increase; other elements can still grow.
                target.Rect.localScale = Scaled(target.BaseScale, target.AppliedScale / parentScale);
                if (target.AppliedScale > 1f && !FitsScreen(target.Rect))
                {
                    target.Rect.localScale = Scaled(target.BaseScale, 1f / parentScale);
                    target.AppliedScale = 1f;
                }
                continue;
            }
            target.AppliedScale = requested;
        }
    }

    private static Vector3 Scaled(Vector3 original, float factor) => new Vector3(original.x * factor, original.y * factor, original.z);

    private float GetAncestorScale(RectTransform rect)
    {
        for (Transform parent = rect.parent; parent != null; parent = parent.parent)
            if (parent is RectTransform parentRect && targets.TryGetValue(parentRect, out Target target))
                return Mathf.Max(0.01f, target.AppliedScale);
        return 1f;
    }

    private bool IsManagedChild(RectTransform child, RectTransform owner)
    {
        for (Transform current = child; current != null && current != owner; current = current.parent)
            if (current is RectTransform rect && targets.ContainsKey(rect)) return true;
        return false;
    }

    private bool FitsScreen(RectTransform rect)
    {
        Canvas canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        Rect viewport = camera != null ? camera.pixelRect : new Rect(0, 0, Screen.width, Screen.height);
        Rect area = Screen.safeArea;
        if (area.width > 0f && area.height > 0f)
            viewport = Rect.MinMaxRect(Mathf.Max(viewport.xMin, area.xMin), Mathf.Max(viewport.yMin, area.yMin),
                Mathf.Min(viewport.xMax, area.xMax), Mathf.Min(viewport.yMax, area.yMax));
        if (!FitsRect(rect, camera, viewport)) return false;
        graphics.Clear(); rect.GetComponentsInChildren(false, graphics);
        foreach (var graphic in graphics)
            if (graphic.enabled && graphic.color.a > 0f && !IsManagedChild(graphic.rectTransform, rect) &&
                !FitsRect(graphic.rectTransform, camera, viewport)) return false;
        return true;
    }

    private bool FitsRect(RectTransform rect, Camera camera, Rect viewport)
    {
        rect.GetWorldCorners(corners);
        foreach (Vector3 corner in corners)
        {
            if (camera != null && camera.WorldToScreenPoint(corner).z <= 0f) return false;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corner);
            if (point.x < viewport.xMin - 0.5f || point.x > viewport.xMax + 0.5f ||
                point.y < viewport.yMin - 0.5f || point.y > viewport.yMax + 0.5f) return false;
        }
        return true;
    }

    private void OnApplicationPause(bool pause) { if (pause) PlayerPrefs.Save(); }
    private void OnApplicationQuit() => PlayerPrefs.Save();
    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Canvas.willRenderCanvases -= ApplyPending;
        if (localization != null) localization.OnLanguageChanged -= OnLanguageChanged;
        instance = null;
    }
}
