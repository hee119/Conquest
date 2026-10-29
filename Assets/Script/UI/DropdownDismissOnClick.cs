using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Dropdown))]
public sealed class DropdownDismissOnClick : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
{
    private TMP_Dropdown dropdown;
    private RectTransform dropdownRect;
    private bool wasExpandedOnPointerDown;

    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        dropdownRect = transform as RectTransform;
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
        SoundManager.Instance?.PlayUiConfirm();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        wasExpandedOnPointerDown = dropdown.IsExpanded;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!wasExpandedOnPointerDown && dropdown.IsExpanded)
            SoundManager.Instance?.PlayUiClick();

        if (wasExpandedOnPointerDown && dropdown.IsExpanded)
            dropdown.Hide();

        wasExpandedOnPointerDown = false;
    }

    private void LateUpdate()
    {
        if (!dropdown.IsExpanded || !Input.GetMouseButtonDown(0))
            return;

        Vector2 pointer = Input.mousePosition;

        // The opening button is handled on pointer click, after pointer down.
        if (Contains(dropdownRect, pointer))
            return;

        Transform list = dropdown.template != null
            ? dropdown.template.parent.Find("Dropdown List")
            : null;

        if (list is RectTransform listRect && Contains(listRect, pointer))
            return;

        dropdown.Hide();
    }

    private static bool Contains(RectTransform rect, Vector2 pointer)
    {
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, pointer, camera);
    }
}
