using UnityEngine;

/// <summary>현재 보스의 체력을 화면 상단에 고정해 표시한다.</summary>
public sealed class BossHpBar : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private HpBar hpBar;
    private RectTransform screenRoot;

    /// <summary>보스 변경 이벤트를 구독하고 현재 체력바를 연결한다.</summary>
    private void OnEnable()
    {
        EnsureScreenLayout();
        BossHealthSource.Changed += RefreshBoss;
        RefreshBoss();
    }

    /// <summary>보스 체력바를 화면 좌표계로 옮겨 상단에 배치한다.</summary>
    private void EnsureScreenLayout()
    {
        if (panel == null || hpBar == null || screenRoot != null) return;
        var overlay = new GameObject("Boss Screen HUD", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler));
        overlay.transform.SetParent(transform, false);
        screenRoot = (RectTransform)overlay.transform;
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = overlay.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var rect = (RectTransform)panel.transform;
        rect.SetParent(screenRoot, false);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchorMin = new Vector2(0.2f, 1f);
        rect.anchorMax = new Vector2(0.8f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -40f);
        rect.sizeDelta = new Vector2(0f, 60f);

        var bar = (RectTransform)hpBar.transform;
        bar.localScale = Vector3.one;
        bar.anchorMin = new Vector2(0f, 0.5f);
        bar.anchorMax = new Vector2(1f, 0.5f);
        bar.pivot = new Vector2(0.5f, 0.5f);
        bar.anchoredPosition = Vector2.zero;
        bar.sizeDelta = new Vector2(0f, 24f);
        foreach (var graphic in panel.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            graphic.raycastTarget = false;
    }

    /// <summary>보스 이벤트 구독과 체력바 연결을 해제한다.</summary>
    private void OnDisable()
    {
        BossHealthSource.Changed -= RefreshBoss;
        if (hpBar != null)
            hpBar.BindCombat(null);
        if (panel != null)
            panel.SetActive(false);
    }

    /// <summary>현재 보스가 있으면 체력바를 연결하고 없으면 숨긴다.</summary>
    private void RefreshBoss()
    {
        if (panel == null || hpBar == null)
            return;

        EnemyCombat boss = BossHealthSource.CurrentCombat;
        if (boss != null)
        {
            panel.SetActive(true);
            hpBar.BindCombat(boss);
        }
        else
        {
            hpBar.BindCombat(null);
            panel.SetActive(false);
        }
    }
}
