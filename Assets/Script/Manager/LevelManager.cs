using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public int[] levelDatas;
    public int currentLevel;
    public int currentExp;

    public GameObject upgradeUI;

    public event Action expUpdate;
    public event Action levelUpdate;

    // 분당 획득 경험치
    public List<int> expPerMinute = new List<int>();

    // 분당 레벨업 횟수
    public List<int> levelPerMinute = new List<int>();

    private int minuteExp;
    private int minuteLevel;

    private float minuteTimer;

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
            return;
        }

        StartCoroutine(WaitLevelDataDownloader());
    }

    private void Update()
    {
        minuteTimer += Time.deltaTime;

        if (minuteTimer >= 60f)
        {
            minuteTimer -= 60f;

            expPerMinute.Add(minuteExp);
            levelPerMinute.Add(minuteLevel);

            Debug.Log(
                $"[{expPerMinute.Count}분] " +
                $"획득 경험치: {minuteExp}, " +
                $"레벨업: {minuteLevel}회"
            );

            minuteExp = 0;
            minuteLevel = 0;
        }
    }

    IEnumerator WaitLevelDataDownloader()
    {
        yield return new WaitUntil(() =>
            DataManager.Instance != null &&
            DataManager.Instance.levelDatas != null
        );

        levelDatas = DataManager.Instance.levelDatas;
    }

    public void AddExp(int exp)
    {
        currentExp += exp;
        minuteExp += exp;
        expUpdate?.Invoke();

        while (currentLevel < levelDatas.Length &&
               currentExp >= levelDatas[currentLevel])
        {
            LevelUp();
        }
    }

    public void LevelUp()
    {
        currentExp -= levelDatas[currentLevel]; // 초과분 보존 (currentLevel++ 전에 계산)
        currentLevel++;
        minuteLevel++;

        upgradeUI.SetActive(true);
        UpgradeManager.Instance.UpgradeListRenewal();

        levelUpdate?.Invoke();
        Time.timeScale = 0f;
    }
}