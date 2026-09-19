using System.Collections;
using UnityEngine;

public class OctopusKing : MonoBehaviour
{
    public int angle;
    public int rotationCount;
    public int waterStreamObjectId;
    public GameObject waterSpawnPoint;
    public Animator animator;

    public void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void SpinWaterStreamAttack()
    {
        StartCoroutine(SpinWaterStreamAttackCor());
    }

    public void PredictiveWaterCannonAttack()
    {
    
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

        waterStream.transform.position = waterSpawnPoint.transform.position;
        waterStream.transform.rotation = waterSpawnPoint.transform.rotation;

        float totalAngle = 360f * rotationCount;
        float currentAngle = 0f;

        while (currentAngle < totalAngle)
        {
            float rotateAngle = angle * Time.deltaTime;

            // 물줄기 자체를 회전
            waterStream.transform.Rotate(
                Vector3.forward,
                rotateAngle
            );

            currentAngle += rotateAngle;

            yield return null;
        }

        waterStream.GetComponent<PooledObject>().DestroyObj();
    }
}