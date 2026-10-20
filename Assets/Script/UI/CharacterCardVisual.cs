using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image), typeof(Button))]
public sealed class CharacterCardVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Material hoverOutlineMaterial;
    [SerializeField] private Material selectionGlowMaterial;
    [SerializeField, Range(0.5f, 1f)] private float hoverOpacity = 0.88f;

    private Image background;
    private bool selected;
    private bool hovered;

    private void Awake()
    {
        background = GetComponent<Image>();
        Refresh();
    }

    public void SetSelected(bool value)
    {
        selected = value;
        Refresh();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        Refresh();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        Refresh();
    }

    private void Refresh()
    {
        if (background == null)
            background = GetComponent<Image>();

        if (background != null)
        {
            Color color = Color.white;
            color.a = selected ? 1f : hovered ? hoverOpacity : 0.94f;
            background.color = color;
            background.material = selected ? selectionGlowMaterial : hoverOutlineMaterial;
        }
    }
}
