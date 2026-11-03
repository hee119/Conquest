using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>RottingSpore 증강 관련 동작과 상태를 관리한다.</summary>
public class RottingSpore : MonoBehaviour
{
    public Collider2D col;
    public int poisonId;
    public Sprite poisonSprite;

    /// <summary>필요한 컴포넌트와 초기 상태를 준비한다.</summary>
    public void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    /// <summary>PoisonMoldBoom 동작을 처리한다.</summary>
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
    