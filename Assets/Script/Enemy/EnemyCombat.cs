 using System;
 using System.Collections;
 using System.Collections.Generic;
 using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

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
        if (!isDead)
        {
            StartCoroutine(InvincibleCor());
            hp -= damage;
            anim.SetTrigger("Hit");
            HitFlash();
            if (hp <= 0)
            {
                Die();
            }
            else if(GameManager.Instance.burnChance > 0)
            {
                int ran = Random.Range(0, 100);
                if (ran <= GameManager.Instance.burnChance)
                {
                    PoolManager.Instance.Spawn(GameManager.Instance.burnPrefabId, transform.position);
                }
            }
        }
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
        if (!isDead)
        {
            isDead = true;
            PoolManager.Instance.Spawn(unitInfo.dropExpOrbId, transform.position);
            foreach (Transform child in transform)
            {
                if (child.TryGetComponent<DotDamage>(out var dot))
                {
                    child.SetParent(null, true);
                }
            }
            pooledObject.DestroyObj();
        }
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
        if (!isDead)
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
