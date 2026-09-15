using System;
using System.Collections.Generic;
using UnityEngine;

public class RottingSpore : MonoBehaviour
{
    public Collider2D col;
    public int poisonId;
    public Sprite poisonSprite;

    public void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    public void PoisonMoldBoom()
    {
        UnityEngine.Debug.Log($"[RottingSpore] PoisonMoldBoom 호출됨 / poisonId={poisonId}");
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask("Enemy"));
        filter.useTriggers = true;

        Collider2D[] results = new Collider2D[50];

        int count = col.Overlap(filter, results);

        for (int i = 0; i < count; i++)
        {
            if (results[i] == null)
                continue;

            GameObject poisonObj = PoolManager.Instance.Spawn(poisonId, results[i].gameObject.transform.position);
            Poison poison = poisonObj.GetComponent<Poison>();
            DotDamage dotdamage = poisonObj.GetComponent<DotDamage>();
            poisonObj.GetComponent<SpriteRenderer>().sprite = poisonSprite;
            poison.isDamage = false;
            dotdamage.SetTarget(results[i].gameObject);
        }
    }
}
    