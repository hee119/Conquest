using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;
using Random = UnityEngine.Random;
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;
    public List<int> skillUpgrades = new List<int>();
    public List<int> statUpgrades = new List<int>();
    public List<int> upgrades = new List<int>();
    public List<UpgradeUiInfo> upgradesInfo = new List<UpgradeUiInfo>();
    Dictionary<int, Action<int>> upgradeActions = new Dictionary<int, Action<int>>();
    Dictionary<int, Coroutine> upgradeCor = new Dictionary<int, Coroutine>();
    private GameObject player;
    BoxCollider2D playerCollider;
    [SerializeField]
    private DetectUpgrade detectUpgrade;
    
    [SerializeField]
    private Orbit orbit;
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
        upgradesInfo = FindObjectsByType<UpgradeUiInfo>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();        
    }

    public void UpgradesInit()
    {
        
        // 함수 이름을 액션타입으로 변환
        foreach (int upgrade in upgrades)
        {
            string functionName = DataManager.Instance.UpgradeDatasDic[upgrade].function;

            MethodInfo method = GetType().GetMethod(
                functionName,
                BindingFlags.Instance | BindingFlags.Public
            );

            Action<int> action = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), this, method);

            upgradeActions.Add(upgrade, action);
        }
    }

    public void UpgradeListRenewal()
    {
        upgrades = upgrades.OrderBy(x => Random.value).ToList();        
        
            
            
            if (skillUpgrades.Count >= 5)
            {
                upgrades.RemoveAll(id =>
                    DataManager.Instance.UpgradeDatasDic[id].type == "skill"
                );
            }

            if (statUpgrades.Count >= 5)
            {
                upgrades.RemoveAll(id =>
                    DataManager.Instance.UpgradeDatasDic[id].type == "stat"
                );
            }
    
        for (int i = 0; i < 3; i++)
        {
            upgradesInfo[i].upgradeData.id = DataManager.Instance.UpgradeDatasDic[upgrades[i]].id;
            upgradesInfo[i].name = DataManager.Instance.UpgradeDatasDic[upgrades[i]].name;
            upgradesInfo[i].upgradeName.text = DataManager.Instance.UpgradeDatasDic[upgrades[i]].name;
            if(upgradesInfo[i].upgradeData.sprite != null)
                upgradesInfo[i].upgradeData.sprite = DataManager.Instance.UpgradeDatasDic[upgrades[i]].sprite;
            upgradesInfo[i].upgradeData.type = DataManager.Instance.UpgradeDatasDic[upgrades[i]].type;
            upgradesInfo[i].upgradeData.upgradeLevel = DataManager.Instance.UpgradeDatasDic[upgrades[i]].upgradeLevel;
            upgradesInfo[i].upgradeData.description = DataManager.Instance.UpgradeDatasDic[upgrades[i]].description;
        }
    }

    public void SkillUpgradesAdd(int id)
    {
        if (!skillUpgrades.Contains(id))
        {
            skillUpgrades.Add(id);
        }
        else
        {
            DataManager.Instance.UpgradeDatasDic[id].upgradeLevel++;
        }
        if (DataManager.Instance.UpgradeDatasDic[id].upgradeLevel >= 4)
        {
            upgrades.Remove(id);
        }

        upgradeActions[id](id);
    }

    public void StatUpgradesAdd(int id)
    {
        if (!statUpgrades.Contains(id))
        {
            statUpgrades.Add(id);
        }
        if (DataManager.Instance.UpgradeDatasDic[id].upgradeLevel >= 4)
        {
            upgrades.Remove(id);
        }
        
        upgradeActions[id](id);
    }
    
    public void StartSkillCoroutine(int id, IEnumerator coroutine)
    {
        // 이미 실행 중인 코루틴이 있으면 중지
        if (upgradeCor.TryGetValue(id, out Coroutine oldCoroutine))
        {
            StopCoroutine(oldCoroutine);
        }

        // 새로운 코루틴 시작
        Coroutine newCoroutine = StartCoroutine(coroutine);

        upgradeCor[id] = newCoroutine;
    }
    
    public void Lightning(int id)
    {
        if (upgradeCor.ContainsKey(id))
        {
            return;
        }
        StartSkillCoroutine(id, LightningCor(id));
    }

    public void Fireball(int id)
    {
        StartSkillCoroutine(id, detectUpgrade.DetectUpgradeCor(id));
    }

    public void Burn(int id)
    {
        StartSkillCoroutine(id, BurnCor(id));
    }

    public void Poison(int id)
    {
        StartSkillCoroutine(id, detectUpgrade.DetectUpgradeCor(id));
    }

    public void Snail(int id)
    {
        StartSkillCoroutine(id, SnailCor(id));
    }

    public void Shuriken(int id)
    {
        // 이미 수리검 코루틴이 돌고 있다면 다시 시작하지 않음
        if (upgradeCor.ContainsKey(id))
        {
            return;
        }

        StartSkillCoroutine(id, ShurikenCor(id));
    }

    public void Missile(int id)
    {
        if (upgradeCor.ContainsKey(id))
        {
            return;
        }
        StartSkillCoroutine(id, MissileCor(id));
    }

    public void Mushroom(int id)
    {
        StartSkillCoroutine(id, MushroomCor(id));
    }
    
    private IEnumerator LightningCor(int id)
    {
        while (true)
        {
            for (int i = 0; i < DataManager.Instance.UpgradeDatasDic[id].count[DataManager.Instance.UpgradeDatasDic[id].upgradeLevel]; i++)
            {
                PoolManager.Instance.Spawn(id, playerCollider.bounds.center);
            }
            yield return new WaitForSeconds(DataManager.Instance.UpgradeDatasDic[id].coolTime[DataManager.Instance.UpgradeDatasDic[id].upgradeLevel]);
        }    
    }

    private IEnumerator BurnCor(int id)
    {
        GameManager.Instance.burnPrefabId = id;
        GameManager.Instance.burnChance = DataManager.Instance.UpgradeDatasDic[id].debuffChance[DataManager.Instance.UpgradeDatasDic[id].upgradeLevel];
        yield return null;
    }
    

    private IEnumerator SnailCor(int id)
    {
        while (true)
        {
            PoolManager.Instance.Spawn(id, playerCollider.bounds.center);
            Debug.Log(DataManager.Instance.UpgradeDatasDic[id].coolTime[DataManager.Instance.UpgradeDatasDic[id].upgradeLevel]);
            yield return new WaitForSeconds(DataManager.Instance.UpgradeDatasDic[id].coolTime[DataManager.Instance.UpgradeDatasDic[id].upgradeLevel]);
        }
    }

    private IEnumerator ShurikenCor(int id)
    {
        while (true)
        {
            int count = DataManager.Instance.UpgradeDatasDic[id]
                .count[DataManager.Instance.UpgradeDatasDic[id].upgradeLevel];

            for (int i = 0; i < count; i++)
            {
                if (orbit.orbitList.Count < count)
                {
                    orbit.AddOrbit(id);
                }
            }

            yield return StartCoroutine(orbit.RotateOrbits(id));
            
            yield return new WaitForSeconds(DataManager.Instance.UpgradeDatasDic[id].coolTime[DataManager.Instance.UpgradeDatasDic[id].upgradeLevel]);
        }
        
    }
    
    private IEnumerator MissileCor(int id)
    {
        while (true)
        {
            int level =
                DataManager.Instance.UpgradeDatasDic[id].upgradeLevel;

            int count =
                DataManager.Instance.UpgradeDatasDic[id]
                    .count[level];

            float detectionRange =
                DataManager.Instance.UpgradeDatasDic[id]
                    .detectionRange[level];

            // 적이 나타날 때까지 대기
            yield return new WaitUntil(() =>
                detectUpgrade.HasEnemyInRange(detectionRange)
            );

            // 한 번만 적을 선택
            List<Collider2D> enemies =
                detectUpgrade.DetectEnemies(detectionRange);

            Transform target =
                detectUpgrade.FindClosestEnemy(enemies);

            if (target == null)
                continue;

            // 모든 미사일이 같은 타겟을 사용
            for (int i = 0; i < count; i++)
            {
                GameObject missile =
                    PoolManager.Instance.Spawn(
                        id,
                        player.transform.position
                    );

                if (missile != null)
                {
                    Missle missle =
                        missile.GetComponent<Missle>();

                    if (missle != null)
                    {
                        missle.Attack(target);
                    }
                }

                yield return new WaitForSeconds(0.1f);
            }

            float coolTime =
                DataManager.Instance.UpgradeDatasDic[id]
                    .coolTime[level];

            yield return new WaitForSeconds(coolTime);
        }
    }
    
    private IEnumerator MushroomCor(int id)
    {
        GameManager.Instance.mushroomPrefabId = id;
        GameManager.Instance.mushroomChance = DataManager.Instance.UpgradeDatasDic[id].debuffChance[DataManager.Instance.UpgradeDatasDic[id].upgradeLevel];
        yield return null;
    }
}
