using System.Collections.Generic;
using UnityEngine;

/// <summary>현재 보스의 체력을 화면 상단에 고정해 표시한다.</summary>
public sealed class BossHpBar : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private HpBar hpBar;
    private bool layoutCaptured;
    private Vector2 originalPosition;
    private Vector2 originalSizeDelta;
    private float originalWidth;
    private readonly List<HpBar> extraBars = new List<HpBar>();

    /// <summary>보스 변경 이벤트를 구독하고 현재 체력바를 연결한다.</summary>
    private void OnEnable()
    {
        CaptureOriginalLayout();
        BossHealthSource.Changed += RefreshBoss;
        RefreshBoss();
    }

    /// <summary>씬에서 설정한 체력바의 크기와 위치를 유지한다.</summary>
    private void CaptureOriginalLayout()
    {
        if (layoutCaptured || hpBar == null) return;
        var rect = (RectTransform)hpBar.transform;
        originalPosition = rect.anchoredPosition;
        originalSizeDelta = rect.sizeDelta;
        originalWidth = rect.rect.width;
        layoutCaptured = true;
    }

    /// <summary>보스 이벤트 구독과 체력바 연결을 해제한다.</summary>
    private void OnDisable()
    {
        BossHealthSource.Changed -= RefreshBoss;
        if (hpBar != null)
            hpBar.BindCombat(null);
        if (panel != null)
            panel.SetActive(false);
        foreach (HpBar bar in extraBars)
            if (bar != null) bar.BindCombat(null);
    }

    /// <summary>현재 보스가 있으면 체력바를 연결하고 없으면 숨긴다.</summary>
    private void RefreshBoss()
    {
        if (panel == null || hpBar == null)
            return;

        var segments = BossHealthSource.Segments;
        bool hasLivingBoss = false;
        for (int i = 0; i < segments.Count; i++)
            hasLivingBoss |= segments[i].Combat != null;

        panel.SetActive(hasLivingBoss);
        for (int i = 0; i < segments.Count && hasLivingBoss; i++)
        {
            if (i > extraBars.Count)
                extraBars.Add(Instantiate(hpBar, hpBar.transform.parent, false));

            HpBar bar = i == 0 ? hpBar : extraBars[i - 1];
            var segment = segments[i];
            var rect = (RectTransform)bar.transform;
            float left = segment.Min > 0f ? 3f : 0f;
            float right = segment.Max < 1f ? 3f : 0f;
            float widthFraction = segment.Max - segment.Min;
            float pivot = rect.pivot.x;
            rect.sizeDelta = new Vector2(
                originalSizeDelta.x - originalWidth * (1f - widthFraction) - left - right,
                originalSizeDelta.y);
            rect.anchoredPosition = originalPosition + new Vector2(
                originalWidth * (segment.Min + widthFraction * pivot - pivot) +
                left * (1f - pivot) - right * pivot, 0f);
            bar.gameObject.SetActive(true);
            if (!ReferenceEquals(bar.Combat, segment.Combat))
                bar.BindCombat(segment.Combat);
        }

        for (int i = Mathf.Max(0, (hasLivingBoss ? segments.Count : 0) - 1); i < extraBars.Count; i++)
        {
            extraBars[i].gameObject.SetActive(false);
            extraBars[i].BindCombat(null);
        }
        if (!hasLivingBoss)
            hpBar.BindCombat(null);
    }
}
