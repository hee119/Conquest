using System;
using System.Collections;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SelectUpgrade : MonoBehaviour
{
    [SerializeField] private GameObject upgredesMom;
    public UpgradeUiInfo upgradeUiInfo;

    [SerializeField] private int speed;
    
    [SerializeField] private GameObject selectEffect;
    
    [SerializeField] private float fadeOutSpeed;
    
    [SerializeField] private float darknessAmount;
    
    private SelectUpgrade[] siblingObjects;
    
    private Image[] _images;

    private Color[] originalColors;
    
    public Coroutine padeOutCoroutine;
    
    

    public void Awake()
    {
        upgradeUiInfo = GetComponent<UpgradeUiInfo>();

        _images = GetComponentsInChildren<Image>();

        siblingObjects = transform.parent.GetComponentsInChildren<SelectUpgrade>()
            .Where(x => x != this)
            .ToArray();

        originalColors = new Color[_images.Length];

        for (int i = 0; i < _images.Length; i++)
        {
            originalColors[i] = _images[i].color;
        }
    }

    void OnEnable()
    {
        upgredesMom.transform.localScale = Vector3.zero;
        for(int i = 0; i < _images.Length; i++)
        {
            _images[i].color = originalColors[i];
        }
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

        selectEffect.SetActive(true);
        foreach (SelectUpgrade selectUpgrade in siblingObjects)
        {
            selectUpgrade.padeOutCoroutine = selectUpgrade.StartCoroutine(selectUpgrade.FadeOut());
        }
        StartCoroutine(UpgredesMomScale(-1));
    }

    IEnumerator UpgredesMomScale(int scaleDirection)
    {
        yield return new WaitUntil(() => siblingObjects[0].padeOutCoroutine == null);
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
            yield return new WaitForSecondsRealtime(0.5f);
            while (scale > 0.06f)
            {
                scale -= speed * Time.unscaledDeltaTime;
                upgredesMom.transform.localScale = new Vector3(scale, scale, scale);
                yield return null;
            }

            selectEffect.SetActive(false);
            Time.timeScale = 1f;
            upgredesMom.SetActive(false);
        }
    }

    IEnumerator FadeOut()
    {
        float value = 1f;

        while (value > darknessAmount)
        {
            value -= Time.unscaledDeltaTime * fadeOutSpeed;

            for (int i = 0; i < _images.Length; i++)
            {
                _images[i].color = originalColors[i] * value;
            }

            yield return null;
        }

        padeOutCoroutine = null;
    }
}