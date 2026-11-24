using System.Collections.Generic;
using UnityEngine;

public class OptionEvent : MonoBehaviour
{
    [SerializeField]
    private List<OptionSelect> options = new List<OptionSelect>();

    public void OptionSelect(OptionSelect optionSelect)
    {
        foreach (OptionSelect option in options)
        {
            if (option != optionSelect)
            {
                option.SelectCancel();
            }
        }

        optionSelect.SelectConfirmed();
    }
}