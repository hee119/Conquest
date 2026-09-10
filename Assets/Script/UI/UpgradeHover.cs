using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class UpgradeHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    private GameObject outLine;

    [SerializeField]
    private float targetYValue;

    [SerializeField]
    private float upSpeed;

    [SerializeField]
    private float downSpeed;

    [SerializeField]
    private float targetScale = 1.1f;

    private float defaultYValue;
    private Vector3 defaultScale;

    private Coroutine hover;
    private Coroutine hoverCancel;

    public void Awake()
    {
        defaultYValue = transform.localPosition.y;
        defaultScale = new Vector3(1, 1, 1);

        outLine.SetActive(false);
    }

    // 마우스가 오브젝트 위에 들어왔을 때
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"{gameObject.name} Hover 시작");

        if (hoverCancel != null)
        {
            StopCoroutine(hoverCancel);
            hoverCancel = null;
        }

        if (hover == null)
        {
            hover = StartCoroutine(Hover());
        }
    }

    // 마우스가 오브젝트에서 빠져나갔을 때
    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"{gameObject.name} Hover Cancel");

        if (hover != null)
        {
            StopCoroutine(hover);
            hover = null;
        }

        if (hoverCancel == null)
        {
            hoverCancel = StartCoroutine(HoverCancel());
        }
    }

    public IEnumerator Hover()
    {
        outLine.SetActive(true);

        float targetY = defaultYValue + targetYValue;
        Vector3 targetScaleValue = defaultScale * targetScale;

        while (
            transform.localPosition.y < targetY ||
            transform.localScale != targetScaleValue
        )
        {
            // Y 이동
            Vector3 pos = transform.localPosition;

            pos.y = Mathf.MoveTowards(
                pos.y,
                targetY,
                upSpeed * Time.unscaledDeltaTime
            );

            transform.localPosition = pos;

            // Scale 증가
            transform.localScale = Vector3.MoveTowards(
                transform.localScale,
                targetScaleValue,
                upSpeed * Time.unscaledDeltaTime
            );

            yield return null;
        }

        // 정확하게 목표값 맞추기
        Vector3 finalPos = transform.localPosition;
        finalPos.y = targetY;
        transform.localPosition = finalPos;

        transform.localScale = targetScaleValue;

        hover = null;
    }

    public IEnumerator HoverCancel()
    {
        outLine.SetActive(false);

        while (
            transform.localPosition.y > defaultYValue ||
            transform.localScale != defaultScale
        )
        {
            // Y 이동
            Vector3 pos = transform.localPosition;

            pos.y = Mathf.MoveTowards(
                pos.y,
                defaultYValue,
                downSpeed * Time.unscaledDeltaTime
            );

            transform.localPosition = pos;

            // Scale 감소
            transform.localScale = Vector3.MoveTowards(
                transform.localScale,
                defaultScale,
                downSpeed * Time.unscaledDeltaTime
            );

            yield return null;
        }

        // 정확하게 원래값 맞추기
        Vector3 finalPos = transform.localPosition;
        finalPos.y = defaultYValue;
        transform.localPosition = finalPos;

        transform.localScale = defaultScale;

        hoverCancel = null;
    }
}