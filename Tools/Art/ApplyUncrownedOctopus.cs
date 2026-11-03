if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Apply in Edit mode.");
var paths = new[] { "Assets/SpriteSeet/OctopusUncrownedMove.png", "Assets/SpriteSeet/OctopusUncrownedWaterAttack.png" };
var first = new[] { 4, 3 };
var counts = new[] { 21, 22 };
// Normalize visible widths and foot heights to the existing normal octopus.
var ppu = new[] { 230f / 204f * 200f, 214f / 204f * 200f };
var pivotPixels = new[] { new UnityEngine.Vector2(126, 113), new UnityEngine.Vector2(126, 115) };
var frames = new UnityEngine.Sprite[2][];
for (int sheet = 0; sheet < 2; sheet++)
{
    UnityEditor.AssetDatabase.ImportAsset(paths[sheet], UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(paths[sheet]);
    importer.textureType = UnityEditor.TextureImporterType.Sprite;
    importer.spriteImportMode = UnityEditor.SpriteImportMode.Multiple;
    importer.spritePixelsPerUnit = ppu[sheet];
    importer.alphaSource = UnityEditor.TextureImporterAlphaSource.FromInput;
    importer.alphaIsTransparency = true;
    importer.filterMode = UnityEngine.FilterMode.Point;
    importer.mipmapEnabled = false;
    importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
    importer.maxTextureSize = 2048;
    importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
    var settings = new UnityEditor.TextureImporterSettings(); importer.ReadTextureSettings(settings);
    settings.spriteMeshType = UnityEngine.SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
    importer.SaveAndReimport();
    var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(paths[sheet]);
    var factory = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories(); factory.Init();
    var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
    if (provider == null) throw new System.Exception("Missing sprite provider.");
    provider.InitSpriteEditorDataProvider();
    var capability = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
    if (capability == null || !capability.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.CreateAndDeleteSprite))
        throw new System.Exception("Importer does not permit slicing.");
    var names = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteNameFileIdDataProvider>();
    if (names == null) throw new System.Exception("Missing sprite identity provider.");
    var oldRects = provider.GetSpriteRects();
    var rects = new UnityEditor.SpriteRect[counts[sheet]];
    var pairs = new System.Collections.Generic.List<UnityEditor.SpriteNameFileIdPair>();
    for (int n = 0; n < counts[sheet]; n++)
    {
        int index = first[sheet] + n, col = index % 5, row = index / 5;
        int x0 = UnityEngine.Mathf.RoundToInt(col * texture.width / 5f), x1 = UnityEngine.Mathf.RoundToInt((col + 1) * texture.width / 5f);
        int y0 = UnityEngine.Mathf.RoundToInt((4 - row) * texture.height / 5f), y1 = UnityEngine.Mathf.RoundToInt((5 - row) * texture.height / 5f);
        string name = System.IO.Path.GetFileNameWithoutExtension(paths[sheet]) + "_" + index.ToString("00");
        var old = System.Array.Find(oldRects, r => r.name == name);
        rects[n] = new UnityEditor.SpriteRect {
            name = name, rect = new UnityEngine.Rect(x0, y0, x1 - x0, y1 - y0),
            alignment = UnityEngine.SpriteAlignment.Custom,
            pivot = new UnityEngine.Vector2(pivotPixels[sheet].x / (x1-x0), pivotPixels[sheet].y / (y1-y0)),
            spriteID = old == null ? UnityEditor.GUID.Generate() : old.spriteID
        };
        pairs.Add(new UnityEditor.SpriteNameFileIdPair(name, rects[n].spriteID));
    }
    provider.SetSpriteRects(rects); names.SetNameFileIdPairs(pairs); provider.Apply(); importer.SaveAndReimport();
    frames[sheet] = new UnityEngine.Sprite[counts[sheet]];
    foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(paths[sheet]))
    {
        var sprite = asset as UnityEngine.Sprite;
        if (sprite == null) continue;
        int index = int.Parse(sprite.name.Substring(sprite.name.LastIndexOf('_') + 1));
        frames[sheet][index - first[sheet]] = sprite;
    }
    if (System.Array.Exists(frames[sheet], s => s == null)) throw new System.Exception("Incomplete sheet.");
}
var clipPaths = new[] { "Assets/Animation/OctopusUncrownedMove.anim", "Assets/Animation/OctopusUncrownedWaterAttack.anim" };
var fps = new[] { 12f, 24f };
var clips = new UnityEngine.AnimationClip[2];
for (int i = 0; i < 2; i++)
{
    var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(clipPaths[i]);
    if (clip == null) { clip = new UnityEngine.AnimationClip(); UnityEditor.AssetDatabase.CreateAsset(clip, clipPaths[i]); }
    clip.name = System.IO.Path.GetFileNameWithoutExtension(clipPaths[i]); clip.frameRate = fps[i];
    var keys = new UnityEditor.ObjectReferenceKeyframe[counts[i]];
    for (int n = 0; n < counts[i]; n++) keys[n] = new UnityEditor.ObjectReferenceKeyframe { time = n / fps[i], value = frames[i][n] };
    UnityEditor.AnimationUtility.SetObjectReferenceCurve(clip, UnityEditor.EditorCurveBinding.PPtrCurve("", typeof(UnityEngine.SpriteRenderer), "m_Sprite"), keys);
    var settings = UnityEditor.AnimationUtility.GetAnimationClipSettings(clip);
    settings.startTime = 0; settings.stopTime = counts[i] / fps[i]; settings.loopTime = i == 0;
    UnityEditor.AnimationUtility.SetAnimationClipSettings(clip, settings);
    UnityEditor.EditorUtility.SetDirty(clip); clips[i] = clip;
}
var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animation/Octopus.controller");
var machine = controller.layers[0].stateMachine;
var run = System.Array.Find(machine.states, s => s.state.name == "Run").state;
var attack = System.Array.Find(machine.states, s => s.state.name == "Attack").state;
if (run == null || attack == null) throw new System.Exception("Missing existing octopus states.");
run.motion = clips[0]; attack.motion = clips[1]; machine.defaultState = run;
UnityEditor.EditorUtility.SetDirty(controller);
string prefabPath = "Assets/Resources/Prefabs/Enemy/Octopus.prefab";
var prefab = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
try
{
    prefab.GetComponent<UnityEngine.SpriteRenderer>().sprite = frames[0][0];
    var serialized = new UnityEditor.SerializedObject(prefab.GetComponent<RamgedEnemyAttack>());
    // Source cell 12 is the first O-mouth release pose, after nine active frames.
    serialized.FindProperty("attackReleaseDelay").floatValue = 9f / 24f;
    serialized.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(prefab); }
UnityEditor.AssetDatabase.SaveAssets();
var report = "Normal octopus: uncrowned Move (21 frames, 12fps), WaterAttack (22 frames, 24fps), release delay 0.375s. Existing Attack trigger and return to Run preserved; normal body width/foot height normalized. Boss assets unchanged.\n";
System.IO.File.WriteAllText("Tools/Art/UncrownedOctopusApplied.txt", report);
return report;
