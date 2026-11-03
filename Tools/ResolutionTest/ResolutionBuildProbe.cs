using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Opt-in probe for the local resolution test build only.
public sealed class ResolutionBuildProbe : MonoBehaviour
{
    private SettingsPause settings;
    private ResolutionDropdown resolution;
    private ScreenModeDropdown mode;
    private readonly Dictionary<string, int?> saved = new();
    private readonly string[] keys = { "Settings.WindowWidth", "Settings.WindowHeight", "Settings.ScreenMode" };
    private bool busy;
    private string OutputDirectory
    {
        get
        {
            var args = System.Environment.GetCommandLineArgs();
            int index = System.Array.IndexOf(args, "-logFile");
            string path = index >= 0 && index + 1 < args.Length
                ? System.IO.Path.GetDirectoryName(args[index + 1]) : System.Environment.CurrentDirectory;
            System.IO.Directory.CreateDirectory(path);
            return path;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (!Application.absoluteURL.Contains("resolutionTest=1") &&
            !System.Environment.GetCommandLineArgs().Contains("--resolution-test")) return;
        var obj = new GameObject("ResolutionBuildProbe");
        DontDestroyOnLoad(obj);
        obj.AddComponent<ResolutionBuildProbe>();
    }

    private IEnumerator Start()
    {
        foreach (var key in keys) saved[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : null;
        while (!GameSceneManager.AreScenesPrepared) yield return null;
        GameSceneManager.LoadSceneAsync("Lobby");
        yield return null;
        settings = FindObjectsByType<SettingsPause>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(s => s.gameObject.scene.name == "Lobby");
        settings.gameObject.SetActive(true);
        yield return null;
        settings.transform.Find("Safe Area/OptionsSelectButtons").GetChild(0)
            .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        yield return null;
        resolution = settings.GetComponentInChildren<ResolutionDropdown>(true);
        mode = settings.GetComponentInChildren<ScreenModeDropdown>(true);
        Debug.Log("RESOLUTION_PROBE_READY");
        if (Application.platform != RuntimePlatform.WebGLPlayer)
            StartCoroutine(NativeSweep());
    }

    private IEnumerator NativeSweep()
    {
        for (int modeIndex = 1; modeIndex >= 0; modeIndex--)
        for (int index = 0; index < 6; index++)
        {
            Run(modeIndex * 6 + index);
            while (busy) yield return null;
            Popup(1);
            yield return new WaitForSecondsRealtime(.3f);
            Popup(0);
            yield return new WaitForSecondsRealtime(.2f);
        }
        for (int index = 0; index < 6; index++)
        {
            Run(12 + index);
            while (busy) yield return null;
        }
        // Reopen settings to catch resolution changes being applied again on enable.
        settings.gameObject.SetActive(false);
        yield return null;
        settings.gameObject.SetActive(true);
        settings.transform.Find("Safe Area/OptionsSelectButtons").GetChild(0)
            .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        yield return null;
        Run(6);
        while (busy) yield return null;
        yield return MapSweep();
        Finish();
        Debug.Log("RESOLUTION_PROBE_COMPLETE");
    }

    private IEnumerator MapSweep()
    {
        settings.gameObject.SetActive(false);
        GameSceneManager.LoadSceneAsync("Stage1");
        for (int frame = 0; frame < 30; frame++) yield return null;
        StageManager.Instance.StopRun();
        var player = GameObject.FindGameObjectWithTag("Player");
        var input = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (input != null) input.enabled = false;
        player.GetComponent<PlayerCombat>().hp = 100000;
        var body = player.GetComponent<Rigidbody2D>();
        var map = StageManager.Instance.backgroundTilemap;
        var bounds = map.localBounds;
        var min = map.transform.TransformPoint(bounds.min);
        var max = map.transform.TransformPoint(bounds.max);
        settings = FindObjectsByType<SettingsPause>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(s => s.gameObject.scene.name == "Stage1");
        settings.gameObject.SetActive(true);
        yield return null;
        settings.transform.Find("Safe Area/OptionsSelectButtons").GetChild(0)
            .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        yield return null;
        resolution = settings.GetComponentInChildren<ResolutionDropdown>(true);
        mode = settings.GetComponentInChildren<ScreenModeDropdown>(true);
        for (int index = 0; index < 6; index++)
        {
            settings.gameObject.SetActive(true);
            Run(6 + index);
            while (busy) yield return null;
            settings.gameObject.SetActive(false);
            Time.timeScale = 1f;
            for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
            {
                body.position = new Vector2(Mathf.Lerp(min.x + 1, max.x - 1, x * .5f),
                    Mathf.Lerp(min.y + 1, max.y - 1, y * .5f));
                body.linearVelocity = Vector2.zero;
                yield return new WaitForSecondsRealtime(1f);
                var camera = Camera.main;
                int holes = 0;
                for (int sy = 0; sy <= 6; sy++)
                for (int sx = 0; sx <= 10; sx++)
                {
                    var point = camera.ViewportToWorldPoint(new Vector3(.001f + .998f*sx/10f,
                        .001f + .998f*sy/6f, -camera.transform.position.z));
                    if (!map.HasTile(map.WorldToCell(point))) holes++;
                }
                Debug.Log($"MAP_PROBE resolution={Screen.width}x{Screen.height} point={x},{y} holes={holes} camera={camera.transform.position}");
                if (index == 0 && (x == 0 && y == 0 || x == 1 && y == 1))
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(OutputDirectory,$"map-{x}-{y}.png"));
            }
        }
        Debug.Log("MAP_PROBE_COMPLETE");
    }

    // Run directly from a real browser button to preserve fullscreen user activation.
    public void Run(int scenario)
    {
        if (busy || resolution == null) return;
        if (!resolution.isActiveAndEnabled || !mode.isActiveAndEnabled)
        {
            Debug.LogError("RESOLUTION_PROBE_INACTIVE_SETTINGS");
            return;
        }
        busy = true;
        int screenMode = scenario / 6, index = scenario % 6;
        var modeDropdown = mode.GetComponent<TMP_Dropdown>();
        modeDropdown.SetValueWithoutNotify(screenMode);
        modeDropdown.onValueChanged.Invoke(screenMode);
        var dropdown = resolution.GetComponent<TMP_Dropdown>();
        dropdown.SetValueWithoutNotify(index);
        dropdown.onValueChanged.Invoke(index);
        StartCoroutine(Report(scenario));
    }

    private IEnumerator Report(int scenario)
    {
        yield return new WaitForSecondsRealtime(1.2f);
        Canvas.ForceUpdateCanvases();
        var dropdown = resolution.GetComponent<TMP_Dropdown>();
        var rect = (RectTransform)dropdown.transform;
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        var canvas = dropdown.GetComponentInParent<Canvas>();
        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        var max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
        var pointer = new PointerEventData(EventSystem.current) { position = (min + max) * .5f };
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
        bool clickable = hits.Any(h => h.gameObject.GetComponentInParent<TMP_Dropdown>() == dropdown);
        Debug.Log("RESOLUTION_PROBE " + JsonUtility.ToJson(new Result {
            scenario = scenario, width = Screen.width, height = Screen.height, mode = Screen.fullScreenMode.ToString(),
            caption = dropdown.captionText.text, savedWidth = PlayerPrefs.GetInt(keys[0]), savedHeight = PlayerPrefs.GetInt(keys[1]),
            x = min.x, y = min.y, right = max.x, top = max.y, clickable = clickable,
            fits = min.x >= -1 && min.y >= -1 && max.x <= Screen.width+1 && max.y <= Screen.height+1
        }));
        if (Application.platform != RuntimePlatform.WebGLPlayer && (scenario == 6 || scenario == 11 || scenario == 0))
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(OutputDirectory, "scenario-" + scenario + ".png"));
        busy = false;
    }

    public void Popup(int show)
    {
        if (resolution == null) return;
        var dropdown = resolution.GetComponent<TMP_Dropdown>();
        if (show == 1) dropdown.Show(); else dropdown.Hide();
    }

    public void Finish()
    {
        foreach (var entry in saved)
            if (entry.Value.HasValue) PlayerPrefs.SetInt(entry.Key, entry.Value.Value); else PlayerPrefs.DeleteKey(entry.Key);
        PlayerPrefs.Save();
        Debug.Log("RESOLUTION_PROBE_RESTORED");
    }

    [System.Serializable] private class Result
    {
        public int scenario, width, height, savedWidth, savedHeight;
        public string mode, caption;
        public float x, y, right, top;
        public bool clickable, fits;
    }
}
