using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ResetPlayStartScene
{
    [MenuItem("Tools/Reset Play Start Scene")]
    private static void Reset()
    {
        EditorSceneManager.playModeStartScene = null;
        Debug.Log("시작 씬 고정 해제 완료");
    }
}