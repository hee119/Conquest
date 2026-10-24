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
        slider = GetComponent<Slider>();
        CreateButtons();
        lastPlayingState = Application.isPlaying;
        SetButtonsInteractable(lastPlayingState);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        UnityEditor.EditorApplication.delayCall -= EnsureEditorPreview;
        UnityEditor.EditorApplication.delayCall += EnsureEditorPreview;
    }

    private void EnsureEditorPreview()
    {
        if (this == null || Application.isPlaying || !gameObject.scene.IsValid())
            return;

        slider = GetComponent<Slider>();
        CreateButtons();
        SetButtonsInteractable(false);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

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

    private void ClickPlus()
    {
        if (ConsumeSuppressedClick())
            return;

        Plus();
    }

    private void ClickMinus()
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

    private void CreateButtons()
    {
        Transform existingGroup = transform.Find(ButtonGroupName);
        RectTransform group;
        if (existingGroup != null)
        {
            group = (RectTransform)existingGroup;
        }
        else
        {
            var groupObject = new GameObject(ButtonGroupName, typeof(RectTransform));
            group = (RectTransform)groupObject.transform;
            group.SetParent(transform, false);
        }

        group.anchorMin = new Vector2(1f, 0.5f);
        group.anchorMax = new Vector2(1f, 0.5f);
        group.pivot = new Vector2(0f, 0.5f);
        group.anchoredPosition = new Vector2(132f, 0f);
        group.sizeDelta = new Vector2(78f, 38f);

        ConfigureButton(group, "Minus", "-", 0f, ClickMinus, Minus);
        ConfigureButton(group, "Plus", "+", 40f, ClickPlus, Plus);
    }

    private void ConfigureButton(RectTransform parent, string objectName, string label, float x, UnityAction clickAction, UnityAction repeatAction)
    {
        Transform existingButton = parent.Find(objectName);
        GameObject buttonObject = existingButton != null
            ? existingButton.gameObject
            : new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var buttonRect = (RectTransform)buttonObject.transform;
        if (existingButton == null)
            buttonRect.SetParent(parent, false);
        buttonRect.anchorMin = new Vector2(0f, 0.5f);
        buttonRect.anchorMax = new Vector2(0f, 0.5f);
        buttonRect.pivot = new Vector2(0f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(x, 0f);
        buttonRect.sizeDelta = new Vector2(36f, 36f);

        Image background = buttonObject.GetComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.08f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.interactable = Application.isPlaying;
        button.onClick.RemoveListener(clickAction);
        button.onClick.AddListener(clickAction);

        SliderStepHold hold = buttonObject.GetComponent<SliderStepHold>();
        if (hold == null)
            hold = buttonObject.AddComponent<SliderStepHold>();
        hold.Initialize(this, repeatAction, holdDelay, holdRepeatInterval);

        Transform existingLabel = buttonRect.Find("Label");
        GameObject textObject = existingLabel != null
            ? existingLabel.gameObject
            : new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var textRect = (RectTransform)textObject.transform;
        if (existingLabel == null)
            textRect.SetParent(buttonRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 25f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
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
