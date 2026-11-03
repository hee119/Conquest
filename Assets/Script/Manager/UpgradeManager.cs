using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>Runtime values applied to a character's basic attack.</summary>
public struct BasicAttackModifiers
{
    public float rangeMultiplier;
    public int attackCount;
    public int pierceCount;
    public int chainCount;
    public float chainRange;
    public float damageMultiplier;

    public static BasicAttackModifiers Default => new BasicAttackModifiers
    {
        rangeMultiplier = 1f,
        attackCount = 1,
        pierceCount = 0,
        chainCount = 0,
        chainRange = 0f,
        damageMultiplier = 1f
    };
}

/// <summary>기본 공격의 연쇄 피해를 주변 적에게 적용한다.</summary>
public static class BasicAttackChainUtility
{
    /// <summary>전달받은 증강 효과를 적용한다.</summary>
    public static void Apply(
        Vector2 origin,
        EnemyCombat primaryTarget,
        float damage,
        int chainCount,
        float chainRange, int sourceId = 0)
    {
        if (chainCount <= 0 || chainRange <= 0f || damage <= 0)
            return;

        int enemyLayer = LayerMask.GetMask("Enemy");
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, chainRange, enemyLayer);
        IEnumerable<EnemyCombat> targets = hits
            .Select(hit => hit != null ? hit.GetComponentInParent<EnemyCombat>() : null)
            .Where(enemy => enemy != null && enemy != primaryTarget &&
                            enemy.gameObject.activeInHierarchy)
            .Distinct()
            .OrderBy(enemy => ((Vector2)enemy.transform.position - origin).sqrMagnitude)
            .Take(chainCount);

        foreach (EnemyCombat enemy in targets)
            enemy.TakeDamage(sourceId, damage, enemy.transform.position);
    }
}

/// <summary>Applies character basic attack upgrades to a pooled attack instance.</summary>
public sealed class BasicAttackInstanceState : MonoBehaviour
{
    private Vector3 baseScale;
    private float baseDamage;
    private SkillInfo skillInfo;
    private bool initialized;

    /// <summary>실행에 필요한 초기 상태와 참조를 준비한다.</summary>
    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        baseScale = transform.localScale;
        skillInfo = GetComponent<SkillInfo>();
        baseDamage = skillInfo != null ? skillInfo.skillDamage : 0;
    }

    /// <summary>전달받은 증강 효과를 적용한다.</summary>
    public void Apply(BasicAttackModifiers modifiers, float? attackDamage = null)
    {
        Initialize();
        transform.localScale = baseScale * modifiers.rangeMultiplier;

        if (skillInfo != null)
        {
            skillInfo.skillDamage = Mathf.Max(0f, (attackDamage ?? baseDamage) * modifiers.damageMultiplier);
        }

        BasicAttackChainRelay relay = GetComponent<BasicAttackChainRelay>();
        if (relay == null)
            relay = gameObject.AddComponent<BasicAttackChainRelay>();

        relay.Configure(
            skillInfo != null ? skillInfo.skillDamage : 0,
            modifiers.chainCount,
            modifiers.chainRange);
    }

    /// <summary>비활성화될 때 이벤트와 진행 중인 동작을 정리한다.</summary>
    private void OnDisable()
    {
        if (!initialized)
            return;

        transform.localScale = baseScale;
        if (skillInfo != null)
            skillInfo.skillDamage = baseDamage;
    }
}

/// <summary>기본 공격 적중 시 연쇄 공격을 전달한다.</summary>
public sealed class BasicAttackChainRelay : MonoBehaviour
{
    private readonly HashSet<int> triggeredTargets = new HashSet<int>();
    private float damage;
    private int chainCount;
    private float chainRange;

    /// <summary>Configure 동작을 처리한다.</summary>
    public void Configure(float hitDamage, int count, float range)
    {
        damage = hitDamage;
        chainCount = Mathf.Max(0, count);
        chainRange = Mathf.Max(0f, range);
        triggeredTargets.Clear();
        enabled = chainCount > 0 && chainRange > 0f && damage > 0;
    }

    /// <summary>다른 콜라이더가 진입했을 때 적중 효과를 처리한다.</summary>
    public void HandleEnemyContact(Collider2D other)
    {
        if (!isActiveAndEnabled || other == null) return;
        EnemyCombat primary = other.GetComponentInParent<EnemyCombat>();
        if (primary == null || !triggeredTargets.Add(primary.GetInstanceID()))
            return;

        BasicAttackChainUtility.Apply(
            primary.transform.position,
            primary,
            damage,
            chainCount,
            chainRange,
            GetComponent<SkillInfo>()?.skillId ?? 0);
    }
}

/// <summary>증강의 획득, 효과, 레벨 및 쿨타임을 관리한다.</summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;
    public static event Action<UpgradeManager> InstanceChanged;
    public event Action UpgradesChanged;
    public List<int> skillUpgrades = new List<int>();
    public List<int> statUpgrades = new List<int>();
    public List<int> characterSkillUpgrades = new List<int>();
    public List<int> upgrades = new List<int>();
    public List<UpgradeUiInfo> upgradesInfo = new List<UpgradeUiInfo>();
    public List<int> renewableUpgrades = new List<int>();

    Dictionary<int, Action<int>> upgradeActions = new Dictionary<int, Action<int>>();
    public Dictionary<int, IEnumerator> upgradeCor = new Dictionary<int, IEnumerator>();
    private readonly Dictionary<int, float> upgradeCooldownEnds = new Dictionary<int, float>();
    private readonly Dictionary<int, float> upgradeCooldownDurations = new Dictionary<int, float>();
    private readonly HashSet<int> activeShurikenSpinIds = new HashSet<int>();
    private AudioSource shurikenSpinSoundSource;
    [SerializeField]
    private UpgradeCooldownHud upgradeCooldownHud;

    private GameObject player;
    private PlayerInfo playerInfo;
    BoxCollider2D playerCollider;

    public Image[] skillUpgradeIcons;
    public Image[] statUpgradeIcons;

    public int skillUpgradeCount;
    public int statUpgradeCount;

    public bool isFirstUpgrade = true;

    public event Action upgradeRenewal;
    public event Action upgradeSelect;

    [SerializeField]
    private DetectUpgrade detectUpgrade;

    [SerializeField]
    private Orbit orbit;

    public List<int> maxLevelUpgrades = new List<int>();
    
    [Header("디버그 (테스트용, 값은 %)")]
    public float debugCritChance;
    public float debugCritDamage;
    public float debugAttackPower;
    public float debugMaxHp;
    public float debugDefense;
    public float debugSkillCooldown;     // 100 이하로 (90이면 쿨타임 90% 감소)
    public float debugPassiveCooldown;   // 100 이하로

    [ContextMenu("Apply Debug Bonuses")]
    public void ApplyDebugBonuses()
    {
        SetBonusesAndApply(
            debugCritChance, debugCritDamage, debugAttackPower,
            debugMaxHp, debugDefense, debugSkillCooldown, debugPassiveCooldown);
    }

    [ContextMenu("Reset Bonuses To Original")]
    public void ResetBonusesToOriginal()
    {
        SetBonusesAndApply(0f, 0f, 0f, 0f, 0f, 0f, 0f);
    }

    

    // 스킬(upgradeDatasDic) / 스탯(statUpgradeDatasDic) 데이터를 UI 표시용으로 통일한 구조체
    private struct UpgradeView
    {
        public int id;
        public string name;
        public Sprite sprite;
        public string type;
        public int upgradeLevel;
        public string description;
    }

    // =========================================================
    // 스탯 업그레이드용 원본 스냅샷 / 보너스 변수
    // 원본은 절대 수정하지 않고, 원본 + 보너스를 계산해서 현재 스탯에 덮어씀
    // =========================================================

    private struct PlayerStatOrigin
    {
        public int criticalChance;
        public float criticalDamage;
        public float damage;
        public int hp;
        public int defense;
        public int hpRegenRate;
        public float speed;
    }

    private readonly Dictionary<int, PlayerStatOrigin> originalPlayerStats = new Dictionary<int, PlayerStatOrigin>();
    private readonly Dictionary<SkillData, float> originalSkillCoolTimes = new Dictionary<SkillData, float>();
    private readonly Dictionary<SkillData, float> originalMovementLockTimes = new Dictionary<SkillData, float>();
    private readonly Dictionary<int, float[]> originalPassiveCoolTimes = new Dictionary<int, float[]>();

    // 현재 적용 중인 보너스 (%)
    private float critChanceBonus;
    private float critDamageBonus;
    private float attackPowerBonus;
    private float maxHpBonus;
    private float defenseBonus;
    private float skillCooldownBonus;
    private float passiveCooldownBonus;
    private AudioSource snailGroundLoopSource;

    /// <summary>비활성화될 때 이벤트와 진행 중인 동작을 정리한다.</summary>
    private void OnDisable()
    {
        StopSnailGroundLoop();
    }

    /// <summary>필요한 컴포넌트와 초기 상태를 준비한다.</summary>
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        /// <summary>if 동작을 처리한다.</summary>
        else if (Instance != this)
        {
            // Rebind references to controls and skills from the reloaded scene.
            Instance.upgradesInfo = upgradesInfo;
            Instance.skillUpgradeIcons = skillUpgradeIcons;
            Instance.statUpgradeIcons = statUpgradeIcons;
            Instance.detectUpgrade = detectUpgrade;
            Instance.orbit = orbit;
            InstanceChanged?.Invoke(Instance);
            Destroy(gameObject);
            return;
        }

        player = GameObject.FindGameObjectWithTag("Player");
        playerCollider = player.GetComponent<BoxCollider2D>();
        playerInfo = player.GetComponent<PlayerInfo>();
        EnsureUpgradeCooldownHud();
        InstanceChanged?.Invoke(this);
    }

    /// <summary>오브젝트가 제거될 때 사용한 자원을 정리한다.</summary>
    private void OnDestroy()
    {
        if (Instance != this)
            return;
        Instance = null;
        InstanceChanged?.Invoke(null);
    }

    /// <summary>새 게임을 시작할 수 있도록 증강 상태를 초기화한다.</summary>
    public void ResetForNewRun()
    {
        StopRun();
        ResetBonusesToOriginal();

        skillUpgrades.Clear();
        statUpgrades.Clear();
        characterSkillUpgrades.Clear();
        renewableUpgrades.Clear();
        maxLevelUpgrades.Clear();
        upgrades.Clear();
        skillUpgradeCount = 0;
        statUpgradeCount = 0;
        isFirstUpgrade = true;

        foreach (var data in DataManager.Instance.upgradeDatasDic.Values)
        {
            data.upgradeLevel = 0;
            if (data.tier <= 0)
                upgrades.Add(data.id);
        }
        foreach (var data in DataManager.Instance.statUpgradeDatasDic.Values)
        {
            data.upgradeLevel = 0;
            if (data.tier <= 0)
                upgrades.Add(data.id);
        }
        foreach (var data in DataManager.Instance.characterSkillUpgradeDatasDic.Values)
        {
            data.upgradeLevel = 0;
            if (data.tier <= 0)
                upgrades.Add(data.id);
        }

        player = GameObject.FindGameObjectWithTag("Player");
        playerCollider = player != null ? player.GetComponent<BoxCollider2D>() : null;
        UpgradesChanged?.Invoke();
    }

    /// <summary>StopRun 동작을 처리한다.</summary>
    public void StopRun()
    {
        StopSnailGroundLoop();
        StopAllShurikenSpinSounds();
        StopAllCoroutines();
        upgradeCor.Clear();
        upgradeCooldownEnds.Clear();
        upgradeCooldownDurations.Clear();

        ClearCollectedUpgradeIcons(skillUpgradeIcons);
        ClearCollectedUpgradeIcons(statUpgradeIcons);
        if (upgradeCooldownHud != null)
            upgradeCooldownHud.ClearAll();
    }

    /// <summary>ClearCollectedUpgradeIcons 동작을 처리한다.</summary>
    private static void ClearCollectedUpgradeIcons(Image[] icons)
    {
        if (icons == null)
            return;

        foreach (Image icon in icons)
        {
            if (icon == null)
                continue;

            icon.sprite = null;

            CollectedUpgradeIconUI view = icon.GetComponent<CollectedUpgradeIconUI>();
            if (view != null)
                view.Clear();
        }
    }

    /// <summary>UpgradesInit 동작을 처리한다.</summary>
    public void UpgradesInit()
    {
        foreach (var pair in DataManager.Instance.upgradeDatasDic)
        {
            int upgrade = pair.Key;

            string functionName =
                pair.Value.function;

            MethodInfo method = GetType().GetMethod(
                functionName,
                BindingFlags.Instance | BindingFlags.Public
            );

            if (method == null)
            {
                Debug.LogWarning(
                    $"[UpgradeManager] 함수를 찾을 수 없음: {functionName} (ID: {upgrade})"
                );
                continue;
            }

            Action<int> action =
                (Action<int>)Delegate.CreateDelegate(
                    typeof(Action<int>),
                    this,
                    method
                );

            upgradeActions[upgrade] = action;
        }

        foreach (var pair in DataManager.Instance.statUpgradeDatasDic)
        {
            int upgrade = pair.Key;

            string functionName =
                pair.Value.function;

            MethodInfo method = GetType().GetMethod(
                functionName,
                BindingFlags.Instance | BindingFlags.Public
            );

            if (method == null)
            {
                Debug.LogWarning(
                    $"[UpgradeManager] 함수를 찾을 수 없음: {functionName} (ID: {upgrade})"
                );
                continue;
            }

            Action<int> action =
                (Action<int>)Delegate.CreateDelegate(
                    typeof(Action<int>),
                    this,
                    method
                );

            upgradeActions[upgrade] = action;
        }

        SnapshotOriginalStats();
        ApplyPlayerStats();
        RefreshBasicAttackTimings();
    }

    // 원본 스탯 저장 + 보너스 초기화
    /// <summary>SnapshotOriginalStats 동작을 처리한다.</summary>
    private void SnapshotOriginalStats()
    {
        var dm = DataManager.Instance;

        foreach (var pair in dm.playerDatasDic)
        {
            // Data loaders can initialize upgrade tables more than once.
            // Preserve the raw baseline instead of snapshotting permanent bonuses again.
            if (originalPlayerStats.ContainsKey(pair.Key))
                continue;
            PlayerData p = pair.Value;

            originalPlayerStats[pair.Key] = new PlayerStatOrigin
            {
                criticalChance = p.criticalChance,
                criticalDamage = p.criticalDamage,
                damage = p.damage,
                hp = p.hp,
                defense = p.defense,
                hpRegenRate = p.hpRegenRate,
                speed = p.speed
            };
        }

        foreach (SkillData skill in dm.skillDatasDic.Values)
        {
            if (!originalSkillCoolTimes.ContainsKey(skill)) originalSkillCoolTimes[skill] = skill.skillCoolTime;
            if (!originalMovementLockTimes.ContainsKey(skill)) originalMovementLockTimes[skill] = skill.movementLockTime;
        }

        originalPassiveCoolTimes.Clear();
        foreach (var pair in dm.upgradeDatasDic)
        {
            if (pair.Value.type != "Passive")
                continue;

            originalPassiveCoolTimes[pair.Key] = pair.Value.coolTime.ToArray();
        }

        critChanceBonus = 0f;
        critDamageBonus = 0f;
        attackPowerBonus = 0f;
        maxHpBonus = 0f;
        defenseBonus = 0f;
        skillCooldownBonus = 0f;
        passiveCooldownBonus = 0f;
    }

    // =========================================================
    // 스킬/스탯 공통 조회
    // =========================================================

    // id로 upgradeDatasDic(스킬) → statUpgradeDatasDic(스탯) 순서로 찾아서 UI용 데이터로 반환
    /// <summary>GetView 동작의 가능 여부를 확인하고 처리한다.</summary>
    private bool TryGetView(int id, out UpgradeView v)
    {
        var dm = DataManager.Instance;

        if (dm.upgradeDatasDic.TryGetValue(id, out var u))
        {
            bool showFirstSkillDescription = u.upgradeLevel <= 0 &&
                                             (u.function == "Snail" || u.function == "Mold");
            v = new UpgradeView
            {
                id = u.id,
                name = u.name,
                sprite = u.sprite,
                type = u.type,
                upgradeLevel = u.upgradeLevel,
                description = GetUpgradeDescriptionKey(
                    u.description,
                    u.upgradeLevel,
                    showFirstSkillDescription ? "upgread_intro_" + u.function : null)
            };
            return true;
        }

        if (dm.statUpgradeDatasDic.TryGetValue(id, out var s))
        {
            v = new UpgradeView
            {
                id = s.id,
                name = s.name,
                sprite = s.sprite,
                type = s.type,
                upgradeLevel = s.upgradeLevel,
                description = GetUpgradeDescriptionKey(s.description, s.upgradeLevel)
            };
            return true;
        }

        if (dm.characterSkillUpgradeDatasDic.TryGetValue(id, out var characterSkill))
        {
            v = new UpgradeView
            {
                id = characterSkill.id,
                name = characterSkill.name,
                sprite = characterSkill.sprite,
                type = characterSkill.type,
                upgradeLevel = characterSkill.upgradeLevel,
                description = ValueAt(characterSkill.descriptions, characterSkill.upgradeLevel)
            };
            return true;
        }

        v = default;
        return false;
    }

    // DesId stays the base key. Optional _1 ... _5 entries in the localization
    // sheet describe the next upgrade; older data still uses its original text.
    /// <summary>UpgradeDescriptionKey 관련 값을 조회한다.</summary>
    private static string GetUpgradeDescriptionKey(string baseKey, int ownedLevel, string firstDescription = null)
    {
        string levelKey = baseKey + "_" + (Mathf.Clamp(ownedLevel, 0, 4) + 1);
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.HasTranslation(levelKey))
            return levelKey;

        return !string.IsNullOrEmpty(firstDescription) ? firstDescription : baseKey;
    }

    // 스킬/스탯 상관없이 현재 UpgradeLevel 반환
    /// <summary>Level 관련 값을 조회한다.</summary>
    private int GetLevel(int id)
    {
        var dm = DataManager.Instance;

        if (dm.upgradeDatasDic.TryGetValue(id, out var u))
            return u.UpgradeLevel;

        if (dm.statUpgradeDatasDic.TryGetValue(id, out var s))
            return s.UpgradeLevel;

        if (dm.characterSkillUpgradeDatasDic.TryGetValue(id, out var characterSkill))
            return characterSkill.UpgradeLevel;

        return 0;
    }

    // 카드 슬롯 하나에 업그레이드 정보를 채움
    /// <summary>ApplyToSlot 동작을 처리한다.</summary>
    private void ApplyToSlot(int slot, int id)
    {
        if (slot < 0 || slot >= upgradesInfo.Count || upgradesInfo[slot] == null)
        {
            Debug.LogWarning($"[UpgradeManager] 업그레이드 UI 슬롯이 비어 있음 (Slot: {slot})");
            return;
        }

        if (!TryGetView(id, out var v))
        {
            Debug.LogWarning($"[UpgradeManager] 업그레이드 데이터를 찾을 수 없음 (ID: {id})");
            return;
        }

        var info = upgradesInfo[slot];

        // Unity 직렬화 데이터가 누락된 카드도 런타임에서 복구한다.
        if (info.upgradeData == null)
            info.upgradeData = new UpgradeData();

        if (!info.gameObject.activeInHierarchy)
            info.gameObject.SetActive(true);

        info.upgradeData.id = v.id;
        info.name = v.name;
        info.upgradeData.type = v.type;
        info.upgradeData.upgradeLevel = v.upgradeLevel;

        // 카드 프리팹에서 제목 텍스트가 제거된 경우에도 업그레이드 선택은 가능하게 한다.
        if (info.upgradeNameLo != null)
        {
            info.upgradeNameLo.isTextInit = true;
            info.upgradeNameLo.key = v.name;
            info.upgradeNameLo.ForceUpdateText();
        }

        if (info.upgradeImage != null)
            info.upgradeImage.sprite = v.sprite;

        if (info.upgradeDesLo != null)
        {
            info.upgradeDesLo.isTextInit = true;
            info.upgradeDesLo.key = v.description;
            info.upgradeDesLo.SetArguments(GetDescriptionArguments(id));
            info.upgradeDesLo.ForceUpdateText();
        }
    }

    /// <summary>DescriptionArguments 관련 값을 조회한다.</summary>
    public object[] GetDescriptionArguments(int id)
    {
        var dm = DataManager.Instance;

        if (dm.upgradeDatasDic.TryGetValue(id, out var skill))
        {
            // upgradeLevel is the number already owned; UpgradeLevel points to
            // the previous stage and would repeat the old value on the next card.
            int level = Mathf.Clamp(skill.upgradeLevel, 0, 4);
            object primaryValue;
            switch (skill.function)
            {
                case "Burn":
                case "Mold":
                    primaryValue = ValueAt(skill.debuffChance, level) - (level > 0 ? ValueAt(skill.debuffChance, level - 1) : 0);
                    break;
                case "Snail":
                    primaryValue = Mathf.RoundToInt((1f - ValueAt(skill.slow, level)) * 100f);
                    break;
                default:
                    primaryValue = ValueAt(skill.count, level) - (level > 0 ? ValueAt(skill.count, level - 1) : 0);
                    break;
            }

            return new object[]
            {
                primaryValue,
                ValueAt(skill.damage, level),
                ValueAt(skill.coolTime, level),
                ValueAt(skill.detectionRange, level)
            };
        }

        if (dm.statUpgradeDatasDic.TryGetValue(id, out var stat))
        {
            // 누적 효과의 다음 값에서 현재 값을 빼 이번 선택의 증가량만 표시한다.
            int statLevel = Mathf.Clamp(stat.upgradeLevel, 0, 4);
            float[] values = stat.function switch
            {
                "CriticalInstinct" => stat.critChance,
                "CriticalForce" => stat.critDamage,
                "PowerBoost" => stat.attackPower,
                "Vitality" => stat.maxHp,
                "QuickCast" => stat.skillCooldown,
                "RapidCycle" => stat.passiveCooldown,
                "IronWall" => stat.defense,
                _ => null
            };
            float value = ValueAt(values, statLevel) - (statLevel > 0 ? ValueAt(values, statLevel - 1) : 0f);
            return new object[] { value % 1f == 0f ? (object)(int)value : value };
        }

        if (dm.characterSkillUpgradeDatasDic.TryGetValue(id, out var characterSkill))
        {
            // These arrays already store per-level additions (SumThrough), not totals.
            int nextLevel = Mathf.Clamp(
                characterSkill.upgradeLevel,
                0,
                Mathf.Max(0, GetCharacterSkillMaxLevel(characterSkill) - 1));
            return new object[]
            {
                ValueAt(characterSkill.rangePercent, nextLevel),
                ValueAt(characterSkill.attackCount, nextLevel),
                ValueAt(characterSkill.pierce, nextLevel),
                ValueAt(characterSkill.chainCount, nextLevel),
                ValueAt(characterSkill.chainRange, nextLevel),
                ValueAt(characterSkill.damagePercent, nextLevel)
            };
        }

        return System.Array.Empty<object>();
    }

    /// <summary>ValueAt 동작을 처리한다.</summary>
    private static int ValueAt(int[] values, int index)
    {
        return values != null && values.Length > 0 ? values[Mathf.Min(index, values.Length - 1)] : 0;
    }

    /// <summary>ValueAt 동작을 처리한다.</summary>
    private static float ValueAt(float[] values, int index)
    {
        return values != null && values.Length > 0 ? values[Mathf.Min(index, values.Length - 1)] : 0f;
    }

    /// <summary>ValueAt 동작을 처리한다.</summary>
    private static string ValueAt(string[] values, int index)
    {
        return values != null && values.Length > 0
            ? values[Mathf.Clamp(index, 0, values.Length - 1)]
            : string.Empty;
    }

    // =========================================================
    // 리뉴얼
    // =========================================================

    /// <summary>UpgradeListRenewal 동작을 처리한다.</summary>
    public void UpgradeListRenewal()
    {
        upgrades = upgrades.OrderBy(x => Random.value).ToList();

        // 스킬을 5개 이상 보유했으면
        // 아직 보유하지 않은 스킬을 renewableUpgrades로 이동
        if (skillUpgrades.Count >= 5)
        {
            MoveUnownedToRenewable("Skill", skillUpgrades);
        }

        // 최대 레벨 스킬/스탯 추가
        foreach (int id in skillUpgrades.Concat(statUpgrades))
        {
            if (GetLevel(id) >= 4 &&
                !upgrades.Contains(id) &&
                !maxLevelUpgrades.Contains(id))
            {
                maxLevelUpgrades.Add(id);
            }
        }

        // 스탯을 5개 이상 보유했으면
        // 아직 보유하지 않은 스탯을 renewableUpgrades로 이동
        if (statUpgrades.Count >= 5)
        {
            MoveUnownedToRenewable("Stat", statUpgrades);
        }

        List<int> eligibleUpgrades = upgrades
            .Where(IsEligibleForCurrentCharacter)
            .ToList();

        // 업그레이드 UI 갱신
        // 3개 이상이면 앞의 3개, 3개 미만이면 순환하며 채움 (i % Count)
        if (eligibleUpgrades.Count > 0)
        {
            for (int i = 0; i < 3; i++)
            {
                if (isFirstUpgrade)
                {
                    List<int> firstSkills = eligibleUpgrades
                        .Where(id => DataManager.Instance.upgradeDatasDic.TryGetValue(id, out var data)
                                     && data.type == "Skill")
                        .Take(3)
                        .ToList();

                    // A sheet may contain fewer than three starting skills.
                    // Fill the remaining cards from the available upgrades.
                    for (int k = 0; k < 3; k++)
                        ApplyToSlot(k, k < firstSkills.Count
                            ? firstSkills[k]
                            : eligibleUpgrades[k % eligibleUpgrades.Count]);

                   isFirstUpgrade = false;
                   break;
                }
                else
                {
                    ApplyToSlot(i, eligibleUpgrades[i % eligibleUpgrades.Count]);
                }
            }
        }

        upgradeRenewal?.Invoke();
    }

    /// <summary>MoveUnownedToRenewable 동작을 처리한다.</summary>
    private void MoveUnownedToRenewable(string type, List<int> owned)
    {
        var unowned = upgrades
            .Where(id =>
                TryGetView(id, out var v) &&
                v.type == type &&
                !owned.Contains(id)
            )
            .ToList();

        foreach (int id in unowned)
        {
            if (!renewableUpgrades.Contains(id))
                renewableUpgrades.Add(id);
        }

        upgrades.RemoveAll(id => unowned.Contains(id));
    }

    // =========================================================
    // 업그레이드 선택
    // =========================================================

    /// <summary>CombineUpgrades 동작의 가능 여부를 확인하고 처리한다.</summary>
    public bool TryCombineUpgrades(int first, int second, out int result)
    {
        if (!UpgradeFusionRules.CanCombine(this, first, second, out result) ||
            !upgradeActions.ContainsKey(result) || skillUpgradeIcons == null ||
            skillUpgradeIcons.Length < skillUpgrades.Count - 1 ||
            skillUpgradeIcons.Any(icon => icon == null))
            return false;

        ConsumeCombinationMaterial(first);
        ConsumeCombinationMaterial(second);
        skillUpgradeCount = skillUpgrades.Count;
        for (int i = 0; i < skillUpgradeIcons.Length; i++)
        {
            skillUpgradeIcons[i].sprite = i < skillUpgrades.Count ? GetUpgradeIcon(skillUpgrades[i]) : null;
            CollectedUpgradeIconUI.Attach(skillUpgradeIcons[i], i < skillUpgrades.Count ? skillUpgrades[i] : 0);
        }

        // The result always starts at its first level.
        DataManager.Instance.upgradeDatasDic[result].upgradeLevel = 0;
        SkillUpgradesAdd(result);
        foreach (int available in renewableUpgrades)
            if (!upgrades.Contains(available)) upgrades.Add(available);
        renewableUpgrades.Clear();
        return true;
    }

    /// <summary>ConsumeCombinationMaterial 동작을 처리한다.</summary>
    private void ConsumeCombinationMaterial(int id)
    {
        if (upgradeCor.TryGetValue(id, out IEnumerator routine))
        {
            StopCoroutine(routine);
            upgradeCor.Remove(id);
        }
        upgradeCooldownEnds.Remove(id);
        upgradeCooldownDurations.Remove(id);
        skillUpgrades.Remove(id);
        maxLevelUpgrades.Remove(id);
        upgrades.Remove(id);
        renewableUpgrades.Remove(id);

        string function = DataManager.Instance.upgradeDatasDic[id].function;
        if (function == "Snail") StopSnailGroundLoop();
        if (function == "Shuriken" || function == "LightningShuriken")
            StopShurikenSpinSound(id);
        if (function == "Burn" && GameManager.Instance != null) GameManager.Instance.burnChance = 0;
        if (function == "Mold" && GameManager.Instance != null) GameManager.Instance.moldChance = 0;
        if (function == "Shuriken" && orbit != null)
        {
            foreach (GameObject projectile in orbit.orbitList)
                if (projectile != null) projectile.GetComponent<PooledObject>()?.DestroyObj();
            orbit.orbitList.Clear();
            orbit.orbitColList.Clear();
        }
    }

    /// <summary>SkillUpgradesAdd 동작을 처리한다.</summary>
    public void SkillUpgradesAdd(int id)
    {
        if (!skillUpgrades.Contains(id))
        {
            skillUpgrades.Add(id);

            skillUpgradeIcons[skillUpgradeCount].sprite = GetUpgradeIcon(id);
            CollectedUpgradeIconUI.Attach(skillUpgradeIcons[skillUpgradeCount], id);
            skillUpgradeCount++;
        }

        // 내부 레벨 증가
        DataManager.Instance.upgradeDatasDic[id].upgradeLevel++;

        if (DataManager.Instance.upgradeDatasDic[id].UpgradeLevel >= 4)
        {
            upgrades.Remove(id);
        }

        upgradeActions[id](id);
        EnsureUpgradeCooldownHud()?.Track(id);
        UpgradesChanged?.Invoke();
        upgradeSelect?.Invoke();
    }

    /// <summary>StatUpgradesAdd 동작을 처리한다.</summary>
    public void StatUpgradesAdd(int id)
    {
        if (!statUpgrades.Contains(id))
        {
            statUpgrades.Add(id);

            if (DataManager.Instance.statUpgradeIconDic.ContainsKey(id))
            {
                statUpgradeIcons[statUpgradeCount].sprite =
                    DataManager.Instance.statUpgradeIconDic[id];

                CollectedUpgradeIconUI.Attach(statUpgradeIcons[statUpgradeCount], id);

                statUpgradeCount++;
            }
        }

        // 내부 레벨 증가
        DataManager.Instance.statUpgradeDatasDic[id].upgradeLevel++;

        if (DataManager.Instance.statUpgradeDatasDic[id].UpgradeLevel >= 4)
        {
            upgrades.Remove(id);
        }

        upgradeActions[id](id);
        UpgradesChanged?.Invoke();
        upgradeSelect?.Invoke();
    }

    /// <summary>IsEligibleForCurrentCharacter 동작을 처리한다.</summary>
    private bool IsEligibleForCurrentCharacter(int id)
    {
        if (DataManager.Instance.upgradeDatasDic.ContainsKey(id) ||
            DataManager.Instance.statUpgradeDatasDic.ContainsKey(id))
            return true;

        if (!DataManager.Instance.characterSkillUpgradeDatasDic.TryGetValue(
                id,
                out CharacterSkillUpgradeData data))
            return false;

        int currentCharacterId = CharacterManager.Instance != null
            ? CharacterManager.Instance.currentCharacter
            : 0;

        return data.characterId <= 0 ||
               (currentCharacterId > 0 && data.characterId == currentCharacterId);
    }

    /// <summary>CharacterSkillUpgradesAdd 동작을 처리한다.</summary>
    public void CharacterSkillUpgradesAdd(int id)
    {
        if (!DataManager.Instance.characterSkillUpgradeDatasDic.TryGetValue(
                id,
                out CharacterSkillUpgradeData data) ||
            data.type != "CharacterSkill" ||
            !IsEligibleForCurrentCharacter(id))
            return;

        if (!characterSkillUpgrades.Contains(id))
            characterSkillUpgrades.Add(id);

        data.upgradeLevel++;
        if (data.upgradeLevel >= GetCharacterSkillMaxLevel(data))
            upgrades.Remove(id);

        UpgradesChanged?.Invoke();
        upgradeSelect?.Invoke();
    }

    /// <summary>BasicAttackModifiers 관련 값을 조회한다.</summary>
    public BasicAttackModifiers GetBasicAttackModifiers(int characterId)
    {
        BasicAttackModifiers result = CharacterProgression.BasicAttackBonus(characterId);

        foreach (int id in characterSkillUpgrades)
        {
            if (!DataManager.Instance.characterSkillUpgradeDatasDic.TryGetValue(
                    id,
                    out CharacterSkillUpgradeData data) ||
                data.type != "CharacterSkill" ||
                (data.characterId > 0 && data.characterId != characterId))
                continue;

            int level = Mathf.Max(0, data.UpgradeLevel);
            result.rangeMultiplier += SumThrough(data.rangePercent, level) / 100f;
            result.attackCount += SumThrough(data.attackCount, level);
            result.pierceCount += SumThrough(data.pierce, level);
            result.chainCount += SumThrough(data.chainCount, level);
            result.chainRange = Mathf.Max(result.chainRange, MaxThrough(data.chainRange, level));
            result.damageMultiplier += SumThrough(data.damagePercent, level) / 100f;
        }

        result.attackCount = Mathf.Max(1, result.attackCount);
        result.rangeMultiplier = Mathf.Max(0.1f, result.rangeMultiplier);
        result.damageMultiplier = Mathf.Max(0f, result.damageMultiplier);
        return result;
    }

    /// <summary>CharacterSkillMaxLevel 관련 값을 조회한다.</summary>
    private static int GetCharacterSkillMaxLevel(CharacterSkillUpgradeData data)
    {
        return Mathf.Max(
            1,
            data.rangePercent?.Length ?? 0,
            data.attackCount?.Length ?? 0,
            data.pierce?.Length ?? 0,
            data.chainCount?.Length ?? 0,
            data.chainRange?.Length ?? 0,
            data.damagePercent?.Length ?? 0,
            data.descriptions?.Length ?? 0);
    }

    /// <summary>SumThrough 동작을 처리한다.</summary>
    private static int SumThrough(int[] values, int level)
    {
        if (values == null || values.Length == 0)
            return 0;

        int sum = 0;
        int last = Mathf.Min(level, values.Length - 1);
        for (int i = 0; i <= last; i++)
            sum += values[i];
        return sum;
    }

    /// <summary>SumThrough 동작을 처리한다.</summary>
    private static float SumThrough(float[] values, int level)
    {
        if (values == null || values.Length == 0)
            return 0f;

        float sum = 0f;
        int last = Mathf.Min(level, values.Length - 1);
        for (int i = 0; i <= last; i++)
            sum += values[i];
        return sum;
    }

    /// <summary>MaxThrough 동작을 처리한다.</summary>
    private static float MaxThrough(float[] values, int level)
    {
        if (values == null || values.Length == 0)
            return 0f;

        float max = 0f;
        int last = Mathf.Min(level, values.Length - 1);
        for (int i = 0; i <= last; i++)
            max = Mathf.Max(max, values[i]);
        return max;
    }

    // =========================================================
    // 스킬 코루틴
    // =========================================================

    /// <summary>StartSkillCoroutine 동작을 처리한다.</summary>
    public void StartSkillCoroutine(int id, IEnumerator coroutine)
    {
        if (upgradeCor.TryGetValue(id, out IEnumerator oldCoroutine))
        {
            StopCoroutine(oldCoroutine);
        }

        IEnumerator newCoroutine = coroutine;

        upgradeCor[id] = newCoroutine;

        StartCoroutine(newCoroutine);
    }

    /// <summary>화면에 표시할 해당 증강의 레벨을 반환한다.</summary>
    public int GetUpgradeDisplayLevel(int id)
    {
        if (DataManager.Instance == null)
            return 0;

        if (DataManager.Instance.upgradeDatasDic.TryGetValue(id, out UpgradeData skill))
            return Mathf.Max(0, skill.upgradeLevel);

        if (DataManager.Instance.statUpgradeDatasDic.TryGetValue(id, out StatUpgradeData stat))
            return Mathf.Max(0, stat.upgradeLevel);

        if (DataManager.Instance.characterSkillUpgradeDatasDic.TryGetValue(
                id,
                out CharacterSkillUpgradeData characterSkill))
            return Mathf.Max(0, characterSkill.upgradeLevel);

        return 0;
    }

    /// <summary>증강의 쿨타임을 시작하고 표시 정보를 갱신한다.</summary>
    public void BeginUpgradeCooldown(int id, float duration)
    {
        duration = Mathf.Max(0f, duration);
        upgradeCooldownDurations[id] = duration;
        upgradeCooldownEnds[id] = Time.time + duration;
        EnsureUpgradeCooldownHud()?.Track(id);
    }

    /// <summary>증강의 남은 쿨타임과 전체 시간을 조회한다.</summary>
    public bool TryGetUpgradeCooldown(int id, out float remaining, out float duration)
    {
        remaining = 0f;
        duration = 0f;

        if (!upgradeCooldownEnds.TryGetValue(id, out float endTime) ||
            !upgradeCooldownDurations.TryGetValue(id, out duration))
            return false;

        remaining = Mathf.Max(0f, endTime - Time.time);
        return remaining > 0f && duration > 0f;
    }

    /// <summary>IsPersistentUpgrade 동작을 처리한다.</summary>
    public bool IsPersistentUpgrade(int id)
    {
        if (DataManager.Instance == null ||
            !DataManager.Instance.upgradeDatasDic.TryGetValue(id, out UpgradeData data))
            return false;

        if (data.coolTime == null || data.coolTime.Length == 0)
            return true;

        int level = Mathf.Clamp(data.UpgradeLevel, 0, data.coolTime.Length - 1);
        return data.coolTime[level] <= 0f;
    }

    /// <summary>UpgradeIcon 관련 값을 조회한다.</summary>
    public Sprite GetUpgradeIcon(int id)
    {
        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.skillUpgradeIconDic.TryGetValue(id, out Sprite icon))
            return icon;

        return DataManager.Instance.upgradeDatasDic.TryGetValue(id, out UpgradeData data)
            ? data.sprite
            : null;
    }

    /// <summary>EnsureUpgradeCooldownHud 동작을 처리한다.</summary>
    private UpgradeCooldownHud EnsureUpgradeCooldownHud()
    {
        if (upgradeCooldownHud == null)
            upgradeCooldownHud = UpgradeCooldownHud.FindInScene();

        if (upgradeCooldownHud != null)
            upgradeCooldownHud.Initialize(this);

        return upgradeCooldownHud;
    }

    /// <summary>Lightning 동작을 처리한다.</summary>
    public void Lightning(int id)
    {
        if (upgradeCor.ContainsKey(id))
            return;

        StartSkillCoroutine(id, detectUpgrade.DetectUpgradeCor(id));
    }

    /// <summary>Fireball 동작을 처리한다.</summary>
    public void Fireball(int id)
    {
        if (upgradeCor.ContainsKey(id))
            return;

        StartSkillCoroutine(id, detectUpgrade.DetectUpgradeCor(id));
    }

    /// <summary>Burn 동작을 처리한다.</summary>
    public void Burn(int id)
    {
        if (upgradeCor.ContainsKey(id))
            return;

        StartSkillCoroutine(id, BurnCor(id));
    }

    /// <summary>Poison 동작을 처리한다.</summary>
    public void Poison(int id)
    {
        if (upgradeCor.ContainsKey(id))
            return;

        StartSkillCoroutine(id, detectUpgrade.DetectUpgradeCor(id));
    }

    /// <summary>Snail 동작을 처리한다.</summary>
    public void Snail(int id)
    {
        if (upgradeCor.ContainsKey(id))
            return;

        StartSnailGroundLoop();
        StartSkillCoroutine(id, SnailCor(id));
    }

    /// <summary>StartSnailGroundLoop 동작을 처리한다.</summary>
    private void StartSnailGroundLoop()
    {
        if (snailGroundLoopSource == null || !snailGroundLoopSource.isPlaying)
            snailGroundLoopSource = SoundManager.Instance?.Play(SoundIds.SnailGroundLoop, loop: true);
    }

    /// <summary>StopSnailGroundLoop 동작을 처리한다.</summary>
    private void StopSnailGroundLoop()
    {
        SoundManager.Instance?.Stop(snailGroundLoopSource);
        snailGroundLoopSource = null;
    }

    /// <summary>BeginShurikenSpinSound 동작을 처리한다.</summary>
    private void BeginShurikenSpinSound(int id)
    {
        activeShurikenSpinIds.Add(id);
        if (shurikenSpinSoundSource == null || !shurikenSpinSoundSource.isPlaying)
        {
            shurikenSpinSoundSource = SoundManager.Instance?.Play(
                SoundIds.ShurikenSpin,
                loop: true);
        }
    }

    /// <summary>StopShurikenSpinSound 동작을 처리한다.</summary>
    private void StopShurikenSpinSound(int id)
    {
        activeShurikenSpinIds.Remove(id);
        if (activeShurikenSpinIds.Count == 0)
        {
            SoundManager.Instance?.Stop(shurikenSpinSoundSource);
            shurikenSpinSoundSource = null;
        }
    }

    /// <summary>StopAllShurikenSpinSounds 동작을 처리한다.</summary>
    private void StopAllShurikenSpinSounds()
    {
        activeShurikenSpinIds.Clear();
        SoundManager.Instance?.Stop(shurikenSpinSoundSource);
        shurikenSpinSoundSource = null;
    }

    /// <summary>Shuriken 동작을 처리한다.</summary>
    public void Shuriken(int id)
    {
        if (upgradeCor.ContainsKey(id))
            return;

        StartSkillCoroutine(id, ShurikenCor(id));
    }

    /// <summary>Missile 동작을 처리한다.</summary>
    public void Missile(int id)
    {
        if (upgradeCor.ContainsKey(id))
            return;

        StartSkillCoroutine(id, MissileCor(id));
    }

    /// <summary>Mold 동작을 처리한다.</summary>
    public void Mold(int id)
    {
        if (upgradeCor.ContainsKey(id))
            return;

        StartSkillCoroutine(id, MoldCor(id));
    }

    /// <summary>FireStorm 동작을 처리한다.</summary>
    public void FireStorm(int id)
    {
        Fireball(id);
    }

    /// <summary>SporeWarhead 동작을 처리한다.</summary>
    public void SporeWarhead(int id)
    {
        Missile(id);
    }

    /// <summary>LightningShuriken 동작을 처리한다.</summary>
    public void LightningShuriken(int id)
    {
        Shuriken(id);
    }

    /// <summary>Heatwave 동작을 처리한다.</summary>
    public void Heatwave(int id)
    {
        Fireball(id);
    }

    /// <summary>LavaField 동작을 처리한다.</summary>
    public void LavaField(int id)
    {
        Snail(id);
    }

    /// <summary>RottingSpore 동작을 처리한다.</summary>
    public void RottingSpore(int id)
    {
        Mold(id);
    }

    /// <summary>BurnCor 동작을 처리한다.</summary>
    private IEnumerator BurnCor(int id)
    {
        int level =
            DataManager.Instance
                .upgradeDatasDic[id]
                .UpgradeLevel;

        GameManager.Instance.burnPrefabId = id;

        GameManager.Instance.burnChance =
            DataManager.Instance
                .upgradeDatasDic[id]
                .debuffChance[level];

        yield return null;
    }

    /// <summary>SnailCor 동작을 처리한다.</summary>
    private IEnumerator SnailCor(int id)
    {
        while (true)
        {
            int level =
                DataManager.Instance
                    .upgradeDatasDic[id]
                    .UpgradeLevel;

            PoolManager.Instance.Spawn(
                id,
                playerCollider.bounds.center
            );

            float cooldown = DataManager.Instance.upgradeDatasDic[id].coolTime[level];
            BeginUpgradeCooldown(id, cooldown);
            /// <summary>WaitForSeconds 동작을 처리한다.</summary>
            yield return new WaitForSeconds(cooldown);
        }
    }

    /// <summary>ShurikenCor 동작을 처리한다.</summary>
    private IEnumerator ShurikenCor(int id)
    {
        while (true)
        {
            int level =
                DataManager.Instance
                    .upgradeDatasDic[id]
                    .UpgradeLevel;

            int count =
                DataManager.Instance
                    .upgradeDatasDic[id]
                    .count[level];

            for (int i = 0; i < count; i++)
            {
                if (orbit.orbitList.Count < count)
                {
                    orbit.AddOrbit(id);
                }
            }

            BeginShurikenSpinSound(id);
            /// <summary>StartCoroutine 동작을 처리한다.</summary>
            yield return StartCoroutine(
                orbit.RotateOrbits(id)
            );
            StopShurikenSpinSound(id);

            float cooldown = DataManager.Instance.upgradeDatasDic[id].coolTime[level];
            BeginUpgradeCooldown(id, cooldown);
            /// <summary>WaitForSeconds 동작을 처리한다.</summary>
            yield return new WaitForSeconds(cooldown);
        }
    }

    /// <summary>MissileCor 동작을 처리한다.</summary>
    private IEnumerator MissileCor(int id)
    {
        while (true)
        {
            int level =
                DataManager.Instance
                    .upgradeDatasDic[id]
                    .UpgradeLevel;

            int count =
                DataManager.Instance
                    .upgradeDatasDic[id]
                    .count[level];

            float detectionRange =
                DataManager.Instance
                    .upgradeDatasDic[id]
                    .detectionRange[level];

            // 적이 나타날 때까지 대기
            /// <summary>WaitUntil 동작을 처리한다.</summary>
            yield return new WaitUntil(() =>
                detectUpgrade.HasEnemyInRange(detectionRange)
            );

            // 적 탐색
            List<Collider2D> enemies =
                detectUpgrade.DetectEnemies(detectionRange);

            if (enemies.Count == 0)
                continue;

            // 가장 가까운 적
            Transform target =
                detectUpgrade.FindClosestEnemy(enemies);

            if (target == null)
                continue;

            // 발사 순간의 위치 저장
            Vector3 targetPosition =
                target.position;

            // 같은 위치를 향해 count만큼 발사
            for (int i = 0; i < count; i++)
            {
                // 발사 전에 타겟이 사라졌는지 확인
                if (target == null)
                    break;

                GameObject missile =
                    PoolManager.Instance.Spawn(
                        id,
                        player.transform.position
                    );

                if (missile == null)
                    continue;

                Missle missle =
                    missile.GetComponent<Missle>();

                if (missle == null)
                    continue;

                missle.Attack(
                    id,
                    targetPosition,
                    target.gameObject,
                    false,
                    player.transform.position,
                    true
                );

                /// <summary>WaitForSeconds 동작을 처리한다.</summary>
                yield return new WaitForSeconds(0.1f);
            }

            float coolTime =
                DataManager.Instance
                    .upgradeDatasDic[id]
                    .coolTime[level];

            BeginUpgradeCooldown(id, coolTime);
            /// <summary>WaitForSeconds 동작을 처리한다.</summary>
            yield return new WaitForSeconds(coolTime);
        }
    }

    /// <summary>MoldCor 동작을 처리한다.</summary>
    private IEnumerator MoldCor(int id)
    {
        int level =
            DataManager.Instance
                .upgradeDatasDic[id]
                .UpgradeLevel;

        GameManager.Instance.moldPrefabId = id;

        GameManager.Instance.moldChance =
            DataManager.Instance
                .upgradeDatasDic[id]
                .debuffChance[level];

        yield return null;
    }

    // =========================================================
    // 스탯 업그레이드
    // 보너스 변수에 현재 레벨의 % 를 넣고, 원본 + 보너스로 현재 스탯을 덮어씀
    // =========================================================

    // 플레이어 스탯(치명타 확률/피해, 공격력, 체력, 방어력)을 원본 기준으로 재계산해서 덮어씀
    /// <summary>기준 능력치와 증강 보너스로 플레이어 능력치를 다시 계산한다.</summary>
    private void ApplyPlayerStats()
    {
        foreach (var pair in DataManager.Instance.playerDatasDic)
        {
            if (!originalPlayerStats.TryGetValue(pair.Key, out PlayerStatOrigin origin))
                continue;

            PlayerData p = pair.Value;

            var permanent = CharacterProgression.Bonuses(pair.Key, CharacterProgression.Level(pair.Key));
            p.criticalChance = Mathf.Clamp((int)((origin.criticalChance + permanent.criticalChance) * (1f + critChanceBonus / 100f)), 0, 100);
            p.criticalDamage = (origin.criticalDamage + permanent.criticalDamage) * (1f + critDamageBonus / 100f);
            p.damage = origin.damage + CharacterProgression.AttackBonus(pair.Key) + attackPowerBonus;
            p.hp = Mathf.RoundToInt((origin.hp + permanent.hp) * (1f + maxHpBonus / 100f));
            p.defense = Mathf.RoundToInt(origin.defense + permanent.defense + defenseBonus);
            p.hpRegenRate = origin.hpRegenRate + permanent.hpRegenRate;
            p.speed = origin.speed + permanent.speed;
        }
    }

    /// <summary>RefreshPermanentStats 동작을 처리한다.</summary>
    public void RefreshPermanentStats()
    {
        if (DataManager.Instance != null && originalPlayerStats.Count > 0)
            ApplyPlayerStats();
        RefreshBasicAttackTimings();
    }

    /// <summary>RefreshBasicAttackTimings 동작을 처리한다.</summary>
    private void RefreshBasicAttackTimings()
    {
        if (DataManager.Instance == null) return;
        foreach (var skill in DataManager.Instance.skillDatasDic.Values)
        {
            int characterId = skill.skillId == 200 ? 1 : skill.skillId == 201 ? 2 : 0;
            if (characterId == 0 || !originalSkillCoolTimes.TryGetValue(skill, out float originalCooldown) ||
                !originalMovementLockTimes.TryGetValue(skill, out float originalLock)) continue;
            skill.basicAttackCooldown = Mathf.Max(0.01f,
                skill.skillCoolTime / CharacterProgression.BasicAttackSpeed(characterId));
            float ratio = originalCooldown > 0f ? skill.basicAttackCooldown / originalCooldown : 1f;
            skill.movementLockTime = Mathf.Max(0f, originalLock * ratio);
            skill.basicAttackAnimationSpeed = originalCooldown > 0f
                ? originalCooldown / skill.basicAttackCooldown : 1f;
        }
    }

    public bool HasPlayerStatBaseline => originalPlayerStats.Count > 0;

    // 스킬 쿨타임을 원본 기준으로 재계산해서 덮어씀
    /// <summary>스킬의 원래 쿨타임에 감소 보너스를 적용한다.</summary>
    private void ApplySkillCoolTimes()
    {
        foreach (SkillData skill in DataManager.Instance.skillDatasDic.Values)
        {
            if (!originalSkillCoolTimes.TryGetValue(skill, out float origin))
                continue;

            skill.skillCoolTime = origin * (1f - skillCooldownBonus / 100f);
        }
        RefreshBasicAttackTimings();
    }

    // Passive 업그레이드 쿨타임을 원본 기준으로 재계산해서 덮어씀
    /// <summary>증강의 원래 쿨타임에 감소 보너스를 적용한다.</summary>
    private void ApplyPassiveCoolTimes()
    {
        foreach (var pair in DataManager.Instance.upgradeDatasDic)
        {
            if (pair.Value.type != "Skill")
                continue;

            if (!originalPassiveCoolTimes.TryGetValue(pair.Key, out float[] origin))
                continue;

            for (int i = 0; i < origin.Length; i++)
            {
                pair.Value.coolTime[i] = origin[i] * (1f - passiveCooldownBonus / 100f);
            }
        }
    }

    /// <summary>CriticalInstinct 동작을 처리한다.</summary>
    public void CriticalInstinct(int id)
    {
        var data = DataManager.Instance.statUpgradeDatasDic[id];
        critChanceBonus = data.critChance[data.UpgradeLevel];

        ApplyPlayerStats();
    }

    /// <summary>CriticalForce 동작을 처리한다.</summary>
    public void CriticalForce(int id)
    {
        var data = DataManager.Instance.statUpgradeDatasDic[id];
        critDamageBonus = data.critDamage[data.UpgradeLevel];

        ApplyPlayerStats();
    }

    /// <summary>PowerBoost 동작을 처리한다.</summary>
    public void PowerBoost(int id)
    {
        var data = DataManager.Instance.statUpgradeDatasDic[id];
        attackPowerBonus = data.attackPower[data.UpgradeLevel];

        ApplyPlayerStats();
    }

    /// <summary>Vitality 동작을 처리한다.</summary>
    public void Vitality(int id)
    {
        var data = DataManager.Instance.statUpgradeDatasDic[id];
        maxHpBonus = data.maxHp[data.UpgradeLevel];

        // 현재 캐릭터 기준으로 증가량 계산 (모든 캐릭터에 반복 적용되던 문제 방지)
        int character = CharacterManager.Instance.currentCharacter;
        int oldHp = 0;
        bool hasCharacter =
            DataManager.Instance.playerDatasDic.TryGetValue(character, out PlayerData current);

        if (hasCharacter)
            oldHp = current.hp;

        ApplyPlayerStats();

        if (!hasCharacter)
            return;

        int increasedHp = current.hp - oldHp;
        if (increasedHp == 0)
            return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
            return;

        PlayerCombat playerCombat = playerObj.GetComponent<PlayerCombat>();
        if (playerCombat == null)
            return;

        playerCombat.maxHp += increasedHp;
        playerCombat.hp += increasedHp;
        playerCombat.UpdateHpUI();
    }

    /// <summary>QuickCast 동작을 처리한다.</summary>
    public void QuickCast(int id)
    {
        var data = DataManager.Instance.statUpgradeDatasDic[id];
        skillCooldownBonus = data.skillCooldown[data.UpgradeLevel];

        // Skill 쿨타임만 감소
        ApplySkillCoolTimes();
    }

    /// <summary>RapidCycle 동작을 처리한다.</summary>
    public void RapidCycle(int id)
    {
        var data = DataManager.Instance.statUpgradeDatasDic[id];
        passiveCooldownBonus = data.passiveCooldown[data.UpgradeLevel];

        // Passive Upgrade 쿨타임만 감소
        ApplyPassiveCoolTimes();
    }

    /// <summary>IronWall 동작을 처리한다.</summary>
    public void IronWall(int id)
    {
        var data = DataManager.Instance.statUpgradeDatasDic[id];
        defenseBonus = data.defense[data.UpgradeLevel];

        ApplyPlayerStats();
    }
    
    /// <summary>BonusesAndApply 관련 상태를 설정한다.</summary>
    private void SetBonusesAndApply(
        float critChance, float critDamage, float attackPower,
        float maxHp, float defense, float skillCd, float passiveCd)
    {
        if (originalPlayerStats.Count == 0)
        {
            Debug.LogWarning("[UpgradeManager] 원본 스냅샷이 없음 - UpgradesInit()이 먼저 실행되어야 함");
            return;
        }

        critChanceBonus = critChance;
        critDamageBonus = critDamage;
        attackPowerBonus = attackPower;
        maxHpBonus = maxHp;
        defenseBonus = defense;
        skillCooldownBonus = skillCd;
        passiveCooldownBonus = passiveCd;

        int character = CharacterManager.Instance.currentCharacter;
        bool has = DataManager.Instance.playerDatasDic.TryGetValue(character, out PlayerData cur);
        int oldHp = has ? cur.hp : 0;

        ApplyPlayerStats();
        ApplySkillCoolTimes();
        ApplyPassiveCoolTimes();

        // 최대 체력 변화량을 PlayerCombat에 반영
        if (has && cur.hp != oldHp)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            PlayerCombat pc = playerObj != null ? playerObj.GetComponent<PlayerCombat>() : null;
            if (pc != null)
            {
                int delta = cur.hp - oldHp;
                pc.maxHp += delta;
                pc.hp = Mathf.Clamp(pc.hp + delta, 1, pc.maxHp);
                pc.UpdateHpUI();
            }
        }
    }
    
    /// <summary>SkillAnimationSpeed 관련 값을 조회한다.</summary>
    public float GetSkillAnimationSpeed(int skillId)
    {
        if (!DataManager.Instance.skillDatasDic.TryGetValue(skillId, out SkillData skill))
            return 1f;

        if (!originalSkillCoolTimes.TryGetValue(skill, out float originalCoolTime))
            return 1f;

        if (skill.skillCoolTime <= 0f)
            return 1f;

        return originalCoolTime / skill.skillCoolTime;
    }
}

/// <summary>획득한 증강의 아이콘과 레벨을 표시한다.</summary>
public sealed class CollectedUpgradeIconUI : MonoBehaviour
{
    private TextMeshProUGUI levelText;
    private int upgradeId;
    private int displayedLevel = -1;
    private UpgradeManager subscribedManager;

    /// <summary>활성화될 때 필요한 이벤트와 동작을 시작한다.</summary>
    private void OnEnable()
    {
        UpgradeManager.InstanceChanged += BindManager;
        BindManager(UpgradeManager.Instance);
    }

    /// <summary>비활성화될 때 이벤트와 진행 중인 동작을 정리한다.</summary>
    private void OnDisable()
    {
        UpgradeManager.InstanceChanged -= BindManager;
        if (subscribedManager != null)
            subscribedManager.UpgradesChanged -= RefreshVisuals;
        subscribedManager = null;
    }

    /// <summary>BindManager 동작을 처리한다.</summary>
    private void BindManager(UpgradeManager manager)
    {
        if (subscribedManager != null)
            subscribedManager.UpgradesChanged -= RefreshVisuals;
        subscribedManager = manager;
        if (subscribedManager != null)
            subscribedManager.UpgradesChanged += RefreshVisuals;
        RefreshVisuals();
    }

    /// <summary>Attach 동작을 처리한다.</summary>
    public static CollectedUpgradeIconUI Attach(UnityEngine.UI.Image target, int id)
    {
        if (target == null)
            return null;

        CollectedUpgradeIconUI view = target.GetComponent<CollectedUpgradeIconUI>();
        if (view == null)
            view = target.gameObject.AddComponent<CollectedUpgradeIconUI>();

        view.BuildIfNeeded(target);
        UpgradeHudRendering.ApplyCollectedIconStyle(target, id);
        view.Bind(id);
        return view;
    }

    /// <summary>BuildIfNeeded 동작을 처리한다.</summary>
    private void BuildIfNeeded(UnityEngine.UI.Image target)
    {
        target.preserveAspect = true;

        RemoveOldCooldownChild("CooldownText");
        RemoveOldCooldownChild("CooldownFill");

        if (levelText == null)
            levelText = CreateText("LevelText", 19f, TextAlignmentOptions.BottomRight,
                /// <summary>Vector2 동작을 처리한다.</summary>
                new Vector2(0f, 0f), new Vector2(1f, 0.38f), new Vector2(4f, 1f), new Vector2(-5f, -1f));
        levelText.transform.SetAsLastSibling();
    }

    /// <summary>Text 항목을 생성한다.</summary>
    private TextMeshProUGUI CreateText(
        string objectName,
        float fontSize,
        TextAlignmentOptions alignment,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(transform, false);
        RectTransform rect = (RectTransform)textObject.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.color = Color.white;
        UpgradeHudRendering.ApplyTextStyle(text, UpgradeHudRendering.TextStyle.CollectedLevel);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    /// <summary>표시할 대상이나 증강 정보를 연결한다.</summary>
    public void Bind(int id)
    {
        upgradeId = id;
        displayedLevel = -1;
        RefreshVisuals();
    }

    /// <summary>현재 연결된 표시 상태를 초기화한다.</summary>
    public void Clear()
    {
        upgradeId = 0;
        displayedLevel = -1;
        if (levelText != null)
            levelText.text = string.Empty;
    }

    /// <summary>현재 증강 상태에 맞게 시각 표시를 갱신한다.</summary>
    private void RefreshVisuals()
    {
        if (levelText == null || upgradeId <= 0 || UpgradeManager.Instance == null)
            return;

        int level = UpgradeManager.Instance.GetUpgradeDisplayLevel(upgradeId);
        if (displayedLevel == level)
            return;
        displayedLevel = level;
        levelText.text = $"Lv.{level}";
    }

    /// <summary>RemoveOldCooldownChild 동작을 처리한다.</summary>
    private void RemoveOldCooldownChild(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
            return;

        child.gameObject.SetActive(false);
        Destroy(child.gameObject);
    }
}
