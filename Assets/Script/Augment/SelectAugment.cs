using System;
using UnityEngine;
using UnityEngine.Serialization;

public class SelectAugment : MonoBehaviour
{
    public GameObject augmentsMomMom;

    public void Awake()
    {
        augmentsMomMom = gameObject.transform.parent.transform.parent.gameObject;
    }

    public void AugmentIsSelected()
    {
        Time.timeScale = 1f;
        augmentsMomMom.SetActive(false);
    }
}
