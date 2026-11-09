using TMPro;
using UnityEngine;
using DG.Tweening;

public sealed class RunRewardView : MonoBehaviour
{
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text fragmentText;
    [SerializeField] private TMP_Text goldLabel;
    [SerializeField] private TMP_Text fragmentLabel;
    [SerializeField] private UnityEngine.UI.Image portrait;
    [SerializeField] private RectTransform[] rewardCards;
    [SerializeField] private CanvasGroup[] rewardCardGroups;
    [SerializeField, Min(0.05f)] private float appearDuration = 0.25f;
    [SerializeField, Min(0f)] private float displayPause = 0.35f;
    [SerializeField, Min(0.05f)] private float slideDuration = 0.35f;
    private RunReward reward;
    private LocalizationManager localization;
    private Vector2[] cardPositions;
    private Vector3[] cardScales;
    private Sequence reveal;

    private void Awake() => CacheLayout();

    
    
    private void CacheLayout()
    {
        if (rewardCards == null || rewardCards.Length == 0)
            return;

        // 배열이 정상적인 크기로 준비된 경우에만 재사용
        if (cardPositions != null &&
            cardScales != null &&
            cardPositions.Length == rewardCards.Length &&
            cardScales.Length == rewardCards.Length)
            return;

        // 배열 크기가 다르면 다시 생성
        cardPositions = new Vector2[rewardCards.Length];
        cardScales = new Vector3[rewardCards.Length];

        for (int i = 0; i < rewardCards.Length; i++)
        {
            if (rewardCards[i] == null)
            {
                Debug.LogError(
                    $"[RunRewardView] Reward Card {i} 참조 누락",
                    this
                );
                continue;
            }

            cardPositions[i] = rewardCards[i].anchoredPosition;
            cardScales[i] = rewardCards[i].localScale;
        }

        Debug.Log(
            $"[RunRewardView] 캐시 재생성 완료 | " +
            $"Cards={rewardCards.Length}, " +
            $"Positions={cardPositions.Length}, " +
            $"Scales={cardScales.Length}",
            this
        );
    }



    private void OnEnable()
    {
        localization = LocalizationManager.Instance;
        if (localization != null) localization.OnLanguageChanged += Refresh;
        Refresh();
        ResetPresentation();
    }
    private void OnDisable()
    {
        if (localization != null) localization.OnLanguageChanged -= Refresh;
        ResetPresentation();
    }
    public void Bind(RunReward value) { reward = value; Refresh(); ResetPresentation(); }

    
    private void ResetPresentation()
    {
        Debug.Log(
            $"[RunRewardView][Reset START] Object={name} | " +
            $"Active={gameObject.activeInHierarchy}",
            this
        );

        reveal?.Kill();
        reveal = null;

        CacheLayout();

        Debug.Log(
            $"[RunRewardView][ARRAY STATUS]\n" +
            $"Cards = {rewardCards?.Length ?? -1}\n" +
            $"Groups = {rewardCardGroups?.Length ?? -1}\n" +
            $"Positions = {cardPositions?.Length ?? -1}\n" +
            $"Scales = {cardScales?.Length ?? -1}",
            this
        );

        if (cardPositions == null || rewardCardGroups == null)
        {
            Debug.LogError(
                "[RunRewardView] 배열이 NULL이어서 초기화 중단",
                this
            );
            return;
        }

        for (int i = 0; i < rewardCards.Length; i++)
        {
            Debug.Log(
                $"[RunRewardView][INDEX CHECK] i={i} | " +
                $"Card={i < rewardCards.Length} | " +
                $"Group={i < rewardCardGroups.Length} | " +
                $"Position={i < cardPositions.Length} | " +
                $"Scale={i < cardScales.Length}",
                this
            );

            if (i >= rewardCardGroups.Length ||
                i >= cardPositions.Length ||
                i >= cardScales.Length)
            {
                Debug.LogError(
                    $"[RunRewardView] 배열 범위 초과! Index={i}",
                    this
                );
                return;
            }

            if (rewardCards[i] == null || rewardCardGroups[i] == null)
            {
                Debug.LogError(
                    $"[RunRewardView] 참조 누락! Index={i}",
                    this
                );
                continue;
            }

            rewardCards[i].anchoredPosition = cardPositions[i];
            rewardCards[i].localScale = cardScales[i];

            rewardCardGroups[i].alpha = 0f;
            rewardCardGroups[i].interactable = false;
            rewardCardGroups[i].blocksRaycasts = false;
        }

        Debug.Log("[RunRewardView] Reset 완료", this);
    }


    public void PlayReveal()
    {
        if (!isActiveAndEnabled || reward == null) return;
        ResetPresentation();
        if (rewardCards == null || rewardCards.Length == 0) return;
        reveal = DOTween.Sequence().SetUpdate(true);
        float spacing = rewardCards.Length > 1
            ? Mathf.Abs(cardPositions[1].x - cardPositions[0].x) : 0f;
        for (int i = 0; i < rewardCards.Length; i++)
        {
            rewardCards[i].anchoredPosition = new Vector2(0f, cardPositions[i].y);
            rewardCards[i].localScale = cardScales[i] * 0.85f;
            reveal.Append(rewardCardGroups[i].DOFade(1f, appearDuration))
                .Join(rewardCards[i].DOScale(cardScales[i], appearDuration).SetEase(Ease.OutCubic));
            reveal.AppendInterval(displayPause);
            if (i == rewardCards.Length - 1) continue;
            float start = reveal.Duration();
            for (int previous = 0; previous <= i; previous++)
                reveal.Insert(start, rewardCards[previous].DOAnchorPos(
                    new Vector2((previous - i - 1) * spacing, cardPositions[previous].y),
                    slideDuration).SetEase(Ease.InOutCubic));
        }
        float settleStart = reveal.Duration();
        for (int i = 0; i < rewardCards.Length; i++)
            reveal.Insert(settleStart, rewardCards[i].DOAnchorPos(cardPositions[i], slideDuration)
                .SetEase(Ease.InOutCubic));
    }
    private void Refresh()
    {
        if (reward == null) return;
        if (goldText != null) goldText.text = $"×{reward.gold:N0}";
        if (fragmentText != null) fragmentText.text = $"×{reward.fragments:N0}";
        if (goldLabel != null) goldLabel.text = CharacterProgression.Copy("골드", "GOLD", "ゴールド");
        if (fragmentLabel != null) fragmentLabel.text = CharacterProgression.CharacterName(reward.characterId) +
            CharacterProgression.Copy(" 증표", " Tokens", "の証");
        if (portrait != null)
        {
            portrait.sprite = CharacterProgression.Definition(reward.characterId)?.portrait;
            TokenPortrait.Apply(portrait, reward.characterId);
        }
    }
}
