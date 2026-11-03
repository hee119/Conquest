if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
var lobby=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Lobby.unity");
var loading=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Loading.unity");
UnityEngine.Transform lobbyCanvas=null,loadingCanvas=null;
foreach(var go in lobby.GetRootGameObjects())if(go.name=="Lobby Canvas")lobbyCanvas=go.transform;
foreach(var go in loading.GetRootGameObjects())if(go.name=="Loading Canvas")loadingCanvas=go.transform;
if(lobbyCanvas==null||loadingCanvas==null)throw new System.Exception("Missing canvas");
var background=lobbyCanvas.Find("Starfield Frame");
var title=lobbyCanvas.Find("Lobby Navigation/MainMenu/Game Title");
if(background==null||title==null)throw new System.Exception("Missing lobby visuals");
foreach(var source in new[]{background,title}){
 var existing=loadingCanvas.Find(source.name);
 if(existing!=null)throw new System.Exception("Already exists: "+source.name);
 var clone=UnityEngine.Object.Instantiate(source.gameObject,loadingCanvas,false);clone.name=source.name;clone.SetActive(true);
 foreach(var graphic in clone.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))graphic.raycastTarget=false;
 if(source==background)clone.transform.SetAsFirstSibling();
}
// Match the lobby's scaling while retaining the loading controller and progress references.
var sourceScaler=lobbyCanvas.GetComponent<UnityEngine.UI.CanvasScaler>();
var scaler=loadingCanvas.GetComponent<UnityEngine.UI.CanvasScaler>();
scaler.uiScaleMode=sourceScaler.uiScaleMode;scaler.referenceResolution=sourceScaler.referenceResolution;scaler.screenMatchMode=sourceScaler.screenMatchMode;scaler.matchWidthOrHeight=sourceScaler.matchWidthOrHeight;
var bar=(UnityEngine.RectTransform)loadingCanvas.Find("Loading Experience Bar");
var label=(UnityEngine.RectTransform)loadingCanvas.Find("Loading Progress");
bar.anchorMin=bar.anchorMax=new UnityEngine.Vector2(.5f,0);bar.pivot=new UnityEngine.Vector2(.5f,.5f);bar.anchoredPosition=new UnityEngine.Vector2(0,115);bar.sizeDelta=new UnityEngine.Vector2(1100,70);
label.anchorMin=label.anchorMax=new UnityEngine.Vector2(.5f,0);label.pivot=new UnityEngine.Vector2(.5f,.5f);label.anchoredPosition=new UnityEngine.Vector2(0,205);label.sizeDelta=new UnityEngine.Vector2(1000,70);
label.GetComponent<TMPro.TMP_Text>().text="로딩 중... 0%";
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(loading);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(loading);
// Leave only the edited loading scene open, as it was before opening the source lobby.
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(lobby,true);
UnityEditor.Selection.activeGameObject=loadingCanvas.gameObject;
return "Loading saved: exact lobby background and title copied; progress label bottom+205, bar bottom+115; existing loading bindings retained.";
