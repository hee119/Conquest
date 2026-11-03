if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Exit play mode before auditing assets.");
var rows = new System.Collections.Generic.List<string>();
rows.Add("Asset\tObject\tType\tTransition\tNormal\tHighlight\tPressed\tSelected\tTarget\tGraphicColor\tMaterial\tHandlers\tReferences");
System.Func<UnityEngine.Transform,string> hierarchy = t => { var parts = new System.Collections.Generic.List<string>(); while(t != null) {parts.Insert(0,t.name);t=t.parent;} return string.Join("/",parts); };
System.Action<UnityEngine.GameObject,string> inspect = (root,asset) => {
 foreach(var s in root.GetComponentsInChildren<UnityEngine.UI.Selectable>(true)) {
  var handlers=s.GetComponents<UnityEngine.Component>().Where(c=>c is UnityEngine.EventSystems.IPointerEnterHandler && !(c is UnityEngine.UI.Selectable)).Select(c=>c.GetType().Name);
  var refs=new System.Collections.Generic.List<string>();
  if(s.GetComponentInParent<UpgradeHover>()!=null)refs.Add("Parent=UpgradeHover");
  foreach(var c in s.GetComponents<UnityEngine.MonoBehaviour>()) { if(c==null)continue; var so=new UnityEditor.SerializedObject(c);var p=so.GetIterator();while(p.NextVisible(true))if(p.propertyType==UnityEditor.SerializedPropertyType.ObjectReference&&p.objectReferenceValue is UnityEngine.Material)refs.Add(c.GetType().Name+"."+p.name+"="+UnityEditor.AssetDatabase.GetAssetPath(p.objectReferenceValue)); }
  var g=s.targetGraphic; var col=s.colors;
  rows.Add(string.Join("\t",asset,hierarchy(s.transform),s.GetType().Name,s.transition,col.normalColor,col.highlightedColor,col.pressedColor,col.selectedColor,g==null?"NULL":hierarchy(g.transform),g==null?"":g.color.ToString(),g==null?"":UnityEditor.AssetDatabase.GetAssetPath(g.material),string.Join(",",handlers),string.Join(",",refs)));
 }
};
rows.Add("OPEN SCENES: "+ string.Join(";",Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(i=>{var s=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);return s.path+" dirty="+s.isDirty;})));
foreach(var path in UnityEditor.AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}).Select(UnityEditor.AssetDatabase.GUIDToAssetPath)) {
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(path);bool preview=true;
 try{foreach(var root in scene.GetRootGameObjects())inspect(root,path);}finally{if(preview)UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
}
foreach(var path in UnityEditor.AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Resources"}).Select(UnityEditor.AssetDatabase.GUIDToAssetPath)) {
 var root=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);inspect(root,path);
}
System.IO.File.WriteAllLines("Tools/UI/ButtonHoverAudit.tsv",rows);
return "Audit rows="+rows.Count;
