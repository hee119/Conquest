using UnityEngine;

public class BoomSlime : SplitEnemy
{
   public override void Die()
   {
      Boom();
      EnemySplit();
   }

   public void Boom()
   {
      anim.SetTrigger("Boom");
   }
}
