if(UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Edit mode required.");
var plan=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("Tools/Art/SpriteYAlignmentPlan.json"));
var factory=new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();factory.Init();
// Preflight all importers before changing any asset.
foreach(var row in plan){
 var path=(string)row["path"];var importer=UnityEditor.AssetImporter.GetAtPath(path);
 var provider=factory.GetSpriteEditorDataProviderFromObject(importer);
 if(provider==null)throw new System.Exception("Missing provider: "+path);
 provider.InitSpriteEditorDataProvider();
 var cap=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
 if(cap==null || !cap.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.EditPivot))throw new System.Exception("Cannot edit pivot: "+path);
}
var report=new System.Collections.Generic.List<string>();
var worldOffsets=new System.Collections.Generic.Dictionary<string,float>();
foreach(var row in plan){
 string path=(string)row["path"];float delta=(float)row["pivotDeltaPixels"];
 var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
 worldOffsets.Add(path,-delta/importer.spritePixelsPerUnit);
 var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
 var rects=provider.GetSpriteRects();
 var before=Newtonsoft.Json.JsonConvert.SerializeObject(System.Array.ConvertAll(rects,r=>new {name=r.name,id=r.spriteID.ToString(),rect=new[]{r.rect.x,r.rect.y,r.rect.width,r.rect.height},pivot=new[]{r.pivot.x,r.pivot.y},alignment=(int)r.alignment}));
 row["spriteRectsBefore"]=Newtonsoft.Json.Linq.JToken.Parse(before);
 row["ppuBefore"]=importer.spritePixelsPerUnit;
 foreach(var rect in rects){rect.alignment=UnityEngine.SpriteAlignment.Custom;rect.pivot=new UnityEngine.Vector2(rect.pivot.x,rect.pivot.y+delta/rect.rect.height);}
 provider.SetSpriteRects(rects);provider.Apply();importer.SaveAndReimport();
 provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
 var after=provider.GetSpriteRects();
 if(after.Length!=rects.Length)throw new System.Exception("Count changed: "+path);
 foreach(var rect in after){var expected=System.Array.Find(rects,r=>r.name==rect.name);if(expected==null||expected.spriteID!=rect.spriteID||expected.rect!=rect.rect||UnityEngine.Vector2.Distance(expected.pivot,rect.pivot)>.00001f)throw new System.Exception("Verification failed: "+rect.name);}
 if(importer.spritePixelsPerUnit!=(float)row["ppuBefore"])throw new System.Exception("Scale changed");
 report.Add(path+" | pivot Y offset="+delta+"px | sprites="+after.Length);
}
// Mouth positions are stored relative to sprite pivots; follow the same Y shift.
var bossPath="Assets/Resources/Prefabs/Boss/OctopusKing.prefab";
var boss=UnityEditor.PrefabUtility.LoadPrefabContents(bossPath);
int anchors=0;
try{
 var anchor=boss.GetComponent<OctopusMouthAnchor>();
 if(anchor!=null){var so=new UnityEditor.SerializedObject(anchor);var frames=so.FindProperty("frames");
 for(int i=0;i<frames.arraySize;i++){var frame=frames.GetArrayElementAtIndex(i);var sprite=frame.FindPropertyRelative("sprite").objectReferenceValue;float dy;if(sprite!=null&&worldOffsets.TryGetValue(UnityEditor.AssetDatabase.GetAssetPath(sprite),out dy)){var p=frame.FindPropertyRelative("position");var v=p.vector2Value;v.y+=dy;p.vector2Value=v;anchors++;}}
 so.ApplyModifiedPropertiesWithoutUndo();if(anchors>0)UnityEditor.PrefabUtility.SaveAsPrefabAsset(boss,bossPath);
 }
}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(boss);}
// Check every audited animation still has all original sprite references and times.
var audit=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("Tools/Art/SpriteYAlignmentAudit.json"));
int tracks=0;
foreach(var row in audit){
 var clip=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>((string)row["clip"]);
 var binding=UnityEditor.EditorCurveBinding.PPtrCurve((string)row["binding"],typeof(UnityEngine.SpriteRenderer),"m_Sprite");
 var keys=UnityEditor.AnimationUtility.GetObjectReferenceCurve(clip,binding);var old=(Newtonsoft.Json.Linq.JArray)row["frames"];
 if(keys.Length!=old.Count)throw new System.Exception("Animation count mismatch: "+clip.name);
 for(int i=0;i<keys.Length;i++){var sprite=keys[i].value as UnityEngine.Sprite;if(sprite==null||sprite.name!=(string)old[i]["name"]||UnityEditor.AssetDatabase.GetAssetPath(sprite)!=(string)old[i]["path"]||UnityEngine.Mathf.Abs(keys[i].time-(float)old[i]["time"])>.00001f)throw new System.Exception("Broken animation: "+clip.name);}
 tracks++;
}
UnityEditor.AssetDatabase.SaveAssets();
System.IO.File.WriteAllText("Tools/Art/SpriteYAlignmentApplied.json",plan.ToString());
report.Add("PASS: "+plan.Count+" sheets aligned; "+tracks+" animation tracks preserved; "+anchors+" mouth offsets followed Y correction. Move sheets, X pivots, artwork, PPU and animation timing unchanged. Constant sheet offsets preserve pose/jump motion.");
System.IO.File.WriteAllLines("Tools/Art/SpriteYAlignmentVerification.txt",report);
return string.Join("\n",report);
