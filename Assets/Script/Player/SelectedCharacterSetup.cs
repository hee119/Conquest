using UnityEngine;

/// <summary>Applies the selected character to the player already referenced by the stage UI and camera.</summary>
[DefaultExecutionOrder(-100)]
public sealed class SelectedCharacterSetup : MonoBehaviour
{
    [SerializeField] private RuntimeAnimatorController mageAnimatorController;
    [SerializeField] private Sprite mageIdleSprite;

    private RuntimeAnimatorController swordAnimatorController;
    private Sprite swordIdleSprite;
    private string swordName;

    private void Awake()
    {
        swordAnimatorController = GetComponent<Animator>()?.runtimeAnimatorController;
        swordIdleSprite = GetComponentInChildren<SpriteRenderer>()?.sprite;
        swordName = gameObject.name;
    }

    private void Start() => ApplySelectedCharacter();

    public void ApplySelectedCharacter()
    {
        int id = CharacterManager.Instance != null ? CharacterManager.Instance.currentCharacter : 1;
        if (DataManager.Instance == null ||
            !DataManager.Instance.playerDatasDic.TryGetValue(id, out PlayerData data))
            return;

        PlayerInfo info = GetComponent<PlayerInfo>();
        PlayerCombat combat = GetComponent<PlayerCombat>();
        PlayerMove movement = GetComponent<PlayerMove>();
        Animator animator = GetComponent<Animator>();
        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (info == null || combat == null || movement == null || animator == null ||
            spriteRenderer == null || (id == 2 && (mageAnimatorController == null || mageIdleSprite == null)))
        {
            Debug.LogError("Mage setup is missing a player component or animation asset.", this);
            return;
        }

        info.id = data.id;
        info.name = data.name;
        info.hp = data.hp;
        info.hpRegenRate = data.hpRegenRate;
        info.speed = data.speed;
        info.damage = data.damage;
        info.defense = data.defense;
        info.criticalDamage = data.criticalDamage;
        info.criticalChance = data.criticalChance;
        gameObject.name = id == 2 ? "Mage" : swordName;
        animator.runtimeAnimatorController = id == 2 ? mageAnimatorController : swordAnimatorController;
        spriteRenderer.sprite = id == 2 ? mageIdleSprite : swordIdleSprite;
        SkillSpawner skillSpawner = GetComponent<SkillSpawner>();
        if (skillSpawner != null)
            skillSpawner.skillId1 = id == 2 ? 201 : 200;

        movement.speed = data.speed;
        combat.maxHp = data.hp;
        combat.hp = data.hp;
        combat.delayedHp = data.hp;
        combat.UpdateHpUI();
        combat.SetNormalColor(Color.white);
    }
}
