 using System;
 using System.Collections;
 using Unity.VisualScripting;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public int maxHp = 0;
    public int hp;
    public bool isDead = false;
    PooledObject pooledObject;
    UnitInfo unitInfo;
    private SpriteRenderer[] spriteRenderers;


    public void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        unitInfo = GetComponent<UnitInfo>();
        maxHp = unitInfo.hp;
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;
        HitFlash();
        if (hp <= 0)
        {
            Die();
        }
        Debug.Log(hp);
    }

    public void OnEnable()
    {
        isDead = false;
        hp = maxHp;
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            spriteRenderer.color = Color.white;
        }
    }

    public void Die()
    {
        isDead = true;
        gameObject.SetActive(false);
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
}
