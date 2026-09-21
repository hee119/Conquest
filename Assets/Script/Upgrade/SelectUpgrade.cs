using System;
using System.Collections;
using System.Linq;
using TMPro;
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
    
    [SerializeField] private Image[] levelIcons;
    [SerializeField] private ParticleSystem[] levelIconsParticles;
    
    private SelectUpgrade[] siblingObjects;
    
    private Image[] _images;
    
    private TextMeshProUGUI[] _texts;

    private Color[] originalColors;
    
    public Coroutine padeOutCoroutine;

    public Coroutine blinkLevelIcon;
    

    public void Awake()
    {
        upgradeUiInfo = GetComponent<UpgradeUiInfo>();

        _images = GetComponentsInChildren<Image>()
            .Where(image => !levelIcons.Contains(image))
            .ToArray();
        
        _texts = GetComponentsInChildren<TextMeshProUGUI>();

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
            if(i < _texts.Length) _texts[i].color = Color.white;
        }
        
        

        // 패널이 다시 열릴 때 레이캐스트 타겟 복구 (자기 자신 + 자식)
        SetRaycastTarget(true);

        UpgradeManager.Instance.upgradeRenewal += LevelIconsInit;
        UpgradeManager.Instance.upgradeRenewal += BlinkLevelIcon;
        StartCoroutine(UpgredesMomScale(1));
    }

    void OnDisable()
    {
        upgradeUiInfo.upgradeData.id = 0;
        UpgradeManager.Instance.upgradeRenewal -= LevelIconsInit;
        UpgradeManager.Instance.upgradeRenewal -= BlinkLevelIcon;
    }

    public void LevelIconsInit()
    {
        int level = upgradeUiInfo.upgradeData.upgradeLevel;
        Debug.Log("SelectUpgrade " + level);

        for (int i = 0; i < levelIcons.Length; i++)
        {
            Color color = levelIcons[i].color;

            if (i < level)
            {
                // 이미 올린 레벨
                color.a = 1f;
            }
            else
            {
                // 아직 올리지 않은 레벨
                color.a = 0f;
            }

            levelIcons[i].color = color;
        }
    }

    public void BlinkLevelIcon()
    {
        if (blinkLevelIcon == null && gameObject.activeInHierarchy)
        {
            blinkLevelIcon = StartCoroutine(BlinkLevelIconCor());
        }
    }
    
    public void UpgradeIsSelected()
    {
        // 자기 자신 + 형제들의 호버 상태 강제 초기화
        GetComponent<UpgradeHover>()?.ForceHoverCancel();

        foreach (SelectUpgrade selectUpgrade in siblingObjects)
        {
            selectUpgrade.GetComponent<UpgradeHover>()?.ForceHoverCancel();
        }

        // 클릭 즉시 레이캐스트 차단
        SetRaycastTarget(false);

        foreach (SelectUpgrade selectUpgrade in siblingObjects)
        {
            selectUpgrade.SetRaycastTarget(false);
        }

        if (upgradeUiInfo.upgradeData.type == "Stat")
        {
            UpgradeManager.Instance.StatUpgradesAdd(upgradeUiInfo.upgradeData.id);
        }
        else if (upgradeUiInfo.upgradeData.type == "Skill")
        {
            UpgradeManager.Instance.SkillUpgradesAdd(upgradeUiInfo.upgradeData.id);
        }

        StartCoroutine(LevelUpAnim());

        selectEffect.SetActive(true);

        foreach (SelectUpgrade selectUpgrade in siblingObjects)
        {
            if (selectUpgrade.gameObject.activeInHierarchy &&
                selectUpgrade.blinkLevelIcon != null)
            {
                selectUpgrade.StopCoroutine(selectUpgrade.blinkLevelIcon);
                selectUpgrade.blinkLevelIcon = null;

                foreach (var icon in selectUpgrade.levelIcons)
                {
                    Color alpha = icon.color;
                    alpha.a = 0f;
                    icon.color = alpha;
                }
            }

            selectUpgrade.padeOutCoroutine =
                selectUpgrade.StartCoroutine(selectUpgrade.FadeOut());
        }

        StartCoroutine(UpgredesMomScale(-1));
    }

    // 자기 자신과 모든 자식(Image)의 raycastTarget을 일괄 설정
    private void SetRaycastTarget(bool value)
    {
        foreach (Image image in GetComponentsInChildren<Image>(true))
        {
            image.raycastTarget = value;
        }
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
                if(i < _texts.Length) _texts[i].color = originalColors[i] * value;
            }

            yield return null;
        }

        padeOutCoroutine = null;
    }

    IEnumerator LevelUpAnim()
    {
        if (blinkLevelIcon != null)
        {
            StopCoroutine(blinkLevelIcon);
            blinkLevelIcon = null;
        }

        int level = upgradeUiInfo.upgradeData.upgradeLevel;

        if (level >= levelIcons.Length)
            yield break;

        Color color = levelIcons[level].color;
        color.a = 1f;
        levelIcons[level].color = color;

        levelIconsParticles[level].Play();
        
        yield return null;
    }
    
    IEnumerator BlinkLevelIconCor()
    {
        yield return new WaitUntil(() => upgradeUiInfo.upgradeData.id > 0);

        int level = upgradeUiInfo.upgradeData.upgradeLevel;

        if (level >= levelIcons.Length)
            yield break;

        Image image = levelIcons[level];

        float time = 0f;
        float blinkSpeed = 1.5f;

        Color color = image.color;

        while (true)
        {
            time += Time.unscaledDeltaTime;

            color.a = Mathf.PingPong(time * blinkSpeed, 1f);
            image.color = color;
            
            yield return null;
        }
    }
}