using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public static class GameSceneManager
{
    private static readonly Dictionary<string, PreparedScene> preparedScenes = new();
    private static AsyncOperation pendingLoad;
    private static bool switching;
    public static bool IsPreparingScenes { get; private set; }
    public static bool AreScenesPrepared { get; private set; }
    public static bool IsLoading => switching || IsPreparingScenes || (pendingLoad != null && !pendingLoad.isDone);
    public static float TransitionFadeSeconds { get; set; } = 0.8f;
    public static event Action SceneChanging;
    public static event Action<Scene, LoadSceneMode> SceneChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents()
    {
        preparedScenes.Clear();
        pendingLoad = null;
        switching = false;
        IsPreparingScenes = AreScenesPrepared = false;
        TransitionFadeSeconds = 0.8f;
        SceneChanging = null;
        SceneChanged = null;
        UnitySceneManager.sceneLoaded -= OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        UnitySceneManager.sceneLoaded -= OnSceneLoaded;
        UnitySceneManager.sceneLoaded += OnSceneLoaded;
    }

    public static void RegisterPreparedScene(PreparedScene scene) => preparedScenes[scene.Scene.name] = scene;

    public static IEnumerator PrepareScenes(Action<float> progress)
    {
        IsPreparingScenes = true;
        string current = UnitySceneManager.GetActiveScene().path;
        var paths = new List<string>();
        for (int i = 0; i < UnitySceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (path != current) paths.Add(path);
        }
        for (int i = 0; i < paths.Count; i++)
        {
            var scene = UnitySceneManager.GetSceneByPath(paths[i]);
            if (!scene.isLoaded)
            {
                pendingLoad = UnitySceneManager.LoadSceneAsync(paths[i], LoadSceneMode.Additive);
                while (!pendingLoad.isDone)
                {
                    progress?.Invoke((i + Mathf.Clamp01(pendingLoad.progress / 0.9f)) / paths.Count);
                    yield return null;
                }
            }
            scene = UnitySceneManager.GetSceneByPath(paths[i]);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var stage in root.GetComponentsInChildren<StageManager>(true))
                {
                    stage.PrepareTilesForLoading();
                    yield return null;
                }
            progress?.Invoke((float)(i + 1) / paths.Count);
            yield return null;
        }
        pendingLoad = null;
        IsPreparingScenes = false;
        AreScenesPrepared = true;
        progress?.Invoke(1f);
    }

    public static void LoadScene(string sceneName) => LoadSceneAsync(sceneName);

    public static AsyncOperation LoadSceneAsync(string sceneName)
    {
        if (IsLoading) return pendingLoad;
        if (preparedScenes.TryGetValue(sceneName, out var prepared) && prepared != null)
        {
            switching = true;
            try
            {
                SceneChanging?.Invoke();
                var previous = UnitySceneManager.GetActiveScene();
                if (preparedScenes.TryGetValue(previous.name, out var old) && old != null)
                    old.Hide();
                else
                    foreach (var root in previous.GetRootGameObjects()) root.SetActive(false);
                UnitySceneManager.SetActiveScene(prepared.Scene);
                prepared.Show();
                SceneChanged?.Invoke(prepared.Scene, LoadSceneMode.Single);
            }
            finally { switching = false; }
            return null;
        }
        // Direct Editor play can enter a scene without the startup cache.
        SceneChanging?.Invoke();
        pendingLoad = UnitySceneManager.LoadSceneAsync(sceneName);
        return pendingLoad;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsPreparingScenes) SceneChanged?.Invoke(scene, mode);
    }
}
