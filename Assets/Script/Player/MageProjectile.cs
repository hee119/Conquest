using System.Collections.Generic;
using UnityEngine;

/// <summary>A pooled pixel bolt that hits the first enemy along its travel path.</summary>
public sealed class MageProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float remainingLifetime;
    private int damage;
    private int remainingPierces;
    private int chainCount;
    private float chainRange;
    private bool chainTriggered;
    private readonly HashSet<int> hitEnemies = new HashSet<int>();
    private TrailRenderer trail;
    private SkillInfo skillInfo;
    private PooledObject pooledObject;

    private void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        skillInfo = GetComponent<SkillInfo>();
        pooledObject = GetComponent<PooledObject>();
    }

    public void Launch(Vector2 position, Vector2 travelDirection,
        float travelSpeed, float lifetime, int hitDamage,
        int pierceCount = 0, int chainedTargets = 0, float chainedRange = 0f)
    {
        transform.position = position;
        direction = travelDirection;
        transform.rotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        speed = travelSpeed;
        remainingLifetime = lifetime;
        damage = hitDamage;
        remainingPierces = Mathf.Max(0, pierceCount);
        chainCount = Mathf.Max(0, chainedTargets);
        chainRange = Mathf.Max(0f, chainedRange);
        chainTriggered = false;
        hitEnemies.Clear();
        if (skillInfo != null)
            skillInfo.skillDamage = damage;
        if (trail != null)
        {
            trail.emitting = false;
            trail.Clear();
            trail.emitting = true;
        }
    }

    public void ClearTrail()
    {
        if (trail == null)
            trail = GetComponent<TrailRenderer>();
        if (trail == null)
            return;

        trail.emitting = false;
        trail.Clear();
    }

    private void FixedUpdate()
    {
        float distance = speed * Time.fixedDeltaTime;
        transform.position += (Vector3)(direction * distance);
        remainingLifetime -= Time.fixedDeltaTime;
        if (remainingLifetime <= 0f)
            ReturnToPool();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyCombat enemy = other.GetComponentInParent<EnemyCombat>();
        if (enemy == null || !hitEnemies.Add(enemy.GetInstanceID()))
            return;

        if (!chainTriggered && chainCount > 0 && chainRange > 0f)
        {
            chainTriggered = true;
            BasicAttackChainUtility.Apply(
                enemy.transform.position,
                enemy,
                damage,
                chainCount,
                chainRange);
        }

        if (remainingPierces <= 0)
        {
            ReturnToPool();
            return;
        }

        remainingPierces--;
    }

    private void ReturnToPool()
    {
        hitEnemies.Clear();
        ClearTrail();
        if (pooledObject != null)
            pooledObject.DestroyObj();
        else
            gameObject.SetActive(false);
    }
}
