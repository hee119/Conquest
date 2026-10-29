using System;
using UnityEngine;

public interface ICombat
{
    float hp { get; set; }
    float maxHp { get; set; }
    
    public event Action hpUpdate;
}
