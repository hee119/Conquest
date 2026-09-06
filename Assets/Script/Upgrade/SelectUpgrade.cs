using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class SelectUpgrade : MonoBehaviour
{
    [SerializeField] private GameObject upgredesMom;
    public UpgradeUiInfo upgradeUiInfo;

    [SerializeField] private int speed;

    public void Awake()
    {
        upgradeUiInfo = GetComponent<UpgradeUiInfo>();
    }

    void OnEnable()
    {
        upgredesMom.transform.localScale = Vector3.zero;
        StartCoroutine(UpgredesMomScale(1));
    }

    public void UpgradeIsSelected()
    {
        if (upgradeUiInfo.upgradeData.type == "Stat")
        {
            UpgradeManager.Instance.StatUpgradesAdd(upgradeUiInfo.upgradeData.id);
        }
        else if (upgradeUiInfo.upgradeData.type == "Skill")
        {
            UpgradeManager.Instance.SkillUpgradesAdd(upgradeUiInfo.upgradeData.id);
        }

        upgradeUiInfo.upgradeData.upgradeLevel++;

        Time.timeScale = 1f;
        StartCoroutine(UpgredesMomScale(-1));
    }

    IEnumerator UpgredesMomScale(int scaleDirection)
    {
        float scale = upgredesMom.transform.localScale.x;
        if (scaleDirection >= 0)
        {
            upgredesMom.SetActive(true);
            while (scale < 1f)
            {
                scale += speed * Time.unscaledDeltaTime;
                upgredesMom.transform.localScale = new Vector3(scale, scale, scale);
                yield return null;
            }

            upgredesMom.transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            while (scale > 0.06f)
            {
                scale -= speed * Time.unscaledDeltaTime;
                upgredesMom.transform.localScale = new Vector3(scale, scale, scale);
                yield return null;
            }

            upgredesMom.SetActive(false);
        }
    }
}