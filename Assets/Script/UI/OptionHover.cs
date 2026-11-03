using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OptionHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject hoverOutline;

    private Image image;
    private Button button;
    private OptionSelect optionSelect;
    private float defaultAlpha;
    private bool outlineWasActive;
    private bool isHovered;

    public void Awake()
    {
        image = GetComponent<Image>();
        button = GetComponent<Button>();
        optionSelect = GetComponent<OptionSelect>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isHovered || (button != null && !button.IsInteractable()) ||
            (optionSelect != null && optionSelect.isSelected))
            return;

        isHovered = true;
        if (hoverOutline != null)
        {
            outlineWasActive = hoverOutline.activeSelf;
            hoverOutline.SetActive(true);
            return;
        }

        if (image == null) return;
        defaultAlpha = image.color.a;
        Color color = image.color;
        // Keep invisible hit areas invisible; make faint +/- backgrounds readable.
        color.a = defaultAlpha > 0f && defaultAlpha < 0.35f
            ? 0.35f : defaultAlpha * 0.8f;
        image.color = color;
    }

    public void OnPointerExit(PointerEventData eventData) => CancelHover();

    private void OnDisable() => CancelHover();

    private void OnCanvasGroupChanged()
    {
        if (button != null && !button.IsInteractable()) CancelHover();
    }

    private void CancelHover()
    {
        if (!isHovered) return;
        isHovered = false;
        if (hoverOutline != null)
        {
            hoverOutline.SetActive(outlineWasActive);
            return;
        }

        if (image == null || (optionSelect != null && optionSelect.isSelected)) return;
        Color color = image.color;
        color.a = defaultAlpha;
        image.color = color;
    }
}
