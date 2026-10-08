using System.Collections;
using UnityEngine;

public class RamgedEnemyAttack : MonoBehaviour
{
    [SerializeField] private int bulletId;
    
    [SerializeField] private GameObject bulleSpawnPos;
    
    private Animator anim;
    private PlayerCombat playerCombat;
    private EnemyInfo _enemyInfo;
    
    public float attackCoolTime = 1f;

    private bool canAttack = true;
    private bool isDead = false;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        _enemyInfo = GetComponent<EnemyInfo>();
        CachePlayerCombat();
    }
    
    private void OnEnable()
    {
        canAttack = true;
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

            if (anim != null && anim.GetComponent<AnimationController>() != null)
                anim.SetTrigger("Attack");
            
            yield return new WaitUntil(() =>
                isDead || playerCombat == null || !playerCombat.gameObject.activeInHierarchy ||
                (DataManager.Instance != null &&
                 DataManager.Instance.bulletDatasDic.TryGetValue(bulletId, out BulletData bulletData) &&
                 (playerCombat.transform.position - transform.position).sqrMagnitude <= bulletData.range * bulletData.range)
            );

            if (isDead || playerCombat == null || !playerCombat.gameObject.activeInHierarchy)
                yield break;
                
            GameObject bullet = PoolManager.Instance.Spawn(bulletId, bulleSpawnPos.transform.position);
            BulletMove bulletMove = bullet != null ? bullet.GetComponent<BulletMove>() : null;
            if (bulletMove != null)
            {
                bulletMove.target = playerCombat.gameObject;
                bulletMove.Move();
            }
            yield return new WaitForSeconds(attackCoolTime);

            canAttack = true;
        }
    }
}
