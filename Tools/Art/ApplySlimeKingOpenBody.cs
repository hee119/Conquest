if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Apply in Edit mode.");
string path = "Assets/SpriteSeet/SlimeKingOpenBody.png";
UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
importer.textureType = UnityEditor.TextureImporterType.Sprite;
importer.spriteImportMode = UnityEditor.SpriteImportMode.Multiple;
// Match the first frame's visible body width to the existing Move sprite.
importer.spritePixelsPerUnit = 93.75f;
importer.alphaSource = UnityEditor.TextureImporterAlphaSource.FromInput;
importer.alphaIsTransparency = true;
importer.filterMode = UnityEngine.FilterMode.Point;
importer.mipmapEnabled = false;
importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
importer.maxTextureSize = 2048;
importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
var settings = new UnityEditor.TextureImporterSettings();
importer.ReadTextureSettings(settings);
settings.spriteMeshType = UnityEngine.SpriteMeshType.FullRect;
importer.SetTextureSettings(settings);
importer.SaveAndReimport();
var factory = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();
factory.Init();
var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
if (provider == null) throw new System.Exception("Missing sprite provider.");
provider.InitSpriteEditorDataProvider();
var capability = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
if (capability == null || !capability.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.CreateAndDeleteSprite))
    throw new System.Exception("Importer does not permit slicing.");
var nameProvider = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteNameFileIdDataProvider>();
if (nameProvider == null) throw new System.Exception("Missing sprite identity provider.");
var previous = provider.GetSpriteRects();
var rects = new UnityEditor.SpriteRect[25];
var pairs = new System.Collections.Generic.List<UnityEditor.SpriteNameFileIdPair>();
for (int i = 0; i < 25; i++)
{
    string name = "SlimeKingOpenBody_" + i.ToString("00");
    var old = System.Array.Find(previous, s => s.name == name);
    rects[i] = new UnityEditor.SpriteRect {
        name = name,
        rect = new UnityEngine.Rect((i % 5) * 256, (4 - i / 5) * 256, 256, 256),
        alignment = UnityEngine.SpriteAlignment.Custom,
        // Preserve the existing idle sprite's foot height and horizontal center.
        pivot = new UnityEngine.Vector2(127f / 256f, 140f / 256f),
        spriteID = old == null ? UnityEditor.GUID.Generate() : old.spriteID
    };
    pairs.Add(new UnityEditor.SpriteNameFileIdPair(name, rects[i].spriteID));
}
provider.SetSpriteRects(rects);
nameProvider.SetNameFileIdPairs(pairs);
provider.Apply();
importer.SaveAndReimport();
var sprites = new UnityEngine.Sprite[25];
foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
{
    var sprite = asset as UnityEngine.Sprite;
    if (sprite == null) continue;
    int index = int.Parse(sprite.name.Substring(sprite.name.LastIndexOf('_') + 1));
    sprites[index] = sprite;
}
if (System.Array.Exists(sprites, s => s == null)) throw new System.Exception("Incomplete 25-frame sheet.");
var stateNames = new[] { "OpenBodyRaise", "OpenBodyHold", "OpenBodyLower" };
var firstFrames = new[] { 0, 12, 20 };
var counts = new[] { 12, 8, 5 };
var fps = new[] { 12f, 12f, 10f };
var clips = new UnityEngine.AnimationClip[3];
var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animation/SlimeKing.controller");
if (controller == null) throw new System.Exception("Missing king controller.");
var machine = controller.layers[0].stateMachine;
var report = new System.Text.StringBuilder();
for (int section = 0; section < 3; section++)
{
    string clipPath = "Assets/Animation/SlimeKing" + stateNames[section] + ".anim";
    var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(clipPath);
    if (clip == null) { clip = new UnityEngine.AnimationClip(); UnityEditor.AssetDatabase.CreateAsset(clip, clipPath); }
    clip.name = "SlimeKing" + stateNames[section];
    clip.frameRate = fps[section];
    var keys = new UnityEditor.ObjectReferenceKeyframe[counts[section] + 1];
    for (int frame = 0; frame < keys.Length; frame++)
        keys[frame] = new UnityEditor.ObjectReferenceKeyframe {
            time = frame / fps[section],
            value = sprites[firstFrames[section] + UnityEngine.Mathf.Min(frame, counts[section] - 1)]
        };
    var binding = UnityEditor.EditorCurveBinding.PPtrCurve("", typeof(UnityEngine.SpriteRenderer), "m_Sprite");
    UnityEditor.AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
    var clipSettings = UnityEditor.AnimationUtility.GetAnimationClipSettings(clip);
    clipSettings.startTime = 0f;
    clipSettings.stopTime = counts[section] / fps[section];
    clipSettings.loopTime = section == 1;
    UnityEditor.AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
    var state = System.Array.Find(machine.states, s => s.state.name == stateNames[section]).state;
    if (state == null) state = machine.AddState(stateNames[section], new UnityEngine.Vector3(250 + section * 250, 220, 0));
    state.motion = clip;
    state.speed = 1f;
    UnityEditor.EditorUtility.SetDirty(clip);
    clips[section] = clip;
    report.AppendLine(stateNames[section] + ": frames " + firstFrames[section] + ".." + (firstFrames[section] + counts[section] - 1) + ", length=" + clip.length + ", loop=" + clipSettings.loopTime);
}
UnityEditor.EditorUtility.SetDirty(controller);
string prefabPath = "Assets/Resources/Prefabs/Boss/SlimeKing.prefab";
var prefab = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
try
{
    var boss = prefab.GetComponent<SlimeKing>();
    boss.openBodyRaiseClip = clips[0]; boss.openBodyHoldClip = clips[1]; boss.openBodyLowerClip = clips[2];
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(prefab); }
UnityEditor.AssetDatabase.SaveAssets();
report.AppendLine("25 transparent frames; open/hold/close assigned to king minion-spawn pattern. Move and lethal Hit preserved.");
System.IO.File.WriteAllText("Tools/Art/SlimeKingOpenBodyApplied.txt", report.ToString());
return report.ToString();
