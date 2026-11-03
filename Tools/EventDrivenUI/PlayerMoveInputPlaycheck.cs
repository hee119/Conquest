using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public sealed class PlayerMoveInputPlaycheck : MonoBehaviour
{
    public RuntimeAnimatorController controller;
    private readonly List<string> errors=new List<string>();
    private readonly Dictionary<string,object> report=new Dictionary<string,object>();
    private Mouse mouse;
    private void OnEnable()=>Application.logMessageReceived+=Log;
    private void OnDisable(){Application.logMessageReceived-=Log;if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);Time.timeScale=1;}
    private void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+trace);}
    private IEnumerator MouseButton(bool down)
    {
        var state=new MouseState();if(down)state=state.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Right);
        InputSystem.QueueStateEvent(mouse,state);yield return null;yield return null;
    }
    private IEnumerator Start()
    {
        var actor=new GameObject("Input Verification Player");actor.SetActive(false);
        var rb=actor.AddComponent<Rigidbody2D>();rb.gravityScale=0;rb.constraints=RigidbodyConstraints2D.FreezeRotation;
        actor.AddComponent<SpriteRenderer>();actor.AddComponent<Animator>().runtimeAnimatorController=controller;
        actor.AddComponent<AnimationController>();var move=actor.AddComponent<PlayerMove>();actor.SetActive(true);
        mouse=InputSystem.AddDevice<Mouse>("Dash Verification Mouse");
        int drops=0;float last=move.DashEnergyNormalized;
        move.DashEnergyChanged+=value=>{if(value<last-.001f)drops++;last=value;};
        move.moveDir=Vector2.left;yield return null;
        yield return MouseButton(true);report["rightClickStartsDash"]=move.IsDashing;report["dashUsesMoveDirection"]=rb.linearVelocity.x<0;report["dashConsumesEnergy"]=move.DashEnergyNormalized<1;
        yield return new WaitForSeconds(1f);report["holdingMouseDoesNotRepeat"]=drops==1;report["dashEnds"]=!move.IsDashing;report["energyRecovers"]=move.DashEnergyNormalized>.75f;
        yield return MouseButton(false);yield return MouseButton(true);report["nextPressStartsAnotherDash"]=move.IsDashing&&drops==2;
        yield return MouseButton(false);move.enabled=false;int before=drops;yield return MouseButton(true);report["disabledPlayerIgnoresMouse"]=drops==before&&!move.IsDashing&&rb.linearVelocity==Vector2.zero;
        yield return MouseButton(false);move.enabled=true;move.enabled=false;move.enabled=true;yield return null;
        before=drops;yield return MouseButton(true);report["reenableHasOneSubscription"]=drops==before+1;
        yield return MouseButton(false);yield return new WaitForSeconds(.25f);Time.timeScale=0;before=drops;yield return MouseButton(true);report["pausedClickDoesNotSpendEnergy"]=drops==before&&!move.IsDashing;
        Time.timeScale=1;yield return MouseButton(false);move.moveDir=Vector2.zero;actor.transform.localScale=new Vector3(-1,1,1);yield return MouseButton(true);report["idleDashUsesFacingDirection"]=move.IsDashing&&rb.linearVelocity.x<0;
        yield return MouseButton(false);Destroy(actor);yield return null;yield return MouseButton(true);report["destroyedPlayerHasNoCallbackError"]=errors.Count==0;
        InputSystem.RemoveDevice(mouse);mouse=null;report["errors"]=errors;report["completed"]=true;
        File.WriteAllText("Tools/EventDrivenUI/PlayerMoveInputVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        Debug.Log("PLAYER_MOVE_INPUT_CHECK_COMPLETE");
    }
}
