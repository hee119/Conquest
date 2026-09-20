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
    PlayerInfo _playerInfo;
    private SpriteRenderer[] spriteRenderers;
    public event Action hpUpdate;


    public void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        _playerInfo = GetComponent<PlayerInfo>();
        maxHp = _playerInfo.hp;
    }

    public void TakeDamage(int damage)
    {
        int defense =
            DataManager.Instance.playerDatasDic[CharacterManager.Instance.currentCharacter].defense;

        int finalDamage =
            (int)(damage * (1f - defense / 100f));

        finalDamage = Mathf.Max(1, finalDamage);

        hp -= finalDamage;

        hpUpdate?.Invoke();
        HitFlash();

        if (hp <= 0)
        {
            Die();
        }

        UnityEngine.Debug.Log(hp);
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
    
    public void UpdateHpUI()
    {
        hpUpdate?.Invoke();
    }
}
