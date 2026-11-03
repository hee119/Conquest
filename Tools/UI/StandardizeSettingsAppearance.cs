if(UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Scene authoring requires edit mode; leave running game untouched.");
System.IO.Directory.CreateDirectory("Assets/Resources/Prefabs/UI/Settings");
var paths=new[]{"Assets/Scenes/Lobby.unity","Assets/Scenes/Stage1.unity"};
var report=new System.Collections.Generic.List<string>();
foreach(var path in paths){
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Transform>(true)).ToArray();
var graphic=all.First(t=>t.name=="Graphic");
var slider=graphic.GetComponentInChildren<UnityEngine.UI.Slider>(true);
var dropdown=graphic.GetComponentInChildren<TMPro.TMP_Dropdown>(true);
var sources=new[]{slider.transform.parent,dropdown.transform.parent};
if(path==paths[0])for(int k=0;k<2;k++){
var clone=UnityEngine.Object.Instantiate(sources[k].gameObject);clone.name=k==0?"Settings Slider Row":"Settings Dropdown Row";
var cd=clone.GetComponentInChildren<SharedCanvasDropdown>(true);
if(cd!=null){var orig=dropdown as SharedCanvasDropdown;var so=new UnityEditor.SerializedObject(orig);var pop=(UnityEngine.RectTransform)so.FindProperty("popupRoot").objectReferenceValue;var popup=UnityEngine.Object.Instantiate(pop,clone.transform);var cso=new UnityEditor.SerializedObject(cd);cso.FindProperty("popupRoot").objectReferenceValue=popup;cso.FindProperty("blocker").objectReferenceValue=popup.GetComponentInChildren<UnityEngine.UI.Button>(true);cso.ApplyModifiedPropertiesWithoutUndo();cd.template=popup.GetComponentsInChildren<UnityEngine.RectTransform>(true).First(t=>t.name==dropdown.template.name);}
UnityEditor.PrefabUtility.SaveAsPrefabAsset(clone,"Assets/Resources/Prefabs/UI/Settings/"+clone.name+".prefab");UnityEngine.Object.DestroyImmediate(clone);
}
int count=0;
var controls=all.Where(t=>t.GetComponent<UnityEngine.UI.Slider>()!=null||t.GetComponent<TMPro.TMP_Dropdown>()!=null).Where(t=>t.GetComponentsInParent<UnityEngine.Transform>(true).Any(p=>p.name=="Options"||p.name=="Graphic"||p.name=="Sound"||p.name=="Language")).ToArray();
foreach(var control in controls){var source=control.GetComponent<UnityEngine.UI.Slider>()!=null?sources[0]:sources[1];var row=control.parent;if(row==source)continue;
var sourceLabel=source.GetComponentsInChildren<TMPro.TMP_Text>(true).FirstOrDefault(t=>t.transform.parent==source);
var label=row.GetComponentsInChildren<TMPro.TMP_Text>(true).FirstOrDefault(t=>t.transform.parent==row);
if(sourceLabel!=null&&label!=null){label.font=sourceLabel.font;label.fontSize=sourceLabel.fontSize;label.enableAutoSizing=sourceLabel.enableAutoSizing;label.alignment=sourceLabel.alignment;label.color=sourceLabel.color;}
foreach(var type in new[]{typeof(UnityEngine.UI.HorizontalLayoutGroup),typeof(SettingsOptionRowLayout),typeof(UnityEngine.UI.LayoutElement)}){var src=source.GetComponent(type);if(src!=null){var dst=row.GetComponent(type)??row.gameObject.AddComponent(type);UnityEditor.EditorUtility.CopySerialized(src,dst);}}
var sc=source.GetComponentsInChildren<UnityEngine.Transform>(true).First(t=>t.GetComponent<UnityEngine.UI.Slider>()!=null||t.GetComponent<TMPro.TMP_Dropdown>()!=null);
foreach(var target in control.GetComponentsInChildren<UnityEngine.Transform>(true)){
string rel=UnityEditor.AnimationUtility.CalculateTransformPath(target,control);var match=rel.Length==0?sc:sc.Find(rel);if(match==null)continue;
if(target is UnityEngine.RectTransform tr&&match is UnityEngine.RectTransform sr){tr.anchorMin=sr.anchorMin;tr.anchorMax=sr.anchorMax;tr.pivot=sr.pivot;tr.sizeDelta=sr.sizeDelta;tr.anchoredPosition=sr.anchoredPosition;tr.localScale=sr.localScale;}
var image=target.GetComponent<UnityEngine.UI.Image>();var si=match.GetComponent<UnityEngine.UI.Image>();if(image!=null&&si!=null){image.sprite=si.sprite;image.color=si.color;image.type=si.type;image.preserveAspect=si.preserveAspect;}
var text=target.GetComponent<TMPro.TMP_Text>();var st=match.GetComponent<TMPro.TMP_Text>();if(text!=null&&st!=null){text.font=st.font;text.fontSize=st.fontSize;text.enableAutoSizing=st.enableAutoSizing;text.color=st.color;text.alignment=st.alignment;}
}count++;}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);report.Add(scene.name+": styled "+count+" rows; callbacks and localization retained");
if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
}
return report.ToArray();
