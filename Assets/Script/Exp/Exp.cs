using System;
using UnityEngine;

public class Exp : MonoBehaviour
{
    ExpInfo expInfo;
    PooledObject pooledObject;

    void Awake()
    {
        expInfo = GetComponent<ExpInfo>();
        pooledObject = GetComponent<PooledObject>();
    }
    
    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            LevelManager.Instance.AddExp(expInfo.exp);
            pooledObject.DestroyObj();
        }
    }
}
