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
        playerCombat = GameObject.FindGameObjectWithTag("Player")
            .GetComponent<PlayerCombat>();
        _enemyInfo = GetComponent<EnemyInfo>();
    }

    private void OnEnable()
    {
        canAttack = true;
        isDead = false;
    }

    public void Attack()
    {
        playerCombat.TakeDamage(_enemyInfo.damage);
    }

    private IEnumerator AttackCoolTimeCor()
    {
        canAttack = false;

        if (HasAnimatorParameter("Attack"))
        {
            anim.SetTrigger("Attack");
        }
        else if (!HasAnimatorParameter("Attack"))
        {
            Attack();
        }
        

        yield return new WaitForSeconds(attackCoolTime);

        canAttack = true;
    }

    public void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && canAttack && !isDead && gameObject.activeInHierarchy)
        {
            StartCoroutine(AttackCoolTimeCor());
        }
    }
    
    protected bool HasAnimatorParameter(string paramName)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName)
                return true;
        }

        return false;
    }
}