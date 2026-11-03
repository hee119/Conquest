using UnityEngine;

/// <summary>HeatWave 증강 관련 동작과 상태를 관리한다.</summary>
public class HeatWave : HomingProjectile
{

    private Boom boom;
    private bool isBoom;

    /// <summary>필요한 컴포넌트와 초기 상태를 준비한다.</summary>
    protected override void Awake()
    {
        base.Awake();

        boom = GetComponent<Boom>();
    }

    /// <summary>활성화될 때 필요한 이벤트와 동작을 시작한다.</summary>
    protected override void OnEnable()
    {
        base.OnEnable();

        isBoom = false;

        SoundManager.Instance?.Play(SoundIds.FireballLaunch);
    }

    /// <summary>다른 콜라이더가 진입했을 때 적중 효과를 처리한다.</summary>
    public override void HandleEnemyContact(Collider2D other)
{
    if (isBoom)
        return;

    if (other.gameObject.layer ==
        LayerMask.NameToLayer("Enemy"))
    {
        EnemyCombat enemyCombat =
            other.GetComponent<EnemyCombat>();

        Collider2D targetCollider =
            other.GetComponent<Collider2D>();

        if (enemyCombat != null &&
            targetCollider != null)
        {
            Vector2 hitPoint =
                targetCollider.ClosestPoint(
                    transform.position
                );
        }

        isBoom = true;

        // 이동 정지
        rb.linearVelocity =
            Vector2.zero;

        // 폭발 시작
        if (boom != null)
        {
            SoundManager.Instance?.Play(SoundIds.HeatwaveExplosion);

            boom.BoomStart();
        }
        else
        {
            UnityEngine.Debug.LogError(
                "[HeatWave] Boom 컴포넌트가 없음"
            );

            pooledObject.DestroyObj();
        }
    }

    if (other.CompareTag("Wall"))
    {
        pooledObject.DestroyObj();
    }
}
}