using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Hover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Image image;
    float defaultAlpha = 0f;

    public void Awake()
    {
        image = GetComponent<Image>();
        defaultAlpha = image.color.a;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        float alpha = image.color.a * 0.8f;
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        float alpha = defaultAlpha;
        Color color = image.color;
        color.a = alpha;
        image.color = color;    
    }
}