using System.Collections;
using UnityEngine;

public class Lightning : DetectUpgrade
{
    [SerializeField]
    protected LineRenderer lineRenderer;

    [SerializeField]
    protected float height = 6f;

    [SerializeField]
    protected float strikeDuration = 0.15f;

    [SerializeField]
    protected float damageDistance = 0.1f;

    [SerializeField]
    private AudioSource launchAudio;

    private int id;

    protected override float ChainAttackInterval =>
        strikeDuration;

    protected override void Awake()
    {
        if (player == null)
        {
            player =
                GameObject.FindGameObjectWithTag("Player");
        }

        id =
            GetComponent<UpgradeInfo>().id;

        lineRenderer =
            GetComponent<LineRenderer>();

        pooledObject =
            GetComponent<PooledObject>();
    }

    protected virtual void OnEnable()
    {
        StartCoroutine(
            DetectUpgradeCor(id)
        );
    }

    public override void Attack(
        int id,
        Vector3 targetPos,
        GameObject target = null,
        bool isChain = false,
        Vector3? startPosition = null,
        bool destroyAfter = true)
    {
        StartCoroutine(
            Strike(
                targetPos,
                target,
                isChain ? startPosition : null,
                destroyAfter
            )
        );
    }

    private IEnumerator Strike(
        Vector3 targetPos,
        GameObject target,
        Vector3? startPos = null,
        bool destroyAfter = true)
    {
        if (launchAudio != null)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySfx(launchAudio.clip);
            else
                launchAudio.Play();
        }

        Vector3 endPos =
            targetPos;

        // 시작 위치가 있으면 그 위치,
        // 없으면 타겟 위쪽
        Vector3 strikeStartPos =
            startPos ??
            (endPos + Vector3.up * height);

        // =====================================
        // 데미지 계산
        // 번개 1회당 한 번만 계산
        // =====================================

        float damage =
            DataManager.Instance
                .upgradeDatasDic[id]
                .damage[
                    DataManager.Instance
                        .upgradeDatasDic[id]
                        .UpgradeLevel
                ];

        int characterId =
            CharacterManager.Instance.currentCharacter;

        PlayerData playerData =
            DataManager.Instance
                .playerDatasDic[characterId];

        int playerDamage =
            playerData.damage;

        int critChance =
            playerData.criticalChance;

        float critDamage =
            playerData.criticalDamage;

        // 공격력 적용
        damage =
            (int)(
                damage *
                (1f + playerDamage / 100f)
            );

        // =====================================
        // 크리티컬 판정
        // =====================================

        bool isCritical =
            Random.Range(0, 100) < critChance;

        if (isCritical)
        {
            damage =
                (int)(
                    damage *
                    (2f + critDamage / 100f)
                );
        }

        // =====================================
        // 번개 연출
        // =====================================

        lineRenderer.positionCount = 2;
        lineRenderer.enabled = true;

        float time = 0f;
        bool damaged = false;

        while (time < strikeDuration)
        {
            time += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    time / strikeDuration
                );

            // 위에서 타겟까지 일정한 속도로 내려찍기
            Vector3 currentPos =
                Vector3.Lerp(
                    strikeStartPos,
                    endPos,
                    t
                );

            lineRenderer.SetPosition(
                0,
                strikeStartPos
            );

            lineRenderer.SetPosition(
                1,
                currentPos
            );

            // =====================================
            // 타겟 도착
            // =====================================

            if (!damaged &&
                Vector3.SqrMagnitude(
                    currentPos - endPos
                ) <=
                damageDistance * damageDistance)
            {
                damaged = true;

                if (target == null)
                {
                    lineRenderer.enabled = false;

                    if (destroyAfter)
                    {
                        pooledObject.DestroyObj();
                    }

                    yield break;
                }

                EnemyCombat enemy =
                    target.GetComponent<EnemyCombat>();

                if (enemy == null ||
                    enemy.isDead ||
                    !enemy.gameObject.activeInHierarchy)
                {
                    lineRenderer.enabled = false;

                    if (destroyAfter)
                    {
                        pooledObject.DestroyObj();
                    }

                    yield break;
                }

                Collider2D targetCollider =
                    enemy.GetComponent<Collider2D>();

                if (targetCollider == null)
                {
                    lineRenderer.enabled = false;

                    if (destroyAfter)
                    {
                        pooledObject.DestroyObj();
                    }

                    yield break;
                }

                Vector2 hitPoint =
                    targetCollider.ClosestPoint(
                        transform.position
                    );

                enemy.TakeDamage(
                    damage,
                    hitPoint
                );
            }

            yield return null;
        }

        // =====================================
        // 마지막에는 정확히 타겟 위치
        // =====================================

        lineRenderer.SetPosition(
            0,
            strikeStartPos
        );

        lineRenderer.SetPosition(
            1,
            endPos
        );

        // =====================================
        // 혹시 판정이 안 됐으면 마지막에 데미지
        // =====================================

        if (!damaged)
        {
            if (target == null)
            {
                lineRenderer.enabled = false;

                if (destroyAfter)
                {
                    pooledObject.DestroyObj();
                }

                yield break;
            }

            EnemyCombat enemy =
                target.GetComponent<EnemyCombat>();

            if (enemy == null ||
                enemy.isDead ||
                !enemy.gameObject.activeInHierarchy)
            {
                lineRenderer.enabled = false;

                if (destroyAfter)
                {
                    pooledObject.DestroyObj();
                }

                yield break;
            }

            Collider2D targetCollider =
                enemy.GetComponent<Collider2D>();

            if (targetCollider == null)
            {
                lineRenderer.enabled = false;

                if (destroyAfter)
                {
                    pooledObject.DestroyObj();
                }

                yield break;
            }

            Vector2 hitPoint =
                targetCollider.ClosestPoint(
                    transform.position
                );

            enemy.TakeDamage(
                damage,
                hitPoint
            );
        }

        // =====================================
        // 번개 끄기
        // =====================================

        lineRenderer.enabled = false;

        // =====================================
        // 풀 반환
        // =====================================

        if (destroyAfter)
        {
            pooledObject.DestroyObj();
        }
    }
}