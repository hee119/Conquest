using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HpBar : MonoBehaviour
{
    public PlayerCombat playerCombat;
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

    public void Awake()
    {
        hpSlider = GetComponent<Slider>();
        hpSlider.value = 1;

        CreateGhostSlider();
    }

    public void Start()
    {
        playerCombat.hpUpdate += HpUpdate;
    }

    public void HpUpdate()
    {
        if (playerCombat.maxHp <= 0f) return;

        targetHp = Mathf.Clamp01(playerCombat.hp / playerCombat.maxHp);
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
            $"HP={playerCombat.hp}/{playerCombat.maxHp}, " +
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

    private void OnDestroy()
    {
        if (playerCombat != null)
            playerCombat.hpUpdate -= HpUpdate;
    }
}
