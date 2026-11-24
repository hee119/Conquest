using System;
using UnityEngine;
using UnityEngine.UI;

public class OptionSelect : MonoBehaviour
{
    [SerializeField]
    private OptionEvent optionEvent;
    
    [SerializeField]
    private Material outline;
    
    [SerializeField] 
    private GameObject targetOption;
    
    private Image optionImage;
    
    public bool isSelected = false;
    
    public bool isInit = true;

    public void Awake()
    {
        optionImage = GetComponent<Image>();
    }

    public void Start()
    {
        if (isInit)
            SelectCancel();
        else
            OnClick();
    }
    
    public void SelectCancel()
    {
        isSelected = false;
        optionImage.material = null;
        targetOption.SetActive(false);
    }

    public void SelectConfirmed()
    {
        isSelected = true;
        optionImage.material = outline;
        Color color = optionImage.color;
        float alpha = 1f;
        color.a = alpha;
        optionImage.color = color;
        targetOption.SetActive(true);
    }

    public void OnClick()
    {
        optionEvent.OptionSelect(this);
    }
}
