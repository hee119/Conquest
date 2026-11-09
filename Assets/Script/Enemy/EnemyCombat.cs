using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyCombat : MonoBehaviour, ICombat
{
    protected EnemySpriteAnimation anim;
    protected Animator bossAnimator;

    public float maxHp { get; set; }
    public float hp {get; set;}

    public bool isDead = false;
    public bool isBurn = false;

    protected bool isInvincible = false;
    protected bool isDying = false;

    [SerializeField]
    protected ParticleSystem hitParticle;

    [Header("Hit 후 사망")]
    public string hitStateName = "Hit";
    public float hitMaxWait = 1f;

    protected PooledObject pooledObject;
    protected SpriteRenderer[] spriteRenderers;
    private Collider2D bodyCollider;

    public EnemyInfo enemyInfo;
    public EnemyMove enemyMove;

    public event Action hpUpdate;

    protected virtual bool PlayHitOnNonLethalDamage => true;

    protected virtual void PrepareDeathAnimation() { }

    public void Awake()
    {
        anim = GetComponent<EnemySpriteAnimation>();
        bossAnimator = GetComponent<Animator>();
        pooledObject = GetComponent<PooledObject>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        enemyInfo = GetComponent<EnemyInfo>();
        enemyMove = GetComponent<EnemyMove>();
        bodyCollider = GetComponent<Collider2D>();
    }

    public static int CurrentCharacterId => CharacterManager.Instance != null
        ? CharacterManager.Instance.currentCharacterId
        : GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerInfo>()?.id ?? 1;

    public static float CalculateDamage(float baseDamage, PlayerData stats, out bool critical)
    {
        float damage = Mathf.Max(0f, baseDamage) + Mathf.Max(0f, stats.damage);
        critical = Random.Range(0, 100) < stats.criticalChance;
        return critical ? damage * stats.criticalDamage : damage;
    }

    public int LastDamageSourceId { get; private set; }

    public void TakeDamage(int upgradeId, float damage, Vector2 hitPoint)
    {
        if (isDead || isDying || !gameObject.activeInHierarchy) return;
        int characterId = CurrentCharacterId;
        if (DataManager.Instance == null ||
            !DataManager.Instance.playerDatasDic.TryGetValue(characterId, out var stats)) return;
        LastDamageSourceId = upgradeId;
        float finalDamage = CalculateDamage(damage, stats, out bool critical);
        ApplyDamage(finalDamage, hitPoint, critical);
    }

    // Enemy explosions do not use the player's attack or critical stats.
    public void TakeRawDamage(float damage, Vector2 hitPoint) => ApplyDamage(damage, hitPoint, false);

    private void ApplyDamage(float damage, Vector2 hitPoint, bool critical)
    {
        if (!isDead && !isDying && !isInvincible && gameObject.activeInHierarchy)
        {
            damage *= 1f - Mathf.Clamp(enemyInfo != null ? enemyInfo.defense : 0f, 0f, 100f) / 100f;
            if (damage <= 0f) return;
            StartCoroutine(InvincibleCor());

            hp -= damage;
            DamageNumberDisplay.Show(hitPoint, damage, critical);
            hpUpdate?.Invoke();

            if (SoundManager.Instance != null)
                SoundManager.Instance.Play(SoundIds.EnemyHit, enemy: this);

            if (hitParticle != null)
                hitParticle.Play();

            if (hp <= 0)
                PrepareDeathAnimation();

            bool hasHit = false;
            if (hp <= 0 || PlayHitOnNonLethalDamage)
            {
                if (anim != null)
                    hasHit = (hp > 0 && anim.IsPlaying("Hit")) || anim.Hit();
                else if (HasAnimatorParameter("Hit"))
                {
                    bossAnimator.SetTrigger("Hit");
                    hasHit = true;
                }
            }

            HitFlash();

            if (GameManager.Instance != null && GameManager.Instance.burnChance > 0 && !isBurn)
            {
                int ran = Random.Range(0, 100);

                if (ran <= GameManager.Instance.burnChance)
                {
                    GameObject obj = PoolManager.Instance.Spawn(
                        GameManager.Instance.burnPrefabId,
                        transform.position
                    );

                    obj.GetComponent<Burn>().TargetBurn(gameObject);
                }
            }

            if (GameManager.Instance != null && GameManager.Instance.moldChance > 0)
            {
                int ran = Random.Range(0, 100);

                if (ran <= GameManager.Instance.moldChance)
                {
                    GameObject obj = PoolManager.Instance.Spawn(
                        GameManager.Instance.moldPrefabId,
                        hitPoint
                    );

                    obj.GetComponent<Mold>().TargetMushroom(gameObject);
                    obj.transform.SetParent(transform);
                }
            }

            if (hp <= 0)
            {
                if (hasHit)
                {
                    StartCoroutine(DieAfterHitCor());
                }
                else if (hitParticle != null)
                {
                    // Keep the pooled enemy alive briefly so a lethal hit's
                    // particle can render before the whole object is disabled.
                    StartCoroutine(DieAfterParticleCor());
                }
                else
                {
                    Die();
                }
            }
        }
    }

    protected virtual void OnEnable()
    {
        if (hitParticle != null)
            hitParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        isDead = false;
        isDying = false;
        isInvincible = false;

        ResetHealth(enemyInfo != null ? enemyInfo.hp : maxHp);
        if (bodyCollider != null)
            bodyCollider.enabled = true;

        if (spriteRenderers != null)
        {
            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
                if (spriteRenderer != null)
                    spriteRenderer.color = Color.white;
            }
        }
    }

    public void ResetHealth(float maximum)
    {
        maxHp = Mathf.Max(0f, maximum);
        hp = maxHp;
        hpUpdate?.Invoke();
    }

    private IEnumerator DieAfterHitCor()
    {
        isDying = true;

        float t = 0f;
        if (anim != null)
        {
            while (anim.IsPlaying("Hit") && t < hitMaxWait)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }
        else if (bossAnimator != null)
        {
            while (t < 0.3f && !bossAnimator.GetCurrentAnimatorStateInfo(0).IsName(hitStateName))
            {
                t += Time.deltaTime;
                yield return null;
            }
            t = 0f;
            while (t < hitMaxWait)
            {
                var info = bossAnimator.GetCurrentAnimatorStateInfo(0);
                if (!info.IsName(hitStateName) || info.normalizedTime >= 1f) break;
                t += Time.deltaTime;
                yield return null;
            }
        }

        Die();
    }

    private IEnumerator DieAfterParticleCor()
    {
        isDying = true;
        if (bodyCollider != null)
            bodyCollider.enabled = false;

        yield return new WaitForSeconds(0.25f);
        Die();
    }

    public virtual void Die()
    {
        if (isDead)
            return;

        isDead = true;
        isDying = false;

        PoolManager.Instance.Spawn(
            enemyInfo.dropExpOrbId,
            transform.position
        );

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

    public void HitFlash()
    {
        if (!isDead && gameObject.activeInHierarchy)
        {
            StartCoroutine(HitFlashCor());
        }
    }

    public IEnumerator HitFlashCor()
    {
        if (!isDead && gameObject.activeInHierarchy)
        {
            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
                spriteRenderer.color = Color.red;
            }

            yield return new WaitForSeconds(0.1f);

            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
                if (spriteRenderer != null)
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

    /// <summary>공격 정보를 읽어 몬스터 쪽에서 피해·치명타·관통 소모를 처리한다.</summary>
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || !gameObject.activeInHierarchy) return;
        var bullet = other.GetComponentInParent<BulletMove>();
        if (bullet != null && bullet.target != null && bullet.target.CompareTag("Enemy"))
        {
            var info = bullet.GetComponent<BulletInfo>();
            if (info != null) TakeDamage(info.id, info.damage, bodyCollider != null ? bodyCollider.ClosestPoint(other.transform.position) : transform.position);
            bullet.GetComponent<PooledObject>()?.DestroyObj();
            return;
        }
        Vector2 point = bodyCollider != null ? bodyCollider.ClosestPoint(other.transform.position) : transform.position;
        if (other.CompareTag("Skill"))
        {
            var skill = other.GetComponentInParent<SkillInfo>();
            if (skill == null) return;
            var projectile = skill.GetComponent<MageProjectile>();
            if (projectile != null && !projectile.TryRegisterEnemyHit(this)) return;
            if (!isInvincible && !isDead && !isDying)
                TakeDamage(skill.skillId, skill.skillDamage, point);
            if (projectile != null) projectile.HandleEnemyContact(this);
            else skill.GetComponent<BasicAttackChainRelay>()?.HandleEnemyContact(bodyCollider);
            return;
        }
        if (!other.CompareTag("Upgrade")) return;
        var source = other.GetComponentInParent<UpgradeInfo>();
        if (source == null) return;
        var floor = source.GetComponent<Flooring>();
        if (floor != null) { if (!isDead && !isDying) floor.HandleEnemyContact(bodyCollider); return; }
        var lava = source.GetComponent<LavaField>();
        if (lava != null) { if (!isDead && !isDying) lava.HandleEnemyContact(bodyCollider); return; }
        var poison = source.GetComponent<Poison>();
        if (poison != null) { if (!isDead && !isDying) poison.HandleEnemyContact(bodyCollider); return; }
        // DOT, mushrooms and targeted lightning apply damage on their own timed ticks.
        if (source.GetComponent<DotDamage>() != null || source.GetComponent<Mold>() != null ||
            source.GetComponent<Lightning>() != null) return;
        var fireStorm = source.GetComponent<FireStorm>();
        if (fireStorm != null && fireStorm.removeFromTarget == gameObject) return;
        if (!isInvincible && !isDead && !isDying && DataManager.Instance != null &&
            DataManager.Instance.upgradeDatasDic.TryGetValue(source.id, out var data) &&
            data.damage != null && data.damage.Length > 0)
            TakeDamage(source.id, data.damage[Mathf.Clamp(data.UpgradeLevel, 0, data.damage.Length - 1)], point);
        source.GetComponent<Chain>()?.HandleEnemyContact(bodyCollider);
        // Consumption still occurs on invincible enemies; persistent orbiting effects remain.
        source.GetComponent<HomingProjectile>()?.HandleEnemyContact(bodyCollider);
    }

    /// <summary>장판을 벗어나면 해당 장판의 감속 중첩을 해제한다.</summary>
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null || !other.CompareTag("Upgrade")) return;
        other.GetComponentInParent<Flooring>()?.HandleEnemyExit(bodyCollider);
    }

    protected bool HasAnimatorParameter(string paramName)
    {
        if (bossAnimator == null) return false;
        foreach (var parameter in bossAnimator.parameters)
            if (parameter.name == paramName) return true;
        return false;
    }
}
