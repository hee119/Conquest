using UnityEngine;
using UnityEngine.AI;

/// <summary>Separates ordinary enemies from the boss, including scripted dash movement.</summary>
[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class BossBodyObstacle : MonoBehaviour
{
    [SerializeField] private NavMeshObstacle navigationObstacle;
    private BoxCollider2D body;
    private EnemyCombat combat;
    private readonly Collider2D[] overlaps = new Collider2D[256];
    private ContactFilter2D filter;

    private void Awake()
    {
        body = GetComponent<BoxCollider2D>();
        combat = GetComponent<EnemyCombat>();
        filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(1 << gameObject.layer);
    }

    private void LateUpdate()
    {
        bool solid = body.enabled && (combat == null || !combat.isDead);
        if (navigationObstacle != null) navigationObstacle.enabled = solid;
        if (!solid) return;

        // Scripted boss movement happens outside the physics step.
        Physics2D.SyncTransforms();
        int count = body.Overlap(filter, overlaps);
        for (int i = 0; i < count; i++)
        {
            Collider2D other = overlaps[i];
            EnemyMove enemy = other.GetComponentInParent<EnemyMove>();
            if (enemy == null || enemy.gameObject == gameObject ||
                enemy.GetComponent<BossBodyObstacle>() != null) continue;
            var health = enemy.GetComponent<EnemyCombat>();
            if (health != null && health.isDead) continue;
            ColliderDistance2D distance = body.Distance(other);
            if (!distance.isOverlapped) continue;
            Vector3 position = enemy.transform.position - (Vector3)(distance.normal * (distance.distance - 0.02f));
            // Update the agent too, so its next navigation step keeps the separation.
            NavMeshAgent agent = enemy.agent;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.Warp(position);
            else
                enemy.transform.position = position;
        }
    }
}
