using System;
using UnityEngine;

public class FireBall : HomingProjectile
{
    protected override void OnEnable()
    {
        base.OnEnable();

        AudioSource launchAudio = GetComponent<AudioSource>();
        if (launchAudio != null)
            launchAudio.Play();
    }
    
}
