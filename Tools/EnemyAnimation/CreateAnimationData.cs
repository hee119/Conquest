const string folder="Assets/Resources/EnemyAnimationData";
if(!UnityEditor.AssetDatabase.IsValidFolder(folder))UnityEditor.AssetDatabase.CreateFolder("Assets/Resources","EnemyAnimationData");
var names=new[]{"BombSlime","Crap","Octopus","Slime","Bull","Eagle","Goat","Pufferfish","Turtle"};var report=new System.Collections.Generic.List<string>();
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
foreach(var name in names){
 var path="Assets/Resources/Prefabs/Enemy/"+name+".prefab";var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
 try{
  var anim=root.GetComponent<EnemySpriteAnimation>();if(anim==null)throw new System.Exception("Missing animation component "+name);
  var assetPath=folder+"/"+name+"Animation.asset";
  var data=UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyAnimationData>(assetPath);
  if(data!=null)throw new System.Exception("Data already exists, refusing to overwrite: "+assetPath);
  data=UnityEngine.ScriptableObject.CreateInstance<EnemyAnimationData>();
  data.attack=anim.AttackFrames??new UnityEngine.Sprite[0];data.move=anim.MoveFrames??new UnityEngine.Sprite[0];data.hit=anim.HitFrames??new UnityEngine.Sprite[0];data.sequences=anim.sequences;data.defaultState=anim.defaultState;
  UnityEditor.AssetDatabase.CreateAsset(data,assetPath);
  typeof(EnemySpriteAnimation).GetField("animationData",flags).SetValue(anim,data);
  typeof(EnemySpriteAnimation).GetField("attackFrames",flags).SetValue(anim,null);
  typeof(EnemySpriteAnimation).GetField("moveFrames",flags).SetValue(anim,null);
  typeof(EnemySpriteAnimation).GetField("hitFrames",flags).SetValue(anim,null);
  anim.sequences=null;anim.defaultState=null;
  UnityEditor.EditorUtility.SetDirty(anim);UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path);
  report.Add(name+" move="+data.move.Length+" attack="+data.attack.Length+" hit="+data.hit.Length);
 }finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
}
UnityEditor.AssetDatabase.SaveAssets();return string.Join("\n",report);
