using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public float maxHp = 0;
    public float hp;
    public bool isDead = false;
    PooledObject pooledObject;
    PlayerInfo _playerInfo;
    private SpriteRenderer[] spriteRenderers;
    public event Action hpUpdate;

    [Header("체력 재생")]
    public float regenDelay = 5f;       // 마지막 피격 후 이 시간 동안 안 맞으면 회복 시작
    public float regenInterval = 1f;    // 회복 주기
    float noDamageTimer;
    float regenTimer;

    public void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        _playerInfo = GetComponent<PlayerInfo>();
        maxHp = _playerInfo.hp;
    }

    void Update()
    {
        if (isDead || hp >= maxHp) return;

        noDamageTimer += Time.deltaTime;
        if (noDamageTimer < regenDelay) return;

        regenTimer += Time.deltaTime;
        if (regenTimer >= regenInterval)
        {
            regenTimer -= regenInterval;
            Regen();
        }
    }

    void Regen()
    {
        float rate =
            DataManager.Instance.playerDatasDic[CharacterManager.Instance.currentCharacter].hpRegenRate;

        if (rate <= 0f) return;

        int amount = Mathf.Max(1, Mathf.RoundToInt(maxHp * rate / 100f));
        hp = Mathf.Min(maxHp, hp + amount);

        hpUpdate?.Invoke();
    }

    public void TakeDamage(int damage)
    {
        float defense =
            DataManager.Instance.playerDatasDic[CharacterManager.Instance.currentCharacter].defense;

        float finalDamage =
            damage * (1f - defense / 100f);
        
        hp -= finalDamage;

        noDamageTimer = 0f;
        regenTimer = 0f;

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
        noDamageTimer = 0f;
        regenTimer = 0f;
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