var report="Tools/Rendering/OctopusCrabBatchesAfter.txt";
System.IO.File.WriteAllText(report,"Octopus, crab and projectiles isolated rendering comparison\n");
var oldStart=UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene;
UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>("Assets/Scenes/Lobby.unity");
int variant=0,countIndex=0,lastFrame=0,settledFrame=-1,baseline=0;
var counts=new[]{0,1,5,10};
var paths=new[]{"Enemy/Octopus","Enemy/Crap","Boss/OctopusKing","Bullet/WaterCannon","Bullet/WaterCannon 1","Bullet/WaterBamb","Enemy/Octopus","Enemy/Crap","Boss/OctopusKing"};
int assetIndex=0;
UnityEngine.Camera camera=null;
var instances=new System.Collections.Generic.List<UnityEngine.GameObject>();
var samples=new System.Collections.Generic.List<int>();
double started=UnityEditor.EditorApplication.timeSinceStartup;
UnityEditor.EditorApplication.CallbackFunction tick=null;
System.Action finish=()=>{
 UnityEditor.EditorApplication.update-=tick;
 UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=oldStart;
 UnityEditor.EditorApplication.isPlaying=false;
};
System.Action isolate=()=>{
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
 camera=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Camera>(true)).First(c=>c.name=="Main Camera");
 foreach(var root in scene.GetRootGameObjects())if(!camera.transform.IsChildOf(root.transform))root.SetActive(false);
 for(var p=camera.transform;p!=null;p=p.parent)p.gameObject.SetActive(true);
 foreach(var brain in camera.GetComponents<UnityEngine.MonoBehaviour>())if(brain is Unity.Cinemachine.CinemachineBrain)brain.enabled=false;
 foreach(var c in UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>(UnityEngine.FindObjectsSortMode.None))if(c!=camera)c.enabled=false;
 foreach(var canvas in UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(UnityEngine.FindObjectsSortMode.None))canvas.enabled=false;
 if(StageManager.Instance!=null)StageManager.Instance.StopRun();
 if(StageManager.Instance!=null)StageManager.Instance.gameObject.SetActive(false);
 camera.enabled=true;
 System.IO.File.AppendAllText(report,$"variant {variant}: scene={scene.name}, camera={camera.transform.position}, ortho={camera.orthographicSize}, sorting={camera.transparencySortMode}/{camera.transparencySortAxis}\n");
};
System.Action spawn=()=>{
 if(countIndex==0)System.IO.File.AppendAllText(report,$"sorting variant={variant}\n");
 foreach(var obj in instances)if(obj!=null)UnityEngine.Object.DestroyImmediate(obj);instances.Clear();
 var prefab=UnityEngine.Resources.Load<UnityEngine.GameObject>("Prefabs/"+paths[assetIndex]);
 if(countIndex==0)System.IO.File.AppendAllText(report,"asset="+paths[assetIndex]+" ice="+(assetIndex>=6)+"\n");
 for(int i=0;i<counts[countIndex];i++){
  var holder=new UnityEngine.GameObject("RenderProbe");holder.SetActive(false);instances.Add(holder);
  var obj=UnityEngine.Object.Instantiate(prefab,holder.transform);
  obj.transform.position=new UnityEngine.Vector3((i%5-2)*1.5f,(i-(counts[countIndex]-1)*.5f)*.35f,0);
  foreach(var behaviour in obj.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true))behaviour.enabled=false;
  foreach(var body in obj.GetComponentsInChildren<UnityEngine.Rigidbody2D>(true))body.simulated=false;
  foreach(var agent in obj.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true))agent.enabled=false;
  holder.SetActive(true);
  foreach(var animator in obj.GetComponentsInChildren<UnityEngine.Animator>(true)){animator.Update(.18f);animator.speed=0;}
  var particles=obj.GetComponentsInChildren<UnityEngine.ParticleSystem>(true);
  foreach(var particle in particles)particle.Stop(true,UnityEngine.ParticleSystemStopBehavior.StopEmittingAndClear);
  if(assetIndex>=6)foreach(var particle in particles.Where(p=>p.transform.parent==obj.transform)){
   particle.gameObject.SetActive(true);particle.useAutoRandomSeed=false;particle.randomSeed=42;particle.Simulate(.18f,true,true,true);particle.Pause(true);
  }
 }
 settledFrame=UnityEngine.Time.frameCount;samples.Clear();
};
tick=()=>{try{
 if(!UnityEditor.EditorApplication.isPlaying)return;
 UnityEngine.Application.runInBackground=true;
 if(UnityEditor.EditorApplication.timeSinceStartup-started>150)throw new System.Exception("Timeout");
 if(lastFrame==UnityEngine.Time.frameCount)return;lastFrame=UnityEngine.Time.frameCount;
 if(camera==null){if(UnityEngine.Time.frameCount<15)return;isolate();spawn();return;}
 if(UnityEngine.Time.frameCount-settledFrame<12)return;
 samples.Add(UnityEditor.UnityStats.batches);
 if(samples.Count<7)return;
 samples.Sort();int batches=samples[samples.Count/2];if(countIndex==0)baseline=batches;
 System.IO.File.AppendAllText(report,$"count={counts[countIndex]} batches={batches} delta={batches-baseline} setPass={UnityEditor.UnityStats.setPassCalls}\n");
 if(++countIndex<counts.Length){spawn();return;}
 countIndex=0;
 if(++assetIndex<paths.Length){spawn();return;}
 assetIndex=0;variant++;
 if(variant==1){camera=null;UnityEngine.SceneManagement.SceneManager.LoadScene("Stage1");return;}
 System.IO.File.AppendAllText(report,"COMPLETE\n");finish();
}catch(System.Exception e){System.IO.File.AppendAllText(report,"FAIL "+e+"\n");finish();}};
UnityEditor.EditorApplication.update+=tick;
var gameView=typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView");UnityEditor.EditorWindow.GetWindow(gameView).Show();
UnityEditor.EditorApplication.isPlaying=true;
return "Comparing original Lobby/Stage cameras, material-separated sorting and Z-axis sorting";
