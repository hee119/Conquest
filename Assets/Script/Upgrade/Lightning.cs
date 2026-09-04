using System.Collections;
using UnityEngine;

public class Lightning : DetectUpgrade
{
    [SerializeField]
    private LineRenderer lineRenderer;

    [SerializeField]
    private float height = 4f;

    [SerializeField]
    private float strikeDuration = 0.2f;

    [SerializeField]
    private float damageDistance = 0.1f;

    private PooledObject pooledObject;

    protected override void Awake()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        id = GetComponent<UpgradeInfo>().id;
        lineRenderer = GetComponent<LineRenderer>();
        pooledObject = GetComponent<PooledObject>();
    }

    protected void OnEnable()
    {
        StartCoroutine(DetectUpgradeCor(id));
    }

    public override void Attack(Vector3 targetPos, GameObject target)
    {
        StartCoroutine(Strike(targetPos, target));
    }

    private IEnumerator Strike(Vector3 targetPos, GameObject target)
    {
        // 타겟 위치
        Vector3 endPos = targetPos;

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

                if (target == null)
                {
                    lineRenderer.enabled = false;
                    pooledObject.DestroyObj();
                    yield break;
                }

                EnemyCombat enemy = target.GetComponent<EnemyCombat>();

                if (enemy == null ||
                    enemy.isDead ||
                    !enemy.gameObject.activeInHierarchy)
                {
                    lineRenderer.enabled = false;
                    pooledObject.DestroyObj();
                    yield break;
                }

                Collider2D targetCollider =
                    enemy.GetComponent<Collider2D>();

                Vector2 hitPoint =
                    targetCollider.ClosestPoint(transform.position);

                int damage =
                    DataManager.Instance
                    .UpgradeDatasDic[id]
                    .damage[
                        DataManager.Instance
                        .UpgradeDatasDic[id]
                        .upgradeLevel
                    ];

                enemy.TakeDamage(damage, hitPoint);
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

            // 타겟이 죽었거나 비활성화됐으면 종료
            if (enemy == null ||
                enemy.isDead ||
                !enemy.gameObject.activeInHierarchy)
            {
                lineRenderer.enabled = false;
                pooledObject.DestroyObj();
                yield break;
            }

            Collider2D targetCollider =
                enemy.GetComponent<Collider2D>();

            Vector2 hitPoint =
                targetCollider.ClosestPoint(transform.position);

            int damage =
                DataManager.Instance
                .UpgradeDatasDic[id]
                .damage[
                    DataManager.Instance
                    .UpgradeDatasDic[id]
                    .upgradeLevel
                ];

            enemy.TakeDamage(damage, hitPoint);
        }

        // 번개 끄기
        lineRenderer.enabled = false;

        // 풀 반환
        pooledObject.DestroyObj();
    }
}