if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
var previousData=DataManager.Instance;var previousManager=UpgradeManager.Instance;
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var dataObject=new UnityEngine.GameObject("Fusion verification data");dataObject.SetActive(false);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(dataObject,scene);
var managerObject=new UnityEngine.GameObject("Fusion verification manager");managerObject.SetActive(false);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(managerObject,scene);
var buttonObject=new UnityEngine.GameObject("Fusion verification button",typeof(UnityEngine.RectTransform));buttonObject.SetActive(false);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(buttonObject,scene);
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
UpgradeCombineAvailability view=null;
var checks=new System.Collections.Generic.List<string>();
try{
 var data=dataObject.AddComponent<DataManager>();var manager=managerObject.AddComponent<UpgradeManager>();DataManager.Instance=data;UpgradeManager.Instance=manager;
 data.upgradeDatasDic[400]=new UpgradeData{id=400,type="Skill",upgradeLevel=5};
 data.upgradeDatasDic[405]=new UpgradeData{id=405,type="Skill",upgradeLevel=4,combinationList=new[]{400},result=new[]{502}};
 data.upgradeDatasDic[502]=new UpgradeData{id=502,type="Skill"};
 manager.skillUpgrades.Add(400);manager.skillUpgrades.Add(405);
 var button=buttonObject.AddComponent<UnityEngine.UI.Button>();view=buttonObject.AddComponent<UpgradeCombineAvailability>();
 typeof(UpgradeCombineAvailability).GetMethod("OnEnable",flags).Invoke(view,null);
 if(button.interactable)throw new System.Exception("Level 4 must not combine");checks.Add("PASS: incomplete pair disabled");
 data.upgradeDatasDic[405].upgradeLevel=5;
 System.Action notify=()=>{var listeners=(System.Action)typeof(UpgradeManager).GetField("UpgradesChanged",flags).GetValue(manager);listeners?.Invoke();};notify();
 if(!button.interactable)throw new System.Exception("Compatible level 5 pair not enabled");checks.Add("PASS: compatible level 5 pair enabled on upgrade event");
 manager.skillUpgrades.Add(502);notify();if(button.interactable)throw new System.Exception("Owned result must disable duplicate fusion");checks.Add("PASS: owned result disables pair");
 manager.skillUpgrades.Remove(502);notify();if(!button.interactable)throw new System.Exception("Availability failed to recover");
 typeof(UpgradeCombineAvailability).GetMethod("OnDisable",flags).Invoke(view,null);
 var after=(System.Action)typeof(UpgradeManager).GetField("UpgradesChanged",flags).GetValue(manager);if(after!=null)throw new System.Exception("Leaked subscription");view=null;checks.Add("PASS: event unsubscribed on disable");
}finally{
 if(view!=null)typeof(UpgradeCombineAvailability).GetMethod("OnDisable",flags).Invoke(view,null);
 DataManager.Instance=previousData;UpgradeManager.Instance=previousManager;UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
}
var stage=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Stage1.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
try{int count=0;foreach(var root in stage.GetRootGameObjects())foreach(var availability in root.GetComponentsInChildren<UpgradeCombineAvailability>(true)){if(!availability.gameObject.activeSelf)throw new System.Exception("Fusion button still inactive");count++;}if(count!=1)throw new System.Exception("Unexpected fusion button count");checks.Add("PASS: saved Stage1 fusion button active");}finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(stage,true);}
System.IO.File.WriteAllLines("Tools/UI/FusionAvailabilityVerification.txt",checks);return string.Join("\n",checks);
