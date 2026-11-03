var report="Tools/Loading/PreparedTilesVerification.txt";
System.IO.File.WriteAllText(report,"Loading terrain verification\n");
var old=UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene;
UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>("Assets/Scenes/Loading.unity");
double start=UnityEditor.EditorApplication.timeSinceStartup;
int phase=0,frame=0;
StageManager stage=null;
UnityEngine.Tilemaps.TileBase[] ground=null,decor=null;
UnityEngine.BoundsInt groundBounds=default,decorBounds=default;
UnityEditor.EditorApplication.CallbackFunction check=null;
System.Action finish=()=>{UnityEditor.EditorApplication.update-=check;UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=old;UnityEngine.Time.timeScale=1;UnityEditor.EditorApplication.isPlaying=false;};
check=()=>{try{
 if(!UnityEditor.EditorApplication.isPlaying)return;
 UnityEngine.Application.runInBackground=true;
 if(UnityEditor.EditorApplication.timeSinceStartup-start>120)throw new System.Exception("Timeout");
 if(phase==0){
  if(!GameSceneManager.AreScenesPrepared)return;
  var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName("Stage1");
  stage=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<StageManager>(true)).First();
  if(StageManager.Instance!=null||stage.gameObject.activeInHierarchy)throw new System.Exception("Stage activated during preload");
  var prepared=typeof(StageManager).GetField("preparedTerrain",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(stage);
  if(prepared==null)throw new System.Exception("Terrain was not prepared");
  groundBounds=stage.backgroundTilemap.cellBounds;ground=stage.backgroundTilemap.GetTilesBlock(groundBounds);
  decorBounds=stage.decorationTilemap.cellBounds;decor=stage.decorationTilemap.GetTilesBlock(decorBounds);
  int count=ground.Count(t=>t!=null),decorCount=decor.Count(t=>t!=null);
  if(count==0||decorCount==0)throw new System.Exception("Empty tiles");
  System.IO.File.AppendAllText(report,$"PASS during loading: ground={count}, decorations={decorCount}, gameplay inactive\n");
  GameSceneManager.LoadSceneAsync("Stage1");phase=1;frame=UnityEngine.Time.frameCount;return;
 }
 if(UnityEngine.Time.frameCount-frame<20)return;
 StageManager.Instance.StopRun();
 if(!ground.SequenceEqual(stage.backgroundTilemap.GetTilesBlock(groundBounds))||!decor.SequenceEqual(stage.decorationTilemap.GetTilesBlock(decorBounds)))throw new System.Exception("Terrain regenerated on entry");
 System.IO.File.AppendAllText(report,"PASS stage entry: prepared ground and decorations reused unchanged\nCOMPLETE\n");finish();
}catch(System.Exception e){System.IO.File.AppendAllText(report,"FAIL "+e+"\n");finish();}};
UnityEditor.EditorApplication.update+=check;
UnityEditor.EditorApplication.isPlaying=true;
return "Started loading-time tile preparation test";
