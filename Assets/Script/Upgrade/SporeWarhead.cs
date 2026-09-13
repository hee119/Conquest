using UnityEngine;
using System;

public class SporeWarhead : Mold
{
    public override void Boom()
    {
        animator.SetTrigger("Boom");

        Collider2D col = GetComponent<Collider2D>();

        if (col == null)
            return;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask("Enemy"));
        filter.useTriggers = true;

        Collider2D[] enemies = new Collider2D[50];
        int count = col.Overlap(filter, enemies);

        for (int i = 0; i < count; i++)
        {
            EnemyCombat enemy = enemies[i].GetComponent<EnemyCombat>();
            if (enemy == null) continue;

            enemy.TakeDamage(
                DataManager.Instance.UpgradeDatasDic[id].damage[DataManager.Instance.UpgradeDatasDic[id].upgradeLevel],
                enemies[i].ClosestPoint(transform.position)
            );
        }
    }
}