
using System.Collections.Generic;
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

    [SerializeField, Min(0.05f)]
    private float appearDuration = 0.25f;

    [SerializeField, Min(0f)]
    private float displayPause = 0.35f;

    [SerializeField, Min(0.05f)]
    private float slideDuration = 0.35f;

    // 증표 카드가 등록된 배열 인덱스
    public int badgeIndex = 1;

    private RunReward reward;
    private LocalizationManager localization;

    private Vector2[] cardPositions;
    private Vector3[] cardScales;

    private Sequence reveal;

    private void Awake()
    {
        CacheLayout();
    }

    private void CacheLayout()
    {
        if (rewardCards == null || rewardCards.Length == 0)
            return;

        if (cardPositions != null &&
            cardScales != null &&
            cardPositions.Length == rewardCards.Length &&
            cardScales.Length == rewardCards.Length)
            return;

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
    }

    private void OnEnable()
    {
        localization = LocalizationManager.Instance;

        if (localization != null)
            localization.OnLanguageChanged += Refresh;

        Refresh();
        ResetPresentation();
    }

    private void OnDisable()
    {
        if (localization != null)
            localization.OnLanguageChanged -= Refresh;

        localization = null;

        ResetPresentation();
    }

    public void Bind(RunReward value)
    {
        reward = value;

        Refresh();
        ResetPresentation();
    }

    // 증표가 0개라면 badgeIndex에 해당하는 카드 제외
    private bool ShouldSkipCard(int index)
    {
        return reward != null &&
               reward.fragments <= 0 &&
               index == badgeIndex;
    }

    private bool HasValidCardArrays()
    {
        if (rewardCards == null ||
            rewardCardGroups == null ||
            cardPositions == null ||
            cardScales == null)
            return false;

        if (rewardCards.Length != rewardCardGroups.Length)
        {
            Debug.LogError(
                "[RunRewardView] Reward Cards와 Reward Card Groups의 배열 크기가 다릅니다.",
                this
            );

            return false;
        }

        return cardPositions.Length == rewardCards.Length &&
               cardScales.Length == rewardCards.Length;
    }

    private void ResetPresentation()
    {
        reveal?.Kill();
        reveal = null;

        CacheLayout();

        if (!HasValidCardArrays())
            return;

        for (int i = 0; i < rewardCards.Length; i++)
        {
            if (rewardCards[i] == null ||
                rewardCardGroups[i] == null)
            {
                Debug.LogError(
                    $"[RunRewardView] 카드 또는 CanvasGroup 참조 누락: {i}",
                    this
                );

                continue;
            }

            RectTransform card = rewardCards[i];
            CanvasGroup group = rewardCardGroups[i];

            card.anchoredPosition = cardPositions[i];
            card.localScale = cardScales[i];

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            // 증표 0개면 증표 카드만 비활성화
            // 다른 카드는 모두 정상적으로 활성화
            card.gameObject.SetActive(!ShouldSkipCard(i));
        }
    }

    public void PlayReveal()
    {
        if (!isActiveAndEnabled || reward == null)
            return;

        ResetPresentation();

        if (!HasValidCardArrays())
            return;

        // 실제로 등장시킬 카드 인덱스만 수집
        List<int> visibleCards = new List<int>();

        for (int i = 0; i < rewardCards.Length; i++)
        {
            if (rewardCards[i] == null ||
                rewardCardGroups[i] == null)
                continue;

            // 증표가 0개라면 badgeIndex만 제외
            if (ShouldSkipCard(i))
                continue;

            visibleCards.Add(i);
        }

        if (visibleCards.Count == 0)
            return;

        reveal = DOTween.Sequence().SetUpdate(true);

        // 기존 카드 간격 유지
        float spacing = rewardCards.Length > 1
            ? Mathf.Abs(
                cardPositions[1].x - cardPositions[0].x
            )
            : 0f;

        // 실제로 표시할 카드만 순서대로 등장
        for (int order = 0; order < visibleCards.Count; order++)
        {
            int index = visibleCards[order];

            RectTransform card = rewardCards[index];
            CanvasGroup group = rewardCardGroups[index];

            // 처음에는 중앙에서 시작
            card.anchoredPosition = new Vector2(
                0f,
                cardPositions[index].y
            );

            // 크기 85%에서 시작
            card.localScale = cardScales[index] * 0.85f;

            // 투명도 증가 + 크기 증가
            reveal.Append(
                group.DOFade(1f, appearDuration)
            ).Join(
                card.DOScale(
                    cardScales[index],
                    appearDuration
                ).SetEase(Ease.OutCubic)
            );

            reveal.AppendInterval(displayPause);

            // 마지막 카드면 추가 이동 없음
            if (order == visibleCards.Count - 1)
                continue;

            float start = reveal.Duration();

            // 이미 등장한 카드들을 왼쪽으로 이동
            for (int previous = 0; previous <= order; previous++)
            {
                int previousIndex = visibleCards[previous];

                RectTransform previousCard =
                    rewardCards[previousIndex];

                float targetX =
                    (previous - order - 1) * spacing;

                reveal.Insert(
                    start,
                    previousCard.DOAnchorPos(
                        new Vector2(
                            targetX,
                            cardPositions[previousIndex].y
                        ),
                        slideDuration
                    ).SetEase(Ease.InOutCubic)
                );
            }
        }

        // 증표 카드가 제외되었는지 확인
        bool badgeSkipped =
            badgeIndex >= 0 &&
            badgeIndex < rewardCards.Length &&
            ShouldSkipCard(badgeIndex);

        // 카드가 하나만 남았다면 중앙에 유지
        if (badgeSkipped && visibleCards.Count == 1)
            return;

        // 마지막 정렬
        float settleStart = reveal.Duration();

        for (int order = 0; order < visibleCards.Count; order++)
        {
            int index = visibleCards[order];

            Vector2 targetPosition;

            if (badgeSkipped)
            {
                // 증표가 빠진 빈 공간을 제거
                // 남아 있는 카드 개수 기준으로 중앙 정렬
                float targetX =
                    (order - (visibleCards.Count - 1) / 2f)
                    * spacing;

                targetPosition = new Vector2(
                    targetX,
                    cardPositions[index].y
                );
            }
            else
            {
                // 모든 카드가 있다면 기존 위치 사용
                targetPosition = cardPositions[index];
            }

            reveal.Insert(
                settleStart,
                rewardCards[index].DOAnchorPos(
                    targetPosition,
                    slideDuration
                ).SetEase(Ease.InOutCubic)
            );
        }
    }

    private void Refresh()
    {
        if (reward == null)
            return;

        if (goldText != null)
            goldText.text = $"×{reward.gold:N0}";

        if (fragmentText != null)
            fragmentText.text = $"×{reward.fragments:N0}";

        if (goldLabel != null)
        {
            goldLabel.text = CharacterProgression.Copy(
                "골드",
                "GOLD",
                "ゴールド"
            );
        }

        if (fragmentLabel != null)
        {
            fragmentLabel.text =
                CharacterProgression.CharacterName(reward.characterId) +
                CharacterProgression.Copy(
                    " 증표",
                    " Tokens",
                    "の証"
                );
        }

        if (portrait != null)
        {
            portrait.sprite =
                CharacterProgression.Definition(
                    reward.characterId
                )?.portrait;

            TokenPortrait.Apply(
                portrait,
                reward.characterId
            );
        }
    }
}
