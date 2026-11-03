using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shared materials and text styles for the authored upgrade HUD.
/// <summary>UpgradeHudRendering 증강 관련 동작과 상태를 관리한다.</summary>
public static class UpgradeHudRendering
{
    public enum TextStyle { CollectedLevel, CooldownTime, CooldownLevel }

    private static TMP_FontAsset font;
    private static Material collectedSkillIconMaterial;
    private static Material collectedStatIconMaterial;
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
        collectedSkillIconMaterial = null;
        collectedStatIconMaterial = null;
        for (int i = 0; i < materials.Length; i++)
            materials[i] = null;
    }

    /// <summary>ApplyCollectedIconStyle 동작을 처리한다.</summary>
    public static void ApplyCollectedIconStyle(Image image, int upgradeId)
    {
        if (image == null)
            return;
        if (collectedSkillIconMaterial == null)
            collectedSkillIconMaterial = Resources.Load<Material>("UI/UpgradeHud/CollectedSkillIcon");
        if (collectedSkillIconMaterial != null)
        {
            bool isStat = DataManager.Instance != null && DataManager.Instance.statUpgradeIconDic.ContainsKey(upgradeId);
            if (isStat && collectedStatIconMaterial == null)
            {
                collectedStatIconMaterial = new Material(collectedSkillIconMaterial);
                collectedStatIconMaterial.name = "Collected Stat Icon";
                collectedStatIconMaterial.SetFloat("_UseDarkMask", 1f);
            }
            image.material = isStat ? collectedStatIconMaterial : collectedSkillIconMaterial;
            image.color = Color.white;
        }
    }

    /// <summary>ApplyTextStyle 동작을 처리한다.</summary>
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

}
