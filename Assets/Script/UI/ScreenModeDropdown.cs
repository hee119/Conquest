using TMPro;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

[RequireComponent(typeof(TMP_Dropdown))]
public sealed class ScreenModeDropdown : MonoBehaviour
{
    private const string ModePreference = "Settings.ScreenMode";

    [SerializeField] private TMP_Dropdown dropdown;
    private ResolutionDropdown resolutionDropdown;

    internal static FullScreenMode PreferredMode => GetFullScreenMode(
        PlayerPrefs.GetInt(ModePreference, GetModeIndex(Screen.fullScreenMode)));

    private void Awake()
    {
        if (dropdown == null)
            dropdown = GetComponent<TMP_Dropdown>();

        int modeIndex = PlayerPrefs.GetInt(ModePreference, GetModeIndex(Screen.fullScreenMode));
        ApplyMode(modeIndex, true);
        NotifyResolutionDropdown();
    }

    private void OnEnable()
    {
        if (dropdown == null)
            dropdown = GetComponent<TMP_Dropdown>();

        if (dropdown != null)
            dropdown.onValueChanged.AddListener(OnModeChanged);
    }

    private void OnDisable()
    {
        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(OnModeChanged);
    }

    private void OnModeChanged(int modeIndex)
    {
        ApplyMode(modeIndex, false);
        NotifyResolutionDropdown();
    }

    private void ApplyMode(int modeIndex, bool initializing)
    {
        if (dropdown == null || dropdown.options.Count == 0)
            return;

        modeIndex = Mathf.Clamp(modeIndex, 0, Mathf.Min(dropdown.options.Count, 3) - 1);
        FullScreenMode mode = GetFullScreenMode(modeIndex);
        ResolutionDropdown.GetPreferredResolution(out int width, out int height);
        if (modeIndex == 2 && Application.platform != RuntimePlatform.WebGLPlayer)
        {
            width = Screen.currentResolution.width;
            height = Screen.currentResolution.height;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // Browser fullscreen requests must run directly inside the user's input event.
        if (!initializing)
            TerraformerWebGLFullscreen.Set(mode != FullScreenMode.Windowed);
#endif
        Screen.SetResolution(width, height, mode);
        PlayerPrefs.SetInt(ModePreference, modeIndex);
        PlayerPrefs.Save();
        dropdown.SetValueWithoutNotify(modeIndex);
        dropdown.RefreshShownValue();
    }

    private void NotifyResolutionDropdown()
    {
        if (resolutionDropdown == null)
            resolutionDropdown = FindFirstObjectByType<ResolutionDropdown>();

        resolutionDropdown?.OnScreenModeChanged();
    }

    private static FullScreenMode GetFullScreenMode(int modeIndex)
    {
        switch (modeIndex)
        {
            case 0:
                if (Application.platform == RuntimePlatform.WindowsPlayer)
                    return FullScreenMode.ExclusiveFullScreen;

                if (Application.platform == RuntimePlatform.OSXPlayer)
                    return FullScreenMode.MaximizedWindow;

                return FullScreenMode.FullScreenWindow;
            case 1:
                return FullScreenMode.Windowed;
            default:
                return FullScreenMode.FullScreenWindow;
        }
    }

    private static int GetModeIndex(FullScreenMode mode)
    {
        if (mode == FullScreenMode.Windowed)
            return 1;

        if (mode == FullScreenMode.FullScreenWindow)
            return 2;

        return 0;
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private static class TerraformerWebGLFullscreen
    {
        [DllImport("__Internal")]
        private static extern void TerraformerSetFullscreen(int fullscreen);

        public static void Set(bool fullscreen)
        {
            TerraformerSetFullscreen(fullscreen ? 1 : 0);
        }
    }
#endif
}
