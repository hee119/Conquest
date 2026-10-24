using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public sealed class SliderStepHold : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private SliderPlusMinus owner;
    private UnityAction repeatAction;
    private float holdDelay;
    private float repeatInterval;
    private Coroutine repeatCoroutine;
    private bool pointerHeld;
    private bool didRepeat;

    public void Initialize(SliderPlusMinus sliderOwner, UnityAction action, float delay, float interval)
    {
        owner = sliderOwner;
        repeatAction = action;
        holdDelay = delay;
        repeatInterval = interval;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerHeld = true;
        didRepeat = false;
        repeatCoroutine = StartCoroutine(RepeatWhileHeld());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StopRepeating(eventData.pointerEnter == gameObject);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopRepeating(false);
    }

    // +/- 버튼은 Slider의 자식이므로 드래그 이벤트를 처리하지 않으면
    // 부모 Slider가 포인터 위치를 값으로 해석해 갑자기 0%/100%로 이동한다.
    public void OnBeginDrag(PointerEventData eventData)
    {
        eventData.Use();
    }

    public void OnDrag(PointerEventData eventData)
    {
        eventData.Use();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        StopRepeating(false);
        eventData.Use();
    }

    private IEnumerator RepeatWhileHeld()
    {
        yield return new WaitForSecondsRealtime(holdDelay);

        while (pointerHeld)
        {
            didRepeat = true;
            repeatAction?.Invoke();
            yield return new WaitForSecondsRealtime(repeatInterval);
        }

        repeatCoroutine = null;
    }

    private void StopRepeating(bool suppressClick)
    {
        bool wasHeld = pointerHeld;
        pointerHeld = false;
        if (repeatCoroutine != null)
        {
            StopCoroutine(repeatCoroutine);
            repeatCoroutine = null;
        }

        if (suppressClick && wasHeld && didRepeat)
        {
            owner?.SuppressNextClick();
        }

        didRepeat = false;
    }

    private void OnDisable()
    {
        StopRepeating(false);
    }
}
