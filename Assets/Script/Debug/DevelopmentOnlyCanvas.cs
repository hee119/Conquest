using UnityEngine;
using UnityEngine.SceneManagement;

public static class DevelopmentOnlyCanvas
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        if (Debug.isDebugBuild)
            return;

        SceneManager.sceneLoaded -= HideDebugCanvas;
        SceneManager.sceneLoaded += HideDebugCanvas;
    }

    private static void HideDebugCanvas(Scene scene, LoadSceneMode mode)
    {
        foreach (var root in scene.GetRootGameObjects())
        foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
        {
            if (canvas.name != "Debug")
                continue;

            canvas.enabled = false;
            var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = false;
        }
    }
}
