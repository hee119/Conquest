// Only changes the active normal octopus attack sprites' vertical pivot.
if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Edit mode required.");
var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animation/Octopus.controller");
var states = controller.layers[0].stateMachine.states;
var moveClip = System.Array.Find(states, s => s.state.name == "Run").state.motion as UnityEngine.AnimationClip;
var attackClip = System.Array.Find(states, s => s.state.name == "Attack").state.motion as UnityEngine.AnimationClip;
var binding = UnityEditor.EditorCurveBinding.PPtrCurve("", typeof(UnityEngine.SpriteRenderer), "m_Sprite");
var moveKeys = UnityEditor.AnimationUtility.GetObjectReferenceCurve(moveClip, binding);
var attackKeys = UnityEditor.AnimationUtility.GetObjectReferenceCurve(attackClip, binding);
var reference = moveKeys[0].value as UnityEngine.Sprite;
var attackReference = attackKeys[0].value as UnityEngine.Sprite;
if(reference == null || attackReference == null) throw new System.Exception("Missing animation sprites.");
// Decode a temporary copy to measure opaque artwork without changing texture settings.
System.Func<UnityEngine.Sprite, float> footY = sprite => {
    var texture = new UnityEngine.Texture2D(2, 2);
    try {
        UnityEngine.ImageConversion.LoadImage(texture, System.IO.File.ReadAllBytes(UnityEditor.AssetDatabase.GetAssetPath(sprite)));
        var rect = sprite.rect;
        var pixels = texture.GetPixels32();
        for(int y=(int)rect.yMin; y<(int)rect.yMax; y++)
            for(int x=(int)rect.xMin; x<(int)rect.xMax; x++)
                if(pixels[y*texture.width+x].a >= 128) return y-rect.yMin;
        throw new System.Exception("Empty reference frame.");
    } finally { UnityEngine.Object.DestroyImmediate(texture); }
};
float baseline = (footY(reference)-reference.pivot.y)/reference.pixelsPerUnit;
float current = (footY(attackReference)-attackReference.pivot.y)/attackReference.pixelsPerUnit;
float delta = (current-baseline)*attackReference.pixelsPerUnit;
string path = UnityEditor.AssetDatabase.GetAssetPath(attackReference);
var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
var factory = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories(); factory.Init();
var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
if(provider==null) throw new System.Exception("Missing sprite provider.");
provider.InitSpriteEditorDataProvider();
var capability=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
if(capability==null || !capability.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.EditPivot))
    throw new System.Exception("Pivot editing unsupported; aborted.");
var rects=provider.GetSpriteRects();
var before = new System.Collections.Generic.Dictionary<string,UnityEngine.Vector2>();
foreach(var r in rects) {
    before.Add(r.name,r.pivot);
    r.pivot = new UnityEngine.Vector2(r.pivot.x,r.pivot.y+delta/r.rect.height);
}
provider.SetSpriteRects(rects); provider.Apply(); importer.SaveAndReimport();
provider=factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
foreach(var r in provider.GetSpriteRects()) {
    var old=before[r.name];
    if(r.pivot.x!=old.x || UnityEngine.Mathf.Abs((r.pivot.y-old.y)*r.rect.height-delta)>0.001f)
        throw new System.Exception("Y-only verification failed.");
}
var fresh=UnityEditor.AnimationUtility.GetObjectReferenceCurve(attackClip,binding);
if(fresh.Length!=attackKeys.Length || System.Array.Exists(fresh,k=>k.value==null)) throw new System.Exception("Animation references broken.");
var report="PASS: Normal octopus attack aligned to first Move frame's opaque foot baseline.\nAsset: "+path+"\nMove baseline: "+baseline+" world units; attack before: "+current+"\nAttack pivot Y offset: +"+delta+" pixels (artwork moves down).\nFrames: "+rects.Length+"; animation keys: "+fresh.Length+". Only pivot Y changed; X, scale, artwork, timing and sprite IDs preserved.\n";
System.IO.File.WriteAllText("Tools/Art/OctopusYAlignmentVerification.txt",report);
return report;
