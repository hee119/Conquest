var report="Tools/Retry/Verification.txt";
System.IO.File.WriteAllText(report,"Direct Stage1 and prepared-scene retry verification\n");
int attempt=0; float start=0; bool found=false; SkillSpawner player=null;
UnityEditor.EditorApplication.CallbackFunction tick=null;
System.Action begin=()=>{
    StageManager.Instance.StopRun();
    player=UnityEngine.Object.FindFirstObjectByType<SkillSpawner>();
    CharacterManager.Instance.currentCharacter=attempt==2?2:1;
    player.GetComponent<SelectedCharacterSetup>().ApplySelectedCharacter();
    UpgradeManager.Instance.skillUpgradeIcons[0].sprite=DataManager.Instance.skillUpgradeIconDic.Values.First();
    LevelManager.Instance.currentExp=17;
    player.GetComponent<AnimationController>().canSkillUse1=false;
    player.GetComponent<PlayerMove>().canMove=false;
    player.GetComponent<PlayerCombat>().isDead=true;
    UnityEngine.Object.FindFirstObjectByType<GameEndScreenController>().RetryStage();
    StageManager.Instance.StopRun();
    if(!player.GetComponent<AnimationController>().canSkillUse1 || !player.GetComponent<PlayerMove>().canMove || player.GetComponent<PlayerCombat>().isDead || UpgradeManager.Instance.skillUpgradeIcons.Any(x=>x.sprite!=null) || LevelManager.Instance.currentExp!=0)
        throw new System.Exception("Retry state not reset");
    player.GetComponent<AnimationController>().Skill1(player.skillId1);
    start=UnityEngine.Time.time;found=false;
};
tick=()=>{try{
    if(!UnityEditor.EditorApplication.isPlaying){UnityEditor.EditorApplication.update-=tick;return;}
    if(player==null){begin();return;}
    if(UnityEngine.Object.FindObjectsByType<SkillInfo>(UnityEngine.FindObjectsSortMode.None).Any(x=>x.skillId==player.skillId1))found=true;
    if(UnityEngine.Time.time-start<3)return;
    if(!found)throw new System.Exception("No animation-event attack on retry "+attempt);
    System.IO.File.AppendAllText(report,"PASS retry "+(attempt+1)+" icons/EXP/death/movement/cooldown reset; animation-event attack "+player.skillId1+" spawned\n");
    attempt++;
    if(attempt==3){System.IO.File.AppendAllText(report,"COMPLETE\n");UnityEditor.EditorApplication.update-=tick;UnityEditor.EditorApplication.isPlaying=false;return;}
    begin();
}catch(System.Exception e){System.IO.File.AppendAllText(report,"FAIL "+e+"\n");UnityEditor.EditorApplication.update-=tick;UnityEditor.EditorApplication.isPlaying=false;}};
UnityEditor.EditorApplication.update+=tick;
return "Repeated retry verification started";
