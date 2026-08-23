using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;
    public int[] levelDatas;
    public int currentLevel;
    private int currentExp;
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
        if (currentLevel < levelDatas.Length && currentExp >= levelDatas[currentLevel])
        {
            LevelUp();
        }
    }

    public void LevelUp()
    {
        Debug.Log("Level Up");   
        currentLevel++;
        currentExp = 0;
    }
}
