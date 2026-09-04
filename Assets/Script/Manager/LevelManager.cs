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
        StartCoroutine(WaitLevelDataDownloader());
    }
    
    IEnumerator WaitLevelDataDownloader()
    {
        yield return new WaitUntil(() => DataManager.Instance != null && DataManager.Instance.levelDatas != null);
        levelDatas = DataManager.Instance.levelDatas;
    }

    public void AddExp(int exp)
    {
        currentExp += exp;
        expUpdate?.Invoke();
        if (currentLevel < levelDatas.Length && currentExp >= levelDatas[currentLevel])
        {
            LevelUp();
        }
    }

    public void LevelUp()
    {
        Debug.Log("Level Up");   
        currentLevel++;
        upgradeUI.SetActive(true);
        UpgradeManager.Instance.UpgradeListRenewal();
        currentExp = 0;
        levelUpdate?.Invoke();
        Time.timeScale = 0f;
    }
}
