using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum UiSoundCue { Click, Confirm, Hover, Upgrade }

/// <summary>UI publishes events; audio playback remains owned by SoundManager.</summary>
public static class UiSoundEvents
{
    private static readonly HashSet<UiSoundEmitter> controls = new HashSet<UiSoundEmitter>();
    public static IEnumerable<UiSoundEmitter> Controls => controls;
    public static event Action<UiSoundEmitter> Created;
    public static event Action<UiSoundEmitter> Removed;
    public static event Action<UiSoundEmitter> Clicked;
    public static event Action<UiSoundEmitter, bool> ToggleChanged;
    public static event Action<UiSoundCue> Requested;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        controls.Clear();
        Created = null; Removed = null; Clicked = null; ToggleChanged = null; Requested = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SubscribeScenes()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterLoadedScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++) SceneLoaded(SceneManager.GetSceneAt(i), LoadSceneMode.Additive);
    }

    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var root in scene.GetRootGameObjects()) RegisterHierarchy(root);
    }

    // Called once for a loaded scene, or explicitly by a dynamic UI factory.
    public static void RegisterHierarchy(GameObject root)
    {
        if (root == null) return;
        foreach (var selectable in root.GetComponentsInChildren<Selectable>(true))
            if (selectable is UnityEngine.UI.Button || selectable is UnityEngine.UI.Toggle) Register(selectable);
    }

    public static UiSoundEmitter Register(UnityEngine.UI.Selectable control)
    {
        if (control == null) return null;
        var emitter = control.GetComponent<UiSoundEmitter>();
        return emitter != null ? emitter : control.gameObject.AddComponent<UiSoundEmitter>();
    }

    public static void UseDedicatedClick(UnityEngine.UI.Button button)
    {
        var emitter = Register(button);
        if (emitter != null) emitter.SuppressDefaultClick = true;
    }

    internal static void Add(UiSoundEmitter control) { if (controls.Add(control)) Created?.Invoke(control); }
    internal static void Remove(UiSoundEmitter control) { if (controls.Remove(control)) Removed?.Invoke(control); }
    internal static void Click(UiSoundEmitter control) => Clicked?.Invoke(control);
    internal static void Toggle(UiSoundEmitter control, bool value) => ToggleChanged?.Invoke(control, value);
    public static void Request(UiSoundCue cue) => Requested?.Invoke(cue);
}
