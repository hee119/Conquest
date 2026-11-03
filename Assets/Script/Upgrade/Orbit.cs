using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Orbit 증강 관련 동작과 상태를 관리한다.</summary>
public class Orbit : MonoBehaviour
{
    public List<GameObject> orbitList = new();
    public List<Collider2D> orbitColList = new();

    public float radius = 2f;

    // 공전 속도
    public float orbitSpeed = 180f;

    // 표창 자체 회전 속도
    public float selfRotateSpeed = 1500;

    public float spawnTime = 0.5f;
    public float returnTime = 0.5f;

    private float currentAngle = 0f;

    // =====================================
    // 표창 추가
    // =====================================

    /// <summary>AddOrbit 동작을 처리한다.</summary>
    public void AddOrbit(int id)
    {
        GameObject orbit = PoolManager.Instance.Spawn(
            id,
            transform.position
        );

        Debug.Log(
            $"[Orbit] Spawn : {orbit.name} / Active = {orbit.activeInHierarchy}"
        );

        orbitList.Add(orbit);

        Collider2D col = orbit.GetComponent<Collider2D>();
        orbitColList.Add(col);

        // 새로 생성된 표창 콜라이더 활성화
        if (col != null)
            col.enabled = true;

        SetAlpha(orbit, 0f);
    }

    // =====================================
    // 표창 전체 동작
    // =====================================

    /// <summary>RotateOrbits 동작을 처리한다.</summary>
    public IEnumerator RotateOrbits(int id)
    {
        if (orbitList.Count == 0)
            yield break;

        // =====================================
        // 다시 나가기 시작 → 콜라이더 활성화
        // =====================================

        foreach (Collider2D col in orbitColList)
        {
            if (col != null)
                col.enabled = true;
        }

        Debug.Log("[Orbit] Rotate START");

        // =====================================
        // 1. 플레이어 → 반지름
        // =====================================

        float spawnT = 0f;

        int count = orbitList.Count;
        float angleStep = 360f / count;

        List<float> angles = new();

        for (int i = 0; i < count; i++)
        {
            angles.Add(
                currentAngle + angleStep * i
            );
        }

        while (spawnT < spawnTime)
        {
            spawnT += Time.deltaTime;

            float t = Mathf.Clamp01(
                spawnT / spawnTime
            );

            for (int i = 0; i < orbitList.Count; i++)
            {
                if (orbitList[i] == null)
                    continue;

                float rad =
                    angles[i] * Mathf.Deg2Rad;

                Vector3 direction = new Vector3(
                    Mathf.Cos(rad),
                    Mathf.Sin(rad),
                    0f
                );

                // 플레이어 → 반지름
                orbitList[i].transform.position =
                    transform.position +
                    direction * (radius * t);

                // 서서히 나타남
                SetAlpha(
                    orbitList[i],
                    t
                );

                // 자전
                orbitList[i].transform.Rotate(
                    0f,
                    0f,
                    selfRotateSpeed * Time.deltaTime
                );
            }

            yield return null;
        }

        ArrangeOrbits();

        foreach (GameObject orbit in orbitList)
        {
            if (orbit != null)
                SetAlpha(orbit, 1f);
        }

        // =====================================
        // 2. 공전 + 자전
        // =====================================

        float orbitT = 0f;

        float orbitTime =
            DataManager.Instance.upgradeDatasDic[id]
            .orbitTime[
                DataManager.Instance.upgradeDatasDic[id]
                .UpgradeLevel
            ];

        while (orbitT < orbitTime)
        {
            orbitT += Time.deltaTime;

            if (orbitList.Count > 0)
            {
                // -----------------------------
                // 공전
                // -----------------------------

                currentAngle +=
                    orbitSpeed * Time.deltaTime;

                ArrangeOrbits();

                // -----------------------------
                // 자전
                // -----------------------------

                foreach (GameObject orbit in orbitList)
                {
                    if (orbit == null)
                        continue;

                    orbit.transform.Rotate(
                        0f,
                        0f,
                        selfRotateSpeed * Time.deltaTime
                    );
                }
            }

            yield return null;
        }

        // =====================================
        // 3. 플레이어로 돌아오기
        // =====================================

        // 돌아오기 시작 → 콜라이더 비활성화
        foreach (Collider2D col in orbitColList)
        {
            if (col != null)
                col.enabled = false;
        }

        float returnT = 0f;

        List<Vector3> startPositions = new();

        foreach (GameObject orbit in orbitList)
        {
            if (orbit != null)
                startPositions.Add(
                    orbit.transform.position
                );
            else
                startPositions.Add(
                    transform.position
                );
        }

        while (returnT < returnTime)
        {
            returnT += Time.deltaTime;

            float t = Mathf.Clamp01(
                returnT / returnTime
            );

            for (int i = 0; i < orbitList.Count; i++)
            {
                if (orbitList[i] == null)
                    continue;

                // 반지름 → 플레이어
                orbitList[i].transform.position =
                    Vector3.Lerp(
                        startPositions[i],
                        transform.position,
                        t
                    );

                // 서서히 사라짐
                SetAlpha(
                    orbitList[i],
                    1f - t
                );

                // 이동하면서 자전
                orbitList[i].transform.Rotate(
                    0f,
                    0f,
                    selfRotateSpeed * Time.deltaTime
                );
            }

            yield return null;
        }

        // =====================================
        // 4. 플레이어 위치
        // =====================================

        foreach (GameObject orbit in orbitList)
        {
            if (orbit != null)
            {
                orbit.transform.position =
                    transform.position;

                SetAlpha(
                    orbit,
                    0f
                );
            }
        }
    }

    // =====================================
    // 공전 배치
    // =====================================

    /// <summary>ArrangeOrbits 동작을 처리한다.</summary>
    private void ArrangeOrbits()
    {
        int count = orbitList.Count;

        if (count == 0)
            return;

        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            if (orbitList[i] == null)
                continue;

            float angle =
                currentAngle +
                angleStep * i;

            float rad =
                angle * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Cos(rad),
                Mathf.Sin(rad),
                0f
            ) * radius;

            orbitList[i].transform.position =
                transform.position + offset;
        }
    }

    // =====================================
    // 알파값 변경
    // =====================================

    /// <summary>Alpha 관련 상태를 설정한다.</summary>
    private void SetAlpha(
        GameObject obj,
        float alpha)
    {
        SpriteRenderer spriteRenderer =
            obj.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            return;

        Color color =
            spriteRenderer.color;

        color.a = alpha;

        spriteRenderer.color = color;
    }
}