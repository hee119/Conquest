using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class DebugCameraZSliderEditor
{
    private const float MinLensSize = 1f;
    private const float MaxLensSize = 30f;

    static DebugCameraZSliderEditor()
    {
        SceneView.duringSceneGui -= DrawSceneLensControl;
        SceneView.duringSceneGui += DrawSceneLensControl;
    }

    private static void DrawSceneLensControl(SceneView sceneView)
    {
        if (EditorApplication.isPlaying)
            return;

        CinemachineCamera camera = Object.FindFirstObjectByType<CinemachineCamera>();
        if (camera == null)
            return;

        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(12f, 12f, 230f, 76f), GUI.skin.box);
        GUILayout.Label("Camera Lens", EditorStyles.boldLabel);

        LensSettings lens = camera.Lens;
        EditorGUI.BeginChangeCheck();
        float size = EditorGUILayout.Slider("Orthographic Size", lens.OrthographicSize, MinLensSize, MaxLensSize);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(camera, "Adjust Camera Lens");
            lens.OrthographicSize = size;
            camera.Lens = lens;
            EditorUtility.SetDirty(camera);
            EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
            sceneView.Repaint();
        }

        GUILayout.EndArea();
        Handles.EndGUI();
    }

    [MenuItem("Tools/Debug/Set Lens Slider Range (1-30)")]
    private static void SetLensSliderRange()
    {
        Slider slider = FindSlider("Camera Lens Slider", "Camera Z Slider");
        if (slider == null)
        {
            Debug.LogWarning("Debug Canvas의 Camera Lens Slider를 찾지 못했습니다. Play 전에 Debug Camera Lens Slider 컴포넌트를 추가하거나 기존 슬라이더를 확인하세요.");
            return;
        }

        Undo.RecordObject(slider, "Configure Camera Lens Slider");
        slider.gameObject.name = "Camera Lens Slider";
        slider.minValue = MinLensSize;
        slider.maxValue = MaxLensSize;
        slider.wholeNumbers = false;

        Transform label = slider.transform.parent != null ? slider.transform.parent.Find("Camera Z Value") : null;
        if (label != null)
            label.name = "Camera Lens Value";

        EditorUtility.SetDirty(slider);
        EditorSceneManager.MarkSceneDirty(slider.gameObject.scene);
        Selection.activeGameObject = slider.gameObject;
        Debug.Log("Camera Lens Slider 범위를 1–30으로 설정했습니다.");
    }

    private static Slider FindSlider(string preferredName, string legacyName)
    {
        GameObject target = GameObject.Find(preferredName) ?? GameObject.Find(legacyName);
        return target != null ? target.GetComponent<Slider>() : null;
    }
}

[CustomEditor(typeof(DebugCameraZSlider))]
public sealed class DebugCameraLensSliderInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("씬 뷰에서 플레이하지 않고 Cinemachine 카메라의 Orthographic Size를 1–30으로 조절합니다.", MessageType.Info);
    }
}
