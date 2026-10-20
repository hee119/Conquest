using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectUpgrade : MonoBehaviour
{
    [Header("선택")]
    [SerializeField] private Button selectionButton;

    private bool selectionEnabled;


    [Header("업그레이드 UI")]
    [SerializeField] private GameObject upgredesMom;

    public UpgradeUiInfo upgradeUiInfo;


    [Header("UI 등장 / 퇴장")]
    [SerializeField] private float speed = 5f;

    [Tooltip("전체 UI가 닫히는 속도")]
    [SerializeField] private float closeSpeed = 5f;


    [Header("선택 이펙트")]
    [SerializeField] private GameObject selectEffect;

    // selectEffect 안에 들어있는 파티클들
    private ParticleSystem[] selectEffectParticles;


    [Header("다른 카드 어둡게")]
    [SerializeField] private float fadeOutSpeed = 3f;

    [Range(0f, 1f)]
    [SerializeField] private float darknessAmount = 0.35f;


    [Header("레벨 아이콘")]
    [SerializeField] private Image[] levelIcons;

    [SerializeField] private ParticleSystem[] levelIconsParticles;


    private SelectUpgrade[] siblingObjects;

    private Image[] _images;
    private TextMeshProUGUI[] _texts;

    private Color[] originalColors;
    private Color[] originalTextColors;


    public Coroutine padeOutCoroutine;
    public Coroutine blinkLevelIcon;

    private Coroutine selectSequenceCoroutine;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        if (selectionButton == null)
        {
            selectionButton =
                GetComponentInChildren<Button>(true);
        }


        upgradeUiInfo =
            GetComponent<UpgradeUiInfo>();


        // -----------------------------------------------------
        // 선택 이펙트 안에 있는 모든 ParticleSystem 저장
        // -----------------------------------------------------

        if (selectEffect != null)
        {
            selectEffectParticles =
                selectEffect.GetComponentsInChildren<ParticleSystem>(true);
        }


        // -----------------------------------------------------
        // 레벨 아이콘을 제외한 이미지 가져오기
        // -----------------------------------------------------

        _images =
            GetComponentsInChildren<Image>(true)
                .Where(image => !levelIcons.Contains(image))
                .ToArray();


        _texts =
            GetComponentsInChildren<TextMeshProUGUI>(true);


        // -----------------------------------------------------
        // 자기 자신을 제외한 다른 카드들
        // -----------------------------------------------------

        siblingObjects =
            transform.parent
                .GetComponentsInChildren<SelectUpgrade>(true)
                .Where(x => x != this)
                .ToArray();


        // -----------------------------------------------------
        // 이미지 원래 색 저장
        // -----------------------------------------------------

        originalColors =
            new Color[_images.Length];


        for (int i = 0; i < _images.Length; i++)
        {
            originalColors[i] =
                _images[i].color;
        }


        // -----------------------------------------------------
        // 텍스트 원래 색 저장
        // -----------------------------------------------------

        originalTextColors =
            new Color[_texts.Length];


        for (int i = 0; i < _texts.Length; i++)
        {
            originalTextColors[i] =
                _texts[i].color;
        }
    }


    // =========================================================
    // OnEnable
    // =========================================================

    private void OnEnable()
    {
        padeOutCoroutine = null;
        blinkLevelIcon = null;
        selectSequenceCoroutine = null;


        // -----------------------------------------------------
        // 이미지 색 복구
        // -----------------------------------------------------

        for (int i = 0; i < _images.Length; i++)
        {
            _images[i].color =
                originalColors[i];
        }


        // -----------------------------------------------------
        // 텍스트 색 복구
        // -----------------------------------------------------

        for (int i = 0; i < _texts.Length; i++)
        {
            _texts[i].color =
                originalTextColors[i];
        }


        // -----------------------------------------------------
        // 선택 이펙트 초기화
        // -----------------------------------------------------

        if (selectEffect != null)
        {
            selectEffect.SetActive(false);
        }


        SetSelectionEnabled(true);


        UpgradeManager.Instance.upgradeRenewal +=
            LevelIconsInit;

        UpgradeManager.Instance.upgradeRenewal +=
            BlinkLevelIcon;


        StartCoroutine(
            UpgredesMomOpen()
        );
    }


    // =========================================================
    // OnDisable
    // =========================================================

    private void OnDisable()
    {
        if (upgradeUiInfo != null &&
            upgradeUiInfo.upgradeData != null)
        {
            upgradeUiInfo.upgradeData.id = 0;
        }


        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.upgradeRenewal -=
                LevelIconsInit;

            UpgradeManager.Instance.upgradeRenewal -=
                BlinkLevelIcon;
        }


        padeOutCoroutine = null;
        blinkLevelIcon = null;
        selectSequenceCoroutine = null;
    }


    // =========================================================
    // 레벨 아이콘 초기화
    // =========================================================

    public void LevelIconsInit()
    {
        int level =
            upgradeUiInfo.upgradeData.upgradeLevel;


        for (int i = 0; i < levelIcons.Length; i++)
        {
            Color color =
                levelIcons[i].color;


            if (i < level)
            {
                color.a = 1f;
            }
            else
            {
                color.a = 0f;
            }


            levelIcons[i].color =
                color;
        }
    }


    // =========================================================
    // 다음 레벨 아이콘 깜빡임
    // =========================================================

    public void BlinkLevelIcon()
    {
        if (blinkLevelIcon == null &&
            gameObject.activeInHierarchy)
        {
            blinkLevelIcon =
                StartCoroutine(
                    BlinkLevelIconCor()
                );
        }
    }


    // =========================================================
    // 업그레이드 선택
    // =========================================================

    public void UpgradeIsSelected()
    {
        if (!selectionEnabled)
            return;


        if (selectionButton != null &&
            !selectionButton.IsInteractable())
        {
            return;
        }


        // =====================================================
        // 선택한 카드
        //
        // 현재 호버로 커진 상태 그대로 유지
        // =====================================================

        GetComponent<UpgradeHover>()
            ?.LockCurrentHover();


        // =====================================================
        // 나머지 카드는 호버 해제
        // =====================================================

        foreach (SelectUpgrade sibling in siblingObjects)
        {
            sibling
                .GetComponent<UpgradeHover>()
                ?.ForceHoverCancel();
        }


        // =====================================================
        // 중복 클릭 방지
        // =====================================================

        SetSelectionEnabled(false);


        foreach (SelectUpgrade sibling in siblingObjects)
        {
            sibling.SetSelectionEnabled(false);
        }


        // =====================================================
        // 실제 업그레이드 적용
        // =====================================================

        if (upgradeUiInfo.upgradeData.type == "Stat")
        {
            UpgradeManager.Instance.StatUpgradesAdd(
                upgradeUiInfo.upgradeData.id
            );
        }
        else if (upgradeUiInfo.upgradeData.type == "Skill")
        {
            UpgradeManager.Instance.SkillUpgradesAdd(
                upgradeUiInfo.upgradeData.id
            );
        }
        else if (upgradeUiInfo.upgradeData.type == "CharacterSkill")
        {
            UpgradeManager.Instance.CharacterSkillUpgradesAdd(upgradeUiInfo.upgradeData.id);
        }


        // =====================================================
        // 레벨 아이콘 이펙트
        // =====================================================

        StartCoroutine(
            LevelUpAnim()
        );


        // =====================================================
        // 선택 이펙트 재생
        // =====================================================

        PlaySelectEffect();


        // =====================================================
        // 나머지 카드 FadeOut
        // =====================================================

        foreach (SelectUpgrade sibling in siblingObjects)
        {
            if (!sibling.gameObject.activeInHierarchy)
                continue;


            // -------------------------------------------------
            // 깜빡임 정지
            // -------------------------------------------------

            if (sibling.blinkLevelIcon != null)
            {
                sibling.StopCoroutine(
                    sibling.blinkLevelIcon
                );


                sibling.blinkLevelIcon = null;


                foreach (Image icon in sibling.levelIcons)
                {
                    Color color =
                        icon.color;


                    color.a = 0f;


                    icon.color =
                        color;
                }
            }


            // -------------------------------------------------
            // 기존 Fade 코루틴 정리
            // -------------------------------------------------

            if (sibling.padeOutCoroutine != null)
            {
                sibling.StopCoroutine(
                    sibling.padeOutCoroutine
                );


                sibling.padeOutCoroutine = null;
            }


            // -------------------------------------------------
            // 어둡게
            // -------------------------------------------------

            sibling.padeOutCoroutine =
                sibling.StartCoroutine(
                    sibling.FadeOut()
                );
        }


        // =====================================================
        // Fade + 이펙트가 둘 다 끝날 때까지 기다림
        // =====================================================

        if (selectSequenceCoroutine == null)
        {
            selectSequenceCoroutine =
                StartCoroutine(
                    SelectionSequence()
                );
        }
    }


    // =========================================================
    // 선택 이펙트 재생
    // =========================================================

    private void PlaySelectEffect()
    {
        if (selectEffect == null)
            return;


        // 한번 껐다가 다시 켜서
        // 항상 처음부터 재생
        selectEffect.SetActive(false);
        selectEffect.SetActive(true);


        if (selectEffectParticles == null)
            return;


        foreach (ParticleSystem particle in selectEffectParticles)
        {
            if (particle == null)
                continue;


            particle.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );


            particle.Play(true);
        }
    }


    // =========================================================
    // 선택 순서
    //
    // 1. 다른 카드 어두워짐
    // 2. Fade 완료까지 기다림
    // 3. 선택 이펙트 완료까지 기다림
    // 4. 둘 다 완료되면 전체 UI 닫기
    // =========================================================

    private IEnumerator SelectionSequence()
    {
        // -----------------------------------------------------
        // 최소 한 프레임 기다려서
        // ParticleSystem이 정상적으로 재생 상태에 들어가게 함
        // -----------------------------------------------------

        yield return null;


        // =====================================================
        // 1. 다른 카드 FadeOut 완료 기다리기
        // =====================================================

        bool fadeFinished = false;


        while (!fadeFinished)
        {
            fadeFinished = true;


            foreach (SelectUpgrade sibling in siblingObjects)
            {
                if (!sibling.gameObject.activeInHierarchy)
                    continue;


                if (sibling.padeOutCoroutine != null)
                {
                    fadeFinished = false;

                    break;
                }
            }


            yield return null;
        }


        // =====================================================
        // 2. 선택 이펙트가 완전히 끝날 때까지 기다림
        // =====================================================

        bool effectFinished = false;


        while (!effectFinished)
        {
            effectFinished = true;


            if (selectEffectParticles != null &&
                selectEffectParticles.Length > 0)
            {
                foreach (ParticleSystem particle in selectEffectParticles)
                {
                    if (particle == null)
                        continue;


                    // 현재 파티클 또는 자식 파티클이
                    // 아직 살아있으면 기다린다.
                    if (particle.IsAlive(true))
                    {
                        effectFinished = false;

                        break;
                    }
                }
            }


            if (!effectFinished)
            {
                yield return null;
            }
        }


        // =====================================================
        // 3. Fade도 끝남
        //    이펙트도 끝남
        //
        // 이제 전체 UI 닫기
        // =====================================================

        yield return StartCoroutine(
            CloseUpgradeUI()
        );


        selectSequenceCoroutine = null;
    }


    // =========================================================
    // 선택 가능 여부
    // =========================================================

    private void SetSelectionEnabled(bool value)
    {
        selectionEnabled = value;


        if (selectionButton != null)
        {
            selectionButton.interactable =
                value;
        }
    }


    // =========================================================
    // 업그레이드 UI 등장
    // =========================================================

    private IEnumerator UpgredesMomOpen()
    {
        if (upgredesMom == null)
            yield break;


        upgredesMom.SetActive(true);


        float scale =
            upgredesMom.transform.localScale.x;


        while (scale < 1f)
        {
            scale +=
                speed *
                Time.unscaledDeltaTime;


            scale =
                Mathf.Min(
                    scale,
                    1f
                );


            upgredesMom.transform.localScale =
                new Vector3(
                    scale,
                    scale,
                    scale
                );


            yield return null;
        }


        upgredesMom.transform.localScale =
            Vector3.one;
    }


    // =========================================================
    // 전체 업그레이드 UI 닫기
    // =========================================================

    private IEnumerator CloseUpgradeUI()
    {
        if (upgredesMom == null)
            yield break;


        float scale =
            upgredesMom.transform.localScale.x;


        while (scale > 0.06f)
        {
            scale -=
                closeSpeed *
                Time.unscaledDeltaTime;


            scale =
                Mathf.Max(
                    scale,
                    0.06f
                );


            upgredesMom.transform.localScale =
                new Vector3(
                    scale,
                    scale,
                    scale
                );


            yield return null;
        }


        // -----------------------------------------------------
        // 선택 이펙트 종료
        // -----------------------------------------------------

        if (selectEffect != null)
        {
            selectEffect.SetActive(false);
        }


        upgredesMom.transform.localScale =
            Vector3.zero;


        Time.timeScale = 1f;


        upgredesMom.SetActive(false);
    }


    // =========================================================
    // 다른 카드 어둡게
    // =========================================================

    private IEnumerator FadeOut()
    {
        float value = 1f;


        while (value > darknessAmount)
        {
            value -=
                Time.unscaledDeltaTime *
                fadeOutSpeed;


            value =
                Mathf.Max(
                    value,
                    darknessAmount
                );


            // -------------------------------------------------
            // Image
            // -------------------------------------------------

            for (int i = 0; i < _images.Length; i++)
            {
                Color color =
                    originalColors[i];


                color.r *= value;
                color.g *= value;
                color.b *= value;


                // 기존 알파 유지
                color.a =
                    originalColors[i].a;


                _images[i].color =
                    color;
            }


            // -------------------------------------------------
            // Text
            // -------------------------------------------------

            for (int i = 0; i < _texts.Length; i++)
            {
                Color color =
                    originalTextColors[i];


                color.r *= value;
                color.g *= value;
                color.b *= value;


                color.a =
                    originalTextColors[i].a;


                _texts[i].color =
                    color;
            }


            yield return null;
        }


        padeOutCoroutine = null;
    }


    // =========================================================
    // 선택한 레벨 아이콘 효과
    // =========================================================

    private IEnumerator LevelUpAnim()
    {
        if (blinkLevelIcon != null)
        {
            StopCoroutine(
                blinkLevelIcon
            );


            blinkLevelIcon = null;
        }


        int level =
            upgradeUiInfo.upgradeData.upgradeLevel;


        if (level >= levelIcons.Length)
            yield break;


        Color color =
            levelIcons[level].color;


        color.a = 1f;


        levelIcons[level].color =
            color;


        if (level <
            levelIconsParticles.Length &&
            levelIconsParticles[level] != null)
        {
            levelIconsParticles[level].Play();
        }


        yield return null;
    }


    // =========================================================
    // 다음 레벨 아이콘 깜빡임
    // =========================================================

    private IEnumerator BlinkLevelIconCor()
    {
        yield return new WaitUntil(
            () =>
                upgradeUiInfo.upgradeData.id > 0
        );


        int level =
            upgradeUiInfo.upgradeData.upgradeLevel;


        if (level >= levelIcons.Length)
        {
            blinkLevelIcon = null;

            yield break;
        }


        Image image =
            levelIcons[level];


        float time = 0f;

        float blinkSpeed = 1.5f;


        Color color =
            image.color;


        while (true)
        {
            time +=
                Time.unscaledDeltaTime;


            color.a =
                Mathf.PingPong(
                    time * blinkSpeed,
                    1f
                );


            image.color =
                color;


            yield return null;
        }
    }
}
