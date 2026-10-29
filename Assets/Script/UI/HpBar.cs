using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HpBar : MonoBehaviour
{
    [SerializeField] private MonoBehaviour combat;
    public ICombat Combat;

    public Slider hpSlider;
    public Coroutine hpUpdateCor;

    [Header("HP Ghost Bar")]
    [SerializeField] private Color ghostColor = new Color(1f, 1f, 1f, 0.85f);
    [SerializeField] private float ghostDelay = 0.25f;
    [SerializeField] private float ghostSmoothTime = 0.35f;

    private float targetHp;
    private Slider ghostSlider;
    private Coroutine ghostUpdateCor;
    private float ghostVelocity;
    public float velocity;

    [Header("HP 색상")]
    public Color startHpColor = Color.green;
    public Color endHpColor = Color.red;

    public Color currentHpColor { get; private set; }

    [Header("이미지 색상 변경")]
    [Tooltip("HP가 줄어들수록 색이 바뀔 이미지")]
    [SerializeField] private Image targetImage;

    [Tooltip("HP가 0일 때 이미지가 도달할 목표 색")]
    [SerializeField] private Color targetColor = Color.red;

    private Color originColor;
    private bool initialized;
    private ICombat subscribedCombat;

    public void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized)
            return;
        initialized = true;
        hpSlider = GetComponent<Slider>();
        hpSlider.value = 1;

        if (targetImage != null)
            originColor = targetImage.color;

        CreateGhostSlider();
    }

    public void Start()
    {
        BindCombat(combat);
    }

    private void OnEnable()
    {
        BindCombat(combat);
    }

    private void OnDisable()
    {
        Unsubscribe();
        StopGhostUpdate();
    }

    public void BindCombat(MonoBehaviour source)
    {
        Initialize();
        Unsubscribe();
        StopGhostUpdate();
        combat = source;
        Combat = source != null ? source as ICombat : null;
        if (Combat != null && isActiveAndEnabled)
        {
            subscribedCombat = Combat;
            subscribedCombat.hpUpdate += HpUpdate;
        }

        targetHp = Combat != null && Combat.maxHp > 0f
            ? Mathf.Clamp01(Combat.hp / Combat.maxHp)
            : 0f;
        hpSlider.value = targetHp;
        if (ghostSlider != null)
            ghostSlider.value = targetHp;
        ghostVelocity = 0f;
        UpdateHpColor();
    }

    private void Unsubscribe()
    {
        if (subscribedCombat != null)
            subscribedCombat.hpUpdate -= HpUpdate;
        subscribedCombat = null;
    }

    private void StopGhostUpdate()
    {
        if (ghostUpdateCor != null)
            StopCoroutine(ghostUpdateCor);
        ghostUpdateCor = null;
    }

    public void HpUpdate()
    {
        if (Combat == null)
            return;
        UpdateHpColor();
        if (Combat.maxHp <= 0f) return;

        targetHp = Mathf.Clamp01(Combat.hp / Combat.maxHp);
        float previousHp = hpSlider.value;
        bool tookDamage = targetHp < previousHp;

        hpSlider.value = targetHp;

        if (ghostSlider == null)
            return;

        if (ghostUpdateCor != null)
        {
            StopCoroutine(ghostUpdateCor);
            ghostUpdateCor = null;
        }

        if (tookDamage)
        {
            ghostSlider.value = Mathf.Max(ghostSlider.value, previousHp);
            ghostVelocity = 0f;
            ghostUpdateCor = StartCoroutine(GhostUpdateCor());
        }
        else
        {
            ghostSlider.value = targetHp;
            ghostVelocity = 0f;
        }

        Debug.Log(
            $"HP={Combat.hp}/{Combat.maxHp}, " +
            $"Target={targetHp}, Slider={hpSlider.value}"
        );
    }

    private void CreateGhostSlider()
    {
        if (hpSlider.fillRect == null || hpSlider.fillRect.parent == null)
            return;

        Transform fillArea = hpSlider.fillRect.parent;
        GameObject ghostFillArea = Instantiate(fillArea.gameObject, fillArea.parent);
        ghostFillArea.name = "Ghost Fill Area";
        ghostFillArea.transform.SetSiblingIndex(fillArea.GetSiblingIndex());

        ghostSlider = ghostFillArea.AddComponent<Slider>();
        CopySliderSettings(hpSlider, ghostSlider);

        RectTransform ghostFillRect = FindChildRectTransform(ghostFillArea.transform, hpSlider.fillRect.name);
        if (ghostFillRect == null)
        {
            Destroy(ghostFillArea);
            ghostSlider = null;
            return;
        }

        ghostSlider.fillRect = ghostFillRect;
        ghostSlider.value = hpSlider.value;

        foreach (Graphic graphic in ghostFillArea.GetComponentsInChildren<Graphic>())
        {
            graphic.raycastTarget = false;
            graphic.color = ghostColor;
        }
    }

    private void CopySliderSettings(Slider source, Slider target)
    {
        target.transition = Selectable.Transition.None;
        target.interactable = false;
        target.navigation = new Navigation { mode = Navigation.Mode.None };
        target.direction = source.direction;
        target.minValue = source.minValue;
        target.maxValue = source.maxValue;
        target.wholeNumbers = source.wholeNumbers;
        target.value = source.value;
    }

    private RectTransform FindChildRectTransform(Transform parent, string childName)
    {
        foreach (RectTransform rectTransform in parent.GetComponentsInChildren<RectTransform>(true))
        {
            if (rectTransform.name == childName)
                return rectTransform;
        }

        return null;
    }

    private IEnumerator GhostUpdateCor()
    {
        yield return new WaitForSeconds(ghostDelay);

        while (Mathf.Abs(ghostSlider.value - targetHp) > 0.001f)
        {
            ghostSlider.value = Mathf.SmoothDamp(
                ghostSlider.value,
                targetHp,
                ref ghostVelocity,
                ghostSmoothTime
            );
            yield return null;
        }

        ghostSlider.value = targetHp;
        ghostUpdateCor = null;
    }



    public void UpdateHpColor()
    {
        if (Combat == null || Combat.maxHp <= 0f)
        {
            currentHpColor = endHpColor;

            if (targetImage != null)
                targetImage.color = targetColor;

            return;
        }

        float hpRatio =
            Mathf.Clamp01(
                Combat.hp / Combat.maxHp
            );

        currentHpColor =
            Color.Lerp(
                endHpColor,
                startHpColor,
                hpRatio
            );

        // HP가 줄어들수록 원래 색 → 목표 색으로 변화
        if (targetImage != null)
        {
            targetImage.color =
                Color.Lerp(
                    targetColor,
                    originColor,
                    hpRatio
                );
        }
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }
}
