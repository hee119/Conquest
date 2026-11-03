if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Use edit mode");
var output=new System.Collections.Generic.List<object>();
System.Action<UnityEngine.GameObject,string> collect=(root,asset)=>{
 foreach(var text in root.GetComponentsInChildren<TMPro.TMP_Text>(true)){
  var local=text.GetComponent<LocalizedText>();
  var names=new System.Collections.Generic.List<string>();
  for(var p=text.transform;p!=null;p=p.parent)names.Add(p.name);
  names.Reverse();
  output.Add(new{asset,path=string.Join("/",names),text=text.text,key=local==null?null:local.key,
    components=text.GetComponents<UnityEngine.Component>().Where(c=>c!=null).Select(c=>c.GetType().Name).ToArray()});
 }
};
foreach(var build in UnityEditor.EditorBuildSettings.scenes.Where(s=>s.enabled)){
 var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(build.path);
 bool opened=!scene.isLoaded;
 if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(build.path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
 foreach(var root in scene.GetRootGameObjects())collect(root,build.path);
 if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
}
foreach(var guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Resources"})){
 var path=UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
 collect(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path),path);
}
System.IO.File.WriteAllText("Tools/Localization/text-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(output,Newtonsoft.Json.Formatting.Indented));
return output.Count;
