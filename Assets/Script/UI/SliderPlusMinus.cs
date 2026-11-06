using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[ExecuteAlways]
public class SliderPlusMinus : MonoBehaviour
{
    public event Action onValueChanged;
    public Slider slider;

    [SerializeField, Min(0f)]
    private float step = 0.01f;

    [SerializeField, Min(0f)]
    private float holdDelay = 0.35f;

    [SerializeField, Min(0.01f)]
    private float holdRepeatInterval = 0.08f;

    private const string ButtonGroupName = "Value Step Buttons";
    private bool suppressNextClick;
    private bool lastPlayingState;

    private void OnEnable()
    {
        suppressNextClick = false;
        slider = GetComponent<Slider>();
        if (Application.isPlaying) SliderInputBounds.Attach(slider);
        InitializeAuthoredButtons();
        lastPlayingState = Application.isPlaying;
        SetButtonsInteractable(lastPlayingState);
    }



    private void Update()
    {
        if (lastPlayingState == Application.isPlaying)
            return;

        lastPlayingState = Application.isPlaying;
        SetButtonsInteractable(lastPlayingState);
    }

    public void Plus()
    {
        ChangeValue(step);
    }

    public void Minus()
    {
        ChangeValue(-step);
    }

    private void ChangeValue(float delta)
    {
        if (slider == null)
            return;

        float nextValue = Mathf.Clamp(
            slider.value + delta,
            slider.minValue,
            slider.maxValue);
        if (Mathf.Approximately(nextValue, slider.value))
            return;

        // 버튼 변경도 드래그와 같은 Slider 이벤트 경로를 사용한다.
        // 밝기, 음량, 퍼센트 텍스트가 항상 같은 값으로 갱신된다.
        slider.SetValueWithoutNotify(nextValue);
        slider.onValueChanged.Invoke(nextValue);
        onValueChanged?.Invoke();
    }

    public void ClickPlus()
    {
        if (ConsumeSuppressedClick())
            return;

        Plus();
    }

    public void ClickMinus()
    {
        if (ConsumeSuppressedClick())
            return;

        Minus();
    }

    private bool ConsumeSuppressedClick()
    {
        if (!suppressNextClick)
            return false;

        suppressNextClick = false;
        return true;
    }

    internal void SuppressNextClick()
    {
        suppressNextClick = true;
    }

    private void InitializeAuthoredButtons()
    {
        var group = transform.Find(ButtonGroupName);
        if (group == null) return;
        foreach (var button in group.GetComponentsInChildren<Button>(true))
        {
            var hold = button.GetComponent<SliderStepHold>();
            if (hold != null)
                hold.Initialize(this, button.name == "Minus" ? Minus : Plus, holdDelay, holdRepeatInterval);
            if (Application.isPlaying) UiSoundEvents.Register(button);
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        Transform group = transform.Find(ButtonGroupName);
        if (group == null)
            return;

        foreach (Button button in group.GetComponentsInChildren<Button>(true))
            button.interactable = interactable;
    }
}
