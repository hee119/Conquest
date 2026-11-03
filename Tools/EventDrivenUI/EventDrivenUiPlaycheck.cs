using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public sealed class EventDrivenUiPlaycheck : MonoBehaviour
{
    public SPUM_Prefabs unitPrefab;
    public GameObject soundPrefab;
    private readonly Dictionary<string,object> report = new Dictionary<string,object>();
    private readonly List<string> errors = new List<string>();
    private readonly FieldInfo clickFrame = typeof(SoundManager).GetField("lastUiClickFrame",BindingFlags.Instance|BindingFlags.NonPublic);
    private Mouse mouse;
    private void OnEnable() => Application.logMessageReceived += Log;
    private void OnDisable() { Application.logMessageReceived -= Log; if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse); }
    private void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+trace);}
    private Button CreateButton(string name,Transform parent=null)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));
        if(parent!=null)go.transform.SetParent(parent,false);
        var button=go.GetComponent<Button>();UiSoundEvents.Register(button);return button;
    }
    private IEnumerator Click(Vector2 screen)
    {
        InputSystem.QueueStateEvent(mouse,new MouseState{position=screen}.WithButton(MouseButton.Left));yield return null;yield return null;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=screen});yield return null;
    }
    private IEnumerator Start()
    {
        var camera=new GameObject("Verification Camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=5;camera.gameObject.AddComponent<AudioListener>();
        new GameObject("Verification EventSystem",typeof(EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        int created=0,clicked=0,toggled=0;
        System.Action<UiSoundEmitter> onCreate=_=>created++,onClick=_=>clicked++;
        System.Action<UiSoundEmitter,bool> onToggle=(_,__)=>toggled++;
        UiSoundEvents.Created+=onCreate;UiSoundEvents.Clicked+=onClick;UiSoundEvents.ToggleChanged+=onToggle;
        var early=CreateButton("Before SoundManager");
        var manager=Instantiate(soundPrefab).GetComponent<SoundManager>();
        yield return null;early.onClick.Invoke();
        report["uiCreatedBeforeSoundManagerPlays"]=(int)clickFrame.GetValue(manager)==Time.frameCount;
        var late=CreateButton("Dynamic Button");yield return null;late.onClick.Invoke();
        report["dynamicButtonPlays"]=(int)clickFrame.GetValue(manager)==Time.frameCount;
        late.gameObject.SetActive(false);late.gameObject.SetActive(true);yield return null;
        int before=clicked;late.onClick.Invoke();report["noDuplicateAfterReenable"]=clicked-before==1;
        var clone=Instantiate(late);yield return null;before=clicked;clone.onClick.Invoke();report["clonedButtonPublishesOnce"]=clicked-before==1;
        var toggleObject=new GameObject("Dynamic Toggle",typeof(RectTransform),typeof(Toggle));var toggle=toggleObject.GetComponent<Toggle>();UiSoundEvents.Register(toggle);
        yield return null;toggle.isOn=true;bool onPlayed=(int)clickFrame.GetValue(manager)==Time.frameCount;
        yield return null;toggle.isOn=false;report["toggleBothDirectionsPlay"]=onPlayed&&(int)clickFrame.GetValue(manager)==Time.frameCount&&toggled==2;
        manager.enabled=false;yield return null;int previous=(int)clickFrame.GetValue(manager);late.onClick.Invoke();report["soundManagerUnsubscribes"]=(int)clickFrame.GetValue(manager)==previous;
        manager.enabled=true;yield return null;late.onClick.Invoke();report["soundManagerResubscribes"]=(int)clickFrame.GetValue(manager)==Time.frameCount;
        UiSoundEvents.UseDedicatedClick(late);yield return null;previous=(int)clickFrame.GetValue(manager);late.onClick.Invoke();report["dedicatedButtonSuppressesGenericClick"]=(int)clickFrame.GetValue(manager)==previous;
        UiSoundEvents.Request(UiSoundCue.Confirm);report["dedicatedConfirmEventPlays"]=(int)clickFrame.GetValue(manager)==Time.frameCount;
        Destroy(clone.gameObject);yield return null;
        bool stale=false;foreach(var control in UiSoundEvents.Controls)if(control==null)stale=true;report["destroyedControlsUnregistered"]=!stale;
        UiSoundEvents.Created-=onCreate;UiSoundEvents.Clicked-=onClick;UiSoundEvents.ToggleChanged-=onToggle;
        report["uiCreationEvents"]=created;

        var staging=new GameObject("Inactive Setup");staging.SetActive(false);
        var unitObject=new GameObject("Unit");unitObject.SetActive(false);unitObject.tag="Player";unitObject.AddComponent<CircleCollider2D>().radius=.35f;
        var unit=unitObject.AddComponent<PlayerObj>();unit._charMS=3;unit._prefabs=Instantiate(unitPrefab,unitObject.transform);unit._prefabs.transform.localPosition=Vector3.zero;
        var marker=new GameObject("Selection Marker");var goalMarker=new GameObject("Goal Marker");
        var managerObject=new GameObject("Player Manager");managerObject.SetActive(false);var players=managerObject.AddComponent<PlayerManager>();players._savedUnitList.Add(unit._prefabs);players._playerList.Add(unit);players._playerObjCircle=marker.transform;players._goalObjCircle=goalMarker.transform;players._camera=camera;
        unitObject.SetActive(true);managerObject.SetActive(true);
        var ground=new GameObject("Move Destination");ground.transform.position=new Vector3(4,0,0);ground.AddComponent<BoxCollider2D>().size=new Vector2(2,2);
        yield return null;yield return null;
        mouse=InputSystem.AddDevice<Mouse>("Verification Mouse");
        yield return Click(camera.WorldToScreenPoint(unit.transform.position));report["inputActionSelectsCharacter"]=players._nowObj==unit;
        var canvasObject=new GameObject("UI Blocker",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));canvasObject.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var blocker=CreateButton("Blocker",canvasObject.transform);var rect=blocker.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=Vector2.one*.5f;rect.sizeDelta=new Vector2(140,140);rect.anchoredPosition=camera.WorldToScreenPoint(ground.transform.position);
        Canvas.ForceUpdateCanvases();yield return Click(camera.WorldToScreenPoint(ground.transform.position));report["uiClickDoesNotIssueMove"]=!unit.IsMoving&&(Vector2)unit._goalPos==Vector2.zero;
        canvasObject.SetActive(false);yield return Click(camera.WorldToScreenPoint(ground.transform.position));report["inputActionStartsMovement"]=unit.IsMoving;
        yield return new WaitForSeconds(.2f);
        float previousTime=unit._prefabs._anim.GetCurrentAnimatorStateInfo(0).normalizedTime;
        unit.SetMovePos(new Vector2(3,1));yield return new WaitForSeconds(.1f);
        report["retargetKeepsMoveAnimation"]=unit._prefabs._anim.GetCurrentAnimatorStateInfo(0).normalizedTime>=previousTime;
        report["rightFacingPreserved"]=unit._prefabs.transform.localScale.x<0;
        float elapsed=0,maxMarkerError=0;
        while(unit.IsMoving&&elapsed<4){maxMarkerError=Mathf.Max(maxMarkerError,Vector3.Distance(marker.transform.position,unit.transform.position));elapsed+=Time.deltaTime;yield return null;}
        report["arrivesAndStopsCoroutine"]=!unit.IsMoving&&Vector2.Distance(unit.transform.position,new Vector2(3,1))<.011f;
        report["yBasedZSorting"]=Mathf.Abs(unit.transform.position.z-unit.transform.localPosition.y*.01f)<.0001f;
        report["selectionMarkerFollowsOnlyMovement"]=maxMarkerError<.0001f;
        int positionEvents=0;System.Action<PlayerObj> positionChanged=_=>positionEvents++;unit.PositionChanged+=positionChanged;
        yield return new WaitForSeconds(.25f);report["noIdlePositionUpdates"]=positionEvents==0;
        report["idleAnimationOnArrival"]=!unit._prefabs._anim.GetBool("1_Move");
        unit.SetMovePos(new Vector2(0,1));yield return new WaitForSeconds(.15f);report["leftFacingPreserved"]=unit._prefabs.transform.localScale.x>0;
        unit.isAction=true;unit.SetStateAnimationIndex(PlayerState.ATTACK,0);unit.PlayStateAnimation(PlayerState.ATTACK);Vector3 stopped=unit.transform.position;
        yield return new WaitForSeconds(.2f);report["animationPreviewStopsMovement"]=!unit.IsMoving&&unit.transform.position==stopped;
        unit.SetMovePos(new Vector2(0,0));players.enabled=false;Vector3 oldMarker=marker.transform.position;yield return new WaitForSeconds(.15f);report["selectionUnsubscribes"]=marker.transform.position==oldMarker;
        players.enabled=true;yield return null;report["selectionResubscribes"]=Vector3.Distance(marker.transform.position,unit.transform.position)<.0001f;
        unitObject.SetActive(false);yield return null;report["disabledCharacterClearsSelection"]=players._nowObj==null&&!marker.activeSelf;
        InputSystem.RemoveDevice(mouse);mouse=null;
        report["errors"]=errors;report["completed"]=true;
        File.WriteAllText("Tools/EventDrivenUI/RuntimeVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        Debug.Log("EVENT_DRIVEN_UI_CHECK_COMPLETE");
    }
}
