using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OptionHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image image;
    private float defaultAlpha = 0f;
    private OptionSelect optionSelect;

    public void Awake()
    {
        image = GetComponent<Image>();
        defaultAlpha = image.color.a;
        optionSelect = GetComponent<OptionSelect>();
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (optionSelect != null && optionSelect.isSelected)
            return;
        
        float alpha = image.color.a * 0.8f;
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (optionSelect != null && optionSelect.isSelected)
            return;
        
        float alpha = defaultAlpha;
        Color color = image.color;
        color.a = alpha;
        image.color = color;    
    }
}