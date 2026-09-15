using UnityEngine;

public class HeatWave : HomingProjectile
{
    private Boom boom;
    private bool isBoom;

    protected override void Awake()
    {
        base.Awake();

        boom = GetComponent<Boom>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        isBoom = false;
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (isBoom)
            return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            EnemyCombat enemyCombat =
                other.GetComponent<EnemyCombat>();

            Collider2D targetCollider =
                other.GetComponent<Collider2D>();

            if (enemyCombat != null && targetCollider != null)
            {
                Vector2 hitPoint =
                    targetCollider.ClosestPoint(transform.position);

                enemyCombat.TakeDamage(
                    GetDamage(),
                    hitPoint
                );
            }

            isBoom = true;

            // 이동 정지
            rb.linearVelocity = Vector2.zero;

            // 폭발 시작
            if (boom != null)
            {
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