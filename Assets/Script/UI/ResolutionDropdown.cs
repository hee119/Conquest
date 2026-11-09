using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public sealed class ResolutionDropdown : MonoBehaviour
{
    internal const string WidthPreference = "Settings.WindowWidth";
    internal const string HeightPreference = "Settings.WindowHeight";
    internal const int DefaultWidth = 1280;
    internal const int DefaultHeight = 720;

    private static readonly Vector2Int[] CommonResolutions =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1366, 768),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440),
        new Vector2Int(3840, 2160)
    };

    [SerializeField] private TMP_Dropdown dropdown;

    private void OnEnable()
    {
        CacheDropdown();
        if (dropdown == null)
            return;

        EnsureOptions();
        GetPreferredResolution(out int width, out int height);
        int selectedIndex = FindClosestResolution(width, height);

        dropdown.SetValueWithoutNotify(selectedIndex);
        dropdown.RefreshShownValue();
        dropdown.interactable = true;
        dropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    private void OnDisable()
    {
        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(OnResolutionChanged);
    }

    internal static void GetPreferredResolution(out int width, out int height)
    {
        width = PlayerPrefs.GetInt(WidthPreference, DefaultWidth);
        height = PlayerPrefs.GetInt(HeightPreference, DefaultHeight);
    }

    private void CacheDropdown()
    {
        if (dropdown == null)
            dropdown = GetComponent<TMP_Dropdown>();
    }

    private void EnsureOptions()
    {
        if (dropdown.options.Count == CommonResolutions.Length)
            return;

        dropdown.ClearOptions();
        foreach (Vector2Int resolution in CommonResolutions)
            dropdown.options.Add(new TMP_Dropdown.OptionData($"{resolution.x} × {resolution.y}"));
    }

    private void OnResolutionChanged(int index)
    {
        ApplyResolution(index);
    }

    private void ApplyResolution(int index)
    {
        index = Mathf.Clamp(index, 0, CommonResolutions.Length - 1);
        Vector2Int resolution = CommonResolutions[index];
        SaveResolution(index);
        // Mode changes are applied at the end of the frame. Use the requested
        // mode so a quick resolution change cannot cancel a pending mode switch.
        ScreenModeDropdown.ApplyResolution(resolution.x, resolution.y, ScreenModeDropdown.PreferredMode);
    }

    private void SaveResolution(int index)
    {
        Vector2Int resolution = CommonResolutions[index];
        PlayerPrefs.SetInt(WidthPreference, resolution.x);
        PlayerPrefs.SetInt(HeightPreference, resolution.y);
        PlayerPrefs.Save();
    }

    private int FindClosestResolution(int width, int height)
    {
        int closestIndex = 0;
        long closestDistance = long.MaxValue;
        for (int i = 0; i < CommonResolutions.Length; i++)
        {
            long widthDifference = CommonResolutions[i].x - (long)width;
            long heightDifference = CommonResolutions[i].y - (long)height;
            long distance = widthDifference * widthDifference + heightDifference * heightDifference;
            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            closestIndex = i;
        }

        return closestIndex;
    }
}
