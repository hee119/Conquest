using System.Collections;
using UnityEngine;

public class MissileProjectile : HomingProjectile
{
    [SerializeField]
    private float curveHeight = 1.5f;

    private Coroutine moveCoroutine;

    public override void SetTarget(Vector3 target)
    {
        Debug.Log(
            $"[Missile] SetTarget 호출 | " +
            $"Name={gameObject.name} | " +
            $"ID={GetInstanceID()} | " +
            $"Active={gameObject.activeInHierarchy} | " +
            $"Start={transform.position} | " +
            $"Target={target}"
        );

        if (moveCoroutine != null)
        {
            Debug.Log(
                $"[Missile] 기존 MoveCoroutine 중지 | " +
                $"ID={GetInstanceID()}"
            );

            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        // 기존 물리 이동 제거
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // 현재 생성된 위치를 시작점으로 확실하게 고정
        Vector2 startPosition = transform.position;
        rb.position = startPosition;

        Debug.Log(
            $"[Missile] 이동 시작 | " +
            $"ID={GetInstanceID()} | " +
            $"Start={startPosition} | " +
            $"Target={target}"
        );

        moveCoroutine = StartCoroutine(
            MoveParabola(startPosition, target)
        );
    }

    private IEnumerator MoveParabola(
        Vector2 startPosition,
        Vector3 target
    )
    {
        Debug.Log(
            $"[Missile] MoveParabola 시작 | " +
            $"ID={GetInstanceID()} | " +
            $"Start={startPosition} | " +
            $"Target={target}"
        );

        float speed =
            DataManager.Instance.UpgradeDatasDic[id].speed;

        speed = Mathf.Max(speed, 0.01f);

        float distance =
            Vector2.Distance(startPosition, target);

        float moveTime =
            distance / speed;

        Debug.Log(
            $"[Missile] 이동 정보 | " +
            $"ID={GetInstanceID()} | " +
            $"Speed={speed} | " +
            $"Distance={distance} | " +
            $"MoveTime={moveTime}"
        );

        // 거리가 너무 가까운 경우
        if (moveTime <= 0.01f)
        {
            Debug.Log(
                $"[Missile] 거리가 너무 가까움 → 즉시 제거 | " +
                $"ID={GetInstanceID()}"
            );

            rb.position = target;
            rb.linearVelocity = Vector2.zero;

            Debug.Log(
                $"[Missile] DestroyObj 호출 직전 | " +
                $"ID={GetInstanceID()} | " +
                $"Active={gameObject.activeInHierarchy}"
            );

            pooledObject.DestroyObj();

            Debug.Log(
                $"[Missile] DestroyObj 호출 완료 | " +
                $"ID={GetInstanceID()} | " +
                $"Active={gameObject.activeInHierarchy}"
            );

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

        Debug.Log(
            $"[Missile] 포물선 설정 | " +
            $"ID={GetInstanceID()} | " +
            $"CurveDirection={curveDirection} | " +
            $"CurveHeight={curveHeight}"
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

            // 0.2초마다 진행 상황 출력
            if (Mathf.Abs(
                    (elapsed % 0.2f) -
                    0f
                ) < Time.fixedDeltaTime)
            {
                Debug.Log(
                    $"[Missile] 이동 중 | " +
                    $"ID={GetInstanceID()} | " +
                    $"Elapsed={elapsed:F2}/{moveTime:F2} | " +
                    $"T={t:F2} | " +
                    $"Position={position} | " +
                    $"Active={gameObject.activeInHierarchy}"
                );
            }
        }

        Debug.Log(
            $"[Missile] while 종료 | " +
            $"ID={GetInstanceID()} | " +
            $"Elapsed={elapsed} | " +
            $"MoveTime={moveTime}"
        );

        // 마지막 위치 정확하게 목표에 맞춤
        rb.position = target;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        Debug.Log(
            $"[Missile] 목표 도착 | " +
            $"ID={GetInstanceID()} | " +
            $"Position={rb.position}"
        );

        Debug.Log(
            $"[Missile] DestroyObj 호출 직전 | " +
            $"ID={GetInstanceID()} | " +
            $"Active={gameObject.activeInHierarchy}"
        );

        // 도착하면 풀 반환
        pooledObject.DestroyObj();

        Debug.Log(
            $"[Missile] DestroyObj 호출 완료 | " +
            $"ID={GetInstanceID()} | " +
            $"Active={gameObject.activeInHierarchy}"
        );

        moveCoroutine = null;
    }

    protected override void OnDisable()
    {
        Debug.Log(
            $"[Missile] OnDisable 호출 ★ | " +
            $"ID={GetInstanceID()} | " +
            $"Name={gameObject.name}"
        );

        if (moveCoroutine != null)
        {
            Debug.Log(
                $"[Missile] OnDisable에서 MoveCoroutine 중지 | " +
                $"ID={GetInstanceID()}"
            );

            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        base.OnDisable();

        Debug.Log(
            $"[Missile] OnDisable 종료 | " +
            $"ID={GetInstanceID()} | " +
            $"Active={gameObject.activeInHierarchy}"
        );
    }
}