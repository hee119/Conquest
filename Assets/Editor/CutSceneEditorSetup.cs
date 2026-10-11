using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CutSceneEditorSetup
{
    private const string ScenePath = "Assets/Scenes/CutSceen.unity";
    private const string StagePath = "Assets/Scenes/Stage1.unity";

    private static readonly string[] ImagePaths =
    {
        "Assets/Cutscene/01_Earth_Departure.png",
        "Assets/Cutscene/02_Space_Travel.png",
        "Assets/Cutscene/03_Crab_Attack.png",
        "Assets/Cutscene/04_Crab_Defeated.png"
    };

    [MenuItem("Tools/Conquest/Set Up Cut Scene")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("플레이 모드를 종료한 뒤 컷신을 설정하세요.");

        foreach (string path in ImagePaths)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new System.InvalidOperationException($"이미지를 찾을 수 없습니다: {path}");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            var platform = importer.GetDefaultPlatformTextureSettings();
            platform.maxTextureSize = 2048;
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(platform);
            importer.SaveAndReimport();
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var canvas = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Canvas>(true)).FirstOrDefault();
        if (canvas == null)
            throw new System.InvalidOperationException("CutSceen Canvas를 찾을 수 없습니다.");

        var image = canvas.GetComponentsInChildren<UnityEngine.UI.Image>(true).FirstOrDefault();
        if (image == null)
            throw new System.InvalidOperationException("CutSceen Image를 찾을 수 없습니다.");

        var controller = image.GetComponent<CutSceneController>();
        if (controller == null)
            controller = image.gameObject.AddComponent<CutSceneController>();

        var sprites = ImagePaths.Select(AssetDatabase.LoadAssetAtPath<Sprite>).ToArray();
        if (sprites.Any(sprite => sprite == null))
            throw new System.InvalidOperationException("컷신 스프라이트 가져오기에 실패했습니다.");

        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("displayImage").objectReferenceValue = image;
        var frames = serializedController.FindProperty("frames");
        frames.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            frames.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        serializedController.FindProperty("stageSceneName").stringValue = "Stage1";
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        image.sprite = sprites[0];
        image.raycastTarget = true;
        image.preserveAspect = true;
        image.color = Color.white;
        image.gameObject.name = "CutSceneImage";

        var settings = EditorBuildSettings.scenes.ToList();
        settings.RemoveAll(item => item.path == ScenePath || item.path == StagePath);
        settings.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        settings.Insert(1, new EditorBuildSettingsScene(StagePath, true));
        EditorBuildSettings.scenes = settings.ToArray();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("CutSceen 설정 완료: 4장 클릭 진행 후 Stage1로 이동합니다.");
    }

    [MenuItem("Tools/Conquest/Use Point Filtering For Cut Scene")]
    public static void UsePointFiltering()
    {
        foreach (string path in ImagePaths)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new System.InvalidOperationException($"이미지를 찾을 수 없습니다: {path}");

            var platform = importer.GetDefaultPlatformTextureSettings();
            if (importer.filterMode == FilterMode.Point &&
                !importer.mipmapEnabled &&
                importer.maxTextureSize == 2048 &&
                importer.textureCompression == TextureImporterCompression.Uncompressed &&
                platform.maxTextureSize == 2048 &&
                platform.textureCompression == TextureImporterCompression.Uncompressed)
                continue;

            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            platform.maxTextureSize = 2048;
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(platform);
            importer.SaveAndReimport();
        }
    }
}
