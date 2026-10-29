using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public static class GameSceneManager
{
    public static event Action SceneChanging;
    public static event Action<Scene, LoadSceneMode> SceneChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents()
    {
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

    public static void LoadScene(string sceneName)
    {
        SceneChanging?.Invoke();
        UnitySceneManager.LoadScene(sceneName);
    }

    public static AsyncOperation LoadSceneAsync(string sceneName)
    {
        SceneChanging?.Invoke();
        return UnitySceneManager.LoadSceneAsync(sceneName);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneChanged?.Invoke(scene, mode);
    }
}
