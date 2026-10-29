// Run through Unity Pipeline's eval_file command. This is an editor-only, one-shot operation.
var names = new[] { "FireBall", "Lightning", "Burn", "FireStorm", "Shuriken", "LightningShuriken", "Poison", "Mold", "Snail", "Missile", "Heatwave", "LavaField" };
var backup = "Tools/Art/ApprovedUpgradeCardsBefore_2026-10-29.tsv";
var before = new System.Collections.Generic.List<string> { "card\tpreviousSpritePath\tpreviousGuid\tpreviousLocalId" };
foreach (var name in names)
{
    var card = UnityEditor.AssetDatabase.LoadAssetAtPath<ImageInfo>("Assets/Resources/UpgradeImageScrip/" + name + ".asset");
    if (card == null) throw new System.Exception("Missing card: " + name);
    string guid; long localId;
    UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(card.image, out guid, out localId);
    before.Add(name + "\t" + UnityEditor.AssetDatabase.GetAssetPath(card.image) + "\t" + guid + "\t" + localId);
}
if (!System.IO.File.Exists(backup)) System.IO.File.WriteAllLines(backup, before);
var report = new System.Collections.Generic.List<string>();
foreach (var name in names)
{
    var path = "Assets/Sprite/ApprovedUpgradeCardIcons/" + name + "Card.png";
    UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    importer.textureType = UnityEditor.TextureImporterType.Sprite;
    importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
    // White illustration on black: derive transparency during import, preserving source PNGs.
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
    if (sprite == null) throw new System.Exception("Import failed: " + path);
    var pixels = sprite.texture.GetPixels32();
    int transparent = 0, opaque = 0;
    foreach (var pixel in pixels) { if (pixel.a < 8) transparent++; if (pixel.a > 240) opaque++; }
    if (transparent < pixels.Length / 5 || opaque < pixels.Length / 100)
        throw new System.Exception("Invalid alpha: " + name);
    var card = UnityEditor.AssetDatabase.LoadAssetAtPath<ImageInfo>("Assets/Resources/UpgradeImageScrip/" + name + ".asset");
    card.image = sprite;
    UnityEditor.EditorUtility.SetDirty(card);
    report.Add(name + "\t" + path + "\t" + sprite.texture.width + "x" + sprite.texture.height + "\ttransparent=" + transparent + "\topaque=" + opaque);
    importer.isReadable = false;
    importer.SaveAndReimport();
}
UnityEditor.AssetDatabase.SaveAssets();
foreach (var name in names)
{
    var card = UnityEditor.AssetDatabase.LoadAssetAtPath<ImageInfo>("Assets/Resources/UpgradeImageScrip/" + name + ".asset");
    var expected = "Assets/Sprite/ApprovedUpgradeCardIcons/" + name + "Card.png";
    if (UnityEditor.AssetDatabase.GetAssetPath(card.image) != expected) throw new System.Exception("Reference mismatch: " + name);
}
System.IO.File.WriteAllLines("Tools/Art/ApprovedUpgradeCardsVerification.tsv", report);
return string.Join("\n", report);
