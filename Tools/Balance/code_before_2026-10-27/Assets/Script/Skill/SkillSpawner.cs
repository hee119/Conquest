using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SkillSpawner : MonoBehaviour
{
    [Header("Skill Id")]
    public int skillId1;
    public int skillId2;
    public int skillId3;
    public int skillId4;

    [Header("Basic Attack Spread")]
    [SerializeField, Min(0f)] private float basicAttackSpreadAngle = 7f;

    AnimationController animationController;
    
    PlayerMove playerMove;

    private bool isBasicAttackFacingLocked;
    private float attackFacingSign = 1f;
    private float movementFacingSign = 1f;
    private Vector2 basicAttackDirection = Vector2.right;
    private PlayerCombat combat;
    private PointerEventData uiPointer;
    private EventSystem uiEventSystem;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    public void Awake()
    {
        animationController = GetComponent<AnimationController>();
        playerMove = GetComponent<PlayerMove>();
        combat = GetComponent<PlayerCombat>();
        movementFacingSign = transform.localScale.x < 0f ? -1f : 1f;
    }

    private void Update()
    {
        // Held projectile attacks use the same SkillUse/cooldown path as clicks.
        if (DataManager.Instance != null &&
            DataManager.Instance.skillDatasDic.TryGetValue(skillId1, out SkillData skill) &&
            skill.projectileSpeed > 0f && Mouse.current != null &&
            Mouse.current.leftButton.isPressed)
        {
            SkillUse(skillId1);
        }
    }

    private void LateUpdate()
    {
        if (playerMove != null && Mathf.Abs(playerMove.MoveDir.x) > 0.001f)
            movementFacingSign = Mathf.Sign(playerMove.MoveDir.x);

        if (isBasicAttackFacingLocked)
        {
            if (animationController.IsBasicAttacking)
                Face(attackFacingSign);
            else
                EndBasicAttackFacing();
        }
    }

    public void SkillUse(int skillId)
    {
        if (Time.timeScale <= 0f || (combat != null && combat.isDead) ||
            IsPointerOverInteractiveUI())
            return;

        if (animationController.canSkillUse1 && skillId == skillId1)
        {
            BeginBasicAttackFacing();
            animationController.Skill1(skillId);
        }
        else if (animationController.canSkillUse2 && skillId == skillId2)
        {
            animationController.Skill2(skillId);
        }
        else if (animationController.canSkillUse3 && skillId == skillId3)
        {
            animationController.Skill3(skillId);
        }
        else if(animationController.canSkillUse4 && skillId == skillId4)
        {
            animationController.Skill4(skillId);
        }
    }

    private bool IsPointerOverInteractiveUI()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || Mouse.current == null)
            return false;

        // InputAction callbacks run before the UI module updates its cached
        // pointer state. Raycast the current position instead of reading that cache.
        if (uiPointer == null || uiEventSystem != eventSystem)
        {
            uiEventSystem = eventSystem;
            uiPointer = new PointerEventData(eventSystem);
        }
        uiPointer.Reset();
        uiPointer.position = Mouse.current.position.ReadValue();
        uiHits.Clear();
        eventSystem.RaycastAll(uiPointer, uiHits);
        foreach (RaycastResult hit in uiHits)
        {
            if (hit.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>() != null ||
                hit.gameObject.GetComponentInParent<UnityEngine.UI.ScrollRect>() != null)
                return true;
        }
        return false;
    }
    
    
    public void PlayerInput(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        // Read the current IDs: character setup can change them after Awake.
        int skillId = context.control.path switch
        {
            "/Mouse/leftButton" => skillId1,
            "/Keyboard/1" => skillId2,
            "/Keyboard/2" => skillId3,
            "/Keyboard/3" => skillId4,
            _ => 0
        };
        if (skillId > 0)
            SkillUse(skillId);
    }

    public IEnumerator LifeTime(int skillId, GameObject obj)
    {
        yield return new WaitForSeconds(DataManager.Instance.skillDatasDic[skillId].lifetime);
        
        obj.GetComponent<PooledObject>().DestroyObj();
    }

    public void MovementLock(int canMove)
    {
        if(canMove > 0)
        {
            playerMove.StopMove(1);
            EndBasicAttackFacing();
        }
        else
            playerMove.StopMove(0);
    }

    public void SkillSpawn(int skillId)
    {
        float offset = 1f;
        bool isBasicAttack = skillId == skillId1;
        int characterId = CharacterManager.Instance != null
            ? CharacterManager.Instance.currentCharacter
            : 0;
        BasicAttackModifiers modifiers = isBasicAttack && UpgradeManager.Instance != null
            ? UpgradeManager.Instance.GetBasicAttackModifiers(characterId)
            : BasicAttackModifiers.Default;

        int attackCount = isBasicAttack ? modifiers.attackCount : 1;
        float facingSign = transform.localScale.x >= 0f ? 1f : -1f;
        Vector2 baseDirection = facingSign > 0f ? Vector2.right : Vector2.left;
        SkillData skill = DataManager.Instance.skillDatasDic[skillId];
        bool isProjectile = skill.projectileSpeed > 0f;
        if (isProjectile)
        {
            baseDirection = basicAttackDirection;
            offset = skill.spawnOffset;
            if (SoundManager.Instance != null)
                SoundManager.Instance.Play(SoundIds.MageAttack);
        }

        for (int i = 0; i < attackCount; i++)
        {
            if (isBasicAttack && isBasicAttackFacingLocked)
            {
                Face(attackFacingSign);
                facingSign = attackFacingSign;
                if (!isProjectile)
                    baseDirection = facingSign > 0f ? Vector2.right : Vector2.left;
            }

            float spreadAngle = isBasicAttack
                ? (i - (attackCount - 1) * 0.5f) * basicAttackSpreadAngle
                : 0f;
            Vector2 spawnDirection =
                Quaternion.Euler(0f, 0f, spreadAngle) * baseDirection;
            Vector3 spawnPos = transform.position + (Vector3)(spawnDirection * offset);

            GameObject skillPrefab = PoolManager.Instance.Spawn(skillId, spawnPos);
            if (skillPrefab == null)
                continue;

            MageProjectile projectile = skillPrefab.GetComponent<MageProjectile>();
            if (projectile != null)
            {
                PlayerData player = DataManager.Instance.playerDatasDic[characterId];
                int damage = Mathf.Max(1, player.damage);
                if (Random.value * 100f < player.criticalChance)
                    damage = Mathf.Max(1, Mathf.RoundToInt(damage * player.criticalDamage));

                projectile.Launch(spawnPos, spawnDirection, skill.projectileSpeed,
                    skill.projectileLifetime * modifiers.rangeMultiplier,
                    Mathf.Max(1, Mathf.RoundToInt(damage * modifiers.damageMultiplier)),
                    modifiers.pierceCount, modifiers.chainCount, modifiers.chainRange);
                // The projectile owns its lifetime and pool return. A second timer
                // could return this object after it has already been reused.
                continue;
            }

            skillPrefab.transform.rotation = Quaternion.Euler(0f, 0f, spreadAngle);

            if (isBasicAttack)
            {
                BasicAttackInstanceState state =
                    skillPrefab.GetComponent<BasicAttackInstanceState>();
                if (state == null)
                    state = skillPrefab.AddComponent<BasicAttackInstanceState>();
                state.Apply(modifiers);
            }

            ParticleSystemRenderer[] particleRenderers =
                skillPrefab.GetComponentsInChildren<ParticleSystemRenderer>();

            foreach (ParticleSystemRenderer particleRenderer in particleRenderers)
            {
                particleRenderer.flip = new Vector3(
                    playerMove.transform.localScale.x < 0 ? 1 : 0,
                    particleRenderer.flip.y,
                    particleRenderer.flip.z
                );
            }

            StartCoroutine(LifeTime(skillId, skillPrefab));
        }
    }

    private void BeginBasicAttackFacing()
    {
        attackFacingSign = transform.localScale.x < 0f ? -1f : 1f;
        basicAttackDirection = attackFacingSign > 0f ? Vector2.right : Vector2.left;
        if (DataManager.Instance != null &&
            DataManager.Instance.skillDatasDic.TryGetValue(skillId1, out SkillData skill) &&
            skill.projectileSpeed > 0f && Mouse.current != null && Camera.main != null)
        {
            Camera camera = Camera.main;
            Vector2 pointer = Mouse.current.position.ReadValue();
            Vector3 target = camera.ScreenToWorldPoint(new Vector3(
                pointer.x, pointer.y, transform.position.z - camera.transform.position.z));
            Vector2 direction = target - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
                basicAttackDirection = direction.normalized;
            if (Mathf.Abs(basicAttackDirection.x) > 0.001f)
                attackFacingSign = Mathf.Sign(basicAttackDirection.x);
        }
        isBasicAttackFacingLocked = true;
        Face(attackFacingSign);
    }

    private void EndBasicAttackFacing()
    {
        if (!isBasicAttackFacingLocked)
            return;

        isBasicAttackFacingLocked = false;
        Face(movementFacingSign);
    }

    private void Face(float sign)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * sign;
        transform.localScale = scale;
    }

    private void OnDisable()
    {
        isBasicAttackFacingLocked = false;
    }
}
