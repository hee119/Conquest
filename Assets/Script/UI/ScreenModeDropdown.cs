using TMPro;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

[RequireComponent(typeof(TMP_Dropdown))]
public sealed class ScreenModeDropdown : MonoBehaviour
{
    private const string ModePreference = "Settings.ScreenMode";
    private static FullScreenMode? requestedMode;
    private static int requestFrame;
    private static float requestDeadline;

    [SerializeField] private TMP_Dropdown dropdown;

    internal static FullScreenMode PreferredMode
    {
        get
        {
            int modeIndex = ReadModeIndex();
            return requestedMode ?? (Application.isEditor ? GetFullScreenMode(modeIndex) : Screen.fullScreenMode);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RestoreDisplaySettings()
    {
        requestedMode = null;
        if (!PlayerPrefs.HasKey(ModePreference) && !PlayerPrefs.HasKey(ResolutionDropdown.WidthPreference))
            return;

        int modeIndex = PlayerPrefs.GetInt(ModePreference, Screen.fullScreen ? 0 : 1);
        FullScreenMode mode = GetFullScreenMode(modeIndex);
#if UNITY_WEBGL && !UNITY_EDITOR
        if (mode != FullScreenMode.Windowed)
            return;
#endif
        RequestMode(mode);
    }

    private void OnEnable()
    {
        if (dropdown == null)
            dropdown = GetComponent<TMP_Dropdown>();

        if (dropdown != null)
        {
            dropdown.onValueChanged.AddListener(OnModeChanged);
            RefreshMode();
        }
    }

    private void OnDisable()
    {
        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(OnModeChanged);
    }

    private void OnModeChanged(int modeIndex)
    {
        if (dropdown == null || dropdown.options.Count == 0)
            return;

        modeIndex = Mathf.Clamp(modeIndex, 0, Mathf.Min(dropdown.options.Count, 3) - 1);
        FullScreenMode mode = GetFullScreenMode(modeIndex);

#if UNITY_WEBGL && !UNITY_EDITOR
        // Browser fullscreen requests must run directly inside the user's input event.
        TerraformerWebGLFullscreen.Set(mode != FullScreenMode.Windowed);
#endif
        SaveModeIndex(modeIndex);
        RequestMode(mode);
        RefreshMode();
    }

    private static void RequestMode(FullScreenMode mode)
    {
        requestedMode = mode;
        requestFrame = Time.frameCount;
        requestDeadline = Time.realtimeSinceStartup + 1f;
        ResolutionDropdown.GetPreferredResolution(out int width, out int height);
        ApplyResolution(width, height, mode);
    }

    private void Update() => RefreshMode();

    private void RefreshMode()
    {
        int modeIndex = ReadModeIndex();
        if (dropdown != null && dropdown.value != modeIndex)
            dropdown.SetValueWithoutNotify(modeIndex);
    }

    private static int ReadModeIndex()
    {
        bool fullscreen = Screen.fullScreen;
        int savedIndex = Mathf.Clamp(PlayerPrefs.GetInt(ModePreference, fullscreen ? 0 : 1), 0, 2);

        // Unity applies requests after the current frame, and the OS can animate the transition.
        if (requestedMode.HasValue && Time.frameCount != requestFrame &&
            (Screen.fullScreenMode == requestedMode.Value || Time.realtimeSinceStartup >= requestDeadline))
            requestedMode = null;

        if (requestedMode.HasValue || Application.isEditor)
            return savedIndex;

        int modeIndex = fullscreen ? (savedIndex == 2 ? 2 : 0) : 1;
        if (modeIndex != savedIndex)
            SaveModeIndex(modeIndex);
        return modeIndex;
    }

    private static void SaveModeIndex(int modeIndex)
    {
        PlayerPrefs.SetInt(ModePreference, modeIndex);
        PlayerPrefs.Save();
    }

    internal static void ApplyResolution(int width, int height, FullScreenMode mode)
    {
        if (mode == FullScreenMode.FullScreenWindow && Application.platform != RuntimePlatform.WebGLPlayer)
        {
            // Keep the selected vertical resolution and match the display aspect to avoid bars.
            DisplayInfo display = Screen.mainWindowDisplayInfo;
            if (display.width > 0 && display.height > 0)
                width = Mathf.Max(1, Mathf.RoundToInt(height * ((float)display.width / display.height)));
        }

        Screen.SetResolution(width, height, mode);
    }

    private static FullScreenMode GetFullScreenMode(int modeIndex) =>
        modeIndex == 1 ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;

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
