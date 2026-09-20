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
        if (!TryGetView(id, out var v))
        {
            Debug.LogWarning($"[UpgradeManager] 업그레이드 데이터를 찾을 수 없음 (ID: {id})");
            return;
        }

        var info = upgradesInfo[slot];

        if (!info.gameObject.activeInHierarchy)
            info.gameObject.SetActive(true);

        info.upgradeData.id = v.id;
        info.name = v.name;
        info.upgradeNameLo.key = v.name;
        info.upgradeImage.sprite = v.sprite;
        info.upgradeData.type = v.type;

        // 실제 레벨 + 1을 UI에 표시
        info.upgradeData.upgradeLevel = v.upgradeLevel;

        info.upgradeDesLo.key = v.description;

        info.upgradeDesLo.ForceUpdateText();
        info.upgradeNameLo.ForceUpdateText();
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
    // =========================================================

    public void CriticalInstinct(int id)
    {
        float percent =
            DataManager.Instance.statUpgradeDatasDic[id]
                .critChance[DataManager.Instance.statUpgradeDatasDic[id].UpgradeLevel];

        foreach (PlayerData player in DataManager.Instance.playerDatasDic.Values)
        {
            player.criticalChance =
                (int)(player.criticalChance * (1f + percent / 100f));
        }
    }

    public void CriticalForce(int id)
    {
        float percent =
            DataManager.Instance.statUpgradeDatasDic[id]
                .critDamage[DataManager.Instance.statUpgradeDatasDic[id].UpgradeLevel];

        foreach (PlayerData player in DataManager.Instance.playerDatasDic.Values)
        {
            player.criticalDamage *= 1f + percent / 100f;
        }
    }

    public void PowerBoost(int id)
    {
        float percent =
            DataManager.Instance.statUpgradeDatasDic[id]
                .attackPower[DataManager.Instance.statUpgradeDatasDic[id].UpgradeLevel];

        foreach (PlayerData player in DataManager.Instance.playerDatasDic.Values)
        {
            player.damage =
                (int)(player.damage * (1f + percent / 100f));
        }
    }

    public void Vitality(int id)
    {
        float percent =
            DataManager.Instance.statUpgradeDatasDic[id]
                .maxHp[DataManager.Instance.statUpgradeDatasDic[id].UpgradeLevel];

        foreach (PlayerData player in DataManager.Instance.playerDatasDic.Values)
        {
            int oldHp = player.hp;

            player.hp =
                (int)(player.hp * (1f + percent / 100f));

            int increasedHp = player.hp - oldHp;

            PlayerCombat playerCombat =
                GameObject.FindGameObjectWithTag("Player")
                    .GetComponent<PlayerCombat>();

            playerCombat.maxHp += increasedHp;
            playerCombat.hp += increasedHp;
            playerCombat.UpdateHpUI();
        }
    }

    public void QuickCast(int id)
    {
        float percent =
            DataManager.Instance.statUpgradeDatasDic[id]
                .skillCooldown[DataManager.Instance.statUpgradeDatasDic[id].UpgradeLevel];

        // Skill 쿨타임만 감소
        foreach (SkillData skill in DataManager.Instance.skillDatasDic.Values)
        {
            skill.skillCoolTime *= 1f - percent / 100f;
        }
    }

    public void RapidCycle(int id)
    {
        float percent =
            DataManager.Instance.statUpgradeDatasDic[id]
                .passiveCooldown[DataManager.Instance.statUpgradeDatasDic[id].UpgradeLevel];

        // Passive Upgrade 쿨타임만 감소
        foreach (UpgradeData upgrade in DataManager.Instance.upgradeDatasDic.Values)
        {
            if (upgrade.type != "Passive")
                continue;

            for (int i = 0; i < upgrade.coolTime.Length; i++)
            {
                upgrade.coolTime[i] *= 1f - percent / 100f;
            }
        }
    }

    public void IronWall(int id)
    {
        float percent =
            DataManager.Instance.statUpgradeDatasDic[id]
                .defense[DataManager.Instance.statUpgradeDatasDic[id].UpgradeLevel];

        foreach (PlayerData player in DataManager.Instance.playerDatasDic.Values)
        {
            player.defense =
                (int)(player.defense * (1f + percent / 100f));
        }
    }
}