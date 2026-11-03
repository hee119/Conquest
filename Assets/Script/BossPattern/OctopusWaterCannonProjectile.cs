using UnityEngine;

public sealed class OctopusWaterCannonProjectile : PredictionBulletMove
{
    [SerializeField] private int impactEffectId = 1003;
    [SerializeField] private float maxLifetime = 8f;
    private float elapsed;
    private void OnEnable() => elapsed = 0f;
    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= maxLifetime) pooledObject?.DestroyObj();
    }
    public override void Move()
    {
        base.Move();
        if (rb != null && rb.linearVelocity.sqrMagnitude > .001f)
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg);
    }
    public override void OnTriggerEnter2D(Collider2D other)
    {
        if (target != null && other.CompareTag(target.tag) && PoolManager.Instance != null)
            PoolManager.Instance.Spawn(impactEffectId, transform.position);
        base.OnTriggerEnter2D(other);
    }
}
