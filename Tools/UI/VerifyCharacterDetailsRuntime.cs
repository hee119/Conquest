var lobby=UnityEngine.Object.FindFirstObjectByType<LobbyController>(UnityEngine.FindObjectsInactive.Include);
if(lobby==null)throw new System.Exception("Lobby not loaded");
var serialized=new UnityEditor.SerializedObject(lobby);var panel=(UnityEngine.GameObject)serialized.FindProperty("characterPanel").objectReferenceValue;var menu=(UnityEngine.GameObject)serialized.FindProperty("mainMenu").objectReferenceValue;
var view=panel.GetComponent<CharacterDetailsPreview>();var data=new UnityEditor.SerializedObject(view);
var checks=new System.Collections.Generic.List<string>();
void Check(bool value,string message){if(!value)throw new System.Exception(message);checks.Add("PASS: "+message);}
System.Collections.IEnumerator Run(){
 var language=LocalizationManager.Instance.currentLanguage;
 try{
  menu.SetActive(false);panel.SetActive(true);lobby.PreviewSwordman();view.ShowStats();
  CharacterEnhancementTable.TryGetStats(1,0,out var baseSword);CharacterEnhancementTable.TryGetStats(1,5,out var maxSword);CharacterEnhancementTable.TryGetStats(2,0,out var baseMage);
  Check(baseSword.hp==200&&maxSword.hp==250&&maxSword.attack==15&&baseMage.hp==60,"Base and enhanced stats match CSV");
  var values=data.FindProperty("statValues");var hp=(TMPro.TMP_Text)values.GetArrayElementAtIndex(0).objectReferenceValue;
  CharacterEnhancementTable.TryGetStats(1,CharacterProgression.Level(1),out var actual);Check(hp.text==actual.hp.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture),"Stat popup reflects current enhancement");
  foreach(var lang in new[]{"ko","en","jp"}){
   LocalizationManager.Instance.SetLanguage(lang);yield return null;UnityEngine.Canvas.ForceUpdateCanvases();
   foreach(var text in panel.transform.Find("Character Stats").GetComponentsInChildren<TMPro.TMP_Text>(true)){text.ForceMeshUpdate();if(text.isTextOverflowing)checks.Add("OVERFLOW "+lang+" "+text.name);}
   Check(panel.transform.Find("Recessed Panel/RightDetails/View Stats/Label").GetComponent<TMPro.TMP_Text>().text!= "character_view_stats","Stats labels localized in "+lang);
  }
  LocalizationManager.Instance.SetLanguage(language);view.HideStats();
  for(int i=0;i<2;i++){
   if(i==0)lobby.PreviewSwordman();else lobby.PreviewMage();
   var item=data.FindProperty("previews").GetArrayElementAtIndex(i);var image=(UnityEngine.UI.Image)item.FindPropertyRelative("image").objectReferenceValue;var animator=(UnityEngine.Animator)item.FindPropertyRelative("idleAnimator").objectReferenceValue;
   view.PlayAttack();Check(!animator.enabled,"Attack playback controls selected character "+(i+1));
   var first=image.sprite;yield return new UnityEngine.WaitForSecondsRealtime(.35f);Check(image.sprite!=first,"Attack frames advance "+(i+1));
   yield return new UnityEngine.WaitForSecondsRealtime(item.FindPropertyRelative("duration").floatValue+.2f);Check(animator.enabled,"Idle restored after attack "+(i+1));
  }
  lobby.PreviewSwordman();view.PlayAttack();lobby.PreviewMage();Check(((UnityEngine.GameObject)serialized.FindProperty("swordmanIdlePreview").objectReferenceValue).GetComponent<UnityEngine.Animator>().enabled,"Changing character cancels playback");
  view.PlayAttack();panel.SetActive(false);Check(((UnityEngine.GameObject)serialized.FindProperty("mageIdlePreview").objectReferenceValue).GetComponent<UnityEngine.Animator>().enabled,"Closing panel restores idle");
  checks.Add("COMPLETE");
 }finally{
  LocalizationManager.Instance.SetLanguage(language);UnityEngine.PlayerPrefs.SetInt("SelectedCharacter",UnityEditor.SessionState.GetInt("CharacterPreviewPreviousSelected",1));UnityEngine.PlayerPrefs.Save();
  System.IO.File.WriteAllLines("Tools/UI/CharacterDetailsRuntimeVerification.txt",checks);
 }
}
lobby.StartCoroutine(Run());return "Runtime stat, localization and animation checks started";
