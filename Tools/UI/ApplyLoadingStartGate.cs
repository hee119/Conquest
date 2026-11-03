if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Loading.unity");
UnityEngine.Transform canvas=null;StartupLoading loader=null;
foreach(var root in scene.GetRootGameObjects()){if(root.name=="Loading Canvas")canvas=root.transform;if(root.name=="Startup Loading")loader=root.GetComponent<StartupLoading>();}
if(canvas==null||loader==null)throw new System.Exception("Missing loading objects");
var title=canvas.Find("Game Title").GetComponent<TMPro.TextMeshProUGUI>();
var promptTransform=canvas.Find("Touch To Start");
var prompt=promptTransform!=null?promptTransform.GetComponent<TMPro.TextMeshProUGUI>():new UnityEngine.GameObject("Touch To Start",typeof(UnityEngine.RectTransform),typeof(TMPro.TextMeshProUGUI)).GetComponent<TMPro.TextMeshProUGUI>();
prompt.transform.SetParent(canvas,false);prompt.font=title.font;prompt.fontSharedMaterial=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Resources/UI/Loading/ProgressOutline.mat");prompt.fontSize=48;prompt.enableAutoSizing=false;prompt.color=UnityEngine.Color.white;prompt.alignment=TMPro.TextAlignmentOptions.Center;prompt.raycastTarget=false;prompt.text="터치하여 시작";
prompt.rectTransform.anchorMin=prompt.rectTransform.anchorMax=new UnityEngine.Vector2(.5f,0);prompt.rectTransform.pivot=new UnityEngine.Vector2(.5f,.5f);prompt.rectTransform.anchoredPosition=new UnityEngine.Vector2(0,180);prompt.rectTransform.sizeDelta=new UnityEngine.Vector2(1200,100);
var localized=prompt.GetComponent<LocalizedText>();if(localized==null)localized=prompt.gameObject.AddComponent<LocalizedText>();localized.key="loading_touch_to_start";prompt.gameObject.SetActive(false);
var fadeTransform=canvas.Find("Start Transition Fade");var fade=fadeTransform!=null?fadeTransform.gameObject:new UnityEngine.GameObject("Start Transition Fade",typeof(UnityEngine.RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.CanvasGroup));
fade.transform.SetParent(canvas,false);fade.transform.SetAsLastSibling();var rect=(UnityEngine.RectTransform)fade.transform;rect.anchorMin=UnityEngine.Vector2.zero;rect.anchorMax=UnityEngine.Vector2.one;rect.offsetMin=rect.offsetMax=UnityEngine.Vector2.zero;
fade.GetComponent<UnityEngine.UI.Image>().color=UnityEngine.Color.black;var group=fade.GetComponent<UnityEngine.CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;group.interactable=false;
var serialized=new UnityEditor.SerializedObject(loader);serialized.FindProperty("touchToStartText").objectReferenceValue=prompt;serialized.FindProperty("transitionFade").objectReferenceValue=group;serialized.FindProperty("blinkPeriod").floatValue=2.8f;serialized.FindProperty("fadeDuration").floatValue=.8f;serialized.FindProperty("nextScene").stringValue="CutSceen";serialized.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.AssetDatabase.ImportAsset("Assets/Resources/TextData/Localization.csv");UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return "Saved touch-to-start prompt, localized key, full-screen black fade (0.8s), and CutSceen target.";
