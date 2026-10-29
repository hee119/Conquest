using System;
using UnityEngine;

public class FireBall : HomingProjectile
{
    protected override void OnEnable()
    {
        base.OnEnable();

        SoundManager.Instance?.Play(SoundIds.FireballLaunch);
    }
    
}
