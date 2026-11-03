using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>UpgradeAppeared 증강 관련 동작과 상태를 관리한다.</summary>
public class UpgradeAppeared : MonoBehaviour
{
    [SerializeField] private float turnSecond;
    [SerializeField] private float numberOfTurns;
    [SerializeField] private float bigScale;
    [SerializeField] private float bigScaleSecond;
    [SerializeField] private float bigScaleDuration;
    [SerializeField] private Graphic raycastGraphic; // 레이캐스트 껐다 켤 대상 (Image/Text 등)

    private Vector3 defaultScale;

    /// <summary>필요한 컴포넌트와 초기 상태를 준비한다.</summary>
    private void Awake()
    {
        defaultScale = transform.localScale;
    }

    /// <summary>활성화될 때 필요한 이벤트와 동작을 시작한다.</summary>
    private void OnEnable()
    {
        if (raycastGraphic != null)
            raycastGraphic.raycastTarget = false;

        StartCoroutine(StartTurn());
    }

    /// <summary>StartTurn 동작을 처리한다.</summary>
    IEnumerator StartTurn()
    {
        float count = 0;
        float rotateTime = turnSecond / numberOfTurns;

        while (count < numberOfTurns)
        {
            float elapsed = 0;

            while (elapsed < rotateTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float angle = 360f * (elapsed / rotateTime);
                float scaleX = Mathf.Cos(angle * Mathf.Deg2Rad);
                transform.localScale = new Vector3(defaultScale.x * scaleX, defaultScale.y, defaultScale.z);

                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                {
                    transform.localScale = defaultScale;
                    StartCoroutine(BigScale());
                    yield break;
                }

                yield return null;
            }

            count++;
        }

        transform.localScale = defaultScale;

        StartCoroutine(BigScale());
    }

    /// <summary>BigScale 동작을 처리한다.</summary>
    IEnumerator BigScale()
    {
        Vector3 peakScale = defaultScale * bigScale;

        float elapsed = 0;
        while (elapsed < bigScaleSecond)
        {
            elapsed += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(defaultScale, peakScale, Mathf.Clamp01(elapsed / bigScaleSecond));
            yield return null;
        }
        transform.localScale = peakScale;

        elapsed = 0;
        while (elapsed < bigScaleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(peakScale, defaultScale, Mathf.Clamp01(elapsed / bigScaleDuration));
            yield return null;
        }
        transform.localScale = defaultScale;

        if (raycastGraphic != null)
            raycastGraphic.raycastTarget = true;
    }
}