using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orbit : MonoBehaviour
{
    public List<GameObject> orbitList = new();

    public float radius = 2f;
    public float rotateSpeed = 180f;
    public float spawnTime = 0.5f;
    public float returnTime = 0.5f;

    private float currentAngle = 0f;

    // 표창 추가
    public void AddOrbit(int id)
    {
        GameObject orbit = PoolManager.Instance.Spawn(
            id,
            transform.position
        );

        orbitList.Add(orbit);

        // 처음에는 투명
        SetAlpha(orbit, 0f);
    }

    // 표창 전체 동작
    public IEnumerator RotateOrbits(int id)
    {
        if (orbitList.Count == 0)
            yield break;

        // =====================================
        // 1. 플레이어 위치에서 생성
        // → 바깥으로 이동하면서 서서히 나타남
        // =====================================

        float spawnT = 0f;

        int count = orbitList.Count;
        float angleStep = 360f / count;

        List<float> angles = new();

        for (int i = 0; i < count; i++)
        {
            angles.Add(currentAngle + angleStep * i);
        }

        while (spawnT < spawnTime)
        {
            spawnT += Time.deltaTime;

            float t = Mathf.Clamp01(spawnT / spawnTime);

            for (int i = 0; i < orbitList.Count; i++)
            {
                if (orbitList[i] == null)
                    continue;

                float rad = angles[i] * Mathf.Deg2Rad;

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
                SetAlpha(orbitList[i], t);

                // 표창 자체 회전
                orbitList[i].transform.Rotate(
                    0f,
                    0f,
                    rotateSpeed * Time.deltaTime
                );
            }

            yield return null;
        }

        // 정확하게 반지름 위치
        ArrangeOrbits();

        // 완전히 보이게
        foreach (GameObject orbit in orbitList)
        {
            if (orbit != null)
                SetAlpha(orbit, 1f);
        }


        // =====================================
        // 2. 일정 시간 동안 공전
        // =====================================

        float orbitT = 0f;

        float orbitTime =
            DataManager.Instance.UpgradeDatasDic[id]
            .orbitTime[
                DataManager.Instance.UpgradeDatasDic[id].upgradeLevel
            ];

        while (orbitT < orbitTime)
        {
            orbitT += Time.deltaTime;

            if (orbitList.Count > 0)
            {
                currentAngle += rotateSpeed * Time.deltaTime;

                ArrangeOrbits();
            }

            yield return null;
        }


        // =====================================
        // 3. 플레이어 위치로 돌아오면서
        //    서서히 투명해짐
        // =====================================

        float returnT = 0f;

        List<Vector3> startPositions = new();

        foreach (GameObject orbit in orbitList)
        {
            if (orbit != null)
                startPositions.Add(orbit.transform.position);
            else
                startPositions.Add(transform.position);
        }

        while (returnT < returnTime)
        {
            returnT += Time.deltaTime;

            float t = Mathf.Clamp01(returnT / returnTime);

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

                // 1 → 0 으로 투명해짐
                SetAlpha(
                    orbitList[i],
                    1f - t
                );

                // 이동하면서 자체 회전
                orbitList[i].transform.Rotate(
                    0f,
                    0f,
                    rotateSpeed * Time.deltaTime
                );
            }

            yield return null;
        }


        // =====================================
        // 4. 플레이어 위치에 도착
        // =====================================

        foreach (GameObject orbit in orbitList)
        {
            if (orbit != null)
            {
                orbit.transform.position = transform.position;
                SetAlpha(orbit, 0f);
            }
        }
    }


    // =====================================
    // 표창들을 플레이어 주변에 균등하게 배치
    // =====================================

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

            float angle = currentAngle + angleStep * i;
            float rad = angle * Mathf.Deg2Rad;

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

    private void SetAlpha(GameObject obj, float alpha)
    {
        SpriteRenderer spriteRenderer =
            obj.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            return;

        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
    }
}