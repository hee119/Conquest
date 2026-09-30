using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class ExpBar : MonoBehaviour
{
    // =========================================================
    // 글로우 설정
    // =========================================================

    [Header("Fill 진행 글로우")]
    [Tooltip("경험치 바가 차오를 때 Fill 맨 끝 글로우 밝기")]
    [SerializeField, Range(0f, 1f)]
    private float fillEndGlowIntensity = 0.7f;


    [Header("일반 경험치 획득 글로우")]
    [Tooltip("경험치를 얻었을 때 Fill 끝에서 번쩍이는 밝기")]
    [SerializeField, Range(0f, 1f)]
    private float gainGlowIntensity = 0.6f;

    [Tooltip("일반 경험치 글로우 지속 시간")]
    [SerializeField]
    private float gainGlowDuration = 0.4f;


    [Header("레벨업 글로우")]
    [Tooltip("레벨업할 때 Fill 끝에서 발생하는 글로우 밝기")]
    [SerializeField, Range(0f, 1f)]
    private float levelUpGlowIntensity = 1f;

    [Tooltip("레벨업 글로우 지속 시간")]
    [SerializeField]
    private float levelUpGlowDuration = 0.75f;


    // =========================================================
    // Slider
    // =========================================================

    private Slider expSlider;

    private RectTransform fillAreaRect;

    private RectTransform fillMaskRect;
    private RectTransform fillTextureRect;


    // =========================================================
    // 둥근 Fill 끝
    // =========================================================

    private RectTransform fillCapRect;
    private RectTransform fillCapTextureRect;

    private Image fillCapMaskImage;
    private Image fillCapImage;

    private Mask fillCapMask;


    // =========================================================
    // Fill 진행 글로우
    // =========================================================

    private RectTransform fillCapGlowRect;
    private Image fillCapGlowImage;


    // =========================================================
    // 레벨업 / EXP 획득 글로우
    // =========================================================

    private RectTransform levelUpRimGlowRect;
    private ExpBarRightEdgeGlow levelUpRimGlow;


    // =========================================================
    // Coroutine
    // =========================================================

    private Coroutine expUpdate;
    private Coroutine expInit;
    private Coroutine levelUpRimGlowUpdate;


    // =========================================================
    // EXP 값
    // =========================================================

    private float targetValue;
    private float velocity;
    private float previousTargetValue;

    private bool pendingLevelUp;


    // =========================================================
    // Editor
    // =========================================================

    private float lastEditorSliderValue = -1f;

    private Vector2 lastEditorFillAreaSize =
        new Vector2(-1f, -1f);


#if UNITY_EDITOR
    private bool editorSetupQueued;
#endif


    // =========================================================
    // 코드로 만든 원형 Mask Sprite
    // =========================================================

    private static Sprite circleMaskSprite;


    // =========================================================
    // Awake
    // =========================================================

    public void Awake()
    {
        if (!Application.isPlaying)
        {
            QueueEditorSetup();
            return;
        }

        SetupExpBar();

        if (expSlider == null)
            return;


        expSlider.value = 0f;

        SetFillCapGlowVisible(false);


        Debug.Log(
            $"[ExpBar] Awake - Slider 초기화 / value = {expSlider.value}"
        );
    }


    // =========================================================
    // Enable
    // =========================================================

    private void OnEnable()
    {
        expSlider =
            GetComponent<Slider>();


        RegisterSliderValueChanged();


        if (Application.isPlaying)
            return;


        QueueEditorSetup();
    }


    private void OnValidate()
    {
        QueueEditorSetup();
    }


    // =========================================================
    // Editor Update
    // =========================================================

    private void Update()
    {
        if (Application.isPlaying)
            return;


        if (expSlider == null)
        {
            expSlider =
                GetComponent<Slider>();
        }


        if (expSlider == null)
            return;


        if (
            fillAreaRect == null ||
            fillMaskRect == null ||
            fillTextureRect == null
        )
        {
            QueueEditorSetup();
            return;
        }


        Vector2 fillAreaSize =
            fillAreaRect.rect.size;


        float sliderValue =
            expSlider.normalizedValue;


        if (
            !Mathf.Approximately(
                lastEditorSliderValue,
                sliderValue
            )
            ||
            lastEditorFillAreaSize != fillAreaSize
        )
        {
            lastEditorSliderValue =
                sliderValue;


            lastEditorFillAreaSize =
                fillAreaSize;


            UpdateFillTextureLayout();


            SetFillCapGlowVisible(false);
        }
    }


    // =========================================================
    // Start
    // =========================================================

    public void Start()
    {
        if (!Application.isPlaying)
            return;


        Debug.Log("[ExpBar] Start");


        LevelManager.Instance.expUpdate +=
            ExpUpdate;


        UpgradeManager.Instance.upgradeSelect +=
            ExpInit;


        Debug.Log(
            "[ExpBar] 이벤트 등록 완료"
        );
    }


    // =========================================================
    // EXP 변경
    // =========================================================

    public void ExpUpdate()
    {
        float currentExp =
            LevelManager.Instance.currentExp;


        float maxExp =
            DataManager.Instance.levelDatas[
                LevelManager.Instance.currentLevel
            ];


        float newTarget =
            currentExp / maxExp;


        bool leveledUp =
            newTarget < previousTargetValue;


        bool gained =
            newTarget > previousTargetValue;


        // =====================================================
        // 레벨업
        // =====================================================

        if (leveledUp)
        {
            pendingLevelUp = true;


            PlayRightEdgeGlow(
                levelUpGlowIntensity,
                levelUpGlowDuration,
                true
            );
        }

        // =====================================================
        // 일반 EXP 획득
        // =====================================================

        else if (
            gained &&
            !pendingLevelUp &&
            expInit == null
        )
        {
            PlayRightEdgeGlow(
                gainGlowIntensity,
                gainGlowDuration,
                false
            );
        }


        previousTargetValue =
            newTarget;


        // 레벨업한 경우
        // 증강 선택 전까지 바를 끝까지 채움
        targetValue =
            (expInit != null || pendingLevelUp)
                ? 1f
                : newTarget;


        if (
            expInit == null &&
            expUpdate == null
        )
        {
            velocity = 0f;


            expUpdate =
                StartCoroutine(
                    ExpUpdateCor()
                );
        }
    }


    // =========================================================
    // EXP 초기화
    // =========================================================

    public void ExpInit()
    {
        Debug.Log(
            $"[ExpBar] ExpInit 호출\n" +
            $"slider.value = {expSlider.value}\n" +
            $"expUpdate 진행 중 = {expUpdate != null}\n" +
            $"expInit 진행 중 = {expInit != null}"
        );


        if (expInit == null)
        {
            expInit =
                StartCoroutine(
                    ExpInitCor()
                );
        }
        else
        {
            Debug.Log(
                "[ExpBar] ExpInitCor 이미 실행 중 → 무시"
            );
        }
    }


    // =========================================================
    // EXP 증가 애니메이션
    // =========================================================

    private IEnumerator ExpUpdateCor()
    {
        int frame = 0;


        SetFillCapGlowVisible(
            targetValue >
            expSlider.value
        );


        while (
            Mathf.Abs(
                expSlider.value -
                targetValue
            ) > 0.001f
        )
        {
            float difference =
                targetValue -
                expSlider.value;


            SetFillCapGlowVisible(
                difference > 0f
            );


            if (
                difference > 0f &&
                velocity < 0f
            )
            {
                velocity = 0f;
            }

            else if (
                difference < 0f &&
                velocity > 0f
            )
            {
                velocity = 0f;
            }


            float beforeValue =
                expSlider.value;


            expSlider.value =
                Mathf.SmoothDamp(
                    expSlider.value,
                    targetValue,
                    ref velocity,
                    0.15f,
                    Mathf.Infinity,
                    Time.unscaledDeltaTime
                );


            UpdateFillTextureLayout();


            if (frame % 10 == 0)
            {
                Debug.Log(
                    $"[ExpBar] ExpUpdateCor 진행\n" +
                    $"slider : {beforeValue} → {expSlider.value}\n" +
                    $"target : {targetValue}\n" +
                    $"difference : {difference}\n" +
                    $"velocity : {velocity}"
                );
            }


            frame++;


            yield return null;
        }


        expSlider.value =
            targetValue;


        UpdateFillTextureLayout();


        SetFillCapGlowVisible(false);


        velocity = 0f;

        expUpdate = null;
    }


    // =========================================================
    // 레벨업 후 초기화
    // =========================================================

    private IEnumerator ExpInitCor()
    {
        if (expUpdate != null)
        {
            StopCoroutine(
                expUpdate
            );


            expUpdate = null;
        }


        SetFillCapGlowVisible(false);


        velocity = 0f;


        float t = 0f;


        float startValue =
            expSlider.value;


        // =====================================================
        // EXP 바 0으로 감소
        // =====================================================

        while (expSlider.value > 0f)
        {
            t +=
                Time.unscaledDeltaTime *
                4f;


            expSlider.value =
                Mathf.Lerp(
                    startValue,
                    0f,
                    t
                );


            UpdateFillTextureLayout();


            yield return null;
        }


        expSlider.value = 0f;


        UpdateFillTextureLayout();


        // =====================================================
        // 현재 EXP 재계산
        // =====================================================

        float currentExp =
            LevelManager.Instance.currentExp;


        float maxExp =
            DataManager.Instance.levelDatas[
                LevelManager.Instance.currentLevel
            ];


        targetValue =
            currentExp /
            maxExp;


        previousTargetValue =
            targetValue;


        pendingLevelUp =
            false;


        velocity = 0f;


        expUpdate =
            StartCoroutine(
                ExpUpdateCor()
            );


        expInit = null;
    }


    // =========================================================
    // Disable
    // =========================================================

    private void OnDisable()
    {
        UnregisterSliderValueChanged();


        if (!Application.isPlaying)
            return;


        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.expUpdate -=
                ExpUpdate;
        }


        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.upgradeSelect -=
                ExpInit;
        }
    }


    // =========================================================
    // Setup
    // =========================================================

    private void SetupExpBar()
    {
        expSlider =
            GetComponent<Slider>();


        if (expSlider == null)
            return;


        fillAreaRect =
            expSlider.fillRect != null
                ? expSlider.fillRect.parent
                    as RectTransform
                : null;


        SetupExpBarImages();


        UpdateFillTextureLayout();


        SetFillCapGlowVisible(false);


        RegisterSliderValueChanged();
    }


    // =========================================================
    // Slider Event
    // =========================================================

    private void RegisterSliderValueChanged()
    {
        if (expSlider == null)
            return;


        expSlider.onValueChanged.RemoveListener(
            HandleSliderValueChanged
        );


        expSlider.onValueChanged.AddListener(
            HandleSliderValueChanged
        );
    }


    private void UnregisterSliderValueChanged()
    {
        if (expSlider != null)
        {
            expSlider.onValueChanged.RemoveListener(
                HandleSliderValueChanged
            );
        }
    }


    private void HandleSliderValueChanged(
        float _
    )
    {
        if (
            fillAreaRect == null ||
            fillMaskRect == null ||
            fillTextureRect == null
        )
        {
            QueueEditorSetup();

            return;
        }


        lastEditorSliderValue =
            expSlider != null
                ? expSlider.normalizedValue
                : -1f;


        if (fillAreaRect != null)
        {
            lastEditorFillAreaSize =
                fillAreaRect.rect.size;
        }


        UpdateFillTextureLayout();


        if (!Application.isPlaying)
        {
            SetFillCapGlowVisible(false);
        }
    }


    // =========================================================
    // Editor Setup
    // =========================================================

    private void QueueEditorSetup()
    {
#if UNITY_EDITOR

        if (editorSetupQueued)
            return;


        editorSetupQueued =
            true;


        EditorApplication.delayCall +=
            DelayedEditorSetup;

#else

        SetupExpBar();

#endif
    }


#if UNITY_EDITOR

    private void DelayedEditorSetup()
    {
        editorSetupQueued =
            false;


        if (
            this == null ||
            !isActiveAndEnabled
        )
            return;


        SetupExpBar();


        if (expSlider != null)
        {
            lastEditorSliderValue =
                expSlider.normalizedValue;
        }


        if (fillAreaRect != null)
        {
            lastEditorFillAreaSize =
                fillAreaRect.rect.size;
        }
    }

#endif


    // =========================================================
    // 이미지 Setup
    // =========================================================

    private void SetupExpBarImages()
    {
        if (expSlider == null)
            return;


        Sprite frameSprite =
            LoadSprite(
                "UI/ExpBarFrame"
            );


        Sprite fillSprite =
            LoadSprite(
                "UI/ExpBarFill"
            );


        Sprite endGlowSprite =
            LoadSprite(
                "UI/ExpBarEndGlow"
            );


        Image backgroundImage =
            transform
                .Find("Background")
                ?.GetComponent<Image>();


        Image sliderFillImage =
            expSlider.fillRect != null
                ? expSlider.fillRect
                    .GetComponent<Image>()
                : null;


        if (
            backgroundImage != null &&
            frameSprite != null
        )
        {
            backgroundImage.sprite =
                frameSprite;


            backgroundImage.color =
                Color.white;


            backgroundImage.type =
                Image.Type.Simple;


            backgroundImage.preserveAspect =
                false;


            backgroundImage.raycastTarget =
                false;
        }


        // Unity 기본 Fill 숨기기
        if (sliderFillImage != null)
        {
            sliderFillImage.color =
                Color.clear;


            sliderFillImage.raycastTarget =
                false;
        }


        CreateFillMask();


        CreateFillTexture(
            fillSprite
        );


        DisableLegacyFillTextures();


        // ★ 둥근 Fill 끝
        CreateFillCap(
            fillSprite
        );


        DisableLegacyCapHighlight();


        CreateFillCapGlow(
            endGlowSprite
        );


        CreateLevelUpRimGlow();
    }


    // =========================================================
    // 사각형 Fill Mask
    // =========================================================

    private void CreateFillMask()
    {
        if (fillAreaRect == null)
            return;


        Transform existingMask =
            fillAreaRect.Find(
                "Galaxy Fill Mask"
            );


        GameObject fillMaskObject =
            existingMask != null
                ? existingMask.gameObject
                : new GameObject(
                    "Galaxy Fill Mask",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(RectMask2D)
                );


        if (existingMask == null)
        {
            fillMaskObject.transform.SetParent(
                fillAreaRect,
                false
            );
        }


        fillMaskObject.transform
            .SetAsFirstSibling();


        fillMaskRect =
            fillMaskObject.GetComponent<
                RectTransform
            >();


        fillMaskRect.anchorMin =
            new Vector2(
                0f,
                0f
            );


        fillMaskRect.anchorMax =
            new Vector2(
                0f,
                1f
            );


        fillMaskRect.pivot =
            new Vector2(
                0f,
                0.5f
            );


        fillMaskRect.anchoredPosition =
            Vector2.zero;
    }


    // =========================================================
    // Sprite Loader
    // =========================================================

    private Sprite LoadSprite(
        string resourcesPath
    )
    {
        Sprite sprite =
            Resources.Load<Sprite>(
                resourcesPath
            );


        if (sprite != null)
            return sprite;


        Texture2D texture =
            Resources.Load<Texture2D>(
                resourcesPath
            );


        if (texture == null)
            return null;


        return Sprite.Create(
            texture,

            new Rect(
                0f,
                0f,
                texture.width,
                texture.height
            ),

            new Vector2(
                0.5f,
                0.5f
            ),

            100f
        );
    }


    // =========================================================
    // Fill 본체
    // =========================================================

    private void CreateFillTexture(
        Sprite fillSprite
    )
    {
        if (
            fillMaskRect == null ||
            fillSprite == null
        )
            return;


        Transform existingFill =
            fillMaskRect.Find(
                "Galaxy Fill"
            );


        GameObject fillTextureObject =
            existingFill != null
                ? existingFill.gameObject
                : new GameObject(
                    "Galaxy Fill",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image)
                );


        if (existingFill == null)
        {
            fillTextureObject.transform.SetParent(
                fillMaskRect,
                false
            );
        }


        fillTextureObject.transform
            .SetAsFirstSibling();


        fillTextureRect =
            fillTextureObject.GetComponent<
                RectTransform
            >();


        Image fillTextureImage =
            fillTextureObject.GetComponent<
                Image
            >();


        if (fillTextureImage == null)
        {
            fillTextureImage =
                fillTextureObject.AddComponent<
                    Image
                >();
        }


        fillTextureRect.anchorMin =
            new Vector2(
                0f,
                0f
            );


        fillTextureRect.anchorMax =
            new Vector2(
                0f,
                1f
            );


        fillTextureRect.pivot =
            new Vector2(
                0f,
                0.5f
            );


        fillTextureRect.anchoredPosition =
            Vector2.zero;


        fillTextureImage.sprite =
            fillSprite;


        fillTextureImage.color =
            Color.white;


        fillTextureImage.type =
            Image.Type.Simple;


        fillTextureImage.preserveAspect =
            false;


        fillTextureImage.raycastTarget =
            false;
    }


    // =========================================================
    // ★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★
    //
    // 둥근 Fill 끝
    //
    // 사각형 Fill 끝에 원을 겹쳐서
    // | 가 아니라 ) 형태로 만든다.
    //
    // ★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★
    // =========================================================

    private void CreateFillCap(
        Sprite fillSprite
    )
    {
        if (
            fillAreaRect == null ||
            fillSprite == null
        )
            return;


        Transform existingCap =
            fillAreaRect.Find(
                "Galaxy Fill Cap"
            );


        GameObject fillCapObject;


        if (existingCap != null)
        {
            fillCapObject =
                existingCap.gameObject;
        }
        else
        {
            fillCapObject =
                new GameObject(
                    "Galaxy Fill Cap",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Mask)
                );


            fillCapObject.transform.SetParent(
                fillAreaRect,
                false
            );
        }


        fillCapObject.SetActive(true);


        // Fill 본체보다 위
        fillCapObject.transform
            .SetAsLastSibling();


        fillCapRect =
            fillCapObject.GetComponent<
                RectTransform
            >();


        // =====================================================
        // 원형 Mask Image
        // =====================================================

        fillCapMaskImage =
            fillCapObject.GetComponent<Image>();


        if (fillCapMaskImage == null)
        {
            fillCapMaskImage =
                fillCapObject.AddComponent<Image>();
        }


        fillCapMask =
            fillCapObject.GetComponent<Mask>();


        if (fillCapMask == null)
        {
            fillCapMask =
                fillCapObject.AddComponent<Mask>();
        }


        fillCapMaskImage.sprite =
            GetCircleMaskSprite();


        fillCapMaskImage.color =
            Color.white;


        fillCapMaskImage.type =
            Image.Type.Simple;


        fillCapMaskImage.preserveAspect =
            false;


        fillCapMaskImage.raycastTarget =
            false;


        // 원 자체는 숨기고
        // 안의 Galaxy Fill만 보임
        fillCapMask.showMaskGraphic =
            false;


        // Fill Area 왼쪽 기준
        fillCapRect.anchorMin =
            new Vector2(
                0f,
                0.5f
            );


        fillCapRect.anchorMax =
            new Vector2(
                0f,
                0.5f
            );


        // ★ 원 중앙을 기준
        fillCapRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );


        // =====================================================
        // 원 안에 들어갈 Galaxy Texture
        // =====================================================

        Transform existingTexture =
            fillCapObject.transform.Find(
                "Galaxy Fill Cap Texture"
            );


        GameObject textureObject =
            existingTexture != null
                ? existingTexture.gameObject
                : new GameObject(
                    "Galaxy Fill Cap Texture",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image)
                );


        if (existingTexture == null)
        {
            textureObject.transform.SetParent(
                fillCapObject.transform,
                false
            );
        }


        textureObject.SetActive(true);


        fillCapTextureRect =
            textureObject.GetComponent<
                RectTransform
            >();


        fillCapImage =
            textureObject.GetComponent<
                Image
            >();


        if (fillCapImage == null)
        {
            fillCapImage =
                textureObject.AddComponent<Image>();
        }


        fillCapImage.sprite =
            fillSprite;


        fillCapImage.color =
            Color.white;


        fillCapImage.type =
            Image.Type.Simple;


        fillCapImage.preserveAspect =
            false;


        fillCapImage.raycastTarget =
            false;


        // ★ 원 전체를 Galaxy Fill로 꽉 채움
        fillCapTextureRect.anchorMin =
            Vector2.zero;


        fillCapTextureRect.anchorMax =
            Vector2.one;


        fillCapTextureRect.offsetMin =
            Vector2.zero;


        fillCapTextureRect.offsetMax =
            Vector2.zero;


        fillCapTextureRect.localScale =
            Vector3.one;


        fillCapTextureRect.anchoredPosition =
            Vector2.zero;
    }


    // =========================================================
    // ★ 원형 Mask Sprite 생성
    // =========================================================

    private Sprite GetCircleMaskSprite()
    {
        if (circleMaskSprite != null)
            return circleMaskSprite;


        const int size = 128;


        Texture2D texture =
            new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false
            );


        texture.name =
            "ExpBar Circle Mask";


        texture.filterMode =
            FilterMode.Bilinear;


        texture.wrapMode =
            TextureWrapMode.Clamp;


        float center =
            (size - 1) *
            0.5f;


        float radius =
            (size - 2) *
            0.5f;


        Color[] pixels =
            new Color[
                size * size
            ];


        for (
            int y = 0;
            y < size;
            y++
        )
        {
            for (
                int x = 0;
                x < size;
                x++
            )
            {
                float dx =
                    x - center;


                float dy =
                    y - center;


                float distance =
                    Mathf.Sqrt(
                        dx * dx +
                        dy * dy
                    );


                // 1픽셀 정도 Anti Alias
                float alpha =
                    Mathf.Clamp01(
                        radius -
                        distance +
                        1f
                    );


                pixels[
                    y * size + x
                ] =
                    new Color(
                        1f,
                        1f,
                        1f,
                        alpha
                    );
            }
        }


        texture.SetPixels(
            pixels
        );


        texture.Apply();


        circleMaskSprite =
            Sprite.Create(
                texture,

                new Rect(
                    0f,
                    0f,
                    size,
                    size
                ),

                new Vector2(
                    0.5f,
                    0.5f
                ),

                100f
            );


        circleMaskSprite.name =
            "ExpBar Circle Mask Sprite";


        return circleMaskSprite;
    }


    // =========================================================
    // Legacy 제거
    // =========================================================

    private void DisableLegacyCapHighlight()
    {
        if (fillAreaRect == null)
            return;


        Transform legacyGlow =
            fillAreaRect.Find(
                "Galaxy Fill Cap Highlight"
            );


        if (legacyGlow != null)
        {
            legacyGlow.gameObject
                .SetActive(false);
        }
    }


    private void DisableLegacyFillTextures()
    {
        if (fillAreaRect == null)
            return;


        DisableLegacyFillTexture(
            fillAreaRect.Find(
                "Galaxy Fill"
            )
        );


        if (
            expSlider == null ||
            expSlider.fillRect == null
        )
            return;


        Transform[] fillChildren =
            expSlider.fillRect
                .GetComponentsInChildren<
                    Transform
                >(true);


        foreach (
            Transform child
            in fillChildren
        )
        {
            if (
                child == expSlider.fillRect ||
                child == fillTextureRect
            )
                continue;


            if (
                child.name ==
                "Galaxy Fill"
            )
            {
                DisableLegacyFillTexture(
                    child
                );
            }
        }
    }


    private void DisableLegacyFillTexture(
        Transform legacyFill
    )
    {
        if (
            legacyFill == null ||
            legacyFill == fillTextureRect
        )
            return;


        legacyFill.gameObject
            .SetActive(false);
    }


    // =========================================================
    // Fill 진행 글로우
    // =========================================================

    private void CreateFillCapGlow(
        Sprite glowSprite
    )
    {
        if (
            fillAreaRect == null ||
            glowSprite == null
        )
            return;


        Transform existingGlow =
            fillAreaRect.Find(
                "Fill End Glow"
            );


        GameObject glowObject =
            existingGlow != null
                ? existingGlow.gameObject
                : new GameObject(
                    "Fill End Glow",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image)
                );


        if (existingGlow == null)
        {
            glowObject.transform.SetParent(
                fillAreaRect,
                false
            );
        }


        glowObject.SetActive(true);


        glowObject.transform
            .SetAsLastSibling();


        fillCapGlowRect =
            glowObject.GetComponent<
                RectTransform
            >();


        fillCapGlowImage =
            glowObject.GetComponent<Image>();


        if (fillCapGlowImage == null)
        {
            fillCapGlowImage =
                glowObject.AddComponent<Image>();
        }


        fillCapGlowRect.anchorMin =
            new Vector2(
                0f,
                0.5f
            );


        fillCapGlowRect.anchorMax =
            new Vector2(
                0f,
                0.5f
            );


        fillCapGlowRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );


        fillCapGlowImage.sprite =
            glowSprite;


        fillCapGlowImage.type =
            Image.Type.Simple;


        fillCapGlowImage.preserveAspect =
            false;


        fillCapGlowImage.raycastTarget =
            false;
    }


    // =========================================================
    // EXP 획득 / 레벨업 글로우
    // =========================================================

    private void CreateLevelUpRimGlow()
    {
        if (fillAreaRect == null)
            return;


        Transform existingGlow =
            fillAreaRect.Find(
                "Level Up Right Rim Glow"
            );


        GameObject glowObject =
            existingGlow != null
                ? existingGlow.gameObject
                : new GameObject(
                    "Level Up Right Rim Glow",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(ExpBarRightEdgeGlow)
                );


        if (existingGlow == null)
        {
            glowObject.transform.SetParent(
                fillAreaRect,
                false
            );
        }


        glowObject.SetActive(true);


        glowObject.transform
            .SetAsLastSibling();


        levelUpRimGlowRect =
            glowObject.GetComponent<
                RectTransform
            >();


        levelUpRimGlow =
            glowObject.GetComponent<
                ExpBarRightEdgeGlow
            >();


        if (levelUpRimGlow == null)
        {
            levelUpRimGlow =
                glowObject.AddComponent<
                    ExpBarRightEdgeGlow
                >();
        }


        // ★ Fill Area 왼쪽 기준
        levelUpRimGlowRect.anchorMin =
            new Vector2(
                0f,
                0.5f
            );


        levelUpRimGlowRect.anchorMax =
            new Vector2(
                0f,
                0.5f
            );


        levelUpRimGlowRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );


        levelUpRimGlow.raycastTarget =
            false;


        SetLevelUpRimGlowAlpha(0f);
    }


    // =========================================================
    // 전체 Fill Layout
    // =========================================================

    private void UpdateFillTextureLayout()
    {
        if (fillAreaRect == null)
            return;


        Vector2 size =
            fillAreaRect.rect.size;


        float width =
            size.x;


        float height =
            size.y;


        UpdateFillMask(
            width,
            height
        );


        UpdateFillTextureSize(
            width
        );


        UpdateFillCap(
            width,
            height
        );


        UpdateFillCapGlow(
            width,
            height
        );


        UpdateLevelUpRimGlow(
            width,
            height
        );
    }


    // =========================================================
    // ★ 사각 Fill 부분
    // =========================================================

    private void UpdateFillMask(
        float width,
        float height
    )
    {
        if (
            fillMaskRect == null ||
            expSlider == null
        )
            return;


        float value =
            GetSliderFillValue();


        float endX =
            width *
            value;


        if (endX <= 0f)
        {
            fillMaskRect.sizeDelta =
                new Vector2(
                    0f,
                    0f
                );


            return;
        }


        // 둥근 Cap의 크기
        float capSize =
            Mathf.Min(
                height,
                endX
            );


        float radius =
            capSize *
            0.5f;


        // ★ 원의 중심까지만 사각 Fill
        float bodyWidth =
            Mathf.Max(
                0f,
                endX -
                radius
            );


        fillMaskRect.sizeDelta =
            new Vector2(
                bodyWidth,
                0f
            );
    }


    // =========================================================
    // Galaxy Texture
    // =========================================================

    private void UpdateFillTextureSize(
        float width
    )
    {
        if (fillTextureRect == null)
            return;


        fillTextureRect.sizeDelta =
            new Vector2(
                width,
                0f
            );
    }


    // =========================================================
    // ★ 둥근 끝 위치
    // =========================================================

    private void UpdateFillCap(
        float width,
        float height
    )
    {
        if (
            fillCapRect == null ||
            fillCapImage == null ||
            expSlider == null
        )
            return;


        float value =
            GetSliderFillValue();


        float endX =
            width *
            value;


        if (endX <= 0.001f)
        {
            fillCapRect.gameObject
                .SetActive(false);


            return;
        }


        fillCapRect.gameObject
            .SetActive(true);


        // =====================================================
        // 평소에는 바 높이와 같은 원
        //
        // EXP가 아주 조금밖에 없으면
        // 왼쪽 밖으로 튀어나가지 않도록 작게 만듦
        // =====================================================

        float capSize =
            Mathf.Min(
                height,
                endX
            );


        float radius =
            capSize *
            0.5f;


        // ★ 원의 오른쪽 끝이 정확히 Fill 끝에 맞음
        float centerX =
            endX -
            radius;


        fillCapRect.sizeDelta =
            new Vector2(
                capSize,
                capSize
            );


        fillCapRect.anchoredPosition =
            new Vector2(
                centerX,
                0f
            );
    }


    // =========================================================
    // Fill 끝 글로우
    // =========================================================

    private void UpdateFillCapGlow(
        float width,
        float height
    )
    {
        if (
            fillCapGlowRect == null ||
            fillCapGlowImage == null ||
            expSlider == null
        )
            return;


        float value =
            GetSliderFillValue();


        float glowSize =
            Mathf.Max(
                1f,
                height * 2.4f
            );


        fillCapGlowRect.sizeDelta =
            new Vector2(
                glowSize,
                glowSize
            );


        // ★ Fill 실제 끝
        fillCapGlowRect.anchoredPosition =
            new Vector2(
                width * value,
                0f
            );


        Color color =
            Color.white;


        color.a =
            Mathf.Clamp01(
                fillEndGlowIntensity
            );


        fillCapGlowImage.color =
            color;
    }


    // =========================================================
    // Slider Value
    // =========================================================

    private float GetSliderFillValue()
    {
        return expSlider != null
            ? Mathf.Clamp01(
                expSlider.normalizedValue
            )
            : 0f;
    }


    // =========================================================
    // 레벨업 글로우 위치
    // =========================================================

    private void UpdateLevelUpRimGlow(
        float width,
        float height
    )
    {
        if (
            levelUpRimGlowRect == null ||
            expSlider == null
        )
            return;


        float value =
            GetSliderFillValue();


        float glowHeight =
            Mathf.Max(
                1f,
                height * 1.22f
            );


        levelUpRimGlowRect.sizeDelta =
            new Vector2(
                glowHeight * 1.45f,
                glowHeight
            );


        // ★ 현재 Fill 끝
        levelUpRimGlowRect.anchoredPosition =
            new Vector2(
                width * value,
                0f
            );
    }


    // =========================================================
    // 글로우 재생
    // =========================================================

    private void PlayRightEdgeGlow(
        float intensity,
        float duration,
        bool pulse
    )
    {
        if (!Application.isPlaying)
            return;


        if (levelUpRimGlow == null)
        {
            SetupExpBar();
        }


        if (levelUpRimGlow == null)
            return;


        if (levelUpRimGlowUpdate != null)
        {
            StopCoroutine(
                levelUpRimGlowUpdate
            );
        }


        levelUpRimGlowUpdate =
            StartCoroutine(
                RightEdgeGlowCor(
                    intensity,
                    duration,
                    pulse
                )
            );
    }


    private IEnumerator RightEdgeGlowCor(
        float intensity,
        float duration,
        bool pulse
    )
    {
        float elapsed =
            0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float progress =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );


            float shape;


            if (pulse)
            {
                // 0 → 밝아짐 → 0
                shape =
                    Mathf.Sin(
                        progress *
                        Mathf.PI
                    );
            }
            else
            {
                // 밝게 시작 → 서서히 감소
                shape =
                    (1f - progress) *
                    (1f - progress);
            }


            SetLevelUpRimGlowAlpha(
                shape *
                intensity
            );


            yield return null;
        }


        SetLevelUpRimGlowAlpha(0f);


        levelUpRimGlowUpdate =
            null;
    }


    // =========================================================
    // 레벨업 / 획득 Glow Alpha
    // =========================================================

    private void SetLevelUpRimGlowAlpha(
        float alpha
    )
    {
        if (levelUpRimGlow == null)
            return;


        Color color =
            Color.white;


        color.a =
            Mathf.Clamp01(
                alpha
            );


        levelUpRimGlow.color =
            color;
    }


    // =========================================================
    // Fill 진행 Glow
    // =========================================================

    private void SetFillCapGlowVisible(
        bool visible
    )
    {
        if (fillCapGlowImage == null)
            return;


        Color color =
            Color.white;


        color.a =
            Mathf.Clamp01(
                fillEndGlowIntensity
            );


        fillCapGlowImage.color =
            color;


        fillCapGlowImage.enabled =
            visible &&
            fillEndGlowIntensity > 0.001f;
    }
}