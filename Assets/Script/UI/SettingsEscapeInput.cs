using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Settings must remain reachable while gameplay input is inactive or time is paused.
public sealed class SettingsEscapeInput : MonoBehaviour
{
    private InputAction escapeAction;
    private SettingsPause settings;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        var controller = new GameObject(nameof(SettingsEscapeInput));
        DontDestroyOnLoad(controller);
        controller.AddComponent<SettingsEscapeInput>();
    }

    private void Awake()
    {
        escapeAction = new InputAction("Settings", InputActionType.Button, "<Keyboard>/escape");
        escapeAction.performed += ToggleSettings;
        escapeAction.Enable();
        SceneManager.sceneLoaded += OnSceneLoaded;
        FindSettings();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => FindSettings();

    private void FindSettings()
    {
        settings = null;
        var scene = SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            settings = root.GetComponentInChildren<SettingsPause>(true);
            if (settings != null)
                return;
        }
    }

    private void ToggleSettings(InputAction.CallbackContext context)
    {
        if (settings == null)
            FindSettings();
        if (settings == null || (settings.transform.parent != null &&
            !settings.transform.parent.gameObject.activeInHierarchy))
            return;

        settings.gameObject.SetActive(!settings.gameObject.activeSelf);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (escapeAction == null) return;
        escapeAction.performed -= ToggleSettings;
        escapeAction.Dispose();
    }
}
