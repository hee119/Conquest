using System;
using UnityEngine;

public class GroundCrack : MonoBehaviour
{
   BossPatternInfo bossPatternInfo;
   PooledObject pooledObject;
   Animator animator;

   public void Awake()
   {
      bossPatternInfo = GetComponent<BossPatternInfo>();
      pooledObject = GetComponent<PooledObject>();
      animator = GetComponent<Animator>();
   }

   public void OnEnable()
   {
      animator.SetTrigger("GroundCrack");
   }

   public void OnTriggerEnter2D(Collider2D collision)
   {
      if (collision.CompareTag("Player"))
      {
         collision.GetComponent<PlayerCombat>().TakeDamage(bossPatternInfo.damage);
      }
   }
}
