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

    public void Awake()
    {
        expSlider = GetComponent<Slider>();
        expSlider.value = 0;
    }

    public void Start()
    {
        LevelManager.Instance.expUpdate += ExpUpdate;
        UpgradeManager.Instance.upgradeSelect += ExpInit;
    }

    public void ExpUpdate()
    {
        float newTarget =
            (float)LevelManager.Instance.currentExp /
            DataManager.Instance.levelDatas[LevelManager.Instance.currentLevel];

        bool leveledUp = newTarget < previousTargetValue;
        previousTargetValue = newTarget;

        // ExpInitCor(리셋 중)가 돌고 있으면 여기서 손대지 않는다.
        // 리셋이 끝나면 그쪽에서 최신 비율을 다시 계산해서 채운다.
        if (expInit != null)
            return;

        targetValue = leveledUp ? 1f : newTarget;

        if (expUpdate == null)
        {
            velocity = 0f;
            expUpdate = StartCoroutine(ExpUpdateCor());
        }
    }

    public void ExpInit()
    {
        if (expInit == null)
            expInit = StartCoroutine(ExpInitCor());
    }

    IEnumerator ExpUpdateCor()
    {
        while (Mathf.Abs(expSlider.value - targetValue) > 0.001f)
        {
            float difference = targetValue - expSlider.value;

            if (difference > 0f && velocity < 0f) velocity = 0f;
            else if (difference < 0f && velocity > 0f) velocity = 0f;

            expSlider.value = Mathf.SmoothDamp(
                expSlider.value,
                targetValue,
                ref velocity,
                0.15f,
                Mathf.Infinity,
                Time.unscaledDeltaTime
            );

            yield return null;
        }

        expSlider.value = targetValue;
        velocity = 0f;
        expUpdate = null;
    }

    IEnumerator ExpInitCor()
    {
        if (expUpdate != null)
        {
            StopCoroutine(expUpdate);
            expUpdate = null;
        }

        velocity = 0f;

        // 1) 0으로 리셋
        float t = 0f;
        float startValue = expSlider.value;

        while (expSlider.value > 0f)
        {
            t += Time.unscaledDeltaTime * 4f;
            expSlider.value = Mathf.Lerp(startValue, 0f, t);
            yield return null;
        }
        expSlider.value = 0f;

        // 2) 리셋이 끝난 "지금" 시점의 정확한 비율을 다시 계산해서 이어서 채운다
        targetValue =
            (float)LevelManager.Instance.currentExp /
            DataManager.Instance.levelDatas[LevelManager.Instance.currentLevel];
        previousTargetValue = targetValue;

        velocity = 0f;
        expUpdate = StartCoroutine(ExpUpdateCor());

        expInit = null;
    }

    private void OnDestroy()
    {
        if (LevelManager.Instance != null)
            LevelManager.Instance.expUpdate -= ExpUpdate;

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.upgradeSelect -= ExpInit;
    }
}