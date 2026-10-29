using UnityEngine;
using UnityEngine.EventSystems;

public class UpgradeHover : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Hover")]
    [SerializeField] private GameObject outLine;
    [SerializeField] private RectTransform hoverTarget;

    [Header("Move")]
    [Tooltip("기본 위치에서 위로 얼마나 올라갈지")]
    [SerializeField] private float targetYValue = 30f;

    [SerializeField] private float upSpeed = 10f;
    [SerializeField] private float downSpeed = 10f;

    [Header("Scale")]
    [SerializeField] private float targetScale = 1.1f;


    private RectTransform visualRect;

    private Vector3 defaultScale;
    private float defaultYValue;

    private bool isHovering;

    // 호버 입력 자체를 막는 용도
    private bool hoverBlocked;

    // 선택 후 현재 커진 상태를 유지하는 용도
    private bool hoverLocked;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        // hoverTarget을 지정하지 않았다면 자기 자신 사용
        visualRect = hoverTarget != null
            ? hoverTarget
            : transform as RectTransform;


        if (visualRect == null)
        {
            Debug.LogError(
                $"{name} : UpgradeHover에 RectTransform이 없습니다."
            );

            return;
        }


        // 원래 크기 저장
        defaultScale =
            visualRect.localScale;


        // 원래 Y 위치 저장
        defaultYValue =
            visualRect.anchoredPosition.y;


        if (outLine != null)
        {
            outLine.SetActive(false);
        }
    }


    // =========================================================
    // OnEnable
    // =========================================================

    private void OnEnable()
    {
        hoverBlocked = false;
        hoverLocked = false;
        isHovering = false;


        RestoreVisual();
    }


    // =========================================================
    // OnDisable
    // =========================================================

    private void OnDisable()
    {
        hoverBlocked = false;
        hoverLocked = false;
        isHovering = false;


        RestoreVisual();
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (visualRect == null)
            return;


        // -----------------------------------------------------
        // 선택 후 호버 고정 상태
        //
        // 이미 커진 상태 그대로 유지
        // -----------------------------------------------------

        if (hoverLocked)
        {
            return;
        }


        // -----------------------------------------------------
        // 호버 중
        // -----------------------------------------------------

        if (isHovering && !hoverBlocked)
        {
            MoveUp();
            ScaleUp();
        }

        // -----------------------------------------------------
        // 호버 해제
        // -----------------------------------------------------

        else
        {
            MoveDown();
            ScaleDown();
        }
    }


    // =========================================================
    // Pointer Enter
    // =========================================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 선택 후 잠긴 상태라면 아무것도 하지 않음
        if (hoverLocked)
            return;


        // 강제로 호버가 막힌 상태
        if (hoverBlocked)
            return;


        isHovering = true;

        SoundManager.Instance?.PlayUiHover();


        if (outLine != null)
        {
            outLine.SetActive(true);
        }
    }


    // =========================================================
    // Pointer Exit
    // =========================================================

    public void OnPointerExit(PointerEventData eventData)
    {
        // 선택한 카드는 마우스가 나가도
        // 현재 커진 상태 유지
        if (hoverLocked)
            return;


        isHovering = false;


        if (outLine != null)
        {
            outLine.SetActive(false);
        }
    }


    // =========================================================
    // 위로 이동
    // =========================================================

    private void MoveUp()
    {
        Vector2 position =
            visualRect.anchoredPosition;


        float targetY =
            defaultYValue + targetYValue;


        position.y =
            Mathf.MoveTowards(
                position.y,
                targetY,
                upSpeed * Time.unscaledDeltaTime
            );


        visualRect.anchoredPosition =
            position;
    }


    // =========================================================
    // 원래 위치로 복귀
    // =========================================================

    private void MoveDown()
    {
        Vector2 position =
            visualRect.anchoredPosition;


        position.y =
            Mathf.MoveTowards(
                position.y,
                defaultYValue,
                downSpeed * Time.unscaledDeltaTime
            );


        visualRect.anchoredPosition =
            position;
    }


    // =========================================================
    // 확대
    // =========================================================

    private void ScaleUp()
    {
        Vector3 targetScaleValue =
            defaultScale * targetScale;


        visualRect.localScale =
            Vector3.MoveTowards(
                visualRect.localScale,
                targetScaleValue,
                upSpeed * Time.unscaledDeltaTime
            );
    }


    // =========================================================
    // 원래 크기로 복귀
    // =========================================================

    private void ScaleDown()
    {
        visualRect.localScale =
            Vector3.MoveTowards(
                visualRect.localScale,
                defaultScale,
                downSpeed * Time.unscaledDeltaTime
            );
    }


    // =========================================================
    // 선택한 카드용
    //
    // 현재 호버 상태 그대로 고정
    //
    // 중요:
    // RestoreVisual()을 호출하지 않음
    // 그래서 현재 커져있는 크기와 위치가 유지됨
    // =========================================================

    public void LockCurrentHover()
    {
        hoverLocked = true;
        hoverBlocked = true;
        isHovering = true;


        // 현재 상태 그대로 유지
        // localScale이나 위치를 건드리지 않음


        if (outLine != null)
        {
            outLine.SetActive(true);
        }
    }


    // =========================================================
    // 다른 카드용
    //
    // 호버 강제로 취소
    // 원래 크기 / 위치로 즉시 복귀
    // =========================================================

    public void ForceHoverCancel()
    {
        hoverLocked = false;
        hoverBlocked = true;
        isHovering = false;


        RestoreVisual();
    }


    // =========================================================
    // 다시 호버 가능하게
    // =========================================================

    public void EnableHover()
    {
        hoverLocked = false;
        hoverBlocked = false;
        isHovering = false;


        RestoreVisual();
    }


    // =========================================================
    // 완전 초기화
    // =========================================================

    private void RestoreVisual()
    {
        if (visualRect == null)
            return;


        // Y 원래 위치
        Vector2 position =
            visualRect.anchoredPosition;

        position.y =
            defaultYValue;

        visualRect.anchoredPosition =
            position;


        // 크기 원래대로
        visualRect.localScale =
            defaultScale;


        // Outline OFF
        if (outLine != null)
        {
            outLine.SetActive(false);
        }
    }
}
