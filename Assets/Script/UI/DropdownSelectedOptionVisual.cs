using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public sealed class DropdownSelectedOptionVisual : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    private static readonly Color HoverBackgroundColor =
        new Color(1f, 1f, 1f, 0.35f);

    private Toggle option;
    private Image background;
    private TMP_Text label;
    private bool isHovered;

    private void Awake()
    {
        option = GetComponent<Toggle>();
        background = GetComponent<Image>();
        label = transform.Find("Item Label")?.GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        isHovered = false;
        option.onValueChanged.AddListener(OnSelectionChanged);
        Apply(option.isOn, false);
    }

    private void OnDisable()
    {
        option.onValueChanged.RemoveListener(OnSelectionChanged);
    }

    private void LateUpdate()
    {
        if (transform.GetSiblingIndex() == 1)
            MoveBlockerBehindSettings();
    }

    private void MoveBlockerBehindSettings()
    {
        TMP_Dropdown dropdown = GetComponentInParent<TMP_Dropdown>();

        if (dropdown == null)
            return;

        Canvas rootCanvas = dropdown.GetComponentInParent<Canvas>()?.rootCanvas;
        Canvas listCanvas = transform.parent.parent.parent.GetComponent<Canvas>();

        if (rootCanvas == null || listCanvas == null)
            return;

        foreach (Transform child in rootCanvas.transform)
        {
            if (child.name != "Blocker")
                continue;

            Canvas blockerCanvas = child.GetComponent<Canvas>();

            if (blockerCanvas != null &&
                blockerCanvas.sortingOrder == listCanvas.sortingOrder - 1)
            {
                blockerCanvas.sortingOrder = rootCanvas.sortingOrder - 1;
            }
        }
    }

    private void OnSelectionChanged(bool selected)
    {
        Apply(selected, isHovered);
    }

    private void Apply(bool selected, bool hovered)
    {
        if (background != null)
            background.color = selected
                ? Color.white
                : hovered ? HoverBackgroundColor : Color.clear;

        if (label != null)
            label.color = selected ? Color.black : Color.white;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        Apply(option.isOn, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        Apply(option.isOn, false);
    }
}
