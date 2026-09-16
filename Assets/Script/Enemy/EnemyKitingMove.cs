using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyKitingMove : EnemyMove
{
    [SerializeField] private float kiteDistance = 3f;
    [SerializeField] private float directionCheckDistance = 2f;
    [SerializeField] private int directionCount = 16;

    protected override IEnumerator Move()
    {
        while (true)
        {
            if (target != null && !agent.isStopped)
            {
                Vector2 toPlayer =
                    (Vector2)target.transform.position -
                    (Vector2)transform.position;

                float distance = toPlayer.magnitude;

                // 플레이어에게서 충분히 멀면 접근
                if (distance > stoppingDistance)
                {
                    agent.SetDestination(target.transform.position);
                }
                else
                {
                    // 플레이어와 반대 방향
                    Vector2 awayDirection = -toPlayer.normalized;

                    Vector2 bestDirection = awayDirection;
                    float bestScore = float.MinValue;

                    for (int i = 0; i < directionCount; i++)
                    {
                        float angle = 360f / directionCount * i;

                        Vector2 direction =
                            Quaternion.Euler(0, 0, angle) *
                            awayDirection;

                        Vector3 checkPosition =
                            transform.position +
                            (Vector3)(direction * directionCheckDistance);

                        if (!NavMesh.SamplePosition(
                                checkPosition,
                                out NavMeshHit hit,
                                0.5f,
                                NavMesh.AllAreas))
                        {
                            continue;
                        }

                        // 플레이어에게서 멀어지는 방향일수록 높은 점수
                        float score =
                            Vector2.Dot(
                                direction.normalized,
                                awayDirection
                            );

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestDirection = direction;
                        }
                    }

                    Vector3 kitePosition =
                        transform.position +
                        (Vector3)(bestDirection.normalized * kiteDistance);

                    if (NavMesh.SamplePosition(
                            kitePosition,
                            out NavMeshHit finalHit,
                            kiteDistance,
                            NavMesh.AllAreas))
                    {
                        agent.SetDestination(finalHit.position);
                    }
                    else
                    {
                        agent.SetDestination(
                            transform.position +
                            (Vector3)(bestDirection.normalized * directionCheckDistance)
                        );
                    }
                }
            }

            yield return new WaitForSeconds(pathUpdateInterval);
        }
    }
}