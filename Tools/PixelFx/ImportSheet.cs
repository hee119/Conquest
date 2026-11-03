// Standalone Unity eval snippet. export.mjs supplies the JSON job below.
var job = Newtonsoft.Json.Linq.JObject.Parse(__PIXELFX_JOB__);
if (UnityEditor.EditorApplication.isPlaying)
    throw new System.InvalidOperationException("Exit Play mode before importing pixelfx.");
string assetPath = (string)job["assetPath"];
float ppu = (float)job["ppu"];
string jsonPath = System.IO.Path.ChangeExtension(assetPath, ".json");
var sheet = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(jsonPath));
var metadata = sheet["meta"]["pixelfx"];
var frames = (Newtonsoft.Json.Linq.JArray)sheet["frames"];
if (frames.Count == 0 || ppu <= 0) throw new System.Exception("Invalid sheet or PPU.");
UnityEditor.AssetDatabase.ImportAsset(assetPath, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
if (importer == null) throw new System.Exception("PNG importer unavailable.");
importer.textureType = UnityEditor.TextureImporterType.Sprite;
importer.spriteImportMode = UnityEditor.SpriteImportMode.Multiple;
importer.spritePixelsPerUnit = ppu;
importer.alphaSource = UnityEditor.TextureImporterAlphaSource.FromInput;
importer.alphaIsTransparency = true;
importer.filterMode = UnityEngine.FilterMode.Point;
importer.mipmapEnabled = false;
importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
importer.npotScale = UnityEditor.TextureImporterNPOTScale.None;
importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
int width = (int)sheet["meta"]["size"]["w"], height = (int)sheet["meta"]["size"]["h"];
if (width > 8192 || height > 8192) throw new System.Exception("Sheet exceeds 8192 pixels; lower FPS or size.");
importer.maxTextureSize = UnityEngine.Mathf.NextPowerOfTwo(UnityEngine.Mathf.Max(width, height));
var textureSettings = new UnityEditor.TextureImporterSettings();
importer.ReadTextureSettings(textureSettings);
textureSettings.spriteMeshType = UnityEngine.SpriteMeshType.FullRect;
importer.SetTextureSettings(textureSettings);
importer.SaveAndReimport();
var factory = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();
factory.Init();
var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
if (provider == null) throw new System.Exception("Sprite data provider unavailable.");
provider.InitSpriteEditorDataProvider();
var capabilities = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
if (capabilities == null || !capabilities.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.CreateAndDeleteSprite)
    || !capabilities.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.EditPivot)
    || !capabilities.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.EditSpriteRect)
    || !capabilities.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.EditSpriteName))
    throw new System.Exception("Importer does not allow sprite creation, rectangle, name or pivot editing.");
var identities = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteNameFileIdDataProvider>();
if (identities == null) throw new System.Exception("Sprite identity provider unavailable.");
var previous = provider.GetSpriteRects();
var rects = new UnityEditor.SpriteRect[frames.Count];
var pairs = new System.Collections.Generic.List<UnityEditor.SpriteNameFileIdPair>();
string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
for (int i = 0; i < frames.Count; i++)
{
    var frame = frames[i]["frame"];
    int w = (int)frame["w"], h = (int)frame["h"];
    if (w <= 0 || h <= 0 || (bool)frames[i]["rotated"] || (bool)frames[i]["trimmed"])
        throw new System.Exception("Only full unrotated frames are supported.");
    string spriteName = name + "_" + i.ToString("000");
    var old = System.Array.Find(previous, candidate => candidate.name == spriteName);
    rects[i] = new UnityEditor.SpriteRect
    {
        name = spriteName,
        rect = new UnityEngine.Rect((int)frame["x"], height - (int)frame["y"] - h, w, h),
        alignment = UnityEngine.SpriteAlignment.Custom,
        pivot = new UnityEngine.Vector2((float)metadata["pivot"][0] / w, 1f - (float)metadata["pivot"][1] / h),
        spriteID = old == null ? UnityEditor.GUID.Generate() : old.spriteID
    };
    pairs.Add(new UnityEditor.SpriteNameFileIdPair(spriteName, rects[i].spriteID));
}
provider.SetSpriteRects(rects);
identities.SetNameFileIdPairs(pairs);
provider.Apply();
importer.SaveAndReimport();
var sprites = new System.Collections.Generic.Dictionary<string, UnityEngine.Sprite>();
foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath))
    if (asset is UnityEngine.Sprite sprite) sprites.Add(sprite.name, sprite);
string directory = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
string clipPath = directory + "/" + name + ".anim";
var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(clipPath);
if (clip == null) { clip = new UnityEngine.AnimationClip(); UnityEditor.AssetDatabase.CreateAsset(clip, clipPath); }
clip.name = name;
clip.frameRate = (float)metadata["fps"];
float time = 0f;
var keys = new UnityEditor.ObjectReferenceKeyframe[frames.Count];
for (int i = 0; i < frames.Count; i++)
{
    keys[i] = new UnityEditor.ObjectReferenceKeyframe { time = time, value = sprites[rects[i].name] };
    time += (float)frames[i]["duration"] / 1000f;
}
UnityEditor.AnimationUtility.SetObjectReferenceCurve(clip,
    UnityEditor.EditorCurveBinding.PPtrCurve("", typeof(UnityEngine.SpriteRenderer), "m_Sprite"), keys);
var clipSettings = UnityEditor.AnimationUtility.GetAnimationClipSettings(clip);
clipSettings.startTime = 0;
clipSettings.stopTime = time;
clipSettings.loopTime = (bool)metadata["loop"];
UnityEditor.AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
UnityEditor.EditorUtility.SetDirty(clip);
string controllerPath = directory + "/" + name + ".controller";
var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
if (controller == null) controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
var machine = controller.layers[0].stateMachine;
var state = System.Array.Find(machine.states, candidate => candidate.state.name == name).state;
if (state == null) state = machine.AddState(name);
state.motion = clip;
machine.defaultState = state;
UnityEditor.EditorUtility.SetDirty(controller);
string prefabPath = directory + "/" + name + ".prefab";
var existingPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabPath);
var root = existingPrefab == null ? new UnityEngine.GameObject(name) : UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
try
{
    var renderer = root.GetComponent<UnityEngine.SpriteRenderer>();
    if (renderer == null) renderer = root.AddComponent<UnityEngine.SpriteRenderer>();
    renderer.sprite = sprites[rects[0].name];
    if (existingPrefab == null) renderer.sortingOrder = 20;
    var animator = root.GetComponent<UnityEngine.Animator>();
    if (animator == null) animator = root.AddComponent<UnityEngine.Animator>();
    animator.runtimeAnimatorController = controller;
    var playback = root.GetComponent<PixelFxEffect>();
    if (playback == null) playback = root.AddComponent<PixelFxEffect>();
    var serialized = new UnityEditor.SerializedObject(playback);
    serialized.FindProperty("clip").objectReferenceValue = clip;
    serialized.FindProperty("loop").boolValue = (bool)metadata["loop"];
    serialized.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
}
finally
{
    if (existingPrefab == null) UnityEngine.Object.DestroyImmediate(root);
    else UnityEditor.PrefabUtility.UnloadPrefabContents(root);
}
UnityEditor.AssetDatabase.SaveAssets();
UnityEditor.Selection.activeObject = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabPath);
return $"Imported {metadata["id"]}: {sprites.Count} frames, {clip.frameRate} FPS, duration={time:F3}s, PPU={ppu}; {prefabPath}";
