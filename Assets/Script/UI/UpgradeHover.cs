using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>증강 항목에 포인터를 올렸을 때의 표시를 관리한다.</summary>
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

    /// <summary>필요한 컴포넌트와 초기 상태를 준비한다.</summary>
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

    /// <summary>활성화될 때 필요한 이벤트와 동작을 시작한다.</summary>
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

    /// <summary>비활성화될 때 이벤트와 진행 중인 동작을 정리한다.</summary>
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

    /// <summary>프레임마다 갱신이 필요한 상태를 처리한다.</summary>
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
            ScaleUp(targetScale);
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

    /// <summary>포인터가 항목에 들어왔을 때 표시 상태를 변경한다.</summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 선택 후 잠긴 상태라면 아무것도 하지 않음
        if (hoverLocked)
            return;


        // 강제로 호버가 막힌 상태
        if (hoverBlocked)
            return;


        isHovering = true;

        UiSoundEvents.Request(UiSoundCue.Hover);


        if (outLine != null)
        {
            outLine.SetActive(true);
        }
    }


    // =========================================================
    // Pointer Exit
    // =========================================================

    /// <summary>포인터가 항목에서 나갔을 때 표시 상태를 복원한다.</summary>
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

    /// <summary>MoveUp 동작을 처리한다.</summary>
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

    /// <summary>MoveDown 동작을 처리한다.</summary>
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

    /// <summary>ScaleUp 동작을 처리한다.</summary>
    public void ScaleUp(float _targetScale)
    {
        Vector3 targetScaleValue =
            defaultScale * _targetScale;


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

    /// <summary>ScaleDown 동작을 처리한다.</summary>
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

    /// <summary>LockCurrentHover 동작을 처리한다.</summary>
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

    /// <summary>ForceHoverCancel 동작을 처리한다.</summary>
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

    /// <summary>EnableHover 동작을 처리한다.</summary>
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

    /// <summary>RestoreVisual 동작을 처리한다.</summary>
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
