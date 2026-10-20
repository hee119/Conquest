using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public sealed class DropdownSelectedOptionVisual : MonoBehaviour, IPointerClickHandler
{
    private Toggle option;
    private Image background;
    private Image oldBackground;
    private TMP_Text label;

    private void Awake()
    {
        option = GetComponent<Toggle>();
        background = GetComponent<Image>();
        oldBackground = transform.Find("Item Background")?.GetComponent<Image>();
        label = transform.Find("Item Label")?.GetComponent<TMP_Text>();

        if (oldBackground != null)
        {
            oldBackground.enabled = true;
            oldBackground.sprite = null;
            oldBackground.type = Image.Type.Simple;
            oldBackground.raycastTarget = false;
        }

        if (background != null)
        {
            background.sprite = null;
            background.type = Image.Type.Simple;
            background.raycastTarget = true;
        }
    }

    private void OnEnable()
    {
        option.enabled = false;
        Apply(IsSelected());
    }

    private void LateUpdate()
    {
        if (transform.GetSiblingIndex() == 1)
            MoveBlockerBehindSettings();

        bool selected = IsSelected();

        if ((oldBackground != null &&
             oldBackground.color != (selected ? Color.white : Color.clear)) ||
            (label != null &&
             label.color != (selected ? Color.black : Color.white)))
        {
            Apply(selected);
        }
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

    private bool IsSelected()
    {
        TMP_Dropdown dropdown = GetComponentInParent<TMP_Dropdown>();

        return dropdown != null &&
               transform.GetSiblingIndex() - 1 == dropdown.value;
    }

    private void Apply(bool selected)
    {
        if (background != null)
            background.color = Color.clear;

        if (oldBackground != null)
            oldBackground.color = selected ? Color.white : Color.clear;

        if (label != null)
            label.color = selected ? Color.black : Color.white;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TMP_Dropdown dropdown = GetComponentInParent<TMP_Dropdown>();

        if (dropdown == null)
            return;

        int index = transform.GetSiblingIndex() - 1;

        if (index < 0 || index >= dropdown.options.Count)
            return;

        dropdown.value = index;
        dropdown.RefreshShownValue();

        Transform content = transform.parent;

        for (int i = 1; i < content.childCount; i++)
        {
            Toggle item = content.GetChild(i).GetComponent<Toggle>();

            if (item != null)
                item.SetIsOnWithoutNotify(i - 1 == index);
        }

        // 선택지를 클릭하면 드롭다운 닫기
        dropdown.Hide();
    }
}