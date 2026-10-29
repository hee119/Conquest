using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Keeps infrequently changed HUD graphics out of the cooldown canvas rebuilds.
public static class UpgradeHudRendering
{
    public enum TextStyle { CollectedLevel, CooldownTime, CooldownLevel }

    private static TMP_FontAsset font;
    private static readonly Material[] materials = new Material[3];
    private static readonly string[] materialPaths =
    {
        "UI/UpgradeHud/CollectedLevel",
        "UI/UpgradeHud/CooldownTime",
        "UI/UpgradeHud/CooldownLevel"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetReferences()
    {
        font = null;
        for (int i = 0; i < materials.Length; i++)
            materials[i] = null;
    }

    public static void ApplyTextStyle(TextMeshProUGUI text, TextStyle style)
    {
        if (font == null)
            font = Resources.Load<TMP_FontAsset>("UI/UpgradeHud/Font");
        int index = (int)style;
        if (materials[index] == null)
            materials[index] = Resources.Load<Material>(materialPaths[index]);

        // The shared preset includes the outline; per-text outline setters
        // would create another material instance for every acquired upgrade.
        if (font != null && materials[index] != null)
        {
            text.font = font;
            text.fontSharedMaterial = materials[index];
        }
        text.enableAutoSizing = false;
    }

    public static Canvas EnsureCanvas(RectTransform rect)
    {
        Canvas parentCanvas = rect.parent != null
            ? rect.parent.GetComponentInParent<Canvas>()
            : null;
        Canvas canvas = rect.GetComponent<Canvas>();
        if (canvas == null)
            canvas = rect.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = false;
        if (parentCanvas != null)
            canvas.additionalShaderChannels = parentCanvas.additionalShaderChannels;
        return canvas;
    }

    public static void IsolateCollectedIcons(Image[] skillIcons, Image[] statIcons)
    {
        Transform common = null;
        IncludeIcons(skillIcons, ref common);
        IncludeIcons(statIcons, ref common);
        if (common is RectTransform rect)
            EnsureCanvas(rect);
    }

    private static void IncludeIcons(Image[] icons, ref Transform common)
    {
        if (icons == null)
            return;
        foreach (Image icon in icons)
        {
            if (icon == null)
                continue;
            if (common == null)
                common = icon.transform.parent;
            while (common != null && !icon.transform.IsChildOf(common))
                common = common.parent;
        }
    }
}
