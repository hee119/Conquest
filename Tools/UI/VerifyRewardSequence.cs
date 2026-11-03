if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Runtime verification required.");
var fixture=new UnityEngine.GameObject("RewardSequenceVerification",typeof(UnityEngine.RectTransform));
var inactive=new UnityEngine.GameObject("InactivePlayerVerification");inactive.SetActive(false);
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
DG.Tweening.Sequence sequence=null;float timeScale=UnityEngine.Time.timeScale;
try{
 var player=inactive.AddComponent<PlayerMove>();player.Move(default(UnityEngine.InputSystem.InputAction.CallbackContext));player.StopMove(0);typeof(PlayerMove).GetMethod("SetIdleDelayed",flags).Invoke(player,null);
 if(typeof(PlayerMove).GetField("idleCor",flags).GetValue(player)!=null)throw new System.Exception("Inactive player started coroutine");
 var view=fixture.AddComponent<RunRewardView>();var cards=new UnityEngine.RectTransform[2];var groups=new UnityEngine.CanvasGroup[2];
 for(int i=0;i<2;i++){var go=new UnityEngine.GameObject("Card"+i,typeof(UnityEngine.RectTransform),typeof(UnityEngine.CanvasGroup));go.transform.SetParent(fixture.transform,false);cards[i]=(UnityEngine.RectTransform)go.transform;cards[i].anchoredPosition=new UnityEngine.Vector2(i==0?-104f:104f,0f);groups[i]=go.GetComponent<UnityEngine.CanvasGroup>();}
 typeof(RunRewardView).GetField("rewardCards",flags).SetValue(view,cards);typeof(RunRewardView).GetField("rewardCardGroups",flags).SetValue(view,groups);
 view.Bind(new RunReward{gold=22,fragments=3,characterId=2});UnityEngine.Time.timeScale=0f;view.PlayReveal();sequence=(DG.Tweening.Sequence)typeof(RunRewardView).GetField("reveal",flags).GetValue(view);DG.Tweening.TweenExtensions.Pause(sequence);
 DG.Tweening.TweenExtensions.Goto(sequence,.3f,false);if(groups[0].alpha<.99f||groups[1].alpha!=0f||cards[0].anchoredPosition.x!=0f)throw new System.Exception("First reward must appear alone at center");
 DG.Tweening.TweenExtensions.Goto(sequence,.8f,false);if(cards[0].anchoredPosition.x>=-1f||groups[1].alpha!=0f)throw new System.Exception("First reward must move left before next appears");
 DG.Tweening.TweenExtensions.Goto(sequence,1.1f,false);if(groups[1].alpha<=0f||cards[0].anchoredPosition.x>=-200f)throw new System.Exception("Second reward must follow the slide");
 DG.Tweening.TweenExtensions.Complete(sequence);if(groups.Any(g=>g.alpha<.99f)||UnityEngine.Mathf.Abs(cards[0].anchoredPosition.x+104f)>.01f||UnityEngine.Mathf.Abs(cards[1].anchoredPosition.x-104f)>.01f)throw new System.Exception("Final authored positions not restored");
 view.Bind(new RunReward{gold=100,fragments=6,characterId=1});if(groups.Any(g=>g.alpha!=0f))throw new System.Exception("Replay must reset visibility");
 var report="PASS: inactive movement/cancel starts no coroutine; first reward alone; left slide before second; final positions; replay reset. Sequence uses unscaled update.";System.IO.File.WriteAllText("Tools/UI/RewardSequenceVerification.txt",report);return report;
}finally{if(sequence!=null)DG.Tweening.TweenExtensions.Kill(sequence);UnityEngine.Time.timeScale=timeScale;UnityEngine.Object.Destroy(fixture);UnityEngine.Object.Destroy(inactive);}
