using Sirenix.OdinInspector.Editor;
using UnityEditor;

[CustomEditor(typeof(SoundManager))]
public sealed class SoundManagerEditor : OdinEditor
{
    [MenuItem("Conquest/사운드 매니저")]
    private static void SelectManager()
    {
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(
            "Assets/Resources/SoundManager.prefab");
    }
}
