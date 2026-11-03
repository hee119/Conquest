if (UnityEditor.EditorApplication.isPlaying)
    throw new System.Exception("Apply in Edit mode only.");

var sheetPaths = new[] { "Assets/SpriteSeet/SlimeKingMove.png", "Assets/SpriteSeet/SlimeKingHit.png" };
var frames = new UnityEngine.Sprite[2][];
var report = new System.Text.StringBuilder();
for (int sheet = 0; sheet < sheetPaths.Length; sheet++)
{
    string path = sheetPaths[sheet];
    UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    importer.textureType = UnityEditor.TextureImporterType.Sprite;
    importer.spriteImportMode = UnityEditor.SpriteImportMode.Multiple;
    importer.spritePixelsPerUnit = 75f;
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
    if (provider == null) throw new System.Exception("Missing sprite provider: " + path);
    provider.InitSpriteEditorDataProvider();
    var capabilities = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
    if (capabilities == null || !capabilities.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.CreateAndDeleteSprite))
        throw new System.Exception("Importer does not permit slicing: " + path);
    var names = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteNameFileIdDataProvider>();
    if (names == null) throw new System.Exception("Missing sprite identity provider: " + path);
    var old = provider.GetSpriteRects();
    var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path);
    var rects = new UnityEditor.SpriteRect[25];
    var pairs = new System.Collections.Generic.List<UnityEditor.SpriteNameFileIdPair>();
    for (int i = 0; i < 25; i++)
    {
        int column = i % 5, row = i / 5;
        int x0 = UnityEngine.Mathf.RoundToInt(column * texture.width / 5f);
        int x1 = UnityEngine.Mathf.RoundToInt((column + 1) * texture.width / 5f);
        int y0 = UnityEngine.Mathf.RoundToInt((4 - row) * texture.height / 5f);
        int y1 = UnityEngine.Mathf.RoundToInt((5 - row) * texture.height / 5f);
        string name = System.IO.Path.GetFileNameWithoutExtension(path) + "_" + i.ToString("00");
        var previous = System.Array.Find(old, r => r.name == name);
        rects[i] = new UnityEditor.SpriteRect {
            name = name, rect = new UnityEngine.Rect(x0, y0, x1 - x0, y1 - y0),
            alignment = UnityEngine.SpriteAlignment.Custom,
            pivot = new UnityEngine.Vector2((x1 - x0) / 2 / (float)(x1 - x0), (y1 - y0) / 2 / (float)(y1 - y0)),
            spriteID = previous == null ? UnityEditor.GUID.Generate() : previous.spriteID
        };
        pairs.Add(new UnityEditor.SpriteNameFileIdPair(name, rects[i].spriteID));
    }
    provider.SetSpriteRects(rects);
    names.SetNameFileIdPairs(pairs);
    provider.Apply();
    importer.SaveAndReimport();
    var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
    frames[sheet] = new UnityEngine.Sprite[25];
    foreach (var asset in all)
    {
        var sprite = asset as UnityEngine.Sprite;
        if (sprite == null) continue;
        int index = int.Parse(sprite.name.Substring(sprite.name.LastIndexOf('_') + 1));
        frames[sheet][index] = sprite;
    }
    if (System.Array.Exists(frames[sheet], s => s == null)) throw new System.Exception("Incomplete slice: " + path);
    report.AppendLine(path + ": 25 sprites, Point, no mipmaps, uncompressed, alpha from PNG.");
}

var clipPaths = new[] { "Assets/Animation/SlimeKingMove.anim", "Assets/Animation/SlimeKingHit.anim" };
var sourcePaths = new[] { "Assets/Animation/SlimeJump.anim", "Assets/Animation/SlimeHit.anim" };
var clips = new UnityEngine.AnimationClip[2];
for (int i = 0; i < 2; i++)
{
    var source = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(sourcePaths[i]);
    var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(clipPaths[i]);
    if (clip == null) { clip = UnityEngine.Object.Instantiate(source); UnityEditor.AssetDatabase.CreateAsset(clip, clipPaths[i]); }
    else UnityEditor.EditorUtility.CopySerialized(source, clip);
    clip.name = System.IO.Path.GetFileNameWithoutExtension(clipPaths[i]);
    foreach (var binding in UnityEditor.AnimationUtility.GetObjectReferenceCurveBindings(clip))
    {
        if (binding.type != typeof(UnityEngine.SpriteRenderer) || binding.propertyName != "m_Sprite") continue;
        var keys = UnityEditor.AnimationUtility.GetObjectReferenceCurve(clip, binding);
        for (int k = 0; k < keys.Length; k++)
        {
            var original = keys[k].value as UnityEngine.Sprite;
            int index = int.Parse(original.name.Substring(original.name.LastIndexOf('_') + 1));
            keys[k].value = frames[i][index];
        }
        UnityEditor.AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
    }
    var clipSettings = UnityEditor.AnimationUtility.GetAnimationClipSettings(clip);
    clipSettings.loopTime = i == 0;
    UnityEditor.AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
    // The boss has no jump-sound receiver; its attack patterns own their audio.
    UnityEditor.AnimationUtility.SetAnimationEvents(clip, new UnityEngine.AnimationEvent[0]);
    UnityEditor.EditorUtility.SetDirty(clip);
    clips[i] = clip;
    report.AppendLine(clipPaths[i] + ": original frame timing preserved, length=" + clip.length);
}

string controllerPath = "Assets/Animation/SlimeKing.controller";
var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
if (controller == null) controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
var machine = controller.layers[0].stateMachine;
var move = System.Array.Find(machine.states, x => x.state.name == "Move").state;
if (move == null) move = machine.AddState("Move", new UnityEngine.Vector3(250, 60, 0));
var hit = System.Array.Find(machine.states, x => x.state.name == "Hit").state;
if (hit == null) hit = machine.AddState("Hit", new UnityEngine.Vector3(500, 60, 0));
move.motion = clips[0]; hit.motion = clips[1]; machine.defaultState = move;
if (!System.Array.Exists(controller.parameters, p => p.name == "Hit"))
    controller.AddParameter("Hit", UnityEngine.AnimatorControllerParameterType.Trigger);
foreach (var oldTransition in machine.anyStateTransitions) machine.RemoveAnyStateTransition(oldTransition);
foreach (var oldTransition in hit.transitions) hit.RemoveTransition(oldTransition);
var transition = machine.AddAnyStateTransition(hit);
transition.hasExitTime = false; transition.duration = 0f; transition.canTransitionToSelf = false;
transition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, "Hit");
UnityEditor.EditorUtility.SetDirty(controller);

string prefabPath = "Assets/Resources/Prefabs/Boss/SlimeKing.prefab";
var prefab = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
try
{
    var animator = prefab.GetComponent<UnityEngine.Animator>();
    animator.runtimeAnimatorController = controller;
    prefab.GetComponent<UnityEngine.SpriteRenderer>().sprite = frames[0][0];
    var combat = prefab.GetComponent<SlimeKing>();
    combat.hitStateName = "Hit";
    combat.hitMaxWait = UnityEngine.Mathf.Max(1f, clips[1].length + .1f);
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(prefab); }
UnityEditor.AssetDatabase.SaveAssets();
report.AppendLine("SlimeKing prefab: crowned Move loop + terminal Hit. Regular slime assets unchanged.");
System.IO.File.WriteAllText("Tools/Art/SlimeKingAnimationApplied.txt", report.ToString());
return report.ToString();
