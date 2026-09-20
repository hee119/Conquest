using UnityEngine;

public class BoomSlime : SplitEnemy
{
   public override void Die()
   {
      BoomAnim();
      EnemySplit();
   }

   public void BoomAnim()
   {
      anim.SetTrigger("Boom");
   }

   public void Boom()
   {
      var _enemyInfo =
         DataManager.Instance.enemyDatasDic[enemyInfo.id];

      int explosionRange = _enemyInfo.explosionRange;
      int explosionDamage = _enemyInfo.explosionDamage;

      Collider2D[] enemies = Physics2D.OverlapCircleAll(
         transform.position,
         explosionRange
      );

      foreach (Collider2D enemy in enemies)
      {
         EnemyCombat enemyCombat = enemy.GetComponent<EnemyCombat>();

         if (enemyCombat != null)
         {
            enemyCombat.TakeDamage(explosionDamage, enemyCombat.transform.position);
         }
      }
   }

}
