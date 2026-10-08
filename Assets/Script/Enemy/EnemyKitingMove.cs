using UnityEngine;
using UnityEngine.AI;

public class EnemyKitingMove : EnemyMove
{
    [SerializeField] private float rangeTolerance = 0.15f;

    private Vector3 lastDestination;
    private Vector2 lastDesiredPosition;
    private bool hasLastDestination;

    protected override void OnEnable()
    {
        hasLastDestination = false;
        base.OnEnable();
    }

    protected override void Update()
    {
        if (target == null || !target.activeInHierarchy)
            return;

        Vector3 targetPosition = target.transform.position;
        if (!HasTargetMoved(targetPosition))
            return;

        UpdateKitingDestination();
    }

    private void UpdateKitingDestination()
    {
        if (target == null || !target.activeInHierarchy || agent == null ||
            !agent.enabled || !agent.isOnNavMesh)
        {
            return;
        }

        // Don't override the NavMeshAgent while the base knockback routine owns it.
        if (agent.isStopped && knockbackCoroutine != null)
            return;

        Vector2 origin = transform.position;
        Vector2 targetPosition = target.transform.position;
        Vector2 toTarget = targetPosition - origin;
        float centerDistance = toTarget.magnitude;
        Vector2 directionToTarget = centerDistance > 0.001f
            ? toTarget / centerDistance
            : Vector2.up;
        float desiredRange = Mathf.Max(0f, stoppingDistance);
        float tolerance = Mathf.Max(0.01f, rangeTolerance);

        if (Mathf.Abs(centerDistance - desiredRange) <= tolerance)
        {
            // Keep a small dead zone around the desired range to avoid direction
            // changes every frame when the target is right at the boundary.
            agent.isStopped = true;
            hasLastDestination = false;
            RememberTargetPosition(target.transform.position);
            return;
        }

        // This point lies on the desired range circle around the target. From
        // inside the range it is behind the enemy; from outside it is toward it.
        Vector2 desiredPosition = targetPosition - directionToTarget * desiredRange;
        if (hasLastDestination && !agent.isStopped &&
            (desiredPosition - lastDesiredPosition).sqrMagnitude <= 0.01f)
        {
            return;
        }

        float sampleRadius = Mathf.Max(0.5f, agent.radius);
        if (NavMesh.SamplePosition(
                desiredPosition,
                out NavMeshHit destinationHit,
                sampleRadius,
                NavMesh.AllAreas))
        {
            bool wasStopped = agent.isStopped;
            agent.isStopped = false;
            if (!hasLastDestination || wasStopped ||
                (destinationHit.position - lastDestination).sqrMagnitude > 0.01f)
            {
                agent.SetDestination(destinationHit.position);
                lastDestination = destinationHit.position;
            }

            lastDesiredPosition = desiredPosition;
            hasLastDestination = true;
            RememberTargetPosition(target.transform.position);
        }
        else
        {
            agent.isStopped = true;
            hasLastDestination = false;
        }
    }

    // Knockback completion resumes kiting immediately; Update handles it afterward.
    protected override void Move()
    {
        hasLastDestination = false;
        UpdateKitingDestination();
    }
}
