using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

public static class TmpPercentRepair
{
    private const string FontAssetPath = "Assets/Pont/GowunBatang-Regular SDF.asset";
    private const string SourceFontPath = "Assets/Pont/GowunBatang-Regular.ttf";
    private const string StageScenePath = "Assets/Scenes/Stage1.unity";
    private const string RequiredCharacters = "0123456789%";

    public static void ScheduleRepairFontOnly()
    {
        EditorApplication.delayCall += RepairFontOnly;
        Debug.Log("[TmpPercentRepair] Scheduled TMP percent font repair.");
    }

    public static void ScheduleRecreateFontAsset()
    {
        EditorApplication.delayCall += RecreateFontAsset;
        Debug.Log("[TmpPercentRepair] Scheduled TMP font asset recreation.");
    }

    [MenuItem("Tools/Recreate TMP Percent Font Asset")]
    public static void RecreateFontAsset()
    {
        TMP_FontAsset fontAsset =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);

        if (fontAsset == null)
        {
            throw new System.InvalidOperationException(
                $"TMP font asset not found: {FontAssetPath}"
            );
        }

        if (sourceFont == null)
        {
            throw new System.InvalidOperationException(
                $"Source font not found: {SourceFontPath}"
            );
        }

        TMP_FontAsset generated = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic,
            true
        );

        if (generated == null)
        {
            throw new System.InvalidOperationException(
                "Failed to create TMP font asset from source font."
            );
        }

        generated.name = fontAsset.name;
        generated.material.name = $"{sourceFont.name} Atlas Material";

        Texture2D atlasTexture = generated.atlasTextures[0];
        atlasTexture.name = $"{sourceFont.name} Atlas";

        if (!generated.TryAddCharacters(RequiredCharacters, out string missing))
        {
            Debug.LogWarning(
                $"[TmpPercentRepair] Missing characters in recreated TMP asset: {missing}"
            );
        }

        AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
        AssetDatabase.AddObjectToAsset(generated.material, fontAsset);

        EditorUtility.CopySerialized(generated, fontAsset);
        EditorUtility.SetDirty(fontAsset);

        generated.atlasTextures = null;
        generated.material = null;
        Object.DestroyImmediate(generated);

        RepairStageTextMaterials(fontAsset);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[TmpPercentRepair] TMP font asset recreated from source font.");
    }

    [MenuItem("Tools/Repair TMP Percent")]
    public static void Repair()
    {
        TMP_FontAsset fontAsset =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);

        if (fontAsset == null)
        {
            throw new System.InvalidOperationException(
                $"TMP font asset not found: {FontAssetPath}"
            );
        }

        if (sourceFont == null)
        {
            throw new System.InvalidOperationException(
                $"Source font not found: {SourceFontPath}"
            );
        }

        RepairFontAsset(fontAsset, sourceFont);
        RepairStageTextMaterials(fontAsset);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[TmpPercentRepair] TMP percent glyph and materials repaired.");
    }

    [MenuItem("Tools/Repair TMP Percent Font Only")]
    public static void RepairFontOnly()
    {
        TMP_FontAsset fontAsset =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);

        if (fontAsset == null)
        {
            throw new System.InvalidOperationException(
                $"TMP font asset not found: {FontAssetPath}"
            );
        }

        if (sourceFont == null)
        {
            throw new System.InvalidOperationException(
                $"Source font not found: {SourceFontPath}"
            );
        }

        RepairFontAsset(fontAsset, sourceFont);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[TmpPercentRepair] TMP percent font glyph repaired.");
    }

    private static void RepairFontAsset(TMP_FontAsset fontAsset, Font sourceFont)
    {
        Undo.RecordObject(fontAsset, "Repair TMP percent font asset");

        SerializedObject serializedFontAsset = new SerializedObject(fontAsset);
        SerializedProperty sourceFontFile =
            serializedFontAsset.FindProperty("m_SourceFontFile");
        if (sourceFontFile != null)
        {
            sourceFontFile.objectReferenceValue = sourceFont;
            serializedFontAsset.ApplyModifiedPropertiesWithoutUndo();
        }

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontAsset.isMultiAtlasTexturesEnabled = true;

        if (fontAsset.atlasTextures == null || fontAsset.atlasTextures.Length == 0)
        {
            fontAsset.ClearFontAssetData(true);
        }

        if (!fontAsset.TryAddCharacters(RequiredCharacters, out string missing))
        {
            Debug.LogWarning(
                $"[TmpPercentRepair] Missing characters after TMP repair: {missing}"
            );
        }

        EditorUtility.SetDirty(fontAsset);
    }

    private static void RepairStageTextMaterials(TMP_FontAsset fontAsset)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        bool restoreActiveScene =
            activeScene.IsValid() && activeScene.path != StageScenePath;

        Scene scene = EditorSceneManager.OpenScene(
            StageScenePath,
            restoreActiveScene ? OpenSceneMode.Additive : OpenSceneMode.Single
        );

        Material defaultMaterial = fontAsset.material;

        int repairedCount = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            TextMeshProUGUI[] texts =
                root.GetComponentsInChildren<TextMeshProUGUI>(true);

            foreach (TextMeshProUGUI text in texts.Where(t => t.font == fontAsset))
            {
                Undo.RecordObject(text, "Reset TMP material to font default");
                text.fontSharedMaterial = defaultMaterial;
                EditorUtility.SetDirty(text);
                repairedCount++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (restoreActiveScene)
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        Debug.Log($"[TmpPercentRepair] Repaired TMP materials: {repairedCount}");
    }
}
