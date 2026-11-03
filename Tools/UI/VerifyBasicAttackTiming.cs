if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
var previousData=DataManager.Instance;var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var go=new UnityEngine.GameObject("Timing verification");go.SetActive(false);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
var checks=new System.Collections.Generic.List<string>();void Near(float actual,float expected,string name){if(UnityEngine.Mathf.Abs(actual-expected)>.0001f)throw new System.Exception(name+": "+actual+" != "+expected);checks.Add("PASS: "+name+" = "+actual);}
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
System.Collections.Generic.SortedDictionary<int,CharacterEnhancementBonus> curve=null;CharacterEnhancementBonus oldBase=default;
try{
 var data=go.AddComponent<DataManager>();var manager=go.AddComponent<UpgradeManager>();DataManager.Instance=data;
 var sword=new SkillData{skillId=200,skillCoolTime=.75f,movementLockTime=.7f};var mage=new SkillData{skillId=201,skillCoolTime=.4f,movementLockTime=0};data.skillDatasDic.Add(200,sword);data.skillDatasDic.Add(201,mage);
 var cooldowns=(System.Collections.Generic.Dictionary<SkillData,float>)typeof(UpgradeManager).GetField("originalSkillCoolTimes",flags).GetValue(manager);var locks=(System.Collections.Generic.Dictionary<SkillData,float>)typeof(UpgradeManager).GetField("originalMovementLockTimes",flags).GetValue(manager);
 cooldowns[sword]=.75f;cooldowns[mage]=.4f;locks[sword]=.7f;locks[mage]=0;
 var apply=typeof(UpgradeManager).GetMethod("ApplySkillCoolTimes",flags);
 apply.Invoke(manager,null);Near(sword.basicAttackCooldown,.75f,"base sword cooldown");Near(sword.movementLockTime,.7f,"base sword lock");
 typeof(UpgradeManager).GetField("skillCooldownBonus",flags).SetValue(manager,20f);apply.Invoke(manager,null);Near(sword.basicAttackCooldown,.6f,"20% cooldown reduction");Near(sword.movementLockTime,.56f,"proportional lock reduction");Near(sword.basicAttackAnimationSpeed,1.25f,"cached animation speed");apply.Invoke(manager,null);Near(sword.movementLockTime,.56f,"repeated refresh does not compound");Near(mage.movementLockTime,0,"zero mage lock preserved");
 CharacterEnhancementTable.BasicAttack(1,0,out _);var curves=(System.Collections.Generic.Dictionary<int,System.Collections.Generic.SortedDictionary<int,CharacterEnhancementBonus>>)typeof(CharacterEnhancementTable).GetField("curves",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).GetValue(null);curve=curves[1];oldBase=curve[0];var changed=oldBase;changed.attackSpeed=2;curve[0]=changed;
 apply.Invoke(manager,null);Near(sword.basicAttackCooldown,.3f,"permanent attack speed combines with cooldown reduction");Near(sword.movementLockTime,.28f,"permanent speed scales movement lock once");
 float before=sword.movementLockTime;for(int i=0;i<100;i++){var cached=sword.basicAttackCooldown;var speed=sword.basicAttackAnimationSpeed;}Near(sword.movementLockTime,before,"reading cached timing does not recalculate");
}finally{if(curve!=null)curve[0]=oldBase;DataManager.Instance=previousData;UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
System.IO.File.WriteAllLines("Tools/UI/BasicAttackTimingVerification.txt",checks);return string.Join("\n",checks);
