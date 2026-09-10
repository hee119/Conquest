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
    Dictionary<int, Coroutine> upgradeCor = new Dictionary<int, Coroutine>();
    private GameObject player;
    BoxCollider2D playerCollider;
    public Image[] skillUpgradeIcons;
    public Image[] statUpgradeIcons;
    public int skillUpgradeCount;
    public int statUpgradeCount;
    public event Action upgradeSelect;
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
                DataManager.Instance.UpgradeDatasDic[id].type == "Skill"
                && !skillUpgrades.Contains(id)
            );
        }

        if (statUpgrades.Count >= 5)
        {
            upgrades.RemoveAll(id =>
                DataManager.Instance.UpgradeDatasDic[id].type == "Stat"
                && !statUpgrades.Contains(id)
            );
        }
    
        for (int i = 0; i < 3; i++)
        {
            upgradesInfo[i].upgradeData.id = DataManager.Instance.UpgradeDatasDic[upgrades[i]].id;
            upgradesInfo[i].name = DataManager.Instance.UpgradeDatasDic[upgrades[i]].name;
            upgradesInfo[i].upgradeNameLo.key = DataManager.Instance.UpgradeDatasDic[upgrades[i]].name;
            if (DataManager.Instance.UpgradeDatasDic[upgrades[i]].sprite != null)
            {
                upgradesInfo[i].upgradeImage.sprite = DataManager.Instance.UpgradeDatasDic[upgrades[i]].sprite;
            }
            else
            {
                upgradesInfo[i].upgradeImage.sprite = null;
            }
            upgradesInfo[i].upgradeData.type = DataManager.Instance.UpgradeDatasDic[upgrades[i]].type;
            upgradesInfo[i].upgradeData.upgradeLevel = DataManager.Instance.UpgradeDatasDic[upgrades[i]].upgradeLevel;
            upgradesInfo[i].upgradeDesLo.key = DataManager.Instance.UpgradeDatasDic[upgrades[i]].description;
            upgradesInfo[i].upgradeDesLo.ForceUpdateText();
            upgradesInfo[i].upgradeNameLo.ForceUpdateText();
        }
    }

    public void SkillUpgradesAdd(int id)
    {
        if (!skillUpgrades.Contains(id))
        {
            skillUpgrades.Add(id);
            if (DataManager.Instance.skillUpgradeIconDic.ContainsKey(id))
            {
                skillUpgradeIcons[skillUpgradeCount].sprite = DataManager.Instance.skillUpgradeIconDic[id];
                skillUpgradeCount++;
            }
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
        upgradeSelect?.Invoke();
    }

    public void StatUpgradesAdd(int id)
    {
        if (!statUpgrades.Contains(id))
        {
            statUpgrades.Add(id);
            if (DataManager.Instance.statUpgradeIconDic.ContainsKey(id))
            {
                statUpgradeIcons[statUpgradeCount].sprite = DataManager.Instance.statUpgradeIconDic[id];
                statUpgradeCount++;
            }
        }
        if (DataManager.Instance.UpgradeDatasDic[id].upgradeLevel >= 4)
        {
            upgrades.Remove(id);
        }
        
        upgradeActions[id](id);
        upgradeSelect?.Invoke();
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
    Debug.Log($"[MissileCor] 시작 | ID={id}");

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

        Debug.Log(
            $"[MissileCor] 데이터 | " +
            $"ID={id} | Level={level} | Count={count} | " +
            $"Range={detectionRange} | PlayerPos={player.transform.position}"
        );

        // 적이 나타날 때까지 대기
        Debug.Log(
            $"[MissileCor] 적 대기 시작 | " +
            $"Range={detectionRange}"
        );

        yield return new WaitUntil(() =>
            detectUpgrade.HasEnemyInRange(detectionRange)
        );

        Debug.Log(
            $"[MissileCor] 적 감지됨 | " +
            $"PlayerPos={player.transform.position}"
        );

        // 적 목록 가져오기
        List<Collider2D> enemies =
            detectUpgrade.DetectEnemies(detectionRange);

        Debug.Log(
            $"[MissileCor] 적 목록 생성 | " +
            $"Count={enemies.Count}"
        );

        if (enemies.Count == 0)
        {
            Debug.LogWarning(
                $"[MissileCor] 적 감지됐다고 했는데 목록이 비어있음"
            );

            continue;
        }

        // 가장 가까운 적 한 번만 찾기
        Transform target =
            detectUpgrade.FindClosestEnemy(enemies);

        if (target == null)
        {
            Debug.LogWarning(
                $"[MissileCor] 타겟이 NULL"
            );

            continue;
        }

        Vector3 targetPos = target.position;

        Debug.Log(
            $"[MissileCor] 타겟 선정 | " +
            $"Target={target.name} | " +
            $"TargetPos={targetPos} | " +
            $"Active={target.gameObject.activeInHierarchy}"
        );

        // 모든 미사일이 같은 타겟을 사용
        for (int i = 0; i < count; i++)
        {
            Debug.Log(
                $"[MissileCor] 미사일 생성 시도 | " +
                $"Index={i + 1}/{count} | " +
                $"SpawnPos={player.transform.position} | " +
                $"TargetPos={targetPos}"
            );

            GameObject missile =
                PoolManager.Instance.Spawn(
                    id,
                    player.transform.position
                );

            if (missile == null)
            {
                Debug.LogError(
                    $"[MissileCor] Spawn 실패 | ID={id}"
                );
            }
            else
            {
                Debug.Log(
                    $"[MissileCor] Spawn 성공 | " +
                    $"Missile={missile.name} | " +
                    $"InstanceID={missile.GetInstanceID()} | " +
                    $"Active={missile.activeInHierarchy} | " +
                    $"Position={missile.transform.position}"
                );

                Missle missle =
                    missile.GetComponent<Missle>();

                if (missle == null)
                {
                    Debug.LogError(
                        $"[MissileCor] Missle 컴포넌트 없음 | " +
                        $"Missile={missile.name}"
                    );
                }
                else
                {
                    bool targetValid =
                        target != null &&
                        target.gameObject != null &&
                        target.gameObject.activeInHierarchy;

                    Debug.Log(
                        $"[MissileCor] Attack 호출 준비 | " +
                        $"TargetValid={targetValid} | " +
                        $"TargetPos={targetPos}"
                    );

                    if (targetValid)
                    {
                        missle.Attack(
                            targetPos,
                            target.gameObject
                        );

                        Debug.Log(
                            $"[MissileCor] Attack 호출 완료 | " +
                            $"Missile={missile.name}"
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[MissileCor] 타겟 무효 → 위치만 전달 | " +
                            $"TargetPos={targetPos}"
                        );

                        missle.Attack(
                            targetPos,
                            null
                        );
                    }
                }
            }

            yield return new WaitForSeconds(0.1f);
        }

        float coolTime =
            DataManager.Instance.UpgradeDatasDic[id]
                .coolTime[level];

        Debug.Log(
            $"[MissileCor] 미사일 발사 완료 → 쿨타임 | " +
            $"CoolTime={coolTime}"
        );

        yield return new WaitForSeconds(coolTime);

        Debug.Log(
            $"[MissileCor] 쿨타임 종료 → 다시 탐색"
        );
    }
}
    
    private IEnumerator MoldCor(int id)
    {
        GameManager.Instance.moldPrefabId = id;
        GameManager.Instance.moldChance = DataManager.Instance.UpgradeDatasDic[id].debuffChance[DataManager.Instance.UpgradeDatasDic[id].upgradeLevel];
        yield return null;
    }
}
