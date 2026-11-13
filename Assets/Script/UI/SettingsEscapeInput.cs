using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Settings must remain reachable while gameplay input is inactive or time is paused.
public sealed class SettingsEscapeInput : MonoBehaviour
{
    private InputAction escapeAction;

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
    }

    private static SettingsPause FindSettings()
    {
        var scene = SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var settings in root.GetComponentsInChildren<SettingsPause>(true))
            {
                var parent = settings.transform.parent;
                if (parent == null || parent.gameObject.activeInHierarchy)
                    return settings;
            }
        }
        return null;
    }

    private void ToggleSettings(InputAction.CallbackContext context)
    {
        // Prepared scenes can become active without raising sceneLoaded again.
        var settings = FindSettings();
        if (settings != null)
            settings.gameObject.SetActive(!settings.gameObject.activeSelf);
    }

    private void OnDestroy()
    {
        if (escapeAction == null) return;
        escapeAction.performed -= ToggleSettings;
        escapeAction.Dispose();
    }
}
