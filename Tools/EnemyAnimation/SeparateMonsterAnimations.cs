var names=new[]{"BombSlime","Crap","Octopus","Slime","Bull","Eagle","Goat","Pufferfish","Turtle"};
var types=new[]{typeof(BombSlimeSpriteAnimation),typeof(CrabSpriteAnimation),typeof(OctopusSpriteAnimation),typeof(SlimeSpriteAnimation),typeof(BullSpriteAnimation),typeof(EagleSpriteAnimation),typeof(GoatSpriteAnimation),typeof(PufferfishSpriteAnimation),typeof(TurtleSpriteAnimation)};
var lines=new System.Collections.Generic.List<string>();
for(int i=0;i<names.Length;i++){
 var path="Assets/Resources/Prefabs/Enemy/"+names[i]+".prefab";
 var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
 try{
  var old=root.GetComponent<EnemySpriteAnimation>();
  var seqs=old!=null?old.sequences:new EnemySpriteAnimation.Sequence[0];
  var defaultState=old!=null?old.defaultState:"";
  if(old!=null)UnityEngine.Object.DestroyImmediate(old,true);
  var current=(EnemySpriteAnimation)root.AddComponent(types[i]);
  current.sequences=seqs;current.defaultState=defaultState;
  var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
  foreach(var s in seqs){
   var field=s.state=="Attack"?"attackFrames":s.state=="Hit"?"hitFrames":s.state==defaultState?"moveFrames":null;
   if(field!=null){typeof(EnemySpriteAnimation).GetField(field,flags).SetValue(current,s.frames.Select(f=>f.sprite).ToArray());for(int j=0;j<s.frames.Length;j++){var f=s.frames[j];f.sprite=null;s.frames[j]=f;}}
   if(names[i]=="Octopus"&&s.state=="Attack"){
    var ranged=root.GetComponent<RamgedEnemyAttack>();var so=new UnityEditor.SerializedObject(ranged);
    var delay=so.FindProperty("attackReleaseDelay").floatValue;
    s.events=s.events.Concat(new[]{new EnemySpriteAnimation.Event{time=delay,function="FireProjectile"}}).OrderBy(e=>e.time).ToArray();
   }
  }
  UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path);
  lines.Add(names[i]+" "+current.GetType().Name+" states="+seqs.Length);
 }finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
}
foreach(var name in new[]{"OctopusKing","SlimeKing"}){
 var path="Assets/Resources/Prefabs/Boss/"+name+".prefab";var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
 try{
  var old=root.GetComponent<EnemySpriteAnimation>();if(old!=null)UnityEngine.Object.DestroyImmediate(old,true);
  var animator=root.GetComponent<UnityEngine.Animator>();if(animator==null)animator=root.AddComponent<UnityEngine.Animator>();
  animator.runtimeAnimatorController=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.RuntimeAnimatorController>("Assets/Animation/"+name+".controller");
  animator.cullingMode=UnityEngine.AnimatorCullingMode.AlwaysAnimate;
  var boss=root.GetComponent<OctopusKing>();if(boss!=null)boss.animator=animator;
  UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path);
  lines.Add(name+" restored Animator");
 }finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
}
return string.Join("\n",lines);
