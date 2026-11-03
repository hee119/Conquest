if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Loading.unity");
UnityEngine.Transform canvas=null;StartupLoading loader=null;
foreach(var root in scene.GetRootGameObjects()){if(root.name=="Loading Canvas")canvas=root.transform;if(root.name=="Startup Loading")loader=root.GetComponent<StartupLoading>();}
if(canvas==null||loader==null)throw new System.Exception("Missing loading UI");
var tip=canvas.Find("Loading Progress").GetComponent<TMPro.TextMeshProUGUI>();tip.name="Loading Tip";
tip.text="골드와 요원 파편을 모아 요원을 강화 시킬 수 있습니다.";tip.fontSize=30;tip.enableAutoSizing=false;tip.alignment=TMPro.TextAlignmentOptions.Center;tip.raycastTarget=false;
tip.rectTransform.anchoredPosition=new UnityEngine.Vector2(0,230);tip.rectTransform.sizeDelta=new UnityEngine.Vector2(1500,110);
var bar=canvas.Find("Loading Experience Bar");
var labelObject=new UnityEngine.GameObject("Loading Progress",typeof(UnityEngine.RectTransform),typeof(TMPro.TextMeshProUGUI));labelObject.transform.SetParent(bar,false);
var label=labelObject.GetComponent<TMPro.TextMeshProUGUI>();label.font=tip.font;label.fontSize=34;label.enableAutoSizing=false;label.alignment=TMPro.TextAlignmentOptions.MidlineRight;label.color=UnityEngine.Color.white;label.raycastTarget=false;label.text="0/100";
var rect=label.rectTransform;rect.anchorMin=rect.anchorMax=new UnityEngine.Vector2(1,.5f);rect.pivot=new UnityEngine.Vector2(1,.5f);rect.anchoredPosition=new UnityEngine.Vector2(-16,0);rect.sizeDelta=new UnityEngine.Vector2(220,70);
if(!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources/UI/Loading"))UnityEditor.AssetDatabase.CreateFolder("Assets/Resources/UI","Loading");
string materialPath="Assets/Resources/UI/Loading/ProgressOutline.mat";
var material=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(materialPath);
if(material==null){material=new UnityEngine.Material(tip.fontSharedMaterial);material.name="Loading Progress Black Outline";UnityEditor.AssetDatabase.CreateAsset(material,materialPath);}
material.SetColor(TMPro.ShaderUtilities.ID_FaceColor,UnityEngine.Color.white);material.SetColor(TMPro.ShaderUtilities.ID_OutlineColor,UnityEngine.Color.black);material.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth,.24f);material.SetFloat(TMPro.ShaderUtilities.ID_OutlineSoftness,0);material.EnableKeyword("OUTLINE_ON");UnityEditor.EditorUtility.SetDirty(material);label.fontSharedMaterial=material;label.UpdateMeshPadding();
var data=new UnityEditor.SerializedObject(loader);data.FindProperty("progressText").objectReferenceValue=label;data.FindProperty("tipText").objectReferenceValue=tip;data.FindProperty("tipInterval").floatValue=4;
var tips=data.FindProperty("tips");tips.arraySize=2;tips.GetArrayElementAtIndex(0).stringValue="골드와 요원 파편을 모아 요원을 강화 시킬 수 있습니다.";tips.GetArrayElementAtIndex(1).stringValue="증강의 레벨이 5 이고 합성 가능한 증강이 있다면 합성 버튼이 활성화 됩니다.";data.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return "Saved: outlined progress on bar right; two tips rotate every 4 real-time seconds.";
