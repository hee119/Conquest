using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;

public class EnemyKitingMove : EnemyMove
{
    [SerializeField, Min(0.01f)] private float rangeTolerance = 0.15f;

    private const float DestinationChangeThresholdSqr = 0.01f;
    private static readonly ProfilerMarker SamplePositionMarker = new("EnemyKitingMove.SamplePosition");
    private static readonly ProfilerMarker SetDestinationMarker = new("EnemyKitingMove.SetDestination");

    private Vector3 lastDestination;
    private Vector2 lastDesiredPosition;
    private bool hasLastDestination;
    private float nextDestinationUpdateTime;

    protected override void OnEnable()
    {
        hasLastDestination = false;
        nextDestinationUpdateTime = 0f;
        base.OnEnable();
    }

    protected override void Update()
    {
        UpdateKitingDestination(false);
    }

    private void UpdateKitingDestination(bool forceUpdate)
    {
        if (target == null || !target.activeInHierarchy || agent == null ||
            !agent.enabled || !agent.isOnNavMesh)
            return;

        // The base coroutine controls the agent during knockback.
        if (agent.isStopped && knockbackCoroutine != null)
            return;

        Vector2 targetPosition = target.transform.position;
        Vector2 toTarget = targetPosition - (Vector2)transform.position;
        float distanceSqr = toTarget.sqrMagnitude;
        float desiredRange = Mathf.Max(0f, stoppingDistance);
        float tolerance = Mathf.Max(0.01f, rangeTolerance);
        float minRange = Mathf.Max(0f, desiredRange - tolerance);
        float maxRange = desiredRange + tolerance;

        // Check the enemy's distance even when the target is stationary. Squared
        // bounds avoid a square root on the common stop/wait paths.
        if (distanceSqr >= minRange * minRange && distanceSqr <= maxRange * maxRange)
        {
            if (!agent.isStopped)
                agent.isStopped = true;
            hasLastDestination = false;
            nextDestinationUpdateTime = 0f;
            return;
        }

        float now = Time.time;
        if (!forceUpdate && now < nextDestinationUpdateTime)
            return;
        nextDestinationUpdateTime = now + Mathf.Max(0.01f, pathUpdateInterval);

        // Don't keep replacing a request that is still being computed.
        if (agent.pathPending)
            return;

        Vector2 directionToTarget = distanceSqr > 0.000001f
            ? toTarget / Mathf.Sqrt(distanceSqr)
            : Vector2.up;
        Vector2 desiredPosition = targetPosition - directionToTarget * desiredRange;
        bool needsPath = !hasLastDestination || agent.isStopped ||
            !agent.hasPath || agent.isPathStale;
        if (!needsPath &&
            (desiredPosition - lastDesiredPosition).sqrMagnitude <= DestinationChangeThresholdSqr)
            return;

        // Sample only surfaces and areas this particular agent can traverse.
        var filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };
        NavMeshHit destinationHit;
        bool found;
        using (SamplePositionMarker.Auto())
        {
            found = NavMesh.SamplePosition(desiredPosition, out destinationHit,
                Mathf.Max(0.5f, agent.radius), filter);
        }
        if (!found)
        {
            if (!agent.isStopped)
                agent.isStopped = true;
            hasLastDestination = false;
            return;
        }

        if (needsPath ||
            (destinationHit.position - lastDestination).sqrMagnitude > DestinationChangeThresholdSqr)
        {
            bool accepted;
            using (SetDestinationMarker.Auto())
                accepted = agent.SetDestination(destinationHit.position);
            if (!accepted)
            {
                hasLastDestination = false;
                return;
            }
            lastDestination = destinationHit.position;
        }

        agent.isStopped = false;
        lastDesiredPosition = desiredPosition;
        hasLastDestination = true;
    }

    // Knockback completion bypasses the timer and immediately reevaluates range.
    protected override void Move()
    {
        hasLastDestination = false;
        UpdateKitingDestination(true);
    }
}
