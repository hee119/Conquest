var report="Tools/Localization/verification.txt";
System.IO.File.WriteAllText(report,"UI localization runtime verification\n");
var oldStart=UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene;
UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>("Assets/Scenes/Lobby.unity");
bool hadLanguage=UnityEngine.PlayerPrefs.HasKey("Language");string oldLanguage=UnityEngine.PlayerPrefs.GetString("Language","ko");
int phase=0,startFrame=0;double start=UnityEditor.EditorApplication.timeSinceStartup;
UnityEditor.EditorApplication.CallbackFunction check=null;
System.Action finish=()=>{
 if(hadLanguage)UnityEngine.PlayerPrefs.SetString("Language",oldLanguage);else UnityEngine.PlayerPrefs.DeleteKey("Language");
 UnityEngine.PlayerPrefs.Save();UnityEngine.Time.timeScale=1;
 UnityEditor.EditorApplication.update-=check;UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=oldStart;UnityEditor.EditorApplication.isPlaying=false;
};
check=()=>{try{
 if(!UnityEditor.EditorApplication.isPlaying)return;
 UnityEngine.Application.runInBackground=true;
 if(UnityEditor.EditorApplication.timeSinceStartup-start>90)throw new System.Exception("Timeout");
 if(phase==0){startFrame=UnityEngine.Time.frameCount;phase=1;return;}
 if(UnityEngine.Time.frameCount-startFrame<20||LocalizationManager.Instance==null)return;
 var owner=UnityEngine.Object.FindFirstObjectByType<LobbyController>();
 var font=owner.GetComponentInChildren<TMPro.TextMeshProUGUI>(true).font;
 var dialog=QuitConfirmationDialog.Create(owner.transform,font);dialog.Show();
 var late=new UnityEngine.GameObject("Late localized test",typeof(UnityEngine.RectTransform),typeof(TMPro.TextMeshProUGUI));
 var localized=late.AddComponent<LocalizedText>();localized.key="ui_yes";localized.ForceUpdateText();
 var menu=UnityEngine.Object.FindFirstObjectByType<StagePlanetMenu>(UnityEngine.FindObjectsInactive.Include);
 for(var parent=menu.transform;parent!=null;parent=parent.parent)parent.gameObject.SetActive(true);
 menu.Select(2);
 var stageLabel=(TMPro.TMP_Text)typeof(StagePlanetMenu).GetField("selectedStageLabel",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(menu);
 var fusionObject=new UnityEngine.GameObject("Fusion translation test");fusionObject.SetActive(false);
 var fusion=fusionObject.AddComponent<UpgradeFusionController>();
 var copy=typeof(UpgradeFusionController).GetMethod("Copy",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
 foreach(var language in new[]{"ko","en","jp"}){
  LocalizationManager.Instance.SetLanguage(language);
  foreach(var key in new[]{"quit_confirmation_question","ui_yes","ui_no","stage_selected_number","upgrade_combine","upgrade_confirm","upgrade_name_placeholder","upgrade_description_placeholder"})
   if(!LocalizationManager.Instance.HasTranslation(key))throw new System.Exception("Missing "+language+":"+key);
  foreach(var text in dialog.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true)){
   var binding=text.GetComponent<LocalizedText>();
   if(binding==null||text.text!=LocalizationManager.Instance.GetTranslation(binding.key))throw new System.Exception("Dialog failed: "+language+":"+text.name);
  }
  if(late.GetComponent<TMPro.TextMeshProUGUI>().text!=LocalizationManager.Instance.GetTranslation("ui_yes"))throw new System.Exception("Late label not refreshed");
  if(stageLabel.text!=LocalizationManager.Instance.GetTranslation("stage_selected_number","03"))throw new System.Exception("Stage number not localized: "+stageLabel.text);
  var expected=language=="ko"?"한국어":language=="en"?"English":"日本語";
  if((string)copy.Invoke(fusion,new object[]{"한국어","English","日本語"})!=expected)throw new System.Exception("Fusion language mismatch");
  if(LocalizationManager.Instance.GetTranslation("game_title")!="Planetary Pioneer")throw new System.Exception("Game title changed");
  System.IO.File.AppendAllText(report,$"PASS {language}: quit dialog, live-created text, numbered stage, fusion translation, unchanged game title\n");
 }
 System.IO.File.AppendAllText(report,"COMPLETE; language preference restored\n");finish();
}catch(System.Exception e){System.IO.File.AppendAllText(report,"FAIL "+e+"\n");finish();}};
UnityEditor.EditorApplication.update+=check;UnityEditor.EditorApplication.isPlaying=true;
return "Started ko/en/jp UI verification";
