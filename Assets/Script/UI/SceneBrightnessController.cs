using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public sealed class SceneBrightnessController : MonoBehaviour
{
    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        if (GetComponent<SoundVolumeSlider>() != null)
        {
            enabled = false;
            return;
        }
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(SceneBrightnessSettings.SavedValue);
    }

    private void OnEnable()
    {
        if (slider == null || !enabled) return;
        slider.SetValueWithoutNotify(SceneBrightnessSettings.SavedValue);
        SceneBrightnessSettings.ApplyLoadedScenes();
    }

    public void ApplyBrightness(float normalizedValue)
    {
        if (!enabled) return;
        SceneBrightnessSettings.SetValue(normalizedValue);
    }
}

// Brightness belongs to scene initialization, not the optional settings window.
public static class SceneBrightnessSettings
{
    private const string PreferenceKey = "SceneBrightness";
    public static float SavedValue => Mathf.Clamp01(PlayerPrefs.GetFloat(PreferenceKey, 0.5f));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyScene(scene, SavedValue);
    }

    public static void SetValue(float value)
    {
        PlayerPrefs.SetFloat(PreferenceKey, Mathf.Clamp01(value));
        ApplyLoadedScenes();
    }

    public static void ApplyLoadedScenes()
    {
        float value = SavedValue;
        for (int i = 0; i < SceneManager.sceneCount; i++)
            ApplyScene(SceneManager.GetSceneAt(i), value);
    }

    private static void ApplyScene(Scene scene, float value)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        float exposure = Mathf.Lerp(-1f, 1f, value);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Volume volume in root.GetComponentsInChildren<Volume>(true))
            {
                if (!volume.isGlobal) continue;
                VolumeProfile profile = volume.profile;
                if (!profile.TryGet(out ColorAdjustments adjustments))
                    adjustments = profile.Add<ColorAdjustments>(true);
                adjustments.active = true;
                adjustments.postExposure.overrideState = true;
                adjustments.postExposure.value = exposure;
            }
        }
    }
}
