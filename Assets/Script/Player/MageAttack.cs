using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>Hold the left mouse button to shoot toward the pointer as the second character.</summary>
public sealed class MageAttack : MonoBehaviour
{
    private static readonly int AttackStateHash = Animator.StringToHash("Base Layer.Attack");
    [SerializeField, Min(0.05f)] private float shotInterval = 0.45f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 12f;
    [SerializeField, Min(0.1f)] private float projectileLifetime = 1.6f;
    [SerializeField, Min(0f)] private float spawnOffset = 0.55f;
    [SerializeField, Min(0f)] private float projectileSpreadAngle = 7f;
    [SerializeField] private int projectileDataId = 201;
    [SerializeField, Range(0f, 1f)] private float attackSfxVolume = 0.8f;

    private AudioClip attackSfx;

    private PlayerCombat combat;
    private PlayerMove movement;
    private Animator animator;
    private float nextShotTime;
    private float attackFacingUntil;
    private float attackClipDuration = 0.42f;
    private float movementFacingSign = 1f;
    private float attackFacingSign = 1f;
    private bool facingAttack;
    private bool shotPending;
    private bool projectileDataApplied;
    private bool bufferedShot;
    private float bufferedShotUntil;
    private Vector2 pendingDirection;
    private int pendingDamage;
    private BasicAttackModifiers pendingModifiers = BasicAttackModifiers.Default;
    private int shotSequence;
    private void Awake()
    {
        combat = GetComponent<PlayerCombat>();
        movement = GetComponent<PlayerMove>();
        animator = GetComponent<Animator>();
        movementFacingSign = transform.localScale.x < 0f ? -1f : 1f;
        attackSfx = Resources.Load<AudioClip>("Audio/SFX/MageAttack");

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == "MageAttack")
                {
                    attackClipDuration = clip.length;
                    break;
                }
            }
        }
    }

    private void Update()
    {
        ApplyProjectileData();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            bufferedShot = true;
            bufferedShotUntil = Mathf.Max(Time.time, nextShotTime) + 0.1f;
        }

        if (Time.timeScale <= 0f)
        {
            bufferedShot = false;
            return;
        }

        if (CharacterManager.Instance == null || CharacterManager.Instance.currentCharacter != 2 ||
            combat == null || combat.isDead || Mouse.current == null ||
            (!Mouse.current.leftButton.isPressed &&
             (!bufferedShot || Time.time > bufferedShotUntil)) ||
            Time.time < nextShotTime)
            return;

        if (IsPointerOverInteractiveUI())
        {
            bufferedShot = false;
            return;
        }

        Camera camera = Camera.main;
        if (camera == null)
            return;

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector3 target = camera.ScreenToWorldPoint(new Vector3(
            screenPosition.x, screenPosition.y, transform.position.z - camera.transform.position.z));
        Vector2 direction = target - transform.position;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();
        int damage = 1;
        if (DataManager.Instance != null &&
            DataManager.Instance.playerDatasDic.TryGetValue(2, out PlayerData data))
        {
            damage = Mathf.Max(1, data.damage);
            if (Random.value * 100f < data.criticalChance)
                damage = Mathf.Max(1, Mathf.RoundToInt(damage * data.criticalDamage));
        }

        // A faster cooldown can start another cast before the previous clip
        // reaches its release event. Complete that shot before replacing it.
        if (shotPending)
            ReleaseProjectile();

        pendingDirection = direction;
        pendingModifiers = UpgradeManager.Instance != null
            ? UpgradeManager.Instance.GetBasicAttackModifiers(2)
            : BasicAttackModifiers.Default;
        pendingDamage = Mathf.Max(
            1,
            Mathf.RoundToInt(damage * pendingModifiers.damageMultiplier));
        shotPending = true;
        bufferedShot = false;
        int sequence = ++shotSequence;

        attackFacingSign = Mathf.Abs(direction.x) > 0.001f
            ? Mathf.Sign(direction.x)
            : (facingAttack ? attackFacingSign : movementFacingSign);
        attackFacingUntil = Time.time + attackClipDuration /
            Mathf.Max(0.01f, animator != null ? animator.speed : 1f);
        facingAttack = true;
        Face(attackFacingSign);

        // Each accepted shot starts a new cast, even if the previous attack
        // state is still playing. The cooldown above prevents per-frame restarts.
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.ResetTrigger("Skill1");
            animator.Play(AttackStateHash, 0, 0f);
        }
        else
            ReleaseProjectile();

        // Keep a small fallback in case an imported clip loses its animation
        // event. The normal path still uses ReleaseProjectile from MageAttack.anim.
        StartCoroutine(ReleaseProjectileFallback(sequence));
        nextShotTime = Time.time + shotInterval;
    }

    private void LateUpdate()
    {
        if (movement != null && Mathf.Abs(movement.MoveDir.x) > 0.001f)
            movementFacingSign = Mathf.Sign(movement.MoveDir.x);

        if (!facingAttack)
            return;

        if (Time.time < attackFacingUntil)
        {
            // Movement input can change the root facing during Update.
            Face(attackFacingSign);
            return;
        }

        facingAttack = false;
        Face(movementFacingSign);
    }

    private void Face(float sign)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * sign;
        transform.localScale = scale;
    }

    private static bool IsPointerOverInteractiveUI()
    {
        if (EventSystem.current == null || Mouse.current == null)
            return false;

        var pointer = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        foreach (RaycastResult hit in hits)
        {
            if (hit.gameObject.GetComponentInParent<Selectable>() != null ||
                hit.gameObject.GetComponentInParent<ScrollRect>() != null)
                return true;
        }

        return false;
    }

    private void ApplyProjectileData()
    {
        if (projectileDataApplied || DataManager.Instance == null)
            return;

        if (!DataManager.Instance.skillDatasDic.TryGetValue(projectileDataId, out SkillData data))
            return;

        if (data.skillCoolTime > 0f)
            shotInterval = data.skillCoolTime;
        if (data.projectileSpeed > 0f)
            projectileSpeed = data.projectileSpeed;
        if (data.projectileLifetime > 0f)
            projectileLifetime = data.projectileLifetime;
        if (data.spawnOffset > 0f)
            spawnOffset = data.spawnOffset;

        projectileDataApplied = true;
    }

    private System.Collections.IEnumerator ReleaseProjectileFallback(int sequence)
    {
        yield return new WaitForSeconds(0.24f);
        if (sequence == shotSequence)
            ReleaseProjectile();
    }

    // Called once by the MageAttack animation when the staff points forward.
    public void ReleaseProjectile()
    {
        if (!shotPending)
            return;

        shotPending = false;
        if (combat == null || combat.isDead || CharacterManager.Instance == null ||
            CharacterManager.Instance.currentCharacter != 2)
            return;

        if (PoolManager.Instance == null)
        {
            Debug.LogError("PoolManager is unavailable; mage projectile was not spawned.", this);
            return;
        }

        if (attackSfx != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySfx(attackSfx, attackSfxVolume);

        int projectileCount = Mathf.Max(1, pendingModifiers.attackCount);
        SpriteRenderer playerRenderer = GetComponentInChildren<SpriteRenderer>();

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = (i - (projectileCount - 1) * 0.5f) * projectileSpreadAngle;
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, angle) * pendingDirection;
            Vector2 origin = (Vector2)transform.position + shotDirection * spawnOffset;
            GameObject projectileObject = PoolManager.Instance.Spawn(projectileDataId, origin);
            if (projectileObject == null)
                continue;

            MageProjectile projectile = projectileObject.GetComponent<MageProjectile>();
            if (projectile == null)
            {
                projectileObject.GetComponent<PooledObject>()?.DestroyObj();
                Debug.LogError($"Pooled prefab {projectileDataId} needs MageProjectile.", this);
                continue;
            }

            SpriteRenderer renderer = projectileObject.GetComponent<SpriteRenderer>();
            TrailRenderer trail = projectileObject.GetComponent<TrailRenderer>();
            if (playerRenderer != null)
            {
                if (renderer != null)
                {
                    renderer.sortingLayerID = playerRenderer.sortingLayerID;
                    renderer.sortingOrder = playerRenderer.sortingOrder + 1;
                }
                if (trail != null)
                {
                    trail.sortingLayerID = playerRenderer.sortingLayerID;
                    trail.sortingOrder = playerRenderer.sortingOrder;
                }
            }

            projectile.Launch(
                origin,
                shotDirection,
                projectileSpeed,
                projectileLifetime * pendingModifiers.rangeMultiplier,
                pendingDamage,
                pendingModifiers.pierceCount,
                pendingModifiers.chainCount,
                pendingModifiers.chainRange);
        }
    }

    private void OnDisable()
    {
        facingAttack = false;
        shotPending = false;
        bufferedShot = false;
        pendingModifiers = BasicAttackModifiers.Default;
        shotSequence++;
        projectileDataApplied = false;
    }

}
