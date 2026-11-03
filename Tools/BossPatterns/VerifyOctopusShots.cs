var report="Tools/BossPatterns/OctopusShotsVerification.txt";
System.IO.File.WriteAllText(report,"Octopus animation-event pattern verification\n");
int phase=0,lastFrame=-1,stageWait=0,animationStarts=0;
uint baseline=0; bool wasShot=false;float previousTime=0;
OctopusKing boss=null;
var bursts=new System.Collections.Generic.List<float[]>();
var seen=new System.Collections.Generic.Dictionary<int,uint>();
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
double started=UnityEditor.EditorApplication.timeSinceStartup;
System.Func<uint> totalSpawns=()=>PoolManager.Instance.GetComponentsInChildren<PooledObject>(true)
    .Where(p=>p.GetComponent<BulletInfo>()!=null&&p.GetComponent<BulletInfo>().id==601)
    .Aggregate(0u,(sum,p)=>sum+p.SpawnVersion);
System.Action begin=()=>{
    baseline=totalSpawns();animationStarts=0;wasShot=false;previousTime=0;bursts.Clear();seen.Clear();
    foreach(var p in PoolManager.Instance.GetComponentsInChildren<PooledObject>(true))seen[p.GetInstanceID()]=p.SpawnVersion;
    if(phase==1)boss.PredictiveWaterCannonAttack();
    else if(phase==2)boss.FanWaterCannonAttack();
    else boss.RadialWaterCannonAttack();
};
UnityEditor.EditorApplication.CallbackFunction verify=null;
verify=()=>{
try{
 if(!UnityEditor.EditorApplication.isPlaying)return;
 UnityEngine.Application.runInBackground=true;
 if(UnityEditor.EditorApplication.timeSinceStartup-started>100)throw new System.Exception("Timed out at phase "+phase);
 if(lastFrame==UnityEngine.Time.frameCount)return;lastFrame=UnityEngine.Time.frameCount;
 if(phase==0){
  if(StageManager.Instance==null||PoolManager.Instance==null||++stageWait<12)return;
  StageManager.Instance.StopRun();PoolManager.Instance.ResetActiveObjects();
  var player=UnityEngine.GameObject.FindGameObjectWithTag("Player");
  player.GetComponent<PlayerCombat>().maxHp=player.GetComponent<PlayerCombat>().hp=100000;
  player.GetComponent<PlayerMove>().enabled=false;
  boss=PoolManager.Instance.Spawn(150,(UnityEngine.Vector2)player.transform.position+UnityEngine.Vector2.up*8).GetComponent<OctopusKing>();
  boss.runPatternsAutomatically=false;
  var move=boss.GetComponent<EnemyMove>();if(move!=null)move.enabled=false;
  var agent=boss.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent!=null)agent.enabled=false;
  phase=1;begin();return;
 }
 var state=boss.animator.GetCurrentAnimatorStateInfo(0);
 bool isShot=state.shortNameHash==UnityEngine.Animator.StringToHash("WaterShot");
 if(isShot&&(!wasShot||state.normalizedTime<previousTime))animationStarts++;
 wasShot=isShot;previousTime=state.normalizedTime;
 var angles=new System.Collections.Generic.List<float>();
 foreach(var p in PoolManager.Instance.GetComponentsInChildren<PooledObject>(true)){
  var info=p.GetComponent<BulletInfo>();if(info==null||info.id!=601)continue;
  int id=p.GetInstanceID();
  if(!seen.TryGetValue(id,out uint version)||version!=p.SpawnVersion){
   var velocity=p.GetComponent<UnityEngine.Rigidbody2D>().linearVelocity;
   angles.Add(UnityEngine.Mathf.Atan2(velocity.y,velocity.x)*UnityEngine.Mathf.Rad2Deg);
   seen[id]=p.SpawnVersion;
  }
 }
 if(angles.Count>0)bursts.Add(angles.ToArray());
 if((bool)typeof(OctopusKing).GetField("attacking",flags).GetValue(boss))return;
 uint fired=totalSpawns()-baseline;int expected=phase==1?5:50;
 if(fired!=expected||animationStarts!=5||bursts.Count!=5)throw new System.Exception($"phase {phase}: fired={fired}/{expected}, animationStarts={animationStarts}, bursts={bursts.Count}");
 System.IO.File.AppendAllText(report,$"PASS phase {phase}: {animationStarts} animation replays; {fired} projectiles; {bursts.Count} event bursts\n");
 for(int i=0;i<bursts.Count;i++)System.IO.File.AppendAllText(report,"burst "+i+": "+string.Join(",",bursts[i].Select(a=>a.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)))+"\n");
 if(++phase<=3){begin();return;}
 System.IO.File.AppendAllText(report,"COMPLETE\n");
 UnityEditor.EditorApplication.update-=verify;UnityEngine.Time.timeScale=1;UnityEditor.EditorApplication.isPlaying=false;
}catch(System.Exception e){
 System.IO.File.AppendAllText(report,"FAIL: "+e+"\n");
 UnityEditor.EditorApplication.update-=verify;UnityEngine.Time.timeScale=1;UnityEditor.EditorApplication.isPlaying=false;
}};
UnityEditor.EditorApplication.update+=verify;
var gameView=typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView");
UnityEditor.EditorWindow.GetWindow(gameView).Show();
UnityEditor.EditorApplication.isPlaying=true;
return "Started actual animation-event burst test";
