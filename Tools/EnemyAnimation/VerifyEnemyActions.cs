var report="Tools/EnemyAnimation/EnemyActionsVerification.txt";System.IO.File.WriteAllText(report,"Actual enemy animation action verification\n");
int phase=0,frame=-1,wait=0;float phaseTime=0,before=0;UnityEngine.GameObject enemy=null;PlayerCombat player=null;EnemySpriteAnimation animation=null;
double start=UnityEditor.EditorApplication.timeSinceStartup;
UnityEditor.EditorApplication.CallbackFunction tick=null;
System.Action finish=()=>{UnityEditor.EditorApplication.update-=tick;UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=null;UnityEditor.EditorApplication.isPlaying=false;};
System.Action<int> spawn=(id)=>{enemy=PoolManager.Instance.Spawn(id,(UnityEngine.Vector2)player.transform.position+new UnityEngine.Vector2(0,5));var move=enemy.GetComponent<EnemyMove>();if(move!=null)move.enabled=false;var agent=enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent!=null)agent.enabled=false;animation=enemy.GetComponent<EnemySpriteAnimation>();if(animation==null||enemy.GetComponent<UnityEngine.Animator>()!=null)throw new System.Exception("Animator conversion missing "+id);};
tick=()=>{try{
 if(!UnityEditor.EditorApplication.isPlaying)return;
 UnityEngine.Application.runInBackground=true;
 if(UnityEditor.EditorApplication.timeSinceStartup-start>90)throw new System.Exception("timeout phase "+phase);
 if(frame==UnityEngine.Time.frameCount)return;frame=UnityEngine.Time.frameCount;
 if(phase==0){if(StageManager.Instance==null||PoolManager.Instance==null||++wait<12)return;StageManager.Instance.StopRun();PoolManager.Instance.ResetActiveObjects();player=UnityEngine.GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerCombat>();player.maxHp=player.hp=100000;player.GetComponent<PlayerMove>().enabled=false;spawn(101);before=player.hp;animation.Play("Attack");phaseTime=UnityEngine.Time.time;phase=1;return;}
 if(phase==1&&UnityEngine.Time.time-phaseTime>.5f){if(player.hp>=before)throw new System.Exception("Crab attack did not damage player");System.IO.File.AppendAllText(report,"PASS crab Attack event damaged player\n");enemy.SetActive(false);spawn(106);before=player.hp;animation.Play("Attack");phaseTime=UnityEngine.Time.time;phase=2;return;}
 if(phase==2&&UnityEngine.Time.time-phaseTime>.8f){if(player.hp>=before)throw new System.Exception("Slime attack did not damage player");System.IO.File.AppendAllText(report,"PASS slime Attack event damaged player\n");enemy.SetActive(false);spawn(107);var bomb=enemy.GetComponent<BoomSlime>();bomb.splitCount=0;bomb.Die();phaseTime=UnityEngine.Time.time;phase=3;return;}
 if(phase==3&&UnityEngine.Time.time-phaseTime>.7f){if(enemy.activeInHierarchy)throw new System.Exception("Bomb did not complete explosion event");System.IO.File.AppendAllText(report,"PASS bomb Boom event returned enemy to pool\n");spawn(107);if(enemy.GetComponent<SpriteRenderer>().sprite==null||animation.IsPlaying("Boom"))throw new System.Exception("Bomb pool reset failed");System.IO.File.AppendAllText(report,"PASS bomb pool reuse reset\nCOMPLETE\n");finish();}
}catch(System.Exception e){System.IO.File.AppendAllText(report,"FAIL "+e+"\n");finish();}};
UnityEditor.EditorApplication.update+=tick;UnityEditor.EditorApplication.isPlaying=true;return "Started enemy attack and pooling verification";
