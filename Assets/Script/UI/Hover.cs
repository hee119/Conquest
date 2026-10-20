using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Hover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image image;
    private OptionSelect optionSelect;
    
    [SerializeField] private Material outline;

    public void Awake()
    {
        image = GetComponent<Image>();
        optionSelect = GetComponent<OptionSelect>();
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (optionSelect != null && optionSelect.isSelected)
            return;
        
        image.material = outline;
        Color color = image.color;
        float alpha = 1f;
        color.a = alpha;
        image.color = color;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HoverCancel();
    }

    public void HoverCancel()
    {
        if (optionSelect != null && optionSelect.isSelected)
            return;
        
        image.material = null;
        Color color = image.color;
        image.color = color;    
    }
}