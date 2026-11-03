var paths=UnityEditor.AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Resources/Prefabs/Enemy","Assets/Resources/Prefabs/Boss"}).Select(UnityEditor.AssetDatabase.GUIDToAssetPath).ToArray();
var report=new System.Collections.Generic.List<string>();
foreach(var path in paths){
 var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
 var source=prefab.GetComponent<UnityEngine.Animator>();
 if(source==null || source.runtimeAnimatorController==null)continue;
 var controller=source.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
 if(controller==null)throw new System.Exception("Unsupported controller "+path);
 var states=controller.layers[0].stateMachine.states;
 string canonical(string name){if(name=="CrapWalk")return "Move";if(name=="CrapAttack")return "Attack";if(name=="CrapHit")return "Hit";return name;}
 var sequences=new System.Collections.Generic.List<EnemySpriteAnimation.Sequence>();
 foreach(var entry in states){
  var state=entry.state;var clip=state.motion as UnityEngine.AnimationClip;
  var seq=new EnemySpriteAnimation.Sequence{state=canonical(state.name),speed=state.speed,duration=clip==null?0:clip.length,frames=new EnemySpriteAnimation.Frame[0],events=new EnemySpriteAnimation.Event[0]};
  if(clip!=null){
   var bindings=UnityEditor.AnimationUtility.GetObjectReferenceCurveBindings(clip);
   var binding=bindings.FirstOrDefault(b=>b.path==""&&b.propertyName=="m_Sprite");
   if(binding.propertyName!="m_Sprite"||UnityEditor.AnimationUtility.GetCurveBindings(clip).Length!=0)throw new System.Exception("Unsupported clip "+clip.name);
   seq.frames=UnityEditor.AnimationUtility.GetObjectReferenceCurve(clip,binding).Select(k=>new EnemySpriteAnimation.Frame{time=k.time,sprite=k.value as UnityEngine.Sprite}).ToArray();
   seq.events=UnityEditor.AnimationUtility.GetAnimationEvents(clip).Select(e=>new EnemySpriteAnimation.Event{time=e.time,function=e.functionName}).ToArray();
   foreach(var e in seq.events)if(e.function!="Attack"&&e.function!="Boom"&&e.function!="PlayJumpSound"&&e.function!="FireWaterCannon")throw new System.Exception("Unsupported event "+e.function);
   seq.loop=clip.isLooping;
  }
  var n=seq.state;
  if(n=="Attack"||n=="Hit"||n=="WaterShot"||n=="GroundCrack")seq.loop=false;
  if(n=="Attack"||((n=="Hit")&&!path.Contains("Boss/SlimeKing"))||n=="WaterShot"||n=="GroundCrack")seq.next=canonical(controller.layers[0].stateMachine.defaultState.name);
  if(n=="SlideStart")seq.next="SlideLoop";
  sequences.Add(seq);
 }
 var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
 try{
  var old=root.GetComponent<UnityEngine.Animator>();
  var player=root.GetComponent<EnemySpriteAnimation>();if(player==null)player=root.AddComponent<EnemySpriteAnimation>();
  player.sequences=sequences.ToArray();player.defaultState=canonical(controller.layers[0].stateMachine.defaultState.name);
  var boss=root.GetComponent<OctopusKing>();if(boss!=null)boss.animator=player;
  UnityEngine.Object.DestroyImmediate(old,true);
  UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path);
  report.Add(path+" states="+string.Join(",",sequences.Select(s=>s.state+":"+s.frames.Length+"/"+s.events.Length)));
 }finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
}
return string.Join("\n",report);
