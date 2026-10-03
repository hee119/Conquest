using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class CreateJapaneseTmpFont
{
    private const string SourceFontPath = "Assets/Pont/NewClear-mincho.ttf";
    private const string FontAssetPath = "Assets/Pont/NewClear-mincho SDF.asset";
    private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

    [MenuItem("Tools/Create Japanese TMP Font Asset")]
    public static void CreateAndRegister()
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
            throw new System.InvalidOperationException($"Font not found: {SourceFontPath}");

        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (fontAsset == null)
        {
            fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                48,
                5,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true
            );

            if (fontAsset == null || fontAsset.material == null ||
                fontAsset.atlasTextures == null || fontAsset.atlasTextures.Length == 0 ||
                fontAsset.atlasTextures[0] == null)
                throw new System.InvalidOperationException("TMP font creation returned an incomplete asset.");

            fontAsset.name = "NewClear-mincho SDF";
            fontAsset.material.name = "NewClear-mincho Atlas Material";
            fontAsset.atlasTextures[0].name = "NewClear-mincho Atlas";

            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            AssetDatabase.ImportAsset(FontAssetPath);
            fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        }

        TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
        if (settings == null)
            throw new System.InvalidOperationException($"TMP Settings not found: {TmpSettingsPath}");

        SerializedObject serializedSettings = new SerializedObject(settings);
        SerializedProperty fallbackAssets = serializedSettings.FindProperty("m_fallbackFontAssets");
        if (fallbackAssets == null)
            throw new System.InvalidOperationException("TMP fallback font list was not found.");

        bool alreadyRegistered = false;
        List<TMP_FontAsset> orderedFallbacks = new List<TMP_FontAsset> { fontAsset };
        for (int i = 0; i < fallbackAssets.arraySize; i++)
        {
            TMP_FontAsset existing = fallbackAssets.GetArrayElementAtIndex(i).objectReferenceValue as TMP_FontAsset;
            alreadyRegistered |= existing == fontAsset;
            if (existing != null && existing != fontAsset)
                orderedFallbacks.Add(existing);
        }

        if (!alreadyRegistered || orderedFallbacks.Count != fallbackAssets.arraySize ||
            fallbackAssets.GetArrayElementAtIndex(0).objectReferenceValue != fontAsset)
        {
            fallbackAssets.arraySize = orderedFallbacks.Count;
            for (int i = 0; i < orderedFallbacks.Count; i++)
                fallbackAssets.GetArrayElementAtIndex(i).objectReferenceValue = orderedFallbacks[i];
        }

        serializedSettings.ApplyModifiedProperties();
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Created NewClear-mincho TMP font asset and registered it as the first global fallback.");
    }
}
