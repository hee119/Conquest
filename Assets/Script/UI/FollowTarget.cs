using System;
using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    [SerializeField]
    private GameObject target;
    public void Update()
    {
        transform.position = target.transform.position;
    }
}
