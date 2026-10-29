var names = new[] { "FireBall", "Lightning", "Burn", "FireStorm", "Shuriken", "LightningShuriken", "Poison", "Mold", "Snail", "Missile", "Heatwave", "LavaField" };
var backup = "Tools/Art/ApprovedHudIconsBefore_2026-10-29.tsv";
var before = new System.Collections.Generic.List<string> { "icon\tpreviousSpritePath\tpreviousGuid\tpreviousLocalId" };
foreach (var name in names)
{
    var icon = UnityEditor.AssetDatabase.LoadAssetAtPath<ImageInfo>("Assets/Resources/SkillUpgradeIcon/" + name + ".asset");
    if (icon == null) throw new System.Exception("Missing HUD icon: " + name);
    string guid; long localId;
    UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon.image, out guid, out localId);
    before.Add(name + "\t" + UnityEditor.AssetDatabase.GetAssetPath(icon.image) + "\t" + guid + "\t" + localId);
}
if (!System.IO.File.Exists(backup)) System.IO.File.WriteAllLines(backup, before);
foreach (var name in names)
{
    var path = "Assets/Sprite/ApprovedUpgradeCardIcons/" + name + "Card.png";
    var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path);
    if (sprite == null) throw new System.Exception("Missing approved sprite: " + name);
    var icon = UnityEditor.AssetDatabase.LoadAssetAtPath<ImageInfo>("Assets/Resources/SkillUpgradeIcon/" + name + ".asset");
    icon.image = sprite;
    UnityEditor.EditorUtility.SetDirty(icon);
}
UnityEditor.AssetDatabase.SaveAssets();
var report = new System.Collections.Generic.List<string>();
foreach (var name in names)
{
    var card = UnityEditor.AssetDatabase.LoadAssetAtPath<ImageInfo>("Assets/Resources/UpgradeImageScrip/" + name + ".asset");
    var icon = UnityEditor.AssetDatabase.LoadAssetAtPath<ImageInfo>("Assets/Resources/SkillUpgradeIcon/" + name + ".asset");
    var expected = "Assets/Sprite/ApprovedUpgradeCardIcons/" + name + "Card.png";
    if (card.image != icon.image || UnityEditor.AssetDatabase.GetAssetPath(icon.image) != expected)
        throw new System.Exception("Card/HUD mismatch: " + name);
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(expected);
    if (importer.alphaSource != UnityEditor.TextureImporterAlphaSource.FromGrayScale || !importer.alphaIsTransparency)
        throw new System.Exception("Transparency mismatch: " + name);
    report.Add(name + "\tcardId=" + card.id + "\thudId=" + icon.id + "\t" + expected);
}
System.IO.File.WriteAllLines("Tools/Art/ApprovedHudIconsVerification.tsv", report);
return string.Join("\n", report);
