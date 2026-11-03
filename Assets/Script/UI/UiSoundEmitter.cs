using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UiSoundEmitter : MonoBehaviour
{
    [SerializeField] private bool suppressDefaultClick;
    private UnityEngine.UI.Button button;
    private UnityEngine.UI.Toggle toggle;
    private bool bound;
    public bool IsDropdownOption { get; private set; }
    public bool SuppressDefaultClick { get => suppressDefaultClick; set => suppressDefaultClick = value; }

    private void Awake()
    {
        button = GetComponent<UnityEngine.UI.Button>();
        toggle = GetComponent<UnityEngine.UI.Toggle>();
        IsDropdownOption = GetComponentInParent<TMPro.TMP_Dropdown>(true) != null;
        if (button == null) return;
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            string method = button.onClick.GetPersistentMethodName(i);
            if (method == "ConfirmCharacter" || method == "Upgrade" || method == "UpgradeIsSelected" || method == "PlaySelectedStage")
                suppressDefaultClick = true;
        }
    }

    private void OnEnable()
    {
        if (bound) return;
        if (button != null) button.onClick.AddListener(OnClick);
        if (toggle != null) toggle.onValueChanged.AddListener(OnToggle);
        bound = true;
        UiSoundEvents.Add(this);
    }

    private void OnDisable()
    {
        if (!bound) return;
        if (button != null) button.onClick.RemoveListener(OnClick);
        if (toggle != null) toggle.onValueChanged.RemoveListener(OnToggle);
        bound = false;
        UiSoundEvents.Remove(this);
    }

    private void OnClick()
    {
        // A button's existing callback may close its panel before this callback runs.
        if (button != null && button.enabled && button.IsInteractable()) UiSoundEvents.Click(this);
    }

    private void OnToggle(bool value)
    {
        if (toggle != null && toggle.enabled && toggle.IsInteractable()) UiSoundEvents.Toggle(this, value);
    }
}
