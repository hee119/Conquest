if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Apply in Edit mode.");
var paths = new[] { "Assets/SpriteSeet/SlimeKingSlide.png", "Assets/SpriteSeet/SlimeKingSlidePrepare.png", "Assets/SpriteSeet/SlimeKingSlideEnd.png" };
var indices = new[] {
    new[] { 8,9,10,11,12,13,14,15,16,17,18,19,20,21 },
    new[] { 0,1,2,3,4,5,6,7 },
    new[] { 22,23,24 }
};
var frames = new System.Collections.Generic.Dictionary<int, UnityEngine.Sprite>[3];
for (int sheet = 0; sheet < paths.Length; sheet++)
{
    string path = paths[sheet];
    UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    importer.textureType = UnityEditor.TextureImporterType.Sprite;
    importer.spriteImportMode = UnityEditor.SpriteImportMode.Multiple;
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
    var factory = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories(); factory.Init();
    var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
    if (provider == null) throw new System.Exception("Missing sprite provider: " + path);
    provider.InitSpriteEditorDataProvider();
    var capability = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
    if (capability == null || !capability.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.CreateAndDeleteSprite))
        throw new System.Exception("Importer does not permit slicing: " + path);
    var names = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteNameFileIdDataProvider>();
    if (names == null) throw new System.Exception("Missing sprite identity provider: " + path);
    var previous = provider.GetSpriteRects();
    var rects = new UnityEditor.SpriteRect[indices[sheet].Length];
    var pairs = new System.Collections.Generic.List<UnityEditor.SpriteNameFileIdPair>();
    for (int n = 0; n < rects.Length; n++)
    {
        int index = indices[sheet][n];
        string name = System.IO.Path.GetFileNameWithoutExtension(path) + "_" + index.ToString("00");
        var old = System.Array.Find(previous, r => r.name == name);
        rects[n] = new UnityEditor.SpriteRect {
            name = name,
            rect = new UnityEngine.Rect((index % 5) * 256, (4 - index / 5) * 256, 256, 256),
            alignment = UnityEngine.SpriteAlignment.Custom,
            pivot = new UnityEngine.Vector2(128f / 256f, 139f / 256f),
            spriteID = old == null ? UnityEditor.GUID.Generate() : old.spriteID
        };
        pairs.Add(new UnityEditor.SpriteNameFileIdPair(name, rects[n].spriteID));
    }
    provider.SetSpriteRects(rects); names.SetNameFileIdPairs(pairs); provider.Apply();
    importer.SaveAndReimport();
    frames[sheet] = new System.Collections.Generic.Dictionary<int, UnityEngine.Sprite>();
    foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
    {
        var sprite = asset as UnityEngine.Sprite;
        if (sprite != null) frames[sheet].Add(int.Parse(sprite.name.Substring(sprite.name.LastIndexOf('_') + 1)), sprite);
    }
    if (frames[sheet].Count != indices[sheet].Length) throw new System.Exception("Incomplete slicing: " + path);
}

var stateNames = new[] { "SlidePrepare", "SlideStart", "SlideLoop", "SlideEnd" };
var sheetIds = new[] { 1, 0, 0, 2 };
var orders = new[] {
    new[] { 0,1,2,3,4,5,6,7 },
    new[] { 8,9,10,11 },
    // Traverse the central afterimage poses in both directions to avoid a hard jump.
    new[] { 12,13,14,15,16,17,18,19,18,17,16,15,14,13 },
    new[] { 22,23,24 }
};
var rates = new[] { 18f,18f,18f,12f };
var clips = new UnityEngine.AnimationClip[4];
var states = new UnityEditor.Animations.AnimatorState[4];
var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animation/SlimeKing.controller");
if (controller == null) throw new System.Exception("Missing king controller.");
var machine = controller.layers[0].stateMachine;
var report = new System.Text.StringBuilder();
for (int i = 0; i < clips.Length; i++)
{
    string clipPath = "Assets/Animation/SlimeKing" + stateNames[i] + ".anim";
    var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(clipPath);
    if (clip == null) { clip = new UnityEngine.AnimationClip(); UnityEditor.AssetDatabase.CreateAsset(clip, clipPath); }
    clip.name = "SlimeKing" + stateNames[i]; clip.frameRate = rates[i];
    var keys = new UnityEditor.ObjectReferenceKeyframe[orders[i].Length];
    for (int k = 0; k < keys.Length; k++)
        keys[k] = new UnityEditor.ObjectReferenceKeyframe { time = k / rates[i], value = frames[sheetIds[i]][orders[i][k]] };
    UnityEditor.AnimationUtility.SetObjectReferenceCurve(clip, UnityEditor.EditorCurveBinding.PPtrCurve("", typeof(UnityEngine.SpriteRenderer), "m_Sprite"), keys);
    var settings = UnityEditor.AnimationUtility.GetAnimationClipSettings(clip);
    settings.startTime = 0; settings.stopTime = orders[i].Length / rates[i]; settings.loopTime = i == 2;
    UnityEditor.AnimationUtility.SetAnimationClipSettings(clip, settings);
    var state = System.Array.Find(machine.states, s => s.state.name == stateNames[i]).state;
    if (state == null) state = machine.AddState(stateNames[i], new UnityEngine.Vector3(250 + i * 220, 400, 0));
    state.motion = clip; state.speed = 1f;
    foreach (var transition in state.transitions) state.RemoveTransition(transition);
    UnityEditor.EditorUtility.SetDirty(clip);
    clips[i] = clip; states[i] = state;
    report.AppendLine(stateNames[i] + ": source " + System.IO.Path.GetFileName(paths[sheetIds[i]]) + ", frames " + string.Join(",", orders[i]) + ", length=" + clip.length + ", loop=" + settings.loopTime);
}
if (!System.Array.Exists(controller.parameters, p => p.name == "SlidePrepareSpeed"))
    controller.AddParameter("SlidePrepareSpeed", UnityEngine.AnimatorControllerParameterType.Float);
states[0].speedParameter = "SlidePrepareSpeed";
states[0].speedParameterActive = true;
var leadIntoLoop = states[1].AddTransition(states[2]);
leadIntoLoop.hasExitTime = true; leadIntoLoop.exitTime = 1f; leadIntoLoop.duration = 0f;
UnityEditor.EditorUtility.SetDirty(controller);

string prefabPath = "Assets/Resources/Prefabs/Boss/SlimeKing.prefab";
var prefab = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
try
{
    var boss = prefab.GetComponent<SlimeKing>();
    boss.slidePrepareClip = clips[0]; boss.slideStartClip = clips[1]; boss.slideLoopClip = clips[2]; boss.slideEndClip = clips[3];
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(prefab); }
UnityEditor.AssetDatabase.SaveAssets();
report.AppendLine("Empty cells excluded. Fixed ground pivot. Preparation uses slideWindup; central afterimages loop until actual movement or obstacle stop; ending plays once.");
System.IO.File.WriteAllText("Tools/Art/SlimeKingSlideApplied.txt", report.ToString());
return report.ToString();
