using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Missle : DetectUpgrade
{
    public UpgradeInfo upgradeInfo;
    private HomingProjectile projectile;
    protected override void Awake()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
        upgradeInfo = GetComponent<UpgradeInfo>();
        projectile = GetComponent<HomingProjectile>();
        id = upgradeInfo.id;
    }
    
    public override IEnumerator DetectUpgradeCor(int id)
    {
        this.id = id;

        SetUpgradeData();

        List<Collider2D> enemies = DetectEnemies(detectionRange);

        if (enemies.Count == 0)
            yield break;

        Transform closestEnemy = FindClosestEnemy(enemies);

        if (closestEnemy != null)
        {
            Attack(closestEnemy);
        }

        yield return null;
    }
    public override void Attack(Transform target)
    {
       

        Vector3 randomTarget = target.position;
        randomTarget.y += Random.Range(-1f, 1f);

        projectile.SetTarget(randomTarget);
    }
    
    
}
