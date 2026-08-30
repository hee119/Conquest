using System.Collections;
using UnityEngine;

public class Lightning : DetectUpgrade
{
    [SerializeField]
    private LineRenderer lineRenderer;

    [SerializeField]
    private float height = 1f;

    [SerializeField]
    private float strikeDuration = 0.07f;

    [SerializeField]
    private float damageDistance = 0.1f;

    protected override void Awake()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        id = GetComponent<UpgradeInfo>().id;
        lineRenderer = GetComponent<LineRenderer>();
    }

    protected void OnEnable()
    {
        StartCoroutine(DetectUpgradeCor(id));
    }
    
    protected override void Attack(Transform target)
    {
        StartCoroutine(Strike(target));
    }

    private IEnumerator Strike(Transform target)
    {
        if (target == null)
            yield break;

        // 타겟 위치
        Vector3 endPos = target.position;

        // 타겟 기준 위쪽
        Vector3 startPos = endPos + Vector3.up * height;

        lineRenderer.positionCount = 2;
        lineRenderer.enabled = true;

        float time = 0f;
        bool damaged = false;

        while (time < strikeDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / strikeDuration);

            // 위에서 타겟까지 일정한 속도로 내려찍기
            Vector3 currentPos =
                Vector3.Lerp(startPos, endPos, t);

            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, currentPos);

            // 타겟에 도달하면 데미지
            if (!damaged &&
                Vector3.SqrMagnitude(currentPos - endPos)
                <= damageDistance * damageDistance)
            {
                damaged = true;

                EnemyCombat enemy =
                    target.GetComponent<EnemyCombat>();

                if (enemy != null)
                {
                    enemy.TakeDamage(10);
                }
            }

            yield return null;
        }

        // 마지막에는 정확히 타겟 위치
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);

        // 혹시 판정이 안 됐으면 마지막에 데미지
        if (!damaged)
        {
            EnemyCombat enemy =
                target.GetComponent<EnemyCombat>();

            if (enemy != null)
            {
                enemy.TakeDamage(10);
            }
        }

        lineRenderer.enabled = false;
    }
}