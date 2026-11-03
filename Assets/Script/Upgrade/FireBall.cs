using System;
using UnityEngine;

/// <summary>FireBall 증강 관련 동작과 상태를 관리한다.</summary>
public class FireBall : HomingProjectile
{
    /// <summary>활성화될 때 필요한 이벤트와 동작을 시작한다.</summary>
    protected override void OnEnable()
    {
        base.OnEnable();

        SoundManager.Instance?.Play(SoundIds.FireballLaunch);
    }
    
}
