using System;
using UnityEngine;
using UnityEngine.UI;

public class OptionSelect : MonoBehaviour
{
    [SerializeField]
    private Material outline;
    
    [SerializeField]
    private OptionEvent optionEvent;

    [SerializeField] 
    private GameObject targetOption;
    
    private Image optionImage;
    public void Start()
    {
        optionImage.material = null;
        optionEvent.SelectEvent += SelectCancel;
    }

    public void SelectOption()
    {
        optionEvent.ObjectSelect(this);
    }

    public void SelectCancel()
    {
        optionImage.material = null;
        Color color = optionImage.color;
        float alpha = 0.3f;
        color.a = alpha;
        optionImage.color = color;
        targetOption.SetActive(false);
    }

    public void SelectConfirmed()
    {
        optionImage.material = outline;
        Color color = optionImage.color;
        float alpha = 1f;
        color.a = alpha;
        optionImage.color = color;
        targetOption.SetActive(true);
    }
}
