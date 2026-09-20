using System;
using System.Collections;
using UnityEngine;

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
    
    public Animator animator;
    
    private GameObject target;

    public void Awake()
    {
        animator = GetComponent<Animator>();
        target = GameObject.FindWithTag("Player");
    }

    public IEnumerator Start()
    {
        PredictiveWaterCannonAttack();
        yield return new WaitForSeconds(6f);
    }

    public void SpinWaterStreamAttack()
    {
        StartCoroutine(SpinWaterStreamAttackCor());
    }

    public void PredictiveWaterCannonAttack()
    {
        StartCoroutine(PredictiveWaterCannonAttackCor());
    }

    public void GroundCrackAttack()
    {
        
    }

    public void WaterBombRainAttack()
    {
        
    }

    IEnumerator SpinWaterStreamAttackCor()
    {
        GameObject waterStream = PoolManager.Instance.Spawn(
            waterStreamObjectId,
            waterSpawnPoint.transform.position
        );

        waterStream.transform.SetParent(transform);
        HydroPump hydroPump = waterStream.GetComponent<HydroPump>();

        waterStream.transform.position = waterSpawnPoint.transform.position;
        waterStream.transform.rotation = waterSpawnPoint.transform.rotation;

        hydroPump.StartCoroutine(hydroPump.BeamBodyScale(1));
        
        float totalAngle = 360f * rotationCount;
        float currentAngle = 0f;
        bool isHydroPumpEnd = false;

        while (currentAngle < totalAngle)
        {
            float rotateAngle = rotationSpeed * Time.deltaTime;

            // 물줄기 자체를 회전
            waterStream.transform.Rotate(
                Vector3.forward,
                rotateAngle
            );

            currentAngle += rotateAngle;

            if (!isHydroPumpEnd && totalAngle - currentAngle < 90f)
            {
                hydroPump.StartCoroutine(hydroPump.BeamBodyScale(-1));
                isHydroPumpEnd = true;
            }
            yield return null;
        }
        
        yield return new WaitUntil(() => hydroPump.transform.localScale.y < 0.01);
        waterStream.GetComponent<PooledObject>().DestroyObj();
    }

    IEnumerator PredictiveWaterCannonAttackCor()
    {
        int bulletCount = 0;
        
        while (bulletCount < bulletSpawnCount)
        {
            GameObject bulletObj = PoolManager.Instance.Spawn(bulletId, waterSpawnPoint.transform.position);
            PredictionBulletMove bulletMove = bulletObj.GetComponent<PredictionBulletMove>();
            bulletMove.Target = target;
            bulletCount++;
            yield return new WaitForSeconds(bulletSpawnInterval);
        }
    }
}   