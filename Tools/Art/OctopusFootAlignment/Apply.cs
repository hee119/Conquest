if(UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Apply in edit mode before starting verification.");
string path="Assets/SpriteSeet/OctopusKingWaterShot.png";
var factory=new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();factory.Init();
var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
var caps=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
if(caps==null || !caps.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.EditSpriteRect) || !caps.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.EditPivot))throw new System.Exception("Unsupported sprite editing");
var rects=provider.GetSpriteRects();if(rects.Length!=25)throw new System.Exception("Expected 25 sprites");
var texture=new UnityEngine.Texture2D(2,2);UnityEngine.ImageConversion.LoadImage(texture,System.IO.File.ReadAllBytes(path));
var move=UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/SpriteSeet/OctopusKingMove.png").OfType<UnityEngine.Sprite>().OrderBy(s=>s.name).First();
var mt=new UnityEngine.Texture2D(2,2);UnityEngine.ImageConversion.LoadImage(mt,System.IO.File.ReadAllBytes("Assets/SpriteSeet/OctopusKingMove.png"));
int foot=0;bool found=false;for(int y=(int)move.rect.yMin;y<(int)move.rect.yMax&&!found;y++)for(int x=(int)move.rect.xMin;x<(int)move.rect.xMax;x++)if(mt.GetPixel(x,y).a>=.5f){foot=y-(int)move.rect.yMin;found=true;break;}
float baseline=(foot-move.pivot.y)/move.pixelsPerUnit;
var offsets=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("Tools/Art/OctopusFootAlignment/alignment.json"));
var delta=new System.Collections.Generic.Dictionary<string,float>();
foreach(var r in rects){
 int i=int.Parse(r.name.Substring(r.name.LastIndexOf('_')+1));int row=i/5,col=i%5;
 int x0=UnityEngine.Mathf.RoundToInt(col*1254/5f),x1=UnityEngine.Mathf.RoundToInt((col+1)*1254/5f);
 int top=UnityEngine.Mathf.RoundToInt(row*1254/5f),bottom=UnityEngine.Mathf.RoundToInt((row+1)*1254/5f);int y0=1254-bottom;
 int oldFoot=0;bool ok=false;for(int y=(int)r.rect.yMin;y<(int)r.rect.yMax&&!ok;y++)for(int x=(int)r.rect.xMin;x<(int)r.rect.xMax;x++)if(texture.GetPixel(x,y).a>=.5f){oldFoot=y;ok=true;break;}
 float oldWorld=(oldFoot-r.rect.yMin-r.pivot.y*r.rect.height)/importer.spritePixelsPerUnit;
 float newFoot=bottom-top-(int)offsets[i]["foot_after"];
 float px=r.rect.xMin+r.pivot.x*r.rect.width-x0;
 r.rect=new UnityEngine.Rect(x0,y0,x1-x0,bottom-top);r.alignment=UnityEngine.SpriteAlignment.Custom;
 r.pivot=new UnityEngine.Vector2(px/r.rect.width,(newFoot-baseline*importer.spritePixelsPerUnit)/r.rect.height);
 delta[r.name]=baseline-oldWorld;
}
System.IO.File.Copy(path,"Tools/Art/OctopusFootAlignment/OriginalWaterShot.png",true);
System.IO.File.Copy("Tools/Art/OctopusFootAlignment/OctopusKingWaterShotFeetAligned.png",path,true);
provider.SetSpriteRects(rects);provider.Apply();importer.SaveAndReimport();
var prefab=UnityEditor.PrefabUtility.LoadPrefabContents("Assets/Resources/Prefabs/Boss/OctopusKing.prefab");
try{var anchor=prefab.GetComponent<OctopusMouthAnchor>();var so=new UnityEditor.SerializedObject(anchor);var frames=so.FindProperty("frames");for(int i=0;i<frames.arraySize;i++){var f=frames.GetArrayElementAtIndex(i);var s=f.FindPropertyRelative("sprite").objectReferenceValue as UnityEngine.Sprite;if(s!=null&&UnityEditor.AssetDatabase.GetAssetPath(s)==path&&delta.TryGetValue(s.name,out float dy)){var p=f.FindPropertyRelative("position");var v=p.vector2Value;v.y+=dy;p.vector2Value=v;}}so.ApplyModifiedPropertiesWithoutUndo();UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab,"Assets/Resources/Prefabs/Boss/OctopusKing.prefab");}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(prefab);}
var clip=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>("Assets/Animation/OctopusKingWaterShot.anim");var keys=UnityEditor.AnimationUtility.GetObjectReferenceCurve(clip,UnityEditor.EditorCurveBinding.PPtrCurve("",typeof(UnityEngine.SpriteRenderer),"m_Sprite"));if(keys.Length!=25||keys.Any(k=>k.value==null))throw new System.Exception("Animation references invalid");
UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(mt);
return "Applied 25 pixel-aligned frames, sprite IDs/animation references retained; world foot baseline="+baseline+"; mouth anchors follow correction.";
