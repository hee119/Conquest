var report=new System.Collections.Generic.List<string>();
UnityEngine.RectTransform Group(string name,UnityEngine.Transform parent){var g=new UnityEngine.GameObject(name,typeof(UnityEngine.RectTransform));var r=(UnityEngine.RectTransform)g.transform;r.SetParent(parent,false);r.anchorMin=UnityEngine.Vector2.zero;r.anchorMax=UnityEngine.Vector2.one;r.offsetMin=UnityEngine.Vector2.zero;r.offsetMax=UnityEngine.Vector2.zero;return r;}
void RemoveCanvas(UnityEngine.Canvas c){var ray=c.GetComponent<UnityEngine.UI.GraphicRaycaster>();if(ray!=null)UnityEngine.Object.DestroyImmediate(ray);var scaler=c.GetComponent<UnityEngine.UI.CanvasScaler>();if(scaler!=null)UnityEngine.Object.DestroyImmediate(scaler);UnityEngine.Object.DestroyImmediate(c);}
var dropdownScript=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.MonoScript>("Assets/Script/UI/SharedCanvasDropdown.cs");
foreach(var scenePath in UnityEditor.EditorBuildSettings.scenes.Where(x=>x.enabled && x.path.EndsWith("Stage1.unity")).Select(x=>x.path)){
 var existing=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);bool opened=!existing.isLoaded;
 var scene=opened?UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,UnityEditor.SceneManagement.OpenSceneMode.Additive):existing;
 try{
  var canvases=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Canvas>(true)).ToArray();
  var dropdowns=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TMPro.TMP_Dropdown>(true)).ToArray();
  UnityEngine.Canvas screen;
  if(scene.name=="Stage1"){
   var source=canvases.First(c=>c.name=="UpgradeUI");var original=(UnityEngine.RectTransform)source.transform;
   var root=new UnityEngine.GameObject("Screen Canvas",typeof(UnityEngine.RectTransform),typeof(UnityEngine.Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
   var r=(UnityEngine.RectTransform)root.transform;r.SetParent(original.parent,false);r.anchorMin=original.anchorMin;r.anchorMax=original.anchorMax;r.pivot=original.pivot;r.sizeDelta=original.sizeDelta;r.anchoredPosition3D=original.anchoredPosition3D;r.localScale=original.localScale;r.localRotation=original.localRotation;
   screen=root.GetComponent<UnityEngine.Canvas>();UnityEditor.EditorUtility.CopySerialized(source,screen);
   UnityEditor.EditorUtility.CopySerialized(source.GetComponent<UnityEngine.UI.CanvasScaler>(),root.GetComponent<UnityEngine.UI.CanvasScaler>());
   UnityEditor.EditorUtility.CopySerialized(source.GetComponent<UnityEngine.UI.GraphicRaycaster>(),root.GetComponent<UnityEngine.UI.GraphicRaycaster>());
   screen.sortingLayerName="UI";screen.sortingOrder=0;
   foreach(var name in new[]{"Player","UpgradeUI","Debug","Setting","GameEndCanvas"}){
    var c=canvases.First(x=>x!=null&&x.name==name);var child=(UnityEngine.RectTransform)c.transform;
    RemoveCanvas(c);child.SetParent(r,false);child.localScale=UnityEngine.Vector3.one;child.localRotation=UnityEngine.Quaternion.identity;child.anchorMin=UnityEngine.Vector2.zero;child.anchorMax=UnityEngine.Vector2.one;child.offsetMin=UnityEngine.Vector2.zero;child.offsetMax=UnityEngine.Vector2.zero;child.anchoredPosition3D=UnityEngine.Vector3.zero;
   }
   var worldObject=new UnityEngine.GameObject("World Canvas",typeof(UnityEngine.RectTransform),typeof(UnityEngine.Canvas));
   var worldRect=(UnityEngine.RectTransform)worldObject.transform;worldRect.SetParent(r.parent,false);worldRect.localPosition=UnityEngine.Vector3.zero;worldRect.localScale=UnityEngine.Vector3.one*.01f;worldRect.sizeDelta=new UnityEngine.Vector2(2000,2000);
   var world=worldObject.GetComponent<UnityEngine.Canvas>();world.renderMode=UnityEngine.RenderMode.WorldSpace;world.worldCamera=screen.worldCamera;world.sortingLayerName="UI";world.sortingOrder=-1;world.additionalShaderChannels=screen.additionalShaderChannels;
   foreach(var c in canvases.Where(x=>x!=null&&x.renderMode==UnityEngine.RenderMode.WorldSpace&&(x.name=="Player World Bars"||x.name=="Boss"))){var child=c.transform;RemoveCanvas(c);child.SetParent(worldRect,true);}
   var damage=Group("Damage Numbers",worldRect);damage.anchorMin=damage.anchorMax=new UnityEngine.Vector2(.5f,.5f);damage.sizeDelta=new UnityEngine.Vector2(1000,1000);damage.anchoredPosition3D=UnityEngine.Vector3.zero;damage.gameObject.AddComponent<DamageNumberDisplay>();
   var hud=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UpgradeCooldownHud>(true)).Single();
   var dynamic=Group("Dynamic Cooldown Overlay",hud.transform);dynamic.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout=true;
   var hudSO=new UnityEditor.SerializedObject(hud);hudSO.FindProperty("dynamicOverlay").objectReferenceValue=dynamic;hudSO.ApplyModifiedPropertiesWithoutUndo();
  }else{
   screen=canvases.First(c=>c.isRootCanvas&&c.renderMode!=UnityEngine.RenderMode.WorldSpace);
   var children=Enumerable.Range(0,screen.transform.childCount).Select(i=>screen.transform.GetChild(i)).ToArray();
   var container=Group(scene.name+" UI",screen.transform);
   foreach(var child in children)child.SetParent(container,false);
   foreach(var c in canvases.Where(c=>c!=screen&&c!=null&&c.name!="Template")){RemoveCanvas(c);}
  }
  // Popup groups and blockers are saved objects; opening a dropdown adds no canvases.
  foreach(var dropdown in dropdowns){
   var obj=dropdown.gameObject;var template=dropdown.template;
   if(template==null)continue;
   var popup=Group(obj.name+" Popup",screen.transform);
   var block=Group("Blocker",popup);var image=block.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=UnityEngine.Color.clear;var button=block.gameObject.AddComponent<UnityEngine.UI.Button>();button.transition=UnityEngine.UI.Selectable.Transition.None;
   var canvas=template.GetComponent<UnityEngine.Canvas>();if(canvas!=null)RemoveCanvas(canvas);
   template.SetParent(popup,true);template.localScale=UnityEngine.Vector3.one;template.localRotation=UnityEngine.Quaternion.identity;
   var corners=new UnityEngine.Vector3[4];((UnityEngine.RectTransform)obj.transform).GetWorldCorners(corners);
   var left=popup.InverseTransformPoint(corners[0]);var right=popup.InverseTransformPoint(corners[3]);
   template.anchorMin=template.anchorMax=new UnityEngine.Vector2(.5f,.5f);template.pivot=new UnityEngine.Vector2(.5f,1f);template.sizeDelta=new UnityEngine.Vector2(UnityEngine.Mathf.Abs(right.x-left.x),template.rect.height);template.anchoredPosition=new UnityEngine.Vector2((left.x+right.x)*.5f,left.y-2f);template.gameObject.SetActive(false);
   var so=new UnityEditor.SerializedObject(dropdown);so.FindProperty("m_Script").objectReferenceValue=dropdownScript;so.ApplyModifiedPropertiesWithoutUndo();
   var converted=obj.GetComponent<SharedCanvasDropdown>();if(converted==null)throw new System.Exception("Could not convert dropdown "+obj.name);
   so=new UnityEditor.SerializedObject(converted);so.FindProperty("popupRoot").objectReferenceValue=popup;so.FindProperty("blocker").objectReferenceValue=button;so.ApplyModifiedPropertiesWithoutUndo();
   UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(converted);
   popup.gameObject.SetActive(false);
  }
  var dialog=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<QuitConfirmationDialog>(true)).FirstOrDefault();
  if(dialog!=null){var c=dialog.GetComponent<UnityEngine.Canvas>();if(c!=null)RemoveCanvas(c);var rect=(UnityEngine.RectTransform)dialog.transform;rect.SetParent(screen.transform,false);rect.anchorMin=UnityEngine.Vector2.zero;rect.anchorMax=UnityEngine.Vector2.one;rect.offsetMin=UnityEngine.Vector2.zero;rect.offsetMax=UnityEngine.Vector2.zero;rect.localScale=UnityEngine.Vector3.one;rect.anchoredPosition3D=UnityEngine.Vector3.zero;var so=new UnityEditor.SerializedObject(dialog);so.FindProperty("noButton").objectReferenceValue=dialog.transform.Find("Panel/No").GetComponent<UnityEngine.UI.Button>();so.FindProperty("yesButton").objectReferenceValue=dialog.transform.Find("Panel/Yes").GetComponent<UnityEngine.UI.Button>();so.ApplyModifiedPropertiesWithoutUndo();dialog.gameObject.SetActive(false);var lobby=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LobbyController>(true)).First();so=new UnityEditor.SerializedObject(lobby);so.FindProperty("quitDialog").objectReferenceValue=dialog;so.ApplyModifiedPropertiesWithoutUndo();}
  UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
  report.Add(scene.name+": "+string.Join(", ",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Canvas>(true)).Select(c=>c.name+"/"+c.renderMode)));
 }finally{if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
}
System.IO.File.WriteAllLines("Tools/UI/CanvasConsolidation.txt",report);return report.ToArray();
