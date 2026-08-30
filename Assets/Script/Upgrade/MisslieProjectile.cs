using System.Collections;
using UnityEngine;

public class MissileProjectile : HomingProjectile
{
    [SerializeField]
    private float curveHeight = 1.5f;

    private Coroutine moveCoroutine;

    public override void SetTarget(Vector3 target)
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        // 기존 물리 이동 제거
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // 현재 생성된 위치를 시작점으로 확실하게 고정
        Vector2 startPosition = transform.position;
        rb.position = startPosition;

        moveCoroutine = StartCoroutine(
            MoveParabola(startPosition, target)
        );
    }

    private IEnumerator MoveParabola(
        Vector2 startPosition,
        Vector3 target
    )
    {
        float speed =
            DataManager.Instance.UpgradeDatasDic[id].speed;

        speed = Mathf.Max(speed, 0.01f);

        float distance =
            Vector2.Distance(startPosition, target);

        float moveTime =
            distance / speed;

        // 거리가 너무 가까운 경우
        if (moveTime <= 0.01f)
        {
            rb.position = target;
            rb.linearVelocity = Vector2.zero;

            pooledObject.DestroyObj();
            moveCoroutine = null;

            yield break;
        }

        // -1 = 왼쪽
        //  0 = 직선
        //  1 = 오른쪽
        float curveDirection =
            Random.Range(-1f, 1f);

        Vector2 targetDirection =
            ((Vector2)target - startPosition).normalized;

        // 진행 방향에 수직
        Vector2 perpendicular =
            new Vector2(
                -targetDirection.y,
                targetDirection.x
            );

        float elapsed = 0f;

        while (elapsed < moveTime)
        {
            yield return new WaitForFixedUpdate();

            elapsed += Time.fixedDeltaTime;

            float t =
                Mathf.Clamp01(elapsed / moveTime);

            // 시작점 → 목표점
            Vector2 position =
                Vector2.Lerp(
                    startPosition,
                    target,
                    t
                );

            // 포물선
            float parabola =
                4f *
                curveHeight *
                t *
                (1f - t);

            position +=
                perpendicular *
                parabola *
                curveDirection;

            // 현재 위치에서 다음 위치로 이동
            Vector2 currentPosition =
                rb.position;

            Vector2 direction =
                position - currentPosition;

            // 회전
            if (direction.sqrMagnitude > 0.0001f)
            {
                float angle =
                    Mathf.Atan2(
                        direction.y,
                        direction.x
                    ) * Mathf.Rad2Deg;

                rb.MoveRotation(angle);
            }

            // Rigidbody 이동
            rb.MovePosition(position);
        }

        // 마지막 위치 정확하게 목표에 맞춤
        rb.position = target;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // 도착하면 풀 반환
        pooledObject.DestroyObj();

        moveCoroutine = null;
    }

    protected override void OnDisable()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        base.OnDisable();
    }
}