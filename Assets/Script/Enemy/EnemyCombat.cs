 using System;
 using System.Collections;
 using Unity.VisualScripting;
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    Animator anim;
    public int maxHp = 0;
    public int hp;
    public bool isDead = false;
    private bool isInvincible = false;
    private bool canAttack = true;
    public float attckCoolTime = 1;
    PooledObject pooledObject;
    private SpriteRenderer[] spriteRenderers;
    PlayerCombat playerCombat;
    UnitInfo unitInfo;


    public void Awake()
    {
        anim = GetComponent<Animator>();
        pooledObject = GetComponent<PooledObject>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        playerCombat = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerCombat>();    
        unitInfo = GetComponent<UnitInfo>();
    }

    public void TakeDamage(int damage)
    {
        StartCoroutine(InvincibleCor());
        hp -= damage;
        anim.SetTrigger("Hit");
        HitFlash();
        if (hp <= 0)
        {
            Die();
        }
        Debug.Log(hp);
    }

    public void OnEnable()
    {
        canAttack = true;
        isDead = false;
        hp = maxHp;
        isInvincible = false;
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            spriteRenderer.color = Color.white;
        }
    }

    public void Die()
    {
        isDead = true;
        PoolManager.Instance.Spawn(unitInfo.dropExpOrbId, transform.position);
        pooledObject.DestroyObj();
    }

    public void HitFlash()
    {
        if (!isDead)
        {
            StartCoroutine(HitFlashCor());
        }
    }

    public IEnumerator HitFlashCor()
    {
            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
                spriteRenderer.color = Color.red;
            }
        
            yield return new WaitForSeconds(0.1f);

            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
                spriteRenderer.color = Color.white;
            }
            
    }
    
    private IEnumerator InvincibleCor()
    {
        isInvincible = true;

        yield return new WaitForSeconds(0.1f);

        isInvincible = false;
    }

    public void Attack()
    {
        playerCombat.TakeDamage(unitInfo.damage);
    }

    private IEnumerator AttackCoolTimeCor()
    {
        anim.SetTrigger("Attack");

        yield return new WaitForSeconds(attckCoolTime);

        canAttack = true;
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Skill") && isInvincible == false)
        {
            TakeDamage(other.gameObject.GetComponent<SkillInfo>().skillDamage);
        }
    }

    public void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && canAttack)
        {
            canAttack = false;
            StartCoroutine(AttackCoolTimeCor());
        }
    }
}
