if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Use edit mode");
var changes=new System.Collections.Generic.List<string>();
var map=new System.Collections.Generic.Dictionary<string,string>{
 {"Combine","upgrade_combine"},{"강화하기","character_enhance_action"},
 {"합성하기","upgrade_combine"},{"확인하기","upgrade_confirm"},
 {"합성할 만렙 증강 두 개를 선택하세요","upgrade_select_materials"},
 {"Name","upgrade_name_placeholder"},{"Description","upgrade_description_placeholder"}
};
System.Action<UnityEngine.GameObject,string> apply=(root,asset)=>{
 foreach(var text in root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true)){
  if(text.name=="Game Title"||text.text.Contains("Planetary Pioneer"))continue;
  string key=null,value=text.text.Trim();
  if(!map.TryGetValue(value,out key)&&text.name=="Selected Stage Label")key="stage_selected_number";
  if(key==null)continue;
  // These labels are already generated and refreshed by the progression view.
  var progression=root.GetComponentsInChildren<CharacterProgressionPanel>(true);
  if(progression.Any(p=>new UnityEditor.SerializedObject(p).FindProperty("buttonText").objectReferenceValue==text))continue;
  var local=text.GetComponent<LocalizedText>();
  if(local==null)local=text.gameObject.AddComponent<LocalizedText>();
  if(!string.IsNullOrEmpty(local.key))continue;
  local.key=key;local.isTextInit=true;
  UnityEditor.EditorUtility.SetDirty(local);
  if(UnityEditor.PrefabUtility.IsPartOfPrefabInstance(local))UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(local);
  changes.Add(asset+"/"+text.name+" -> "+key);
 }
};
// Update reusable templates first, then only the scene-specific missing bindings.
foreach(var path in new[]{"Assets/Resources/Prefabs/Upgrade/Upgrade.prefab"}){
 var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
 try{apply(root,path);UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path);}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
}
foreach(var build in UnityEditor.EditorBuildSettings.scenes.Where(s=>s.enabled)){
 var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(build.path);bool opened=!scene.isLoaded;
 if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(build.path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
 int before=changes.Count;
 foreach(var root in scene.GetRootGameObjects())apply(root,build.path);
 if(changes.Count!=before){UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);}
 if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
}
UnityEditor.AssetDatabase.SaveAssets();
System.IO.File.WriteAllLines("Tools/Localization/bindings.txt",changes);
return changes;
