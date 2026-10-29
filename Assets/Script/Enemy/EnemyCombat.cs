using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyCombat : MonoBehaviour, ICombat
{
    protected Animator anim;

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

    public void Awake()
    {
        anim = GetComponent<Animator>();
        pooledObject = GetComponent<PooledObject>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        enemyInfo = GetComponent<EnemyInfo>();
        enemyMove = GetComponent<EnemyMove>();
        bodyCollider = GetComponent<Collider2D>();
    }

    public void TakeDamage(float damage, Vector2 hitPoint)
    {
        if (!isDead &&
            !isDying &&
            gameObject.activeInHierarchy)
        {
            StartCoroutine(InvincibleCor());

            hp -= damage;
            hpUpdate?.Invoke();

            if (SoundManager.Instance != null)
                SoundManager.Instance.Play(SoundIds.EnemyHit, enemy: this);

            if (hitParticle != null)
                hitParticle.Play();

            bool hasHit = HasAnimatorParameter("Hit");

            if (hasHit)
            {
                anim.SetTrigger("Hit");
            }

            HitFlash();

            if (GameManager.Instance.burnChance > 0 && !isBurn)
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

            if (GameManager.Instance.moldChance > 0)
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

        while (
            t < 0.3f &&
            !anim.GetCurrentAnimatorStateInfo(0).IsName(hitStateName)
        )
        {
            t += Time.deltaTime;
            yield return null;
        }

        t = 0f;

        while (t < hitMaxWait)
        {
            AnimatorStateInfo info =
                anim.GetCurrentAnimatorStateInfo(0);

            if (!info.IsName(hitStateName) ||
                info.normalizedTime >= 1f)
            {
                break;
            }

            t += Time.deltaTime;
            yield return null;
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

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (
            other.CompareTag("Skill") &&
            !isInvincible &&
            !isDead &&
            !isDying &&
            gameObject.activeInHierarchy
        )
        {
            Collider2D myCollider = GetComponent<Collider2D>();

            Vector2 hitPoint =
                myCollider.ClosestPoint(other.transform.position);

            SkillInfo skillInfo =
                other.gameObject.GetComponent<SkillInfo>();

            if (skillInfo != null)
            {
                TakeDamage(
                    skillInfo.skillDamage,
                    hitPoint
                );
            }
        }
    }

    protected bool HasAnimatorParameter(string paramName)
    {
        if (anim == null)
            return false;

        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName)
                return true;
        }

        return false;
    }
}
