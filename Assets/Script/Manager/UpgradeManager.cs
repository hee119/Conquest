using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;
    public List<int> skillUpgrades = new List<int>();
    public List<int> statUpgrades = new List<int>();
    public List<int> upgrades = new List<int>();
    public List<UpgradeUiInfo> upgradesInfo = new List<UpgradeUiInfo>();
    public List<int> renewableUpgrades = new List<int>();

    Dictionary<int, Action<int>> upgradeActions = new Dictionary<int, Action<int>>();
    public Dictionary<int, IEnumerator> upgradeCor = new Dictionary<int, IEnumerator>();

    private GameObject player;
    BoxCollider2D playerCollider;

    public Image[] skillUpgradeIcons;
    public Image[] statUpgradeIcons;

    public int skillUpgradeCount;
    public int statUpgradeCount;

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

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        player = GameObject.FindGameObjectWithTag("Player");
        playerCollider = player.GetComponent<BoxCollider2D>();
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
            v = new UpgradeView
            {
                id = u.id,
                name = u.name,
                sprite = u.sprite,
                type = u.type,
                upgradeLevel = u.upgradeLevel,
                description = u.description
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
                description = s.description
            };
            return true;
        }

        v = default;
        return false;
    }

    // 스킬/스탯 상관없이 현재 UpgradeLevel 반환
    private int GetLevel(int id)
    {
        var dm = DataManager.Instance;

        if (dm.upgradeDatasDic.TryGetValue(id, out var u))
            return u.UpgradeLevel;

        if (dm.statUpgradeDatasDic.TryGetValue(id, out var s))
            return s.UpgradeLevel;

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
            info.upgradeDesLo.ForceUpdateText();
        }
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

        // 업그레이드 UI 갱신
        // 3개 이상이면 앞의 3개, 3개 미만이면 순환하며 채움 (i % Count)
        if (upgrades.Count > 0)
        {
            for (int i = 0; i < 3; i++)
            {
                ApplyToSlot(i, upgrades[i % upgrades.Count]);
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

        StartSkillCoroutine(id, SnailCor(id));
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

            yield return new WaitForSeconds(
                DataManager.Instance
                    .upgradeDatasDic[id]
                    .coolTime[level]
            );
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

            yield return new WaitForSeconds(
                DataManager.Instance
                    .upgradeDatasDic[id]
                    .coolTime[level]
            );
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

            yield return new WaitForSeconds(
                DataManager.Instance
                    .upgradeDatasDic[id]
                    .coolTime[level]
            );
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
