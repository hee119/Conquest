if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Do not interrupt an active game.");
var lobbyScene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Lobby.unity");var stageScene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Stage1.unity");
SettingsPause Find(UnityEngine.SceneManagement.Scene scene){foreach(var root in scene.GetRootGameObjects()){var result=root.GetComponentInChildren<SettingsPause>(true);if(result!=null)return result;}return null;}
var source=Find(lobbyScene);var previous=Find(stageScene);if(source==null||previous==null)throw new System.Exception("Missing settings roots");
string temp="Assets/Temp/StageSettingsSource.prefab";if(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(temp)!=null)throw new System.Exception("Temporary prefab already exists");
var asset=UnityEditor.PrefabUtility.SaveAsPrefabAsset(source.gameObject,temp);if(asset==null)throw new System.Exception("Cannot capture source settings");
UnityEngine.GameObject replacement=null;
try{
 replacement=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(asset,previous.transform.parent);
 replacement.SetActive(false);replacement.name=previous.name;replacement.transform.SetSiblingIndex(previous.transform.GetSiblingIndex());
 UnityEditor.PrefabUtility.UnpackPrefabInstance(replacement,UnityEditor.PrefabUnpackMode.OutermostRoot,UnityEditor.InteractionMode.AutomatedAction);
 var quit=previous.transform.Find("Quit");if(quit==null)throw new System.Exception("Missing stage action buttons");quit.SetParent(replacement.transform,false);
 var close=replacement.transform.Find("Close");var handlers=close.GetComponents<OpenAndClose>();var handler=System.Array.Find(handlers,h=>h.obj==replacement);if(handler==null)handler=handlers.Length>0?handlers[0]:close.gameObject.AddComponent<OpenAndClose>();handler.obj=replacement;
 var closeButton=close.GetComponent<UnityEngine.UI.Button>();for(int i=closeButton.onClick.GetPersistentEventCount()-1;i>=0;i--)UnityEditor.Events.UnityEventTools.RemovePersistentListener(closeButton.onClick,i);UnityEditor.Events.UnityEventTools.AddPersistentListener(closeButton.onClick,handler.OpenAndCloseFunction);foreach(var extra in handlers)if(extra!=handler)UnityEngine.Object.DestroyImmediate(extra);
 // Redirect scene controls from the old settings subtree to their matching replacements.
 int references=0;
 foreach(var root in stageScene.GetRootGameObjects())foreach(var component in root.GetComponentsInChildren<UnityEngine.Component>(true)){
  if(component==null||component.transform.IsChildOf(previous.transform))continue;
  var so=new UnityEditor.SerializedObject(component);var property=so.GetIterator();bool changed=false;
  while(property.Next(true)){
   if(property.propertyType!=UnityEditor.SerializedPropertyType.ObjectReference)continue;
   var value=property.objectReferenceValue;var target=value as UnityEngine.GameObject;var targetComponent=value as UnityEngine.Component;var transform=target!=null?target.transform:targetComponent!=null?targetComponent.transform:null;
   if(transform==null||!transform.IsChildOf(previous.transform))continue;
   string path=UnityEditor.AnimationUtility.CalculateTransformPath(transform,previous.transform);var next=string.IsNullOrEmpty(path)?replacement.transform:replacement.transform.Find(path);if(next==null)throw new System.Exception("Missing remap target "+path);
   UnityEngine.Object mapped=target!=null?(UnityEngine.Object)next.gameObject:next.GetComponent(value.GetType());if(mapped==null)throw new System.Exception("Missing mapped component "+path);property.objectReferenceValue=mapped;changed=true;references++;
  }
  if(changed)so.ApplyModifiedPropertiesWithoutUndo();
 }
 foreach(var button in quit.GetComponentsInChildren<UnityEngine.UI.Button>(true)){
  if(button.onClick.GetPersistentEventCount()!=1)throw new System.Exception("Unexpected action wiring: "+button.name);
  var image=button.GetComponent<UnityEngine.UI.Image>();image.sprite=null;image.material=null;image.color=UnityEngine.Color.black;var outline=button.GetComponent<UnityEngine.UI.Outline>();if(outline==null)outline=button.gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=UnityEngine.Color.white;outline.effectDistance=new UnityEngine.Vector2(2,-2);foreach(var text in button.GetComponentsInChildren<TMPro.TMP_Text>(true))text.color=UnityEngine.Color.white;
 }
 bool active=previous.gameObject.activeSelf;UnityEngine.Object.DestroyImmediate(previous.gameObject);replacement.SetActive(active);
 int prefabRows=0;foreach(var row in replacement.GetComponentsInChildren<SettingsOptionRowLayout>(true)){if(string.IsNullOrEmpty(UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(row.gameObject)))throw new System.Exception("Lost existing row prefab: "+row.name);prefabRows++;}
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(stageScene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(stageScene);
 var report="Saved Stage1 using Lobby settings visuals; "+prefabRows+" existing row prefab instances retained; "+references+" scene references remapped. Graphic/Sound/Language retained. Footer: ReturnToLobby and QuitConfirmationDialog.Show, one Inspector event each. Close: one self-toggle event; no Lobby scene references.";
 System.IO.File.WriteAllText("Tools/UI/StageSettingsApplied.txt",report);return report;
}finally{UnityEditor.AssetDatabase.DeleteAsset(temp);}
