// [UNITY-SKILL:SPRITEATLAS]
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public sealed class PixelEnemyAtlasPrebuild : IPreprocessBuildWithReport
{
    private const string AtlasPath = "Assets/Atlases/PixelEnemies.spriteatlasv2";
    private static readonly string[] PrefabFolders =
    {
        "Assets/Resources/Prefabs/Enemy",
        "Assets/Resources/Prefabs/Boss"
    };

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        Generate();
    }

    public static string Generate()
    {
        if (EditorSettings.spritePackerMode != SpritePackerMode.SpriteAtlasV2)
            EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;

        if (EditorSettings.spritePackerMode != SpritePackerMode.SpriteAtlasV2)
            throw new BuildFailedException("Sprite Atlas V2 packing could not be enabled.");

        var paths = CollectPixelSpriteSheets();
        if (paths.Count == 0)
            throw new BuildFailedException("No pixel enemy sprite sheets were found.");

        Directory.CreateDirectory(Path.GetDirectoryName(AtlasPath));
        var packables = new List<Object>();
        foreach (string path in paths)
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is Sprite)
                packables.Add(asset);

        if (packables.Count == 0)
            throw new BuildFailedException("No pixel enemy sprites were found.");

        var atlasAsset = new SpriteAtlasAsset();
        atlasAsset.Add(packables.ToArray());
        SpriteAtlasAsset.Save(atlasAsset, AtlasPath);
        AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(AtlasPath) as SpriteAtlasImporter;
        if (importer == null)
            throw new BuildFailedException("Could not import the pixel enemy atlas.");

        var texture = importer.textureSettings;
        texture.filterMode = FilterMode.Point;
        texture.generateMipMaps = false;
        importer.textureSettings = texture;

        var packing = importer.packingSettings;
        packing.padding = 4;
        packing.enableRotation = false;
        packing.enableTightPacking = false;
        importer.packingSettings = packing;

        foreach (string target in new[] { "Standalone", "WebGL", "Android", "iPhone" })
        {
            var platform = importer.GetPlatformSettings(target);
            platform.overridden = true;
            platform.maxTextureSize = 1024;
            platform.format = TextureImporterFormat.RGBA32;
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformSettings(platform);
        }

        importer.includeInBuild = true;
        importer.SaveAndReimport();
        AssetDatabase.SaveAssets();
        Debug.Log($"Pixel enemy atlas: {packables.Count} sprites from {paths.Count} sheets, packer={EditorSettings.spritePackerMode}.");
        return AtlasPath;
    }

    private static List<string> CollectPixelSpriteSheets()
    {
        var paths = new SortedSet<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", PrefabFolders))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            foreach (var renderer in prefab.GetComponentsInChildren<SpriteRenderer>(true))
                AddSprite(renderer.sprite, paths);

            foreach (var animator in prefab.GetComponentsInChildren<Animator>(true))
            {
                if (animator.runtimeAnimatorController == null)
                    continue;

                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                foreach (var key in AnimationUtility.GetObjectReferenceCurve(clip, binding))
                    AddSprite(key.value as Sprite, paths);
            }
        }

        return new List<string>(paths);
    }

    private static void AddSprite(Sprite sprite, ISet<string> paths)
    {
        if (sprite == null)
            return;

        string path = AssetDatabase.GetAssetPath(sprite);
        if (!path.StartsWith("Assets/"))
            return;

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (texture == null || importer == null ||
            texture.width > 512 || texture.height > 512 ||
            importer.textureType != TextureImporterType.Sprite ||
            importer.filterMode != FilterMode.Point)
            return;

        paths.Add(path);
    }
}
