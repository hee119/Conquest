using UnityEngine;
using UnityEngine.Rendering;

/// <summary>HomingProjectile 증강 관련 동작과 상태를 관리한다.</summary>
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

    /// <summary>필요한 컴포넌트와 초기 상태를 준비한다.</summary>
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
        /// <summary>if 동작을 처리한다.</summary>
        else if (renderers.Length > 1)
        {
            SortingGroup sortingGroup = GetComponent<SortingGroup>();
            if (sortingGroup == null)
                sortingGroup = gameObject.AddComponent<SortingGroup>();

            sortingGroup.sortingLayerName = "Upgrade";
        }

        collider.isTrigger = true;
    }

    /// <summary>활성화될 때 필요한 이벤트와 동작을 시작한다.</summary>
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

    /// <summary>Target 관련 상태를 설정한다.</summary>
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
    /// <summary>Target 관련 상태를 설정한다.</summary>
    public virtual void SetTarget(Vector3 targetPosition)
    {
        Vector2 direction =
            (targetPosition -
             (Vector3)transform.position).normalized;

        MoveToDirection(direction);
    }

    /// <summary>MoveToDirection 동작을 처리한다.</summary>
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

    /// <summary>RotateToDirection 동작을 처리한다.</summary>
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

    /// <summary>Damage 관련 값을 조회한다.</summary>
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

    /// <summary>다른 콜라이더가 진입했을 때 적중 효과를 처리한다.</summary>
    public virtual void HandleEnemyContact(
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

            float damage = GetDamage();

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

    /// <summary>벽에 닿은 투사체만 반환하며 적중 판정은 몬스터가 처리한다.</summary>
    protected void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wall")) pooledObject.DestroyObj();
    }

    /// <summary>비활성화될 때 이벤트와 진행 중인 동작을 정리한다.</summary>
    protected virtual void OnDisable()
    {
        rb.linearVelocity =
            Vector2.zero;

        // 타겟 초기화
        target = null;
    }
}
