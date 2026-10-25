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

public static class BasicAttackChainUtility
{
    public static void Apply(
        Vector2 origin,
        EnemyCombat primaryTarget,
        int damage,
        int chainCount,
        float chainRange)
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
            enemy.TakeDamage(damage, enemy.transform.position);
    }
}

/// <summary>Applies character basic attack upgrades to a pooled attack instance.</summary>
public sealed class BasicAttackInstanceState : MonoBehaviour
{
    private Vector3 baseScale;
    private int baseDamage;
    private SkillInfo skillInfo;
    private bool initialized;

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        baseScale = transform.localScale;
        skillInfo = GetComponent<SkillInfo>();
        baseDamage = skillInfo != null ? skillInfo.skillDamage : 0;
    }

    public void Apply(BasicAttackModifiers modifiers)
    {
        Initialize();
        transform.localScale = baseScale * modifiers.rangeMultiplier;

        if (skillInfo != null)
        {
            skillInfo.skillDamage = Mathf.Max(
                1,
                Mathf.RoundToInt(baseDamage * modifiers.damageMultiplier));
        }

        BasicAttackChainRelay relay = GetComponent<BasicAttackChainRelay>();
        if (relay == null)
            relay = gameObject.AddComponent<BasicAttackChainRelay>();

        relay.Configure(
            skillInfo != null ? skillInfo.skillDamage : 0,
            modifiers.chainCount,
            modifiers.chainRange);
    }

    private void OnDisable()
    {
        if (!initialized)
            return;

        transform.localScale = baseScale;
        if (skillInfo != null)
            skillInfo.skillDamage = baseDamage;
    }
}

public sealed class BasicAttackChainRelay : MonoBehaviour
{
    private readonly HashSet<int> triggeredTargets = new HashSet<int>();
    private int damage;
    private int chainCount;
    private float chainRange;

    public void Configure(int hitDamage, int count, float range)
    {
        damage = hitDamage;
        chainCount = Mathf.Max(0, count);
        chainRange = Mathf.Max(0f, range);
        triggeredTargets.Clear();
        enabled = chainCount > 0 && chainRange > 0f && damage > 0;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyCombat primary = other.GetComponentInParent<EnemyCombat>();
        if (primary == null || !triggeredTargets.Add(primary.GetInstanceID()))
            return;

        BasicAttackChainUtility.Apply(
            primary.transform.position,
            primary,
            damage,
            chainCount,
            chainRange);
    }
}

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;
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
    [SerializeField]
    private UpgradeCooldownHud upgradeCooldownHud;

    private GameObject player;
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
        public int damage;
        public int hp;
        public int defense;
    }

    private readonly Dictionary<int, PlayerStatOrigin> originalPlayerStats = new Dictionary<int, PlayerStatOrigin>();
    private readonly Dictionary<SkillData, float> originalSkillCoolTimes = new Dictionary<SkillData, float>();
    private readonly Dictionary<int, float[]> originalPassiveCoolTimes = new Dictionary<int, float[]>();

    // 현재 적용 중인 보너스 (%)
    private float critChanceBonus;
    private float critDamageBonus;
    private float attackPowerBonus;
    private float maxHpBonus;
    private float defenseBonus;
    private float skillCooldownBonus;
    private float passiveCooldownBonus;
    private AudioClip snailGroundLoopClip;
    private AudioSource snailGroundLoopSource;

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else if (Instance != this)
        {
            // Rebind references to controls and skills from the reloaded scene.
            Instance.upgradesInfo = upgradesInfo;
            Instance.skillUpgradeIcons = skillUpgradeIcons;
            Instance.statUpgradeIcons = statUpgradeIcons;
            Instance.detectUpgrade = detectUpgrade;
            Instance.orbit = orbit;
            Destroy(gameObject);
            return;
        }

        player = GameObject.FindGameObjectWithTag("Player");
        playerCollider = player.GetComponent<BoxCollider2D>();
        EnsureUpgradeCooldownHud();
        snailGroundLoopClip = Resources.Load<AudioClip>("Audio/SFX/SnailGroundLoop");
    }

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
    }

    public void StopRun()
    {
        StopSnailGroundLoop();
        StopAllCoroutines();
        upgradeCor.Clear();
        upgradeCooldownEnds.Clear();
        upgradeCooldownDurations.Clear();

        ClearCollectedUpgradeIcons(skillUpgradeIcons);
        ClearCollectedUpgradeIcons(statUpgradeIcons);
        if (upgradeCooldownHud != null)
            upgradeCooldownHud.ClearAll();
    }

    private static void ClearCollectedUpgradeIcons(Image[] icons)
    {
        if (icons == null)
            return;

        foreach (Image icon in icons)
        {
            if (icon == null)
                continue;

            CollectedUpgradeIconUI view = icon.GetComponent<CollectedUpgradeIconUI>();
            if (view != null)
                view.Clear();
        }
    }

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
    }

    // 원본 스탯 저장 + 보너스 초기화
    private void SnapshotOriginalStats()
    {
        var dm = DataManager.Instance;

        originalPlayerStats.Clear();
        foreach (var pair in dm.playerDatasDic)
        {
            PlayerData p = pair.Value;

            originalPlayerStats[pair.Key] = new PlayerStatOrigin
            {
                criticalChance = p.criticalChance,
                criticalDamage = p.criticalDamage,
                damage = p.damage,
                hp = p.hp,
                defense = p.defense
            };
        }

        originalSkillCoolTimes.Clear();
        foreach (SkillData skill in dm.skillDatasDic.Values)
        {
            originalSkillCoolTimes[skill] = skill.skillCoolTime;
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
    private static string GetUpgradeDescriptionKey(string baseKey, int ownedLevel, string firstDescription = null)
    {
        string levelKey = baseKey + "_" + (Mathf.Clamp(ownedLevel, 0, 4) + 1);
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.HasTranslation(levelKey))
            return levelKey;

        return !string.IsNullOrEmpty(firstDescription) ? firstDescription : baseKey;
    }

    // 스킬/스탯 상관없이 현재 UpgradeLevel 반환
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
            info.upgradeNameLo.key = v.name;
            info.upgradeNameLo.ForceUpdateText();
        }

        if (info.upgradeImage != null)
            info.upgradeImage.sprite = v.sprite;

        if (info.upgradeDesLo != null)
        {
            info.upgradeDesLo.key = v.description;
            info.upgradeDesLo.SetArguments(GetDescriptionArguments(id));
            info.upgradeDesLo.ForceUpdateText();
        }
    }

    private object[] GetDescriptionArguments(int id)
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
                    primaryValue = ValueAt(skill.debuffChance, level);
                    break;
                case "Snail":
                    primaryValue = Mathf.RoundToInt((1f - ValueAt(skill.slow, level)) * 100f);
                    break;
                default:
                    primaryValue = ValueAt(skill.count, level);
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
            // 카드에는 현재 보유 레벨이 아니라 이번 선택으로 적용될 단계의 값을 표시한다.
            int statLevel = Mathf.Clamp(stat.upgradeLevel, 0, 4);
            float value = stat.function switch
            {
                "CriticalInstinct" => ValueAt(stat.critChance, statLevel),
                "CriticalForce" => ValueAt(stat.critDamage, statLevel),
                "PowerBoost" => ValueAt(stat.attackPower, statLevel),
                "Vitality" => ValueAt(stat.maxHp, statLevel),
                "QuickCast" => ValueAt(stat.skillCooldown, statLevel),
                "RapidCycle" => ValueAt(stat.passiveCooldown, statLevel),
                "IronWall" => ValueAt(stat.defense, statLevel),
                _ => 0f
            };
            return new object[] { value % 1f == 0f ? (object)(int)value : value };
        }

        if (dm.characterSkillUpgradeDatasDic.TryGetValue(id, out var characterSkill))
        {
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

    private static int ValueAt(int[] values, int index)
    {
        return values != null && values.Length > 0 ? values[Mathf.Min(index, values.Length - 1)] : 0;
    }

    private static float ValueAt(float[] values, int index)
    {
        return values != null && values.Length > 0 ? values[Mathf.Min(index, values.Length - 1)] : 0f;
    }

    private static string ValueAt(string[] values, int index)
    {
        return values != null && values.Length > 0
            ? values[Mathf.Clamp(index, 0, values.Length - 1)]
            : string.Empty;
    }

    // =========================================================
    // 리뉴얼
    // =========================================================

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

    public void SkillUpgradesAdd(int id)
    {
        if (!skillUpgrades.Contains(id))
        {
            skillUpgrades.Add(id);

            if (DataManager.Instance.skillUpgradeIconDic.ContainsKey(id))
            {
                skillUpgradeIcons[skillUpgradeCount].sprite =
                    DataManager.Instance.skillUpgradeIconDic[id];

                // 보유 아이콘에는 레벨만 표시한다. 쿨타임은 좌하단 전용 HUD에서 표시한다.
                CollectedUpgradeIconUI.Attach(skillUpgradeIcons[skillUpgradeCount], id);

                skillUpgradeCount++;
            }
        }

        // 내부 레벨 증가
        DataManager.Instance.upgradeDatasDic[id].upgradeLevel++;

        if (DataManager.Instance.upgradeDatasDic[id].UpgradeLevel >= 4)
        {
            upgrades.Remove(id);
        }

        upgradeActions[id](id);
        EnsureUpgradeCooldownHud()?.Track(id);
        upgradeSelect?.Invoke();
    }

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
        upgradeSelect?.Invoke();
    }

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

        upgradeSelect?.Invoke();
    }

    public BasicAttackModifiers GetBasicAttackModifiers(int characterId)
    {
        BasicAttackModifiers result = BasicAttackModifiers.Default;

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

    public void BeginUpgradeCooldown(int id, float duration)
    {
        duration = Mathf.Max(0f, duration);
        upgradeCooldownDurations[id] = duration;
        upgradeCooldownEnds[id] = Time.time + duration;
        EnsureUpgradeCooldownHud()?.Track(id);
    }

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

    private UpgradeCooldownHud EnsureUpgradeCooldownHud()
    {
        if (upgradeCooldownHud == null)
            upgradeCooldownHud = UpgradeCooldownHud.FindInScene();

        if (upgradeCooldownHud != null)
            upgradeCooldownHud.Initialize(this);

        return upgradeCooldownHud;
    }

    public void Lightning(int id)
    {
        if (upgradeCor.ContainsKey(id))
            return;

        StartSkillCoroutine(id, LightningCor(id));
    }

    public void Fireball(int id)
    {
        if (upgradeCor.ContainsKey(id))
            return;

        StartSkillCoroutine(id, detectUpgrade.DetectUpgradeCor(id));
    }

    public void Burn(int id)
    {
        if (upgradeCor.ContainsKey(id))
            return;

        StartSkillCoroutine(id, BurnCor(id));
    }

    public void Poison(int id)
    {
        if (upgradeCor.ContainsKey(id))
            return;

        StartSkillCoroutine(id, detectUpgrade.DetectUpgradeCor(id));
    }

    public void Snail(int id)
    {
        if (upgradeCor.ContainsKey(id))
            return;

        StartSnailGroundLoop();
        StartSkillCoroutine(id, SnailCor(id));
    }

    private void StartSnailGroundLoop()
    {
        if (snailGroundLoopClip == null)
            return;

        if (snailGroundLoopSource == null)
        {
            GameObject audioObject = new GameObject("Snail Ground Loop Audio");
            audioObject.transform.SetParent(transform, false);

            snailGroundLoopSource = audioObject.AddComponent<AudioSource>();
            snailGroundLoopSource.clip = snailGroundLoopClip;
            snailGroundLoopSource.loop = true;
            snailGroundLoopSource.playOnAwake = false;
            snailGroundLoopSource.spatialBlend = 0f;
            audioObject.AddComponent<SfxAudioSourceVolume>();
        }

        if (!snailGroundLoopSource.isPlaying)
            snailGroundLoopSource.Play();
    }

    private void StopSnailGroundLoop()
    {
        if (snailGroundLoopSource != null)
            snailGroundLoopSource.Stop();
    }

    public void Shuriken(int id)
    {
        if (upgradeCor.ContainsKey(id))
            return;

        StartSkillCoroutine(id, ShurikenCor(id));
    }

    public void Missile(int id)
    {
        if (upgradeCor.ContainsKey(id))
            return;

        StartSkillCoroutine(id, MissileCor(id));
    }

    public void Mold(int id)
    {
        if (upgradeCor.ContainsKey(id))
            return;

        StartSkillCoroutine(id, MoldCor(id));
    }

    public void FireStorm(int id)
    {
        Fireball(id);
    }

    public void SporeWarhead(int id)
    {
        Missile(id);
    }

    public void LightningShuriken(int id)
    {
        Shuriken(id);
    }

    public void Heatwave(int id)
    {
        Fireball(id);
    }

    public void LavaField(int id)
    {
        Snail(id);
    }

    public void RottingSpore(int id)
    {
        Mold(id);
    }

    private IEnumerator LightningCor(int id)
    {
        while (true)
        {
            int level =
                DataManager.Instance
                    .upgradeDatasDic[id]
                    .UpgradeLevel;

            for (int i = 0;
                 i < DataManager.Instance.upgradeDatasDic[id].count[level];
                 i++)
            {
                PoolManager.Instance.Spawn(
                    id,
                    playerCollider.bounds.center
                );
            }

            float cooldown = DataManager.Instance.upgradeDatasDic[id].coolTime[level];
            BeginUpgradeCooldown(id, cooldown);
            yield return new WaitForSeconds(cooldown);
        }
    }

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

            Debug.Log(
                DataManager.Instance
                    .upgradeDatasDic[id]
                    .coolTime[level]
            );

            float cooldown = DataManager.Instance.upgradeDatasDic[id].coolTime[level];
            BeginUpgradeCooldown(id, cooldown);
            yield return new WaitForSeconds(cooldown);
        }
    }

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

            yield return StartCoroutine(
                orbit.RotateOrbits(id)
            );

            float cooldown = DataManager.Instance.upgradeDatasDic[id].coolTime[level];
            BeginUpgradeCooldown(id, cooldown);
            yield return new WaitForSeconds(cooldown);
        }
    }

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

                yield return new WaitForSeconds(0.1f);
            }

            float coolTime =
                DataManager.Instance
                    .upgradeDatasDic[id]
                    .coolTime[level];

            BeginUpgradeCooldown(id, coolTime);
            yield return new WaitForSeconds(coolTime);
        }
    }

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
    private void ApplyPlayerStats()
    {
        foreach (var pair in DataManager.Instance.playerDatasDic)
        {
            if (!originalPlayerStats.TryGetValue(pair.Key, out PlayerStatOrigin origin))
                continue;

            PlayerData p = pair.Value;

            p.criticalChance = (int)(origin.criticalChance * (1f + critChanceBonus / 100f));
            p.criticalDamage = origin.criticalDamage * (1f + critDamageBonus / 100f);
            p.damage = (int)(origin.damage * (1f + attackPowerBonus / 100f));
            p.hp = (int)(origin.hp * (1f + maxHpBonus / 100f));
            p.defense = (int)(origin.defense * (1f + defenseBonus / 100f));
        }
    }

    // 스킬 쿨타임을 원본 기준으로 재계산해서 덮어씀
    private void ApplySkillCoolTimes()
    {
        foreach (SkillData skill in DataManager.Instance.skillDatasDic.Values)
        {
            if (!originalSkillCoolTimes.TryGetValue(skill, out float origin))
                continue;

            skill.skillCoolTime = origin * (1f - skillCooldownBonus / 100f);
        }
    }

    // Passive 업그레이드 쿨타임을 원본 기준으로 재계산해서 덮어씀
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

    public void CriticalInstinct(int id)
    {
        var data = DataManager.Instance.statUpgradeDatasDic[id];
        critChanceBonus = data.critChance[data.UpgradeLevel];

        ApplyPlayerStats();
    }

    public void CriticalForce(int id)
    {
        var data = DataManager.Instance.statUpgradeDatasDic[id];
        critDamageBonus = data.critDamage[data.UpgradeLevel];

        ApplyPlayerStats();
    }

    public void PowerBoost(int id)
    {
        var data = DataManager.Instance.statUpgradeDatasDic[id];
        attackPowerBonus = data.attackPower[data.UpgradeLevel];

        ApplyPlayerStats();
    }

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

    public void QuickCast(int id)
    {
        var data = DataManager.Instance.statUpgradeDatasDic[id];
        skillCooldownBonus = data.skillCooldown[data.UpgradeLevel];

        // Skill 쿨타임만 감소
        ApplySkillCoolTimes();
    }

    public void RapidCycle(int id)
    {
        var data = DataManager.Instance.statUpgradeDatasDic[id];
        passiveCooldownBonus = data.passiveCooldown[data.UpgradeLevel];

        // Passive Upgrade 쿨타임만 감소
        ApplyPassiveCoolTimes();
    }

    public void IronWall(int id)
    {
        var data = DataManager.Instance.statUpgradeDatasDic[id];
        defenseBonus = data.defense[data.UpgradeLevel];

        ApplyPlayerStats();
    }
    
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

public sealed class CollectedUpgradeIconUI : MonoBehaviour
{
    private TextMeshProUGUI levelText;
    private int upgradeId;

    public static CollectedUpgradeIconUI Attach(UnityEngine.UI.Image target, int id)
    {
        if (target == null)
            return null;

        CollectedUpgradeIconUI view = target.GetComponent<CollectedUpgradeIconUI>();
        if (view == null)
            view = target.gameObject.AddComponent<CollectedUpgradeIconUI>();

        view.BuildIfNeeded(target);
        view.Bind(id);
        return view;
    }

    private void BuildIfNeeded(UnityEngine.UI.Image target)
    {
        target.preserveAspect = true;

        RemoveOldCooldownChild("CooldownFill");
        RemoveOldCooldownChild("CooldownText");

        if (levelText == null)
            levelText = CreateText("LevelText", 19f, TextAlignmentOptions.BottomRight,
                new Vector2(0f, 0f), new Vector2(1f, 0.38f), new Vector2(4f, 1f), new Vector2(-5f, -1f));
    }

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
        text.outlineColor = Color.black;
        text.outlineWidth = 0.28f;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    public void Bind(int id)
    {
        upgradeId = id;
        RefreshVisuals();
    }

    public void Clear()
    {
        upgradeId = 0;
        if (levelText != null)
            levelText.text = string.Empty;
    }

    private void Update()
    {
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        if (upgradeId <= 0 || UpgradeManager.Instance == null)
            return;

        levelText.text = $"Lv.{UpgradeManager.Instance.GetUpgradeDisplayLevel(upgradeId)}";
    }

    private void RemoveOldCooldownChild(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
            return;

        child.gameObject.SetActive(false);
        Destroy(child.gameObject);
    }
}
