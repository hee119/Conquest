using System;
using System.Collections;
using UnityEngine;

public class HydroPump : MonoBehaviour
{
    BossPatternInfo bossPatternInfo;
    [SerializeField]
    int beamBodyTagetScaleY;
    [SerializeField]
    float beamBodyScaleSpeed;


    public void Awake()
    {
        bossPatternInfo = GetComponent<BossPatternInfo>();
    }

    public IEnumerator BeamBodyScale(int scale)
    {
        if (scale >= 1)
        {
            transform.localScale = new Vector3(1, 0, 1);
            while (transform.localScale.y < beamBodyTagetScaleY)
            {
                float beamBodyScaleY = Mathf.MoveTowards(transform.localScale.y, beamBodyTagetScaleY, beamBodyScaleSpeed * Time.deltaTime);
                transform.localScale = new Vector3(transform.localScale.x, beamBodyScaleY, transform.localScale.z);
                yield return null;
            }
        }
        else
        {
            while (transform.localScale.y > 0)
            {
                float beamBodyScaleY = Mathf.MoveTowards(transform.localScale.y, 0, beamBodyScaleSpeed * Time.deltaTime);
                transform.localScale = new Vector3(transform.localScale.x, beamBodyScaleY, transform.localScale.z);
                yield return null;
            }
        }
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerCombat>().TakeDamage(bossPatternInfo.damage);
        }
    }
}
