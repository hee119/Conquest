if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required.");
var report=new System.Collections.Generic.List<string>();
var errors=new System.Collections.Generic.List<string>();
int buttons=0, gold=0;
System.Func<UnityEngine.Color,bool> yellow=c=>c.r>c.b+0.1f&&c.g>c.b+0.08f&&c.r>=c.g;
System.Action<UnityEngine.GameObject,string> inspect=(root,path)=>{
 foreach(var b in root.GetComponentsInChildren<UnityEngine.UI.Button>(true)){
  buttons++;
  if(yellow(b.colors.highlightedColor)||yellow(b.colors.pressedColor))errors.Add(path+" yellow: "+b.name);
  var hover=b.GetComponent<OptionHover>();
  bool custom=b.GetComponents<UnityEngine.MonoBehaviour>().Any(c=>c!=null&&c is UnityEngine.EventSystems.IPointerEnterHandler&&!(c is UnityEngine.UI.Selectable))||b.GetComponentInParent<UpgradeHover>()!=null;
  bool tint=b.transition==UnityEngine.UI.Selectable.Transition.ColorTint&&b.targetGraphic!=null&&b.colors.normalColor!=b.colors.highlightedColor;
  if(!custom&&!tint)errors.Add(path+" missing: "+b.name);
  if(hover!=null && b.GetComponent<UnityEngine.UI.Image>().color.a==0f && b.GetComponent<StagePlanetButton>()==null){
   var outline=new UnityEditor.SerializedObject(hover).FindProperty("hoverOutline").objectReferenceValue as UnityEngine.GameObject;
   if(outline==null||outline.activeSelf)errors.Add(path+" invalid card outline: "+b.name);
  }
 }
 foreach(var t in root.GetComponentsInChildren<TMPro.TMP_Text>(true))if(t.name=="Gold"||t.name=="GoldWallet") {gold++;if(t.color!=UnityEngine.Color.white)errors.Add(path+" nonwhite gold");}
};
foreach(var path in new[]{"Assets/Scenes/CutSceen.unity","Assets/Scenes/Lobby.unity","Assets/Scenes/Stage1.unity"}){
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(path);
 try{foreach(var root in scene.GetRootGameObjects())inspect(root,path);}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
}
foreach(var path in UnityEditor.AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Resources"}).Select(UnityEditor.AssetDatabase.GUIDToAssetPath))inspect(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path),path);
report.Add("Serialized audit: "+buttons+" buttons; yellow/missing hover="+errors.Count+"; white gold="+gold);
var fixture=new UnityEngine.GameObject("HoverVerification",typeof(UnityEngine.RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));
fixture.hideFlags=UnityEngine.HideFlags.HideAndDontSave;
try{
 var image=fixture.GetComponent<UnityEngine.UI.Image>();var button=fixture.GetComponent<UnityEngine.UI.Button>();button.transition=UnityEngine.UI.Selectable.Transition.None;
 var hover=fixture.AddComponent<OptionHover>();hover.Awake();
 foreach(var alpha in new[]{1f,0.08f}){
  image.color=new UnityEngine.Color(1,1,1,alpha);hover.OnPointerEnter(null);
  float expected=alpha==1f?0.8f:0.35f;
  if(!UnityEngine.Mathf.Approximately(image.color.a,expected))errors.Add("Hover alpha failed "+alpha);
  hover.OnPointerEnter(null);if(!UnityEngine.Mathf.Approximately(image.color.a,expected))errors.Add("Duplicate enter compounds alpha");
  hover.OnPointerExit(null);if(!UnityEngine.Mathf.Approximately(image.color.a,alpha))errors.Add("Exit restoration failed");
 }
 image.color=UnityEngine.Color.white;button.interactable=false;hover.OnPointerEnter(null);if(image.color.a!=1f)errors.Add("Disabled button hovered");button.interactable=true;
 hover.OnPointerEnter(null);typeof(OptionHover).GetMethod("OnDisable",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(hover,null);if(image.color.a!=1f)errors.Add("Disable restoration failed");
 var outline=new UnityEngine.GameObject("Outline");outline.transform.SetParent(fixture.transform);outline.SetActive(false);
 var so=new UnityEditor.SerializedObject(hover);so.FindProperty("hoverOutline").objectReferenceValue=outline;so.ApplyModifiedPropertiesWithoutUndo();
 hover.OnPointerEnter(null);if(!outline.activeSelf)errors.Add("Outline did not show");hover.OnPointerExit(null);if(outline.activeSelf)errors.Add("Outline did not restore");
 var group=fixture.AddComponent<UnityEngine.CanvasGroup>();hover.OnPointerEnter(null);group.interactable=false;typeof(OptionHover).GetMethod("OnCanvasGroupChanged",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(hover,null);if(outline.activeSelf)errors.Add("Disabled group retained outline");
 report.Add("Pointer probes: normal/faint alpha, repeated enter, exit, disabled button, panel disable, outline and disabled group checked");
}finally{UnityEngine.Object.DestroyImmediate(fixture);}
report.AddRange(errors);System.IO.File.WriteAllLines("Tools/UI/ButtonHoverVerification.txt",report);
if(errors.Count>0)throw new System.Exception(string.Join(";",errors));
return string.Join("\n",report);
