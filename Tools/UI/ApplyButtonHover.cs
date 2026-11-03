if (UnityEditor.EditorApplication.isPlaying || UnityEditor.EditorApplication.isCompiling) throw new System.Exception("Editor must be idle in edit mode.");
var report = new System.Collections.Generic.List<string>();
System.Func<UnityEngine.Transform,string> hierarchy = t => {var parts=new System.Collections.Generic.List<string>();while(t!=null){parts.Insert(0,t.name);t=t.parent;}return string.Join("/",parts);};
System.Func<UnityEngine.Color,bool> yellow = c => c.r > c.b + 0.1f && c.g > c.b + 0.08f && c.r >= c.g;
System.Action<UnityEngine.GameObject,string> apply = (root,path) => {
 foreach(var b in root.GetComponentsInChildren<UnityEngine.UI.Button>(true)) {
  bool changed=false;var colors=b.colors;
  if(yellow(colors.highlightedColor)){colors.highlightedColor=new UnityEngine.Color(0.96f,0.96f,0.96f,colors.highlightedColor.a);changed=true;}
  if(yellow(colors.pressedColor)){colors.pressedColor=new UnityEngine.Color(0.784f,0.784f,0.784f,colors.pressedColor.a);changed=true;}
  if(yellow(colors.selectedColor)){colors.selectedColor=new UnityEngine.Color(0.96f,0.96f,0.96f,colors.selectedColor.a);changed=true;}
  if(changed){UnityEditor.Undo.RecordObject(b,"Use neutral button hover");b.colors=colors;report.Add(path+"\tNeutral tint\t"+hierarchy(b.transform));}
  bool hasHover=b.GetComponents<UnityEngine.MonoBehaviour>().Any(c=>c!=null && c is UnityEngine.EventSystems.IPointerEnterHandler && !(c is UnityEngine.UI.Selectable));
  if(!hasHover && b.GetComponentInParent<UpgradeHover>()==null) {
   var image=b.GetComponent<UnityEngine.UI.Image>();
   if(image!=null){
    var hover=UnityEditor.Undo.AddComponent<OptionHover>(b.gameObject);
    if(image.color.a==0f){
     var outline=b.transform.parent.Find("HoverVisualRoot/OutLine");
     if(outline==null)throw new System.Exception("Transparent button has no hover visual: "+hierarchy(b.transform));
     var so=new UnityEditor.SerializedObject(hover);so.FindProperty("hoverOutline").objectReferenceValue=outline.gameObject;so.ApplyModifiedProperties();
     UnityEditor.Undo.RecordObject(outline.gameObject,"Initialize hover outline");outline.gameObject.SetActive(false);
    }
    if(b.transition==UnityEngine.UI.Selectable.Transition.None){UnityEditor.Undo.RecordObject(b,"Enable button tint");b.transition=UnityEngine.UI.Selectable.Transition.ColorTint;}
    report.Add(path+"\tAdd hover\t"+hierarchy(b.transform));
   } else if(b.name=="SkipText") {
    UnityEditor.Undo.RecordObject(b,"Use white skip hover");colors=b.colors;colors.highlightedColor=new UnityEngine.Color(0.8f,0.8f,0.8f,1f);b.colors=colors;
   } else throw new System.Exception("Button without a supported hover target: "+hierarchy(b.transform));
  }
  UnityEditor.EditorUtility.SetDirty(b);
  if(UnityEditor.PrefabUtility.IsPartOfPrefabInstance(b))UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(b);
 }
 foreach(var text in root.GetComponentsInChildren<TMPro.TMP_Text>(true)){
  if((text.name=="Gold"||text.name=="GoldWallet")&&text.color!=UnityEngine.Color.white){
   UnityEditor.Undo.RecordObject(text,"Use white gold text");text.color=UnityEngine.Color.white;UnityEditor.EditorUtility.SetDirty(text);
   if(UnityEditor.PrefabUtility.IsPartOfPrefabInstance(text))UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(text);
   report.Add(path+"\tWhite gold\t"+hierarchy(text.transform));
  }
 }
};
foreach(var path in new[]{"Assets/Resources/Prefabs/UI/Slider.prefab","Assets/Resources/Prefabs/UI/UpgradeCombine.prefab","Assets/Resources/Prefabs/UI/StageSelectButton.prefab"}){
 var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
 try{apply(root,path);UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path);}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
}
foreach(var path in new[]{"Assets/Scenes/CutSceen.unity","Assets/Scenes/Lobby.unity","Assets/Scenes/Stage1.unity"}){
 var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
 if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
 try{
  foreach(var root in scene.GetRootGameObjects())apply(root,path);
  UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
  if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.Exception("Scene save failed: "+path);
 }finally{if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
}
UnityEditor.AssetDatabase.SaveAssets();
System.IO.File.WriteAllLines("Tools/UI/ButtonHoverChanges.tsv",report);
return string.Join("\n",report);
