if(UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Edit mode required.");
var jobs=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("Tools/PixelFx/OctopusMouthFrames.json"));
// Normalize foot pivots while preserving every existing sprite ID and rectangle.
foreach(var path in new[]{"Assets/SpriteSeet/OctopusKingWaterShot.png","Assets/SpriteSeet/OctopusKingGroundAttack.png"}){
 var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
 var factory=new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();factory.Init();
 var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
 var cap=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
 if(cap==null||!cap.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.EditPivot))throw new System.Exception("Pivot editing unavailable");
 var rects=provider.GetSpriteRects();
 foreach(var rect in rects)foreach(var job in jobs)if((string)job["path"]==path&&(string)job["name"]==rect.name){rect.alignment=UnityEngine.SpriteAlignment.Custom;rect.pivot=new UnityEngine.Vector2((float)job["pivot"][0],(float)job["pivot"][1]);break;}
 provider.SetSpriteRects(rects);provider.Apply();importer.SaveAndReimport();
}
var bossAsset=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Resources/Prefabs/Boss/OctopusKing.prefab");
int sorting=bossAsset.GetComponent<UnityEngine.SpriteRenderer>().sortingLayerID;
UnityEngine.GameObject AddVisual(UnityEngine.GameObject root,string id,float scale,int order){
 var old=root.transform.Find("PixelFX Visual");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
 var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/PixelFx/Sheets/conquest/"+id+"/"+id+".prefab");
 var visual=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(asset,root.transform);
 visual.name="PixelFX Visual";visual.transform.localPosition=UnityEngine.Vector3.zero;visual.transform.localRotation=UnityEngine.Quaternion.identity;visual.transform.localScale=UnityEngine.Vector3.one*scale;
 foreach(var renderer in visual.GetComponentsInChildren<UnityEngine.SpriteRenderer>()){renderer.sortingLayerID=sorting;renderer.sortingOrder=order;}
 return visual;
}
void HideLegacy(UnityEngine.GameObject root){
 var renderer=root.GetComponent<UnityEngine.SpriteRenderer>();if(renderer!=null)renderer.enabled=false;
 var animator=root.GetComponent<UnityEngine.Animator>();if(animator!=null)animator.enabled=false;
}
// New pooled one-shot splash prefabs; IDs are distinct from the existing gameplay objects.
foreach(var name in new[]{"water-cannon-impact","water-bomb-impact"}){
 int id=name=="water-cannon-impact"?1003:1004;
 string path="Assets/Resources/Prefabs/BossPattern/"+name+".prefab";
 var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/PixelFx/Sheets/conquest/"+name+"/"+name+".prefab");
 var go=UnityEngine.Object.Instantiate(source);go.name=name;
 try{
  var info=go.GetComponent<BossPatternInfo>();if(info==null)info=go.AddComponent<BossPatternInfo>();info.id=id;info.damage=0;info.name=name;
  if(go.GetComponent<PooledObject>()==null)go.AddComponent<PooledObject>();
  go.GetComponent<UnityEngine.SpriteRenderer>().sortingLayerID=sorting;go.GetComponent<UnityEngine.SpriteRenderer>().sortingOrder=15;
  UnityEditor.PrefabUtility.SaveAsPrefabAsset(go,path);
 }finally{UnityEngine.Object.DestroyImmediate(go);}
}
// Keep original attack prefab IDs, physics and damage values, replacing only their presentation.
string hydroPath="Assets/Resources/Prefabs/BossPattern/HydroPump.prefab";
var hydro=UnityEditor.PrefabUtility.LoadPrefabContents(hydroPath);
try{
 for(int i=hydro.transform.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(hydro.transform.GetChild(i).gameObject);
 hydro.transform.localScale=UnityEngine.Vector3.one;
 var visual=AddVisual(hydro,"hydro-pump",1,10);
 var body=hydro.GetComponent<UnityEngine.Rigidbody2D>();if(body==null)body=hydro.AddComponent<UnityEngine.Rigidbody2D>();body.bodyType=UnityEngine.RigidbodyType2D.Kinematic;body.gravityScale=0;body.constraints=UnityEngine.RigidbodyConstraints2D.FreezeRotation;
 var box=hydro.GetComponent<UnityEngine.BoxCollider2D>();box.isTrigger=true;box.offset=new UnityEngine.Vector2(1.16f,0);box.size=new UnityEngine.Vector2(2.32f,.22f);
 var so=new UnityEditor.SerializedObject(hydro.GetComponent<HydroPump>());so.FindProperty("beamBodyTagetScaleY").floatValue=3;so.FindProperty("beamBodyScaleSpeed").floatValue=14;so.FindProperty("beamLengthScale").floatValue=4;so.ApplyModifiedPropertiesWithoutUndo();
 UnityEditor.PrefabUtility.SaveAsPrefabAsset(hydro,hydroPath);
}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(hydro);}
foreach(var name in new[]{"WaterCannon","WaterBamb"}){
 string path="Assets/Resources/Prefabs/Bullet/"+name+".prefab";
 var go=UnityEditor.PrefabUtility.LoadPrefabContents(path);
 try{
  bool cannon=name=="WaterCannon";HideLegacy(go);go.transform.localScale=UnityEngine.Vector3.one;go.transform.localPosition=UnityEngine.Vector3.zero;
  AddVisual(go,cannon?"water-cannon":"water-bomb",3,10);
  var old=go.GetComponent<PredictionBulletMove>();if(old!=null&&!(old is OctopusWaterCannonProjectile))UnityEngine.Object.DestroyImmediate(old);
  var body=go.GetComponent<UnityEngine.Rigidbody2D>();body.gravityScale=0;body.linearVelocity=UnityEngine.Vector2.zero;body.angularVelocity=0;body.constraints=UnityEngine.RigidbodyConstraints2D.FreezeRotation;
  body.bodyType=cannon?UnityEngine.RigidbodyType2D.Dynamic:UnityEngine.RigidbodyType2D.Kinematic;
  var circle=go.GetComponent<UnityEngine.CircleCollider2D>();circle.isTrigger=true;circle.offset=UnityEngine.Vector2.zero;circle.radius=cannon?.41f:.9f;circle.enabled=cannon;
  if(cannon){var projectile=go.GetComponent<OctopusWaterCannonProjectile>();if(projectile==null)projectile=go.AddComponent<OctopusWaterCannonProjectile>();var so=new UnityEditor.SerializedObject(projectile);so.FindProperty("predictTime").floatValue=.2f;so.FindProperty("impactEffectId").intValue=1003;so.ApplyModifiedPropertiesWithoutUndo();}
  UnityEditor.PrefabUtility.SaveAsPrefabAsset(go,path);
 }finally{UnityEditor.PrefabUtility.UnloadPrefabContents(go);}
}
string groundPath="Assets/Resources/Prefabs/BossPattern/GroundCrack.prefab";
var ground=UnityEditor.PrefabUtility.LoadPrefabContents(groundPath);
try{
 HideLegacy(ground);ground.transform.localScale=UnityEngine.Vector3.one;ground.transform.localPosition=UnityEngine.Vector3.zero;
 foreach(var collider in ground.GetComponents<UnityEngine.Collider2D>())collider.enabled=false;
 var visual=AddVisual(ground,"radial-stone-eruption",4f/1.3f,-2);
 var previous=ground.transform.Find("Damage Area");if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
 var area=new UnityEngine.GameObject("Damage Area");area.transform.SetParent(ground.transform,false);var trigger=area.AddComponent<UnityEngine.CircleCollider2D>();trigger.radius=1;trigger.isTrigger=true;
 var rb=ground.GetComponent<UnityEngine.Rigidbody2D>();if(rb==null)rb=ground.AddComponent<UnityEngine.Rigidbody2D>();rb.bodyType=UnityEngine.RigidbodyType2D.Kinematic;rb.gravityScale=0;
 var life=ground.GetComponent<OctopusEffectLifetime>();if(life==null)life=ground.AddComponent<OctopusEffectLifetime>();
 var so=new UnityEditor.SerializedObject(ground.GetComponent<GroundCrack>());so.FindProperty("visual").objectReferenceValue=visual.transform;so.FindProperty("damageArea").objectReferenceValue=area.transform;so.FindProperty("radius").floatValue=4;so.ApplyModifiedPropertiesWithoutUndo();
 UnityEditor.PrefabUtility.SaveAsPrefabAsset(ground,groundPath);
}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(ground);}
// Loop only the turning, open-mouth frames, omitting preparation and closed-mouth recovery.
var original=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>("Assets/Animation/OctopusKingSpin.anim");
var binding=UnityEditor.AnimationUtility.GetObjectReferenceCurveBindings(original)[0];
var oldKeys=UnityEditor.AnimationUtility.GetObjectReferenceCurve(original,binding);
string loopPath="Assets/Animation/OctopusKingHydroLoop.anim";
var loop=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(loopPath);if(loop==null){loop=new UnityEngine.AnimationClip();UnityEditor.AssetDatabase.CreateAsset(loop,loopPath);}loop.frameRate=12;
var keys=new UnityEditor.ObjectReferenceKeyframe[16];for(int i=0;i<16;i++)keys[i]=new UnityEditor.ObjectReferenceKeyframe{time=i/12f,value=oldKeys[i+4].value};
UnityEditor.AnimationUtility.SetObjectReferenceCurve(loop,binding,keys);
var settings=UnityEditor.AnimationUtility.GetAnimationClipSettings(loop);settings.loopTime=true;settings.startTime=0;settings.stopTime=16f/12f;UnityEditor.AnimationUtility.SetAnimationClipSettings(loop,settings);UnityEditor.EditorUtility.SetDirty(loop);
var controller=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animation/OctopusKing.controller");
foreach(var state in controller.layers[0].stateMachine.states)if(state.state.name=="SpinWater")state.state.motion=loop;
UnityEditor.EditorUtility.SetDirty(controller);
string bossPath="Assets/Resources/Prefabs/Boss/OctopusKing.prefab";
var boss=UnityEditor.PrefabUtility.LoadPrefabContents(bossPath);
try{
 var mouth=boss.transform.Find("Mouth");if(mouth==null){mouth=new UnityEngine.GameObject("Mouth").transform;mouth.SetParent(boss.transform,false);}
 var anchor=boss.GetComponent<OctopusMouthAnchor>();if(anchor==null)anchor=boss.AddComponent<OctopusMouthAnchor>();
 var so=new UnityEditor.SerializedObject(anchor);so.FindProperty("mouth").objectReferenceValue=mouth;so.FindProperty("body").objectReferenceValue=boss.GetComponent<UnityEngine.SpriteRenderer>();
 var frames=so.FindProperty("frames");frames.arraySize=jobs.Count;
 for(int i=0;i<jobs.Count;i++){
  var job=jobs[i];UnityEngine.Sprite sprite=null;
  foreach(var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath((string)job["path"]))if(asset is UnityEngine.Sprite candidate&&candidate.name==(string)job["name"]){sprite=candidate;break;}
  if(sprite==null)throw new System.Exception("Missing sprite "+job["name"]);
  var entry=frames.GetArrayElementAtIndex(i);entry.FindPropertyRelative("sprite").objectReferenceValue=sprite;
  entry.FindPropertyRelative("position").vector2Value=new UnityEngine.Vector2(((float)job["mouth"][0]-sprite.pivot.x)/sprite.pixelsPerUnit,(sprite.rect.height-(float)job["mouth"][1]-sprite.pivot.y)/sprite.pixelsPerUnit);
  entry.FindPropertyRelative("angle").floatValue=(float)job["angle"];
 }
 so.ApplyModifiedPropertiesWithoutUndo();
 var king=boss.GetComponent<OctopusKing>();king.waterSpawnPoint=mouth.gameObject;king.animator=boss.GetComponent<UnityEngine.Animator>();king.spinAnimation=loop;king.rotationCount=1;
 king.groundAttackAnimation=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>("Assets/Animation/OctopusKingGroundAttack.anim");king.groundStrikeCount=1;king.groundAttackRadius=4;
 king.waterShotReleaseDelay=10f/40f;king.waterBombImpactEffectId=1004;king.waterBombTargetOffsetRange=1;
 king.bulletId=601;king.waterBombId=602;king.waterStreamObjectId=1000;king.groundAttackSkillId=1001;
 mouth.localPosition=frames.GetArrayElementAtIndex(0).FindPropertyRelative("position").vector2Value;
 UnityEditor.PrefabUtility.SaveAsPrefabAsset(boss,bossPath);
}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(boss);}
UnityEditor.AssetDatabase.SaveAssets();
return "Applied six PixelFX effects to octopus attack prefabs; mouth frame bindings="+jobs.Count+"; release=0.25s; spin loop=16 frames; pool IDs preserved, impacts=1003/1004.";
