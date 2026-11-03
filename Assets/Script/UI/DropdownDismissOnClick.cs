using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Dropdown))]
public sealed class DropdownDismissOnClick : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
{
    private TMP_Dropdown dropdown;
    private SharedCanvasDropdown shared;
    private bool Expanded => shared != null ? shared.IsExpanded : dropdown.IsExpanded;
    private void Close() { if (shared != null) shared.Hide(); else dropdown.Hide(); }
    private bool wasExpandedOnPointerDown;

    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        shared = GetComponent<SharedCanvasDropdown>();
    }

    private void OnEnable()
    {
        dropdown.onValueChanged.AddListener(OnOptionSelected);
    }

    private void OnDisable()
    {
        dropdown.onValueChanged.RemoveListener(OnOptionSelected);
    }

    private void OnOptionSelected(int index)
    {
        UiSoundEvents.Request(UiSoundCue.Confirm);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        wasExpandedOnPointerDown = Expanded;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!wasExpandedOnPointerDown && Expanded)
            UiSoundEvents.Request(UiSoundCue.Click);

        if (wasExpandedOnPointerDown && Expanded)
            Close();

        wasExpandedOnPointerDown = false;
    }

}
