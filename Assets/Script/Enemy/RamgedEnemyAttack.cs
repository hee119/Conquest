using System.Collections;
using UnityEngine;

public class RamgedEnemyAttack : MonoBehaviour
{

    [SerializeField] private int bulletId;
    
    [SerializeField] private GameObject bulleSpawnPos;

    [SerializeField, Min(0f)] private float attackReleaseDelay;
    
    private IEnemySpriteAnimation anim;
    private PlayerCombat playerCombat;
    private EnemyInfo _enemyInfo;
    private EnemyCombat combat;
    
    public float attackCoolTime = 1f;

    private bool shotPending;
    private bool canAttack = true;
    private bool isDead = false;

    private void Awake()
    {
        anim = GetComponent<IEnemySpriteAnimation>();
        _enemyInfo = GetComponent<EnemyInfo>();
        combat = GetComponent<EnemyCombat>();
        CachePlayerCombat();
    }
    
    private void OnEnable()
    {
        canAttack = true;
        shotPending = false;
        isDead = false;

        if (playerCombat == null)
            CachePlayerCombat();

        if (playerCombat == null || !playerCombat.gameObject.activeInHierarchy)
            return;

        StartCoroutine(AttackCoolTimeCor());
    }

    private void CachePlayerCombat()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        playerCombat = player != null ? player.GetComponent<PlayerCombat>() : null;
    }
    
    private IEnumerator AttackCoolTimeCor()
    {
        while (true)
        {
            if (isDead || playerCombat == null || !playerCombat.gameObject.activeInHierarchy)
                yield break;

            canAttack = false;

            yield return new WaitUntil(() =>
                isDead || playerCombat == null || !playerCombat.gameObject.activeInHierarchy ||
                (DataManager.Instance != null &&
                 DataManager.Instance.bulletDatasDic.TryGetValue(bulletId, out BulletData bulletData) &&
                 (playerCombat.transform.position - transform.position).sqrMagnitude <= bulletData.range * bulletData.range)
            );

            if (isDead || playerCombat == null || !playerCombat.gameObject.activeInHierarchy)
                yield break;

            shotPending = true;
            PlayAttackAnimation();

            float windup = Mathf.Max(0f, attackReleaseDelay);
            if (windup > 0f)
                yield return new WaitForSeconds(windup);

            // An animation event normally releases the shot. This fallback keeps
            // attacks working if there is no animation or it was interrupted.
            if (shotPending)
            {
                yield return null;
                if (shotPending) FireProjectile();
            }
            // Preserve the interval between shots when anticipation is enabled.
            yield return new WaitForSeconds(Mathf.Max(0.01f, attackCoolTime - windup));

            canAttack = true;
        }
    }

    public void FireProjectile()
    {
        if (!shotPending) return;
        shotPending = false;
        if (isDead || playerCombat == null || !playerCombat.gameObject.activeInHierarchy ||
            (combat != null && (combat.isDead || combat.hp <= 0f)) ||
            PoolManager.Instance == null || bulleSpawnPos == null) return;

        GameObject bullet = PoolManager.Instance.Spawn(bulletId, bulleSpawnPos.transform.position);
        BulletMove bulletMove = bullet != null ? bullet.GetComponent<BulletMove>() : null;
        if (bulletMove != null)
        {
            bulletMove.target = playerCombat.gameObject;
            bulletMove.Move();
        }
    }

    private void PlayAttackAnimation()
    {
        if (anim != null) anim.Attack();
    }
}
