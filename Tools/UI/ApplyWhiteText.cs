if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required.");
var report=new System.Collections.Generic.List<string>();
foreach(var path in new[]{"Assets/Scenes/Lobby.unity","Assets/Scenes/Stage1.unity"}){
 var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
 if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
 try{
  foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<TMPro.TMP_Text>(true)){
   var c=t.color;bool tinted=UnityEngine.Mathf.Abs(c.r-c.g)>0.01f||UnityEngine.Mathf.Abs(c.g-c.b)>0.01f;
   if(!tinted&&t.name!="Next")continue;
   UnityEditor.Undo.RecordObject(t,"Use white text");t.color=new UnityEngine.Color(1f,1f,1f,c.a);UnityEditor.EditorUtility.SetDirty(t);
   if(UnityEditor.PrefabUtility.IsPartOfPrefabInstance(t))UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(t);
   report.Add(path+"\t"+t.name+"\t"+t.text.Replace("\n"," "));
  }
  UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
  if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.Exception("Save failed");
 }finally{if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
}
System.IO.File.WriteAllLines("Tools/UI/WhiteTextChanges.tsv",report);
UnityEditor.AssetDatabase.Refresh();
return string.Join("\n",report);
