var loader=UnityEngine.Object.FindFirstObjectByType<StartupLoading>();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
if(loader==null||!(bool)typeof(StartupLoading).GetField("waitingForStart",flags).GetValue(loader))throw new System.Exception("Not ready");
var group=(UnityEngine.CanvasGroup)typeof(StartupLoading).GetField("transitionFade",flags).GetValue(loader);
var pending=(UnityEngine.AsyncOperation)typeof(StartupLoading).GetField("pendingScene",flags).GetValue(loader);
if(pending.allowSceneActivation)throw new System.Exception("Activated without input");
System.IO.File.WriteAllText("Tools/Loading/StartGateVerification.txt","PASS: actual loading reached 100.00/100 and waits for input with CutSceen preloaded.\n");
var mouse=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("TouchStart Verification");
bool intermediate=false,full=false,gateClosed=false;int samples=0;double begin=UnityEditor.EditorApplication.timeSinceStartup;
UnityEditor.EditorApplication.CallbackFunction observe=null;
observe=()=>{try{
 if(!UnityEditor.EditorApplication.isPlaying){UnityEditor.EditorApplication.update-=observe;if(mouse.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);return;}
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
 if(scene!="Loading"){
  System.IO.File.AppendAllText("Tools/Loading/StartGateVerification.txt","Input transition: scene="+scene+" intermediate fade="+intermediate+" full black="+full+" gate disabled after click="+gateClosed+" samples="+samples+"\n");
  UnityEditor.EditorApplication.update-=observe;if(mouse.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);return;
 }
 samples++;if(group!=null){intermediate|=group.alpha>0&&group.alpha<1;full|=group.alpha>=.999f;}
 if(loader!=null)gateClosed|=!(bool)typeof(StartupLoading).GetField("waitingForStart",flags).GetValue(loader);
 if(UnityEditor.EditorApplication.timeSinceStartup-begin>15){System.IO.File.AppendAllText("Tools/Loading/StartGateVerification.txt","FAIL transition timeout\n");UnityEditor.EditorApplication.update-=observe;if(mouse.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);}
}catch{UnityEditor.EditorApplication.update-=observe;if(mouse.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);throw;}};
UnityEditor.EditorApplication.update+=observe;
UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState().WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
return "Fresh pointer press queued; observing fade and scene activation.";
