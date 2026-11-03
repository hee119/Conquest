if(UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Edit mode required");
var paths = new[] { "Assets/SpriteSeet/OctopusWaterShotAligned.png", "Assets/SpriteSeet/OctopusKingWaterShot.png" };
var first = new[] { 0, 0 };
var counts = new[] { 25, 25 };
// Normalize visible widths and foot heights to the existing normal octopus.
var ppu = new[] { 245f / 204f * 200f, 210f / 204f * 112f };
var pivotPixels = new[] { new UnityEngine.Vector2(160, 68), new UnityEngine.Vector2(125, 22) };
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
    importer.maxTextureSize = 2048; importer.isReadable = true;
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
        if (sheet == 1) {
            int minX=x1, maxX=x0, minY=y1;
            for(int py=y0;py<y1;py++) for(int px=x0;px<x1;px++) {
                if(texture.GetPixel(px,py).a < 0.5f) continue;
                minX=System.Math.Min(minX,px); maxX=System.Math.Max(maxX,px); minY=System.Math.Min(minY,py);
            }
            pivotPixels[sheet]=new UnityEngine.Vector2((minX+maxX+1)/2f-x0,minY-y0);
        }
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

var clipPaths = new[] { "Assets/Animation/OctopusUncrownedWaterAttack.anim", "Assets/Animation/OctopusKingWaterShot.anim" };
var clips = new UnityEngine.AnimationClip[2];
var rates = new[] {24f,40f};
for(int i=0;i<2;i++) {
 var clip=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(clipPaths[i]);
 if(clip==null){clip=new UnityEngine.AnimationClip();UnityEditor.AssetDatabase.CreateAsset(clip,clipPaths[i]);}
 clip.frameRate=rates[i];
 var keys=new UnityEditor.ObjectReferenceKeyframe[25];
 for(int n=0;n<25;n++) keys[n]=new UnityEditor.ObjectReferenceKeyframe{time=n/rates[i],value=frames[i][n]};
 UnityEditor.AnimationUtility.SetObjectReferenceCurve(clip,UnityEditor.EditorCurveBinding.PPtrCurve("",typeof(UnityEngine.SpriteRenderer),"m_Sprite"),keys);
 var settings=UnityEditor.AnimationUtility.GetAnimationClipSettings(clip); settings.startTime=0;settings.stopTime=25/rates[i];settings.loopTime=false;
 UnityEditor.AnimationUtility.SetAnimationClipSettings(clip,settings);UnityEditor.EditorUtility.SetDirty(clip);clips[i]=clip;
}
var normal=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animation/Octopus.controller");
System.Array.Find(normal.layers[0].stateMachine.states,s=>s.state.name=="Attack").state.motion=clips[0];UnityEditor.EditorUtility.SetDirty(normal);
var king=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animation/OctopusKing.controller");
var machine=king.layers[0].stateMachine;
var shot=System.Array.Find(machine.states,s=>s.state.name=="WaterShot").state;
if(shot==null) shot=machine.AddState("WaterShot"); shot.motion=clips[1];
foreach(var t in shot.transitions) shot.RemoveTransition(t);
var move=System.Array.Find(machine.states,s=>s.state.name=="Move").state;
var spin=System.Array.Find(machine.states,s=>s.state.name=="SpinWater").state;
var a=shot.AddTransition(move);a.hasExitTime=true;a.exitTime=1;a.duration=0;a.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot,0,"SpinWater");
var b=shot.AddTransition(spin);b.hasExitTime=true;b.exitTime=1;b.duration=0;b.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If,0,"SpinWater");
UnityEditor.EditorUtility.SetDirty(king);
var pathsPrefab=new[]{"Assets/Resources/Prefabs/Enemy/Octopus.prefab","Assets/Resources/Prefabs/Boss/OctopusKing.prefab"};
for(int i=0;i<2;i++){
 var go=UnityEditor.PrefabUtility.LoadPrefabContents(pathsPrefab[i]);
 try {
  var so=new UnityEditor.SerializedObject(i==0?(UnityEngine.Object)go.GetComponent<RamgedEnemyAttack>():go.GetComponent<OctopusKing>());
  if(i==0) so.FindProperty("attackReleaseDelay").floatValue=9f/24f;
  else {so.FindProperty("waterShotAnimation").objectReferenceValue=clips[1];so.FindProperty("waterShotReleaseDelay").floatValue=9f/40f;}
  so.ApplyModifiedPropertiesWithoutUndo();UnityEditor.PrefabUtility.SaveAsPrefabAsset(go,pathsPrefab[i]);
 } finally {UnityEditor.PrefabUtility.UnloadPrefabContents(go);}
}
UnityEditor.AssetDatabase.SaveAssets();
return "Applied normal and crowned boss water shots: 25 frames each; normal24fps release0.375s, boss40fps release0.225s. Per-frame ground pivots fixed.";
