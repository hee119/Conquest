using System.Collections;
using UnityEngine;

public class MeleeEnemyAttack : MonoBehaviour
{
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
    }

    private void OnEnable()
    {
        canAttack = true;
        isDead = false;
    }

    public void Attack()
    {
        if (!TryCachePlayerCombat())
        {
            return;
        }

        if (_enemyInfo == null)
        {
            return;
        }

        playerCombat.TakeDamage(_enemyInfo.damage);
    }

    private IEnumerator AttackCoolTimeCor()
    {
        canAttack = false;

        bool hasAttackAnimation = HasAnimatorParameter("Attack");
        if (hasAttackAnimation)
        {
            anim.SetTrigger("Attack");
        }
        else
        {
            Attack();
        }
        

        yield return new WaitForSeconds(attackCoolTime);

        canAttack = true;
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
        if (other.CompareTag("Player") && canAttack && !isDead && gameObject.activeInHierarchy)
        {
            playerCombat = other.GetComponentInParent<PlayerCombat>();
            if (playerCombat == null)
            {
                return;
            }

            StartCoroutine(AttackCoolTimeCor());
        }
    }
    
    protected bool HasAnimatorParameter(string paramName)
    {
        if (anim == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName)
                return true;
        }

        return false;
    }

    private bool TryCachePlayerCombat()
    {
        if (playerCombat != null)
        {
            return true;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return false;
        }

        playerCombat = player.GetComponent<PlayerCombat>();
        return playerCombat != null;
    }
}
