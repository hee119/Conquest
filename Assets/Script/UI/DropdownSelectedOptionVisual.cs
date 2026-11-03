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

    private void Start()
    {
        // TMP finishes copying and initializing the item after OnEnable.
        // Reapply once so the initially selected row keeps readable text.
        Apply(option.isOn, isHovered);
    }

    private void OnDisable()
    {
        option.onValueChanged.RemoveListener(OnSelectionChanged);
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
        {
            label.outlineWidth = 0f;
            label.color = selected ? Color.black : Color.white;
        }
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
