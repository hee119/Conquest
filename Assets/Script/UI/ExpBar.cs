using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ExpBar : MonoBehaviour
{
    Slider expSlider;

    Coroutine expUpdate;
    Coroutine expInit;

    private float targetValue;
    private float velocity;
    private float previousTargetValue;
    
    private bool pendingLevelUp;

    public void Awake()
    {
        expSlider = GetComponent<Slider>();
        expSlider.value = 0;

        Debug.Log($"[ExpBar] Awake - Slider 초기화 / value = {expSlider.value}");
    }

    public void Start()
    {
        Debug.Log("[ExpBar] Start");

        LevelManager.Instance.expUpdate += ExpUpdate;
        UpgradeManager.Instance.upgradeSelect += ExpInit;

        Debug.Log("[ExpBar] 이벤트 등록 완료");
    }

    public void ExpUpdate()
    {
        float currentExp = LevelManager.Instance.currentExp;
        float maxExp = DataManager.Instance.levelDatas[LevelManager.Instance.currentLevel];
        float newTarget = currentExp / maxExp;

        if (newTarget < previousTargetValue)
            pendingLevelUp = true;

        previousTargetValue = newTarget;

        // 리셋 대기 중이거나(레벨업 후 증강 선택 전) 리셋이 실제로 진행 중이면
        // targetValue는 무조건 1로 고정 - 여기서 절대 못 내려감
        targetValue = (expInit != null || pendingLevelUp) ? 1f : newTarget;

        if (expInit == null && expUpdate == null)
        {
            velocity = 0f;
            expUpdate = StartCoroutine(ExpUpdateCor());
        }
    }

    public void ExpInit()
    {
        Debug.Log(
            $"[ExpBar] ExpInit 호출\n" +
            $"slider.value = {expSlider.value}\n" +
            $"expUpdate 진행 중 = {expUpdate != null}\n" +
            $"expInit 진행 중 = {expInit != null}"
        );

        if (expInit == null)
        {
            Debug.Log("[ExpBar] ExpInitCor 시작");

            expInit = StartCoroutine(ExpInitCor());
        }
        else
        {
            Debug.Log("[ExpBar] ExpInitCor 이미 실행 중 → 무시");
        }
    }

    IEnumerator ExpUpdateCor()
    {
        Debug.Log(
            $"[ExpBar] ExpUpdateCor 진입\n" +
            $"slider = {expSlider.value}\n" +
            $"target = {targetValue}"
        );

        int frame = 0;

        while (Mathf.Abs(expSlider.value - targetValue) > 0.001f)
        {
            float difference = targetValue - expSlider.value;

            if (difference > 0f && velocity < 0f)
            {
                Debug.Log($"[ExpBar] velocity 방향 반전 → 0 (현재 velocity = {velocity})");
                velocity = 0f;
            }
            else if (difference < 0f && velocity > 0f)
            {
                Debug.Log($"[ExpBar] velocity 방향 반전 → 0 (현재 velocity = {velocity})");
                velocity = 0f;
            }

            float beforeValue = expSlider.value;

            expSlider.value = Mathf.SmoothDamp(
                expSlider.value,
                targetValue,
                ref velocity,
                0.15f,
                Mathf.Infinity,
                Time.unscaledDeltaTime
            );

            // 매 프레임 찍으면 너무 많으니까 10프레임마다 출력
            if (frame % 10 == 0)
            {
                Debug.Log(
                    $"[ExpBar] ExpUpdateCor 진행\n" +
                    $"slider : {beforeValue} → {expSlider.value}\n" +
                    $"target : {targetValue}\n" +
                    $"difference : {difference}\n" +
                    $"velocity : {velocity}"
                );
            }

            frame++;

            yield return null;
        }

        expSlider.value = targetValue;
        velocity = 0f;
        expUpdate = null;

        Debug.Log(
            $"[ExpBar] ExpUpdateCor 종료\n" +
            $"최종 slider = {expSlider.value}\n" +
            $"target = {targetValue}"
        );
    }

    IEnumerator ExpInitCor()
    {
        Debug.Log(
            $"[ExpBar] ExpInitCor 진입\n" +
            $"현재 slider = {expSlider.value}"
        );

        if (expUpdate != null)
        {
            Debug.Log("[ExpBar] 기존 ExpUpdateCor 중지");

            StopCoroutine(expUpdate);
            expUpdate = null;
        }

        velocity = 0f;

        // 1) 0으로 리셋
        float t = 0f;
        float startValue = expSlider.value;

        Debug.Log(
            $"[ExpBar] EXP 바 0으로 리셋 시작\n" +
            $"startValue = {startValue}"
        );

        while (expSlider.value > 0f)
        {
            t += Time.unscaledDeltaTime * 4f;

            expSlider.value = Mathf.Lerp(
                startValue,
                0f,
                t
            );

            yield return null;
        }

        expSlider.value = 0f;

        Debug.Log("[ExpBar] EXP 바 0으로 리셋 완료");

        // 2) 현재 경험치 다시 계산
        float currentExp = LevelManager.Instance.currentExp;
        float maxExp =
            DataManager.Instance.levelDatas[
                LevelManager.Instance.currentLevel
            ];

        targetValue = currentExp / maxExp;
        previousTargetValue = targetValue;
        pendingLevelUp = false;   // 리셋 다 끝났으니 대기 상태 해제

        Debug.Log(
            $"[ExpBar] 리셋 후 최신 EXP 계산\n" +
            $"currentExp = {currentExp}\n" +
            $"maxExp = {maxExp}\n" +
            $"targetValue = {targetValue}"
        );

        velocity = 0f;

        Debug.Log("[ExpBar] 리셋 후 ExpUpdateCor 시작");

        expUpdate = StartCoroutine(ExpUpdateCor());

        expInit = null;

        Debug.Log("[ExpBar] ExpInitCor 종료");
    }

    private void OnDisable()
    {
        Debug.Log("[ExpBar] OnDestroy");

        if (LevelManager.Instance != null)
            LevelManager.Instance.expUpdate -= ExpUpdate;

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.upgradeSelect -= ExpInit;
    }
}