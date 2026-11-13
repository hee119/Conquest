using UnityEngine;

public class MeleeEnemyAttack : MonoBehaviour
{
    private EnemyInfo _enemyInfo;
    private EnemyCombat combat;

    public float attackCoolTime = 1f;

    private float nextAttackTime;

    private void Awake()
    {
        _enemyInfo = GetComponent<EnemyInfo>();
        combat = GetComponent<EnemyCombat>();
    }

    private void OnEnable()
    {
        nextAttackTime = 0f;
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        TryStartAttack(other);
    }

    public void OnTriggerStay2D(Collider2D other)
    {
        TryStartAttack(other);
    }

    private void TryStartAttack(Collider2D other)
    {
        if (!gameObject.activeInHierarchy || !other.CompareTag("Player") ||
            Time.timeScale <= 0f || Time.time < nextAttackTime || _enemyInfo == null ||
            (combat != null && (combat.isDead || combat.hp <= 0f)))
            return;

        PlayerCombat playerCombat = other.GetComponentInParent<PlayerCombat>();
        if (playerCombat == null || playerCombat.isDead)
            return;

        // Reserve the cooldown before damage callbacks so multiple colliders
        // cannot deal damage twice in the same physics step.
        nextAttackTime = Time.time + Mathf.Max(0.01f, attackCoolTime);
        playerCombat.TakeDamage(_enemyInfo.damage);
    }
}
