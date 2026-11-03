var lobby=UnityEngine.Object.FindFirstObjectByType<LobbyController>(UnityEngine.FindObjectsInactive.Include);var data=new UnityEditor.SerializedObject(lobby);var panel=(UnityEngine.GameObject)data.FindProperty("characterPanel").objectReferenceValue;var menu=(UnityEngine.GameObject)data.FindProperty("mainMenu").objectReferenceValue;
var results=new System.Collections.Generic.List<string>();void Check(bool test,string message){if(!test)throw new System.Exception(message);results.Add("PASS: "+message);}
System.Collections.IEnumerator Run(){
 var previousData=DataManager.Instance;UnityEngine.GameObject dataObject=null,actor=null;
 try{
  menu.SetActive(false);panel.SetActive(true);lobby.PreviewSwordman();var summary=panel.GetComponentInChildren<CharacterSummaryStats>();var fields=new UnityEditor.SerializedObject(summary);var cooldown=(TMPro.TMP_Text)fields.FindProperty("cooldownValue").objectReferenceValue;
  Check(cooldown.text.Contains("0.75"),"Sword basic attack cooldown shown");lobby.PreviewMage();Check(cooldown.text.Contains("0.4"),"Mage basic attack cooldown shown");
  var hp=(TMPro.TMP_Text)fields.FindProperty("hpValue").objectReferenceValue;CharacterEnhancementTable.TryGetStats(2,CharacterProgression.Level(2),out var stats);Check(hp.text.Contains(stats.hp.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)),"Character selection updates HP");
  foreach(var text in summary.GetComponentsInChildren<TMPro.TMP_Text>(true)){text.ForceMeshUpdate();Check(!text.isTextOverflowing,"Row fits: "+text.name);}
  dataObject=new UnityEngine.GameObject("Timing data test");dataObject.SetActive(false);var dm=dataObject.AddComponent<DataManager>();DataManager.Instance=dm;dm.skillDatasDic[200]=new SkillData{skillId=200,movementLockTime=.08f,skillCoolTime=.1f,basicAttackCooldown=.1f};
  actor=new UnityEngine.GameObject("Timed movement verification");actor.SetActive(false);var rb=actor.AddComponent<UnityEngine.Rigidbody2D>();rb.gravityScale=0;actor.AddComponent<UnityEngine.SpriteRenderer>();var animator=actor.AddComponent<UnityEngine.Animator>();animator.runtimeAnimatorController=UnityEngine.Resources.Load<UnityEngine.GameObject>("Prefabs/Agent/Swordman").GetComponent<UnityEngine.Animator>().runtimeAnimatorController;actor.AddComponent<AnimationController>();var move=actor.AddComponent<PlayerMove>();var spawner=actor.AddComponent<SkillSpawner>();spawner.skillId1=200;actor.SetActive(true);
  spawner.MovementLock(0);Check(!move.canMove,"Attack begins cached movement lock");spawner.MovementLock(1);Check(!move.canMove,"Fixed clip end event cannot unlock timer early");
  yield return new UnityEngine.WaitForSeconds(.12f);Check(move.canMove,"Movement released after cached duration");
  spawner.MovementLock(0);spawner.enabled=false;Check(move.canMove,"Disabling spawner clears movement lock");spawner.enabled=true;spawner.MovementLock(0);yield return new UnityEngine.WaitForSeconds(.12f);Check(move.canMove,"Reenabled lock has no orphan timer");results.Add("COMPLETE");
 }finally{
  if(actor!=null)UnityEngine.Object.Destroy(actor);DataManager.Instance=previousData;if(dataObject!=null)UnityEngine.Object.Destroy(dataObject);UnityEngine.PlayerPrefs.SetInt("SelectedCharacter",UnityEditor.SessionState.GetInt("SummaryPreviousSelected",1));UnityEngine.PlayerPrefs.Save();System.IO.File.WriteAllLines("Tools/UI/SummaryStatsRuntimeVerification.txt",results);
 }
}
lobby.StartCoroutine(Run());return "Running actual lobby rows and movement lock coroutine checks";
