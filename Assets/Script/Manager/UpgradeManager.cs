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

    Dictionary<int, Action<int>> upgradeActions = new Dictionary<int, Action<int>>();
    public Dictionary<int, Coroutine> upgradeCor = new Dictionary<int, Coroutine>();

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
        foreach (var pair in DataManager.Instance.UpgradeDatasDic)
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

    public void UpgradeListRenewal()
    {
        upgrades = upgrades.OrderBy(x => Random.value).ToList();

        maxLevelUpgrades.AddRange(
            skillUpgrades.Where(id =>
                DataManager.Instance.UpgradeDatasDic[id].UpgradeLevel >= 4
            )
        );

        if (skillUpgrades.Count >= 5)
        {
            upgrades.RemoveAll(id =>
                DataManager.Instance.UpgradeDatasDic[id].type == "Skill"
                && !skillUpgrades.Contains(id)
            );
        }

        if (statUpgrades.Count >= 5)
        {
            maxLevelUpgrades.AddRange(
                statUpgrades.Where(id =>
                    DataManager.Instance.UpgradeDatasDic[id].UpgradeLevel >= 5
                )
            );

            upgrades.RemoveAll(id =>
                DataManager.Instance.UpgradeDatasDic[id].type == "Stat"
                && !statUpgrades.Contains(id)
            );
        }

        for (int i = 0; i < 3; i++)
        {
            upgradesInfo[i].upgradeData.id =
                DataManager.Instance.UpgradeDatasDic[upgrades[i]].id;

            upgradesInfo[i].name =
                DataManager.Instance.UpgradeDatasDic[upgrades[i]].name;

            upgradesInfo[i].upgradeNameLo.key =
                DataManager.Instance.UpgradeDatasDic[upgrades[i]].name;

            if (DataManager.Instance.UpgradeDatasDic[upgrades[i]].sprite != null)
            {
                upgradesInfo[i].upgradeImage.sprite =
                    DataManager.Instance.UpgradeDatasDic[upgrades[i]].sprite;
            }
            else
            {
                upgradesInfo[i].upgradeImage.sprite = null;
            }

            upgradesInfo[i].upgradeData.type =
                DataManager.Instance.UpgradeDatasDic[upgrades[i]].type;

            // 실제 레벨 + 1을 UI에 표시
            upgradesInfo[i].upgradeData.upgradeLevel =
                DataManager.Instance.UpgradeDatasDic[upgrades[i]].upgradeLevel;

            upgradesInfo[i].upgradeDesLo.key =
                DataManager.Instance.UpgradeDatasDic[upgrades[i]].description;

            upgradesInfo[i].upgradeDesLo.ForceUpdateText();
            upgradesInfo[i].upgradeNameLo.ForceUpdateText();
        }

        upgradeRenewal?.Invoke();
    }

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
        DataManager.Instance.UpgradeDatasDic[id].upgradeLevel++;

        if (DataManager.Instance.UpgradeDatasDic[id].UpgradeLevel >= 4)
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
        DataManager.Instance.UpgradeDatasDic[id].upgradeLevel++;

        if (DataManager.Instance.UpgradeDatasDic[id].UpgradeLevel >= 4)
        {
            upgrades.Remove(id);
        }

        upgradeActions[id](id);
        upgradeSelect?.Invoke();
    }

    public void StartSkillCoroutine(int id, IEnumerator coroutine)
    {
        if (upgradeCor.TryGetValue(id, out Coroutine oldCoroutine))
        {
            StopCoroutine(oldCoroutine);
        }

        Coroutine newCoroutine = StartCoroutine(coroutine);

        upgradeCor[id] = newCoroutine;
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
                    .UpgradeDatasDic[id]
                    .UpgradeLevel;

            for (int i = 0;
                 i < DataManager.Instance.UpgradeDatasDic[id].count[level];
                 i++)
            {
                PoolManager.Instance.Spawn(
                    id,
                    playerCollider.bounds.center
                );
            }

            yield return new WaitForSeconds(
                DataManager.Instance
                    .UpgradeDatasDic[id]
                    .coolTime[level]
            );
        }
    }

    private IEnumerator BurnCor(int id)
    {
        int level =
            DataManager.Instance
                .UpgradeDatasDic[id]
                .UpgradeLevel;

        GameManager.Instance.burnPrefabId = id;

        GameManager.Instance.burnChance =
            DataManager.Instance
                .UpgradeDatasDic[id]
                .debuffChance[level];

        yield return null;
    }

    private IEnumerator SnailCor(int id)
    {
        while (true)
        {
            int level =
                DataManager.Instance
                    .UpgradeDatasDic[id]
                    .UpgradeLevel;

            PoolManager.Instance.Spawn(
                id,
                playerCollider.bounds.center
            );

            Debug.Log(
                DataManager.Instance
                    .UpgradeDatasDic[id]
                    .coolTime[level]
            );

            yield return new WaitForSeconds(
                DataManager.Instance
                    .UpgradeDatasDic[id]
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
                    .UpgradeDatasDic[id]
                    .UpgradeLevel;

            int count =
                DataManager.Instance
                    .UpgradeDatasDic[id]
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
                    .UpgradeDatasDic[id]
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
                    .UpgradeDatasDic[id]
                    .UpgradeLevel;

            int count =
                DataManager.Instance
                    .UpgradeDatasDic[id]
                    .count[level];

            float detectionRange =
                DataManager.Instance
                    .UpgradeDatasDic[id]
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

            // 발사 순간의 위치를 저장
            Vector3 targetPosition =
                target.position;

            // 같은 위치를 향해 count만큼 발사
            for (int i = 0; i < count; i++)
            {
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

                missle.Attack(targetPosition);

                yield return new WaitForSeconds(0.1f);
            }

            float coolTime =
                DataManager.Instance
                    .UpgradeDatasDic[id]
                    .coolTime[level];

            yield return new WaitForSeconds(coolTime);
        }
    }

    private IEnumerator MoldCor(int id)
    {
        int level =
            DataManager.Instance
                .UpgradeDatasDic[id]
                .UpgradeLevel;

        GameManager.Instance.moldPrefabId = id;

        GameManager.Instance.moldChance =
            DataManager.Instance
                .UpgradeDatasDic[id]
                .debuffChance[level];

        yield return null;
    }
}