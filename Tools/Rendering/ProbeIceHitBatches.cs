var report="Tools/Rendering/IceHitBatches.txt";
System.IO.File.WriteAllText(report,"Ice Hit isolated scene batching comparison\n");
var oldStart=UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene;
UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>("Assets/Scenes/Lobby.unity");
int variant=0,countIndex=0,lastFrame=0,settledFrame=-1,baseline=0;
var counts=new[]{0,1,5,10};
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
 StageManager.Instance?.StopRun();
 if(StageManager.Instance!=null)StageManager.Instance.gameObject.SetActive(false);
 camera.enabled=true;
 System.IO.File.AppendAllText(report,$"variant {variant}: scene={scene.name}, camera={camera.transform.position}, ortho={camera.orthographicSize}, sorting={camera.transparencySortMode}/{camera.transparencySortAxis}\n");
};
System.Action spawn=()=>{
 if(countIndex==0)System.IO.File.AppendAllText(report,$"sorting variant={variant}\n");
 foreach(var obj in instances)if(obj!=null)UnityEngine.Object.DestroyImmediate(obj);instances.Clear();
 if(variant==3){camera.transparencySortMode=UnityEngine.TransparencySortMode.CustomAxis;camera.transparencySortAxis=UnityEngine.Vector3.forward;}
 var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/GameAssets/Matthew Guz/Hits Effects FREE/Prefab/Ice Hit .prefab");
 for(int i=0;i<counts[countIndex];i++){
  var obj=UnityEngine.Object.Instantiate(prefab,new UnityEngine.Vector3((i%5-2)*1.5f,(i-(counts[countIndex]-1)*.5f)*.35f,0),UnityEngine.Quaternion.identity);
  obj.name="BatchProbe "+i;instances.Add(obj);
  var renderers=obj.GetComponentsInChildren<UnityEngine.ParticleSystemRenderer>(true);
  if(variant==2)for(int n=0;n<renderers.Length;n++)renderers[n].sortingOrder=1+n;
  var particle=obj.GetComponent<UnityEngine.ParticleSystem>();
  particle.useAutoRandomSeed=false;particle.randomSeed=42;
  particle.Simulate(.18f,true,true,true);particle.Pause(true);
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
 if(counts[countIndex]==10&&(variant==1||variant==2))UnityEngine.ScreenCapture.CaptureScreenshot("/Users/hong-yeseong/Documents/GitHub/Conquest/Tools/Rendering/ice-variant-"+variant+".png");
 if(++countIndex<counts.Length){spawn();return;}
 countIndex=0;variant++;
 if(variant==1){camera=null;UnityEngine.SceneManagement.SceneManager.LoadScene("Stage1");return;}
 if(variant<4){spawn();return;}
 System.IO.File.AppendAllText(report,"COMPLETE\n");finish();
}catch(System.Exception e){System.IO.File.AppendAllText(report,"FAIL "+e+"\n");finish();}};
UnityEditor.EditorApplication.update+=tick;
var gameView=typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView");UnityEditor.EditorWindow.GetWindow(gameView).Show();
UnityEditor.EditorApplication.isPlaying=true;
return "Comparing original Lobby/Stage cameras, material-separated sorting and Z-axis sorting";
