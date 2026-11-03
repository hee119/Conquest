if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Apply in Edit mode.");
string path = "Assets/SpriteSeet/OctopusKingGroundAttack.png";
UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
importer.textureType = UnityEditor.TextureImporterType.Sprite;
importer.spriteImportMode = UnityEditor.SpriteImportMode.Multiple;
importer.spritePixelsPerUnit = 112f * 1254f / 1280f;
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
var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path);
var factory = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories(); factory.Init();
var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
if (provider == null) throw new System.Exception("Missing sprite provider.");
provider.InitSpriteEditorDataProvider();
var capability = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
if (capability == null || !capability.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.CreateAndDeleteSprite))
    throw new System.Exception("Importer does not permit slicing.");
var names = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteNameFileIdDataProvider>();
if (names == null) throw new System.Exception("Missing sprite identity provider.");
var previous = provider.GetSpriteRects();
var rects = new UnityEditor.SpriteRect[23];
var pairs = new System.Collections.Generic.List<UnityEditor.SpriteNameFileIdPair>();
for (int n = 0; n < 23; n++)
{
    int index = n + 2, col = index % 5, row = index / 5;
    int x0 = UnityEngine.Mathf.RoundToInt(col * texture.width / 5f), x1 = UnityEngine.Mathf.RoundToInt((col+1)*texture.width/5f);
    int y0 = UnityEngine.Mathf.RoundToInt((4-row)*texture.height/5f), y1 = UnityEngine.Mathf.RoundToInt((5-row)*texture.height/5f);
    string name = "OctopusKingGroundAttack_" + index.ToString("00");
    var old = System.Array.Find(previous, r => r.name == name);
    rects[n] = new UnityEditor.SpriteRect {
        name = name, rect = new UnityEngine.Rect(x0,y0,x1-x0,y1-y0),
        alignment = UnityEngine.SpriteAlignment.Custom,
        pivot = new UnityEngine.Vector2(125f/(x1-x0),122f/(y1-y0)),
        spriteID = old == null ? UnityEditor.GUID.Generate() : old.spriteID
    };
    pairs.Add(new UnityEditor.SpriteNameFileIdPair(name,rects[n].spriteID));
}
provider.SetSpriteRects(rects); names.SetNameFileIdPairs(pairs); provider.Apply(); importer.SaveAndReimport();
var sprites = new UnityEngine.Sprite[23];
foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
{
    var sprite = asset as UnityEngine.Sprite;
    if (sprite != null) sprites[int.Parse(sprite.name.Substring(sprite.name.LastIndexOf('_')+1))-2] = sprite;
}
if (System.Array.Exists(sprites,s=>s==null)) throw new System.Exception("Incomplete sprite sheet.");
string clipPath = "Assets/Animation/OctopusKingGroundAttack.anim";
var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(clipPath);
if (clip == null) { clip = new UnityEngine.AnimationClip(); UnityEditor.AssetDatabase.CreateAsset(clip,clipPath); }
clip.name = "OctopusKingGroundAttack"; clip.frameRate = 12f;
var keys = new UnityEditor.ObjectReferenceKeyframe[23];
for (int n=0;n<23;n++) keys[n] = new UnityEditor.ObjectReferenceKeyframe{time=n/12f,value=sprites[n]};
UnityEditor.AnimationUtility.SetObjectReferenceCurve(clip,UnityEditor.EditorCurveBinding.PPtrCurve("",typeof(UnityEngine.SpriteRenderer),"m_Sprite"),keys);
var clipSettings = UnityEditor.AnimationUtility.GetAnimationClipSettings(clip);
clipSettings.startTime = 0; clipSettings.stopTime = 23f/12f; clipSettings.loopTime = false;
UnityEditor.AnimationUtility.SetAnimationClipSettings(clip,clipSettings); UnityEditor.EditorUtility.SetDirty(clip);
var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animation/OctopusKing.controller");
var machine = controller.layers[0].stateMachine;
var move = System.Array.Find(machine.states,s=>s.state.name=="Move").state;
var spin = System.Array.Find(machine.states,s=>s.state.name=="SpinWater").state;
if(move==null||spin==null) throw new System.Exception("Missing existing boss states.");
var ground = System.Array.Find(machine.states,s=>s.state.name=="GroundCrack").state;
if(ground==null) ground = machine.AddState("GroundCrack",new UnityEngine.Vector3(500,240,0));
ground.motion=clip;
if(!System.Array.Exists(controller.parameters,p=>p.name=="GroundCrack")) controller.AddParameter("GroundCrack",UnityEngine.AnimatorControllerParameterType.Trigger);
foreach(var transition in machine.anyStateTransitions) if(transition.destinationState==ground) machine.RemoveAnyStateTransition(transition);
var enter=machine.AddAnyStateTransition(ground); enter.hasExitTime=false; enter.duration=0; enter.canTransitionToSelf=false;
enter.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If,0,"GroundCrack");
foreach(var transition in ground.transitions) ground.RemoveTransition(transition);
var toSpin=ground.AddTransition(spin); toSpin.hasExitTime=true; toSpin.exitTime=1; toSpin.duration=0;
toSpin.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If,0,"SpinWater");
var toMove=ground.AddTransition(move); toMove.hasExitTime=true; toMove.exitTime=1; toMove.duration=0;
toMove.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot,0,"SpinWater");
UnityEditor.EditorUtility.SetDirty(controller);
string prefabPath="Assets/Resources/Prefabs/Boss/OctopusKing.prefab";
var prefab=UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
try{
    var boss=prefab.GetComponent<OctopusKing>();boss.groundAttackAnimTrigger="GroundCrack";boss.groundAttackImpactDelay=4f/12f;
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);
}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(prefab);}
UnityEditor.AssetDatabase.SaveAssets();
var report="Applied crack-free crowned ground attack: 23 occupied frames, 12fps, "+clip.length+"s; empty cells omitted; GroundCrack trigger connected; first impact delayed 0.333s; returns to Move or ongoing SpinWater.\n";
System.IO.File.WriteAllText("Tools/Art/OctopusKingGroundAttackApplied.txt",report);return report;
