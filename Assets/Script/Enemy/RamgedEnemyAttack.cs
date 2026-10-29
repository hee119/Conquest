using System.Collections;
using UnityEngine;

public class RamgedEnemyAttack : MonoBehaviour
{
    private static readonly int AttackTrigger = Animator.StringToHash("Attack");

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

            yield return new WaitUntil(() =>
                isDead || playerCombat == null || !playerCombat.gameObject.activeInHierarchy ||
                (DataManager.Instance != null &&
                 DataManager.Instance.bulletDatasDic.TryGetValue(bulletId, out BulletData bulletData) &&
                 (playerCombat.transform.position - transform.position).sqrMagnitude <= bulletData.range * bulletData.range)
            );

            if (isDead || playerCombat == null || !playerCombat.gameObject.activeInHierarchy)
                yield break;

            PlayAttackAnimation();

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

    private void PlayAttackAnimation()
    {
        if (anim == null || anim.runtimeAnimatorController == null)
            return;

        foreach (AnimatorControllerParameter parameter in anim.parameters)
        {
            if (parameter.nameHash != AttackTrigger ||
                parameter.type != AnimatorControllerParameterType.Trigger)
                continue;

            anim.ResetTrigger(AttackTrigger);
            anim.SetTrigger(AttackTrigger);
            return;
        }
    }
}
