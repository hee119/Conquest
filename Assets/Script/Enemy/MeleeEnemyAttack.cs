using System.Collections;
using UnityEngine;

public class MeleeEnemyAttack : MonoBehaviour
{
    private Animator anim;
    private PlayerCombat playerCombat;
    private UnitInfo unitInfo;

    public float attackCoolTime = 1f;

    private bool canAttack = true;
    private bool isDead = false;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        playerCombat = GameObject.FindGameObjectWithTag("Player")
            .GetComponent<PlayerCombat>();
        unitInfo = GetComponent<UnitInfo>();
    }

    private void OnEnable()
    {
        canAttack = true;
        isDead = false;
    }

    public void Attack()
    {
        playerCombat.TakeDamage(unitInfo.damage);
    }

    private IEnumerator AttackCoolTimeCor()
    {
        canAttack = false;

        anim.SetTrigger("Attack");

        yield return new WaitForSeconds(attackCoolTime);

        canAttack = true;
    }

    public void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && canAttack && !isDead)
        {
            StartCoroutine(AttackCoolTimeCor());
        }
    }
}