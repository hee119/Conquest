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
      var enemyInfo =
         DataManager.Instance.enemyDatasDic[unitInfo.id];

      int explosionRange = enemyInfo.explosionRange;
      int explosionDamage = enemyInfo.explosionDamage;

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
