using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Burn : DotDamage
{
    private List<EnemyCombat> enemyCombatList = new();
    
    public void TargetBurn(GameObject target)
    {
        if (target.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            enemyCombat = target.GetComponent<EnemyCombat>();

            if (enemyCombat == null)
                return;

            if (enemyCombat.isBurn)
                return;

            enemyCombatList.Add(enemyCombat);

            enemyCombat.TakeDamage(GetDamage());
            enemyCombat.isBurn = true;

            if (enemyCombat.isDead)
            {
                pooledObject.DestroyObj();
                return;
            }

            if (gameObject.activeInHierarchy)
                dotDamage = StartCoroutine(PoisonDamage(target.gameObject));
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            pooledObject.DestroyObj();
        }

    }

    protected override void OnDisable()
    {
        foreach (var enemyCombat in enemyCombatList)
        {
            enemyCombat.isBurn = false;
        }
        
        if (stopAfterSeconds != null)
            StopCoroutine(stopAfterSeconds);

        if (dotDamage != null)
            StopCoroutine(dotDamage);

        stopAfterSeconds = null;
        dotDamage = null;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        col.enabled = true;
    }
}
