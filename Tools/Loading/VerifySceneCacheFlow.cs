var report="Tools/Loading/scene-cache-flow.txt";
System.IO.File.WriteAllText(report, "Scene cache runtime verification\n");
int step=0, playerId=0, lastFrame=-1, settled=0, fadeUp=0, fadeDown=0;
float previousAlpha=1f;
double started=UnityEditor.EditorApplication.timeSinceStartup;
UnityEditor.EditorApplication.CallbackFunction verify=null;
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
verify=()=>{
    try
    {
        if(!UnityEditor.EditorApplication.isPlaying) return;
        UnityEngine.Application.runInBackground=true;
        if(UnityEditor.EditorApplication.timeSinceStartup-started>150) throw new System.Exception("Verification timed out at step "+step);
        if(lastFrame==UnityEngine.Time.frameCount)return;
        lastFrame=UnityEngine.Time.frameCount;
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(step==0)
        {
            var loading=UnityEngine.Object.FindFirstObjectByType<StartupLoading>();
            if(loading==null || !(bool)typeof(StartupLoading).GetField("waitingForStart",flags).GetValue(loading))return;
            if(UnityEngine.SceneManagement.SceneManager.sceneCount!=4 || StageManager.Instance!=null)throw new System.Exception("Dormant scenes ran gameplay or were not prepared.");
            System.IO.File.AppendAllText(report,"PASS: all four scenes resident, gameplay dormant during loading\n");
            typeof(StartupLoading).GetMethod("OnStartPressed",flags).Invoke(loading,new object[]{default(UnityEngine.InputSystem.InputAction.CallbackContext)});
            step=1;
        }
        else if(step==1 && scene.name=="CutSceen")
        {
            var cut=UnityEngine.Object.FindFirstObjectByType<CutSceneController>();
            var color=((UnityEngine.UI.Image)typeof(CutSceneController).GetField("fadeOverlay",flags).GetValue(cut)).color;
            if(color.r!=0 || color.g!=0 || color.b!=0)throw new System.Exception("Cutscene fade is not pure black.");
            if(color.a>0 && color.a<1){if(color.a>previousAlpha)fadeUp++; if(color.a<previousAlpha)fadeDown++;}
            previousAlpha=color.a;
            if((int)typeof(CutSceneController).GetField("currentFrame",flags).GetValue(cut)<1)return;
            if(fadeUp<2 || fadeDown<2)throw new System.Exception("Missing progressive fade in or out.");
            System.IO.File.AppendAllText(report,$"PASS: fade in/out intermediate samples {fadeDown}/{fadeUp}, shared duration {GameSceneManager.TransitionFadeSeconds}\n");
            cut.SkipCutscene();step=2;
        }
        else if(step==2 && scene.name=="Lobby")
        {
            UnityEngine.Object.FindFirstObjectByType<StagePlanetMenu>(UnityEngine.FindObjectsInactive.Include).PlaySelectedStage();step=3;settled=0;
        }
        else if(step==3 && scene.name=="Stage1")
        {
            if(++settled<8)return;
            var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();
            if(player==null || StageManager.Instance==null)throw new System.Exception("Stage failed to initialize.");
            playerId=player.GetInstanceID();player.Die();
            UnityEngine.Object.FindFirstObjectByType<GameEndScreenController>(UnityEngine.FindObjectsInactive.Include).RetryStage();step=4;settled=0;
        }
        else if(step==4 && scene.name=="Stage1")
        {
            if(++settled<8)return;
            var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();
            var end=UnityEngine.Object.FindFirstObjectByType<GameEndScreenController>(UnityEngine.FindObjectsInactive.Include);
            if(player==null || player.GetInstanceID()!=playerId || (bool)typeof(GameEndScreenController).GetField("isShowing",flags).GetValue(end))throw new System.Exception("Retry did not reset the cached scene.");
            GameEndScreenController.ShowGameOver();
            if(!(bool)typeof(GameEndScreenController).GetField("isShowing",flags).GetValue(end))throw new System.Exception("Second game over failed.");
            end.ReturnToLobby();step=5;
            System.IO.File.AppendAllText(report,"PASS: retry reused the same player; second game-over UI opened\n");
        }
        else if(step==5 && scene.name=="Lobby")
        {
            UnityEngine.Object.FindFirstObjectByType<StagePlanetMenu>(UnityEngine.FindObjectsInactive.Include).PlaySelectedStage();step=6;settled=0;
        }
        else if(step==6 && scene.name=="Stage1")
        {
            if(++settled<8)return;
            var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();
            if(player==null || player.GetInstanceID()!=playerId || UnityEngine.SceneManagement.SceneManager.sceneCount!=4)throw new System.Exception("Scene was reloaded instead of reused.");
            System.IO.File.AppendAllText(report,"PASS: lobby return and stage re-entry reused the same scene/player\nCOMPLETE\n");
            UnityEditor.EditorApplication.update-=verify;
            UnityEngine.Time.timeScale=1;UnityEditor.EditorApplication.isPlaying=false;
        }
    }
    catch(System.Exception e)
    {
        System.IO.File.AppendAllText(report,"FAIL: "+e+"\n");
        UnityEditor.EditorApplication.update-=verify;
        UnityEngine.Time.timeScale=1;UnityEditor.EditorApplication.isPlaying=false;
    }
};
UnityEditor.EditorApplication.update+=verify;
UnityEditor.EditorApplication.isPlaying=true;
return "Started cache/fade/retry verification";
