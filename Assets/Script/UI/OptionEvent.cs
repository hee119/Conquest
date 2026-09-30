using System;
using UnityEngine;

public class OptionEvent : MonoBehaviour
{
    public event Action SelectEvent;

    public void ObjectSelect(OptionSelect option)
    {
        SelectEvent?.Invoke();
        option.SelectConfirmed();
    }
}
