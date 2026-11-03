var names = new[] { "SporeWarhead", "RottingSpore" };
var report = new System.Collections.Generic.List<string>();
var backup = new System.Collections.Generic.List<string>();
foreach (var name in names)
{
    var path = "Assets/Sprite/ApprovedUpgradeCardIcons/" + name + "Card.png";
    UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    importer.textureType = UnityEditor.TextureImporterType.Sprite;
    importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
    importer.alphaSource = UnityEditor.TextureImporterAlphaSource.FromGrayScale;
    importer.alphaIsTransparency = true;
    importer.mipmapEnabled = false;
    importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
    importer.maxTextureSize = 2048;
    importer.filterMode = UnityEngine.FilterMode.Bilinear;
    importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
    importer.isReadable = true;
    var settings = new UnityEditor.TextureImporterSettings();
    importer.ReadTextureSettings(settings);
    settings.spriteMeshType = UnityEngine.SpriteMeshType.FullRect;
    importer.SetTextureSettings(settings);
    importer.SaveAndReimport();
    var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path);
    var pixels = sprite.texture.GetPixels32();
    int clear = 0, solid = 0;
    foreach (var pixel in pixels) { if (pixel.a < 8) clear++; if (pixel.a > 240) solid++; }
    if (clear < pixels.Length / 5 || solid < pixels.Length / 100) throw new System.Exception("Alpha failed: " + name);
    var cardName = name == "RottingSpore" ? "RottingSpre" : name;
    foreach (var assetPath in new[] { "Assets/Resources/UpgradeImageScrip/" + cardName + ".asset", "Assets/Resources/SkillUpgradeIcon/" + name + ".asset" })
    {
        var info = UnityEditor.AssetDatabase.LoadAssetAtPath<ImageInfo>(assetPath);
        if (info == null) throw new System.Exception("Missing: " + assetPath);
        string guid; long localId;
        UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(info.image, out guid, out localId);
        backup.Add(assetPath + "\t" + guid + "\t" + localId);
        info.image = sprite;
        UnityEditor.EditorUtility.SetDirty(info);
        report.Add(assetPath + "\t" + info.id + "\t" + path + "\tclear=" + clear + "\tsolid=" + solid);
    }
    importer.isReadable = false;
    importer.SaveAndReimport();
}
var backupPath = "Tools/Art/SporeIconsBefore_2026-10-30.tsv";
if (!System.IO.File.Exists(backupPath)) System.IO.File.WriteAllLines(backupPath, backup);
UnityEditor.AssetDatabase.SaveAssets();
System.IO.File.WriteAllLines("Tools/Art/SporeIconsVerification.tsv", report);
return string.Join("\n", report);
