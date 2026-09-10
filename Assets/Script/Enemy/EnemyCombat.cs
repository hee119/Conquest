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
    public bool isBurn = false;
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

    public void TakeDamage(int damage, Vector2 hitPoint)
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
                return;
            }
            if(GameManager.Instance.burnChance > 0 && !isBurn)
            {
                int ran = Random.Range(0, 100);
                if (ran <= GameManager.Instance.burnChance)
                {
                     GameObject obj = PoolManager.Instance.Spawn(GameManager.Instance.burnPrefabId, transform.position);
                     obj.GetComponent<Burn>().TargetBurn(gameObject);
                }
            }

            if (GameManager.Instance.moldChance > 0)
            {
                int ran = Random.Range(0, 100);
                if (ran <= GameManager.Instance.moldChance)
                {
                    GameObject obj = PoolManager.Instance.Spawn(GameManager.Instance.moldPrefabId, hitPoint);
                    obj.GetComponent<Mold>().TargetMushroom(gameObject);
                    obj.transform.SetParent(transform);
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
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);

                if (child.TryGetComponent<DotDamage>(out var dot))
                {
                    child.SetParent(null, true);
                }
                if (child.TryGetComponent<Mold>(out var mus))
                {
                    child.SetParent(null, true);
                    if (mus.gameObject.activeInHierarchy)
                    {
                        mus.animator.enabled = true;
                        mus.animator.SetTrigger("Boom");
                    }
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
        if (other.CompareTag("Skill") && !isInvincible)
        {
            Collider2D myCollider = GetComponent<Collider2D>();

            Vector2 hitPoint = myCollider.ClosestPoint(
                other.transform.position
            );

            TakeDamage(
                other.gameObject.GetComponent<SkillInfo>().skillDamage,
                hitPoint
            );
        }
    }

    public void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && canAttack && isDead == false)
        {
            canAttack = false;
            StartCoroutine(AttackCoolTimeCor());
        }
    }
}
