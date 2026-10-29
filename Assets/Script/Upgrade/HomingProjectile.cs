using UnityEngine;
using UnityEngine.Rendering;

public abstract class HomingProjectile : MonoBehaviour
{
    protected int id;

    protected UpgradeInfo upgradeInfo;

    protected Rigidbody2D rb;
    protected PooledObject pooledObject;

    protected Vector2 moveDirection;
    protected int hp;

    // 현재 타겟
    protected GameObject target;

    protected virtual void Awake()
    {
        upgradeInfo = GetComponent<UpgradeInfo>();
        id = upgradeInfo.id;

        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0f;

        pooledObject = GetComponent<PooledObject>();

        if (pooledObject == null)
            pooledObject = gameObject.AddComponent<PooledObject>();

        Collider2D collider = GetComponent<Collider2D>();

        if (collider == null)
            collider = gameObject.AddComponent<BoxCollider2D>();

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 1)
        {
            renderers[0].sortingLayerName = "Upgrade";
        }
        else if (renderers.Length > 1)
        {
            SortingGroup sortingGroup = GetComponent<SortingGroup>();
            if (sortingGroup == null)
                sortingGroup = gameObject.AddComponent<SortingGroup>();

            sortingGroup.sortingLayerName = "Upgrade";
        }

        collider.isTrigger = true;
    }

    protected virtual void OnEnable()
    {
        if (DataManager.Instance != null &&
            DataManager.Instance.upgradeDatasDic.TryGetValue(
                id,
                out var data))
        {
            hp = data.hp;
        }
        else
        {
            UnityEngine.Debug.LogWarning(
                $"[HomingProjectile] id={id} 업그레이드 데이터가 아직 준비되지 않음"
            );

            hp = 1;
        }

        target = null;
    }

    public virtual void SetTarget(GameObject target)
    {
        if (target == null)
            return;

        // 타겟 저장
        this.target = target;

        // 타겟 위치를 기준으로 이동
        Vector2 direction =
            (target.transform.position -
             transform.position).normalized;

        MoveToDirection(direction);
    }

    // 기존에 위치를 직접 받는 것도 필요하면 유지
    public virtual void SetTarget(Vector3 targetPosition)
    {
        Vector2 direction =
            (targetPosition -
             (Vector3)transform.position).normalized;

        MoveToDirection(direction);
    }

    protected virtual void MoveToDirection(
        Vector2 direction
    )
    {
        moveDirection =
            direction.normalized;

        RotateToDirection();

        float speed =
            DataManager.Instance
                .upgradeDatasDic[id]
                .speed;

        rb.linearVelocity =
            moveDirection * speed;
    }

    protected void RotateToDirection()
    {
        float angle =
            Mathf.Atan2(
                moveDirection.y,
                moveDirection.x
            ) * Mathf.Rad2Deg;

        transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );
    }

    protected float GetDamage()
    {
        int level =
            DataManager.Instance
                .upgradeDatasDic[id]
                .UpgradeLevel;

        return DataManager.Instance
            .upgradeDatasDic[id]
            .damage[level];
    }

    protected virtual void OnTriggerEnter2D(
        Collider2D other
    )
    {
        if (other.gameObject.layer ==
            LayerMask.NameToLayer("Enemy"))
        {
            EnemyCombat enemyCombat =
                other.GetComponent<EnemyCombat>();

            Collider2D targetCollider =
                other.GetComponent<Collider2D>();

            if (enemyCombat == null ||
                targetCollider == null)
            {
                return;
            }

            Vector2 hitPoint =
                targetCollider.ClosestPoint(
                    transform.position
                );

            // =====================================
            // 현재 캐릭터 스탯
            // =====================================

            int characterId =
                CharacterManager.Instance.currentCharacter;

            PlayerData playerData =
                DataManager.Instance.playerDatasDic[characterId];

            int playerDamage =
                playerData.damage;

            int critChance =
                playerData.criticalChance;

            float critDamage =
                playerData.criticalDamage;

            // =====================================
            // 데미지 계산
            // =====================================

            float damage =
                GetDamage();

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

            enemyCombat.TakeDamage(
                damage,
                hitPoint
            );

            // =====================================
            // HP 감소
            // =====================================

            hp -= 1;

            if (hp <= 0)
                pooledObject.DestroyObj();
        }

        if (other.CompareTag("Wall"))
        {
            pooledObject.DestroyObj();
        }
    }

    protected virtual void OnDisable()
    {
        rb.linearVelocity =
            Vector2.zero;

        // 타겟 초기화
        target = null;
    }
}
