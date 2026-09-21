using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OctopusKing : MonoBehaviour
{
    [Header("WaterSpawnPoint")]
    public GameObject waterSpawnPoint;

    [Header("WaterStream")]
    public int rotationSpeed;
    public int rotationCount;
    public int waterStreamObjectId;

    [Header("PredictiveWater")]
    public int bulletSpawnCount;
    public float bulletSpawnInterval;
    public int bulletId;

    [Header("GroundAttack")]
    public int groundAttackSkillId;
    public string groundAttackAnimTrigger;

    [Header("WaterBombRain")]

    // 물폭탄 생성 위치 목록
    public List<Transform> waterBombSpawnPoints = new List<Transform>();

    public int waterBombCount;
    public float waterBombSpawnInterval;

    // 플레이어 주변 착지 위치 범위
    public float waterBombSpawnRadius;

    // 착지 위치 랜덤 오프셋
    public float waterBombTargetOffsetRange;

    public float waterBombArcHeight;
    public float waterBombFlightDuration;

    public int waterBombWarningId;
    public int waterBombId;

    public float waterBombImpactRadius;
    public int waterBombDamage;

    public LayerMask playerLayer;


    public Animator animator;

    private GameObject target;

    public float patternInterval = 6f;


    public void Awake()
    {
        animator = GetComponent<Animator>();
        target = GameObject.FindWithTag("Player");
    }


    public IEnumerator Start()
    {
        while (true)
        {
            int pattern =
                UnityEngine.Random.Range(0, 4);

            switch (pattern)
            {
                case 0:
                    SpinWaterStreamAttack();
                    break;

                case 1:
                    PredictiveWaterCannonAttack();
                    break;

                case 2:
                    GroundCrackAttack();
                    break;

                case 3:
                    WaterBombRainAttack();
                    break;
            }

            yield return new WaitForSeconds(
                patternInterval
            );
        }
    }


    public void SpinWaterStreamAttack()
    {
        StartCoroutine(
            SpinWaterStreamAttackCor()
        );
    }


    public void PredictiveWaterCannonAttack()
    {
        StartCoroutine(
            PredictiveWaterCannonAttackCor()
        );
    }


    public void GroundCrackAttack()
    {
        StartCoroutine(
            GroundCrackAttackCor()
        );
    }


    public void WaterBombRainAttack()
    {
        StartCoroutine(
            WaterBombRainAttackCor()
        );
    }


    IEnumerator SpinWaterStreamAttackCor()
    {
        GameObject waterStream =
            PoolManager.Instance.Spawn(
                waterStreamObjectId,
                waterSpawnPoint.transform.position
            );

        waterStream.transform.SetParent(
            transform
        );

        HydroPump hydroPump =
            waterStream.GetComponent<HydroPump>();

        waterStream.transform.position =
            waterSpawnPoint.transform.position;

        waterStream.transform.rotation =
            waterSpawnPoint.transform.rotation;


        hydroPump.StartCoroutine(
            hydroPump.BeamBodyScale(1)
        );


        float totalAngle =
            360f * rotationCount;

        float currentAngle = 0f;

        bool isHydroPumpEnd = false;


        while (currentAngle < totalAngle)
        {
            float rotateAngle =
                rotationSpeed *
                Time.deltaTime;


            waterStream.transform.Rotate(
                Vector3.forward,
                rotateAngle
            );


            currentAngle += rotateAngle;


            if (!isHydroPumpEnd &&
                totalAngle - currentAngle < 90f)
            {
                hydroPump.StartCoroutine(
                    hydroPump.BeamBodyScale(-1)
                );

                isHydroPumpEnd = true;
            }


            yield return null;
        }


        yield return new WaitUntil(
            () =>
                hydroPump.transform.localScale.y
                < 0.01f
        );


        waterStream
            .GetComponent<PooledObject>()
            .DestroyObj();
    }


    IEnumerator PredictiveWaterCannonAttackCor()
    {
        int bulletCount = 0;


        while (bulletCount < bulletSpawnCount)
        {
            GameObject bulletObj =
                PoolManager.Instance.Spawn(
                    bulletId,
                    waterSpawnPoint.transform.position
                );


            PredictionBulletMove bulletMove =
                bulletObj.GetComponent<
                    PredictionBulletMove
                >();


            bulletMove.target = target;

            bulletMove.Move();


            bulletCount++;


            yield return new WaitForSeconds(
                bulletSpawnInterval
            );
        }
    }


    IEnumerator GroundCrackAttackCor()
    {
        if (
            animator.GetComponent<
                AnimationController
            >() != null
        )
        {
            animator.SetTrigger(
                groundAttackAnimTrigger
            );
        }


        PoolManager.Instance.Spawn(
            groundAttackSkillId,
            transform.position
        );


        yield return null;
    }


    IEnumerator WaterBombRainAttackCor()
    {
        // 시작 위치가 하나도 없으면 실행 안 함
        if (waterBombSpawnPoints == null ||
            waterBombSpawnPoints.Count == 0)
        {
            Debug.LogWarning(
                "WaterBomb Spawn Point가 없습니다."
            );

            yield break;
        }


        for (int i = 0;
             i < waterBombCount;
             i++)
        {
            // =============================
            // 생성 위치
            // =============================

            Transform selectedSpawnPoint =
                waterBombSpawnPoints[
                    UnityEngine.Random.Range(
                        0,
                        waterBombSpawnPoints.Count
                    )
                ];


            if (selectedSpawnPoint == null)
            {
                continue;
            }


            Vector2 spawnPos =
                selectedSpawnPoint.position;


            // =============================
            // 도착 위치
            // 기존 방식 유지
            // =============================

            Vector2 baseLandPos =
                (Vector2)target.transform.position
                + UnityEngine.Random.insideUnitCircle
                * waterBombSpawnRadius;


            Vector2 landPos =
                baseLandPos
                + UnityEngine.Random.insideUnitCircle
                * waterBombTargetOffsetRange;


            // 착지 지점 경고
            GameObject warning =
                PoolManager.Instance.Spawn(
                    waterBombWarningId,
                    landPos
                );


            // 선택된 SpawnPoint에서 폭탄 생성
            GameObject bomb =
                PoolManager.Instance.Spawn(
                    waterBombId,
                    spawnPos
                );


            // 생성 위치 → 착지 위치
            StartCoroutine(
                WaterBombParabola(
                    bomb,
                    warning,
                    spawnPos,
                    landPos,
                    waterBombArcHeight,
                    waterBombFlightDuration
                )
            );


            yield return new WaitForSeconds(
                waterBombSpawnInterval
            );
        }
    }


    IEnumerator WaterBombParabola(
        GameObject bomb,
        GameObject warning,
        Vector2 start,
        Vector2 end,
        float height,
        float duration)
    {
        WaringSign warningComp =
            warning.GetComponent<WaringSign>();


        float t = 0f;


        while (t < duration)
        {
            if (bomb == null)
            {
                if (warning != null)
                {
                    warning
                        .GetComponent<PooledObject>()
                        .DestroyObj();
                }

                yield break;
            }


            if (warning == null)
            {
                bomb
                    .GetComponent<PooledObject>()
                    .DestroyObj();

                yield break;
            }


            t += Time.deltaTime;


            float n =
                Mathf.Clamp01(
                    t / duration
                );


            // 시작 위치 → 도착 위치
            Vector2 flatPos =
                Vector2.Lerp(
                    start,
                    end,
                    n
                );


            // 포물선 높이
            float arc =
                height *
                Mathf.Sin(
                    n * Mathf.PI
                );


            bomb.transform.position =
                flatPos +
                Vector2.up * arc;


            warningComp.SetProgress(n);


            yield return null;
        }


        bomb.transform.position = end;


        Impact(end);


        bomb
            .GetComponent<PooledObject>()
            .DestroyObj();


        warning
            .GetComponent<PooledObject>()
            .DestroyObj();
    }


    private void Impact(Vector2 pos)
    {
        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                pos,
                waterBombImpactRadius,
                playerLayer
            );


        foreach (var hit in hits)
        {
            hit.GetComponent<PlayerCombat>()
                ?.TakeDamage(
                    waterBombDamage
                );
        }
    }
}