using System.Collections;
using UnityEngine;

/// <summary>증강 합성 화면의 선택과 진행 상태를 관리한다.</summary>
public sealed class UpgradeFusionController : MonoBehaviour
{
    public UpgradeImageSet first;
    public UpgradeImageSet second;
    public UpgradeCombine collection;
    public UnityEngine.UI.HorizontalLayoutGroup layout;
    public UnityEngine.UI.Button combineButton;
    public UnityEngine.UI.Button confirmButton;
    public UnityEngine.UI.Button exitButton;
    public TMPro.TMP_Text statusText;
    public UnityEngine.UI.Image flash;
    public UpgradeFusionBurst burst;
    public GameObject screen;
    public GameObject upgradesMenu;
    public GameObject normalUpgradePanel;
    [Min(0.1f)] public float spreadDuration = 0.65f;
    [Min(0.1f)] public float gatherDuration = 0.55f;
    [Min(0.1f)] public float revealDuration = 0.9f;

    public bool IsBusy { get; private set; }
    public bool IsComplete { get; private set; }
    private Coroutine animationRoutine;
    private Vector3 firstPosition, secondPosition, firstScale, secondScale;
    private Vector3 center, leftEdge, rightEdge;
    private Quaternion firstRotation, secondRotation;
    private CanvasGroup firstGroup, secondGroup;
    private bool captured;
    private bool originalLayout;
    private int pendingResult;
    private bool committed;
    private bool normalPanelWasActive;
    private UpgradeManager subscribedManager;
    private LocalizationManager subscribedLocalization;

    /// <summary>Copy 동작을 처리한다.</summary>
    private string Copy(string ko, string en, string ja)
    {
        string language = LocalizationManager.Instance != null ? LocalizationManager.Instance.currentLanguage : "ko";
        return language == "en" ? en : language == "jp" || language == "ja" ? ja : ko;
    }

    /// <summary>활성화될 때 필요한 이벤트와 동작을 시작한다.</summary>
    private void OnEnable()
    {
        UpgradeManager.InstanceChanged += BindManager;
        LocalizationManager.InstanceChanged += BindLocalization;
        if (first != null) first.SelectionChanged += Refresh;
        if (second != null && second != first) second.SelectionChanged += Refresh;
        IsBusy = IsComplete = false;
        committed = false;
        if (normalUpgradePanel != null)
        {
            normalPanelWasActive = normalUpgradePanel.activeSelf;
            normalUpgradePanel.SetActive(false);
        }
        ResetCard(first);
        ResetCard(second);
        if (confirmButton != null) confirmButton.gameObject.SetActive(false);
        if (flash != null) flash.gameObject.SetActive(false);
        if (burst != null) burst.gameObject.SetActive(false);
        if (combineButton != null) combineButton.gameObject.SetActive(true);
        BindManager(UpgradeManager.Instance);
        BindLocalization(LocalizationManager.Instance);
        Refresh();
    }

    /// <summary>BindManager 동작을 처리한다.</summary>
    private void BindManager(UpgradeManager manager)
    {
        if (subscribedManager != null)
            subscribedManager.UpgradesChanged -= Refresh;
        subscribedManager = manager;
        if (subscribedManager != null)
            subscribedManager.UpgradesChanged += Refresh;
        Refresh();
    }

    /// <summary>BindLocalization 동작을 처리한다.</summary>
    private void BindLocalization(LocalizationManager manager)
    {
        if (subscribedLocalization != null)
            subscribedLocalization.OnLanguageChanged -= Refresh;
        subscribedLocalization = manager;
        if (subscribedLocalization != null)
            subscribedLocalization.OnLanguageChanged += Refresh;
        Refresh();
    }

    /// <summary>ResetCard 동작을 처리한다.</summary>
    private void ResetCard(UpgradeImageSet slot)
    {
        if (slot == null) return;
        slot.gameObject.SetActive(true);
        slot.id = 0;
        var info = slot.GetComponent<UpgradeUiInfo>();
        info.upgradeData = new UpgradeData();
        if (info.upgradeImage != null) info.upgradeImage.gameObject.SetActive(false);
        if (info.upgradeNameLo != null) info.upgradeNameLo.gameObject.SetActive(false);
        if (info.upgradeDesLo != null) info.upgradeDesLo.gameObject.SetActive(false);
    }

    /// <summary>현재 데이터를 바탕으로 표시 상태를 갱신한다.</summary>
    private void Refresh()
    {
        if (IsBusy || IsComplete)
        {
            if (IsComplete) SetLabel(confirmButton, Copy("확인하기", "Confirm", "確認する"));
            if (statusText != null) statusText.text = IsComplete
                /// <summary>Copy 동작을 처리한다.</summary>
                ? Copy("새로운 증강을 획득했습니다", "New upgrade acquired", "新しい増強を獲得しました")
                : Copy("증강 융합 중", "Fusing upgrades", "増強を融合中");
            return;
        }
        bool valid = first != null && second != null &&
            UpgradeFusionRules.CanCombine(UpgradeManager.Instance, first.id, second.id, out pendingResult);
        if (combineButton != null)
        {
            combineButton.interactable = valid;
            SetLabel(combineButton, Copy("합성하기", "Combine", "合成する"));
        }
        if (statusText != null)
            statusText.text = valid ? Copy("합성 준비 완료", "Ready to combine", "合成準備完了") :
                first != null && second != null && first.id > 0 && second.id > 0
                /// <summary>Copy 동작을 처리한다.</summary>
                ? Copy("합성할 수 없는 조합입니다", "These upgrades cannot be combined", "合成できない組み合わせです")
                : Copy("합성할 만렙 증강 두 개를 선택하세요", "Select two compatible max-level upgrades", "最大レベルの増強を2つ選択してください");
    }

    /// <summary>BeginFusion 동작을 처리한다.</summary>
    public void BeginFusion()
    {
        if (IsBusy || IsComplete || first == null || second == null ||
            !UpgradeFusionRules.CanCombine(UpgradeManager.Instance, first.id, second.id, out pendingResult))
            return;
        CaptureLayout();
        IsBusy = true;
        SetInput(false);
        collection.gameObject.SetActive(false);
        combineButton.interactable = false;
        statusText.text = Copy("증강 융합 중", "Fusing upgrades", "増強を融合中");
        animationRoutine = StartCoroutine(Animate());
    }

    /// <summary>CaptureLayout 동작을 처리한다.</summary>
    private void CaptureLayout()
    {
        Canvas.ForceUpdateCanvases();
        firstPosition = first.transform.localPosition;
        secondPosition = second.transform.localPosition;
        firstScale = first.transform.localScale;
        secondScale = second.transform.localScale;
        firstRotation = first.transform.localRotation;
        secondRotation = second.transform.localRotation;
        firstGroup = first.GetComponent<CanvasGroup>();
        secondGroup = second.GetComponent<CanvasGroup>();
        originalLayout = layout.enabled;
        layout.enabled = false;
        var area = (RectTransform)transform;
        var card = (RectTransform)first.transform;
        var parent = first.transform.parent;
        center = parent.InverseTransformPoint(area.TransformPoint(area.rect.center));
        center.z = firstPosition.z;
        float inset = card.rect.width * 0.5f + 70f;
        leftEdge = parent.InverseTransformPoint(area.TransformPoint(new Vector3(area.rect.xMin + inset, area.rect.center.y)));
        rightEdge = parent.InverseTransformPoint(area.TransformPoint(new Vector3(area.rect.xMax - inset, area.rect.center.y)));
        leftEdge.z = rightEdge.z = center.z;
        captured = true;
    }

    /// <summary>Animate 동작을 처리한다.</summary>
    private IEnumerator Animate()
    {
        float duration = spreadDuration + gatherDuration + revealDuration;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            RenderFrame(elapsed);
            if (!IsBusy) yield break;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        RenderFrame(duration);
        if (!IsBusy) yield break;
        flash.gameObject.SetActive(false);
        burst.gameObject.SetActive(false);
        IsBusy = false;
        IsComplete = true;
        animationRoutine = null;
        combineButton.gameObject.SetActive(false);
        confirmButton.gameObject.SetActive(true);
        confirmButton.interactable = true;
        SetLabel(confirmButton, Copy("확인하기", "Confirm", "確認する"));
        statusText.text = Copy("새로운 증강을 획득했습니다", "New upgrade acquired", "新しい増強を獲得しました");
        confirmButton.Select();
    }

    /// <summary>RenderFrame 동작을 처리한다.</summary>
    private void RenderFrame(float elapsed)
    {
        float impact = spreadDuration + gatherDuration;
        if (elapsed < spreadDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / spreadDuration);
            first.transform.localPosition = Vector3.Lerp(firstPosition, leftEdge, t);
            second.transform.localPosition = Vector3.Lerp(secondPosition, rightEdge, t);
            first.transform.localRotation = Quaternion.Euler(0f, 0f, 5f * t);
            second.transform.localRotation = Quaternion.Euler(0f, 0f, -5f * t);
            return;
        }
        if (elapsed < impact)
        {
            float t = Mathf.Pow((elapsed - spreadDuration) / gatherDuration, 3f);
            first.transform.localPosition = Vector3.Lerp(leftEdge, center, t);
            second.transform.localPosition = Vector3.Lerp(rightEdge, center, t);
            first.transform.localScale = firstScale * Mathf.Lerp(1f, 0.72f, t);
            second.transform.localScale = secondScale * Mathf.Lerp(1f, 0.72f, t);
            first.transform.localRotation = Quaternion.Euler(0f, 0f, 5f * (1f - t));
            second.transform.localRotation = Quaternion.Euler(0f, 0f, -5f * (1f - t));
            burst.gameObject.SetActive(true);
            burst.color = new Color(1f, 1f, 1f, t * 0.9f);
            burst.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.4f, t);
            return;
        }
        if (!committed)
        {
            // Inventory is changed once, at the moment of impact.
            if (!UpgradeManager.Instance.TryCombineUpgrades(first.id, second.id, out pendingResult))
            {
                animationRoutine = null;
                IsBusy = false;
                RestoreLayout();
                SetInput(true);
                Refresh();
                statusText.text = Copy("합성 조건이 변경됐습니다. 다시 선택하세요", "The materials changed. Select again", "素材が変わりました。再選択してください");
                return;
            }
            committed = true;
            second.gameObject.SetActive(false);
            first.GetComponent<UpgradeUiInfo>().ShowUpgrade(DataManager.Instance.upgradeDatasDic[pendingResult]);
        }
        float reveal = Mathf.Clamp01((elapsed - impact) / revealDuration);
        first.transform.localPosition = center;
        first.transform.localRotation = firstRotation;
        first.transform.localScale = firstScale * Mathf.Lerp(0.78f, 1f, Mathf.Sin(reveal * Mathf.PI * 0.5f));
        flash.gameObject.SetActive(reveal < 1f);
        flash.color = new Color(1f, 1f, 1f, Mathf.Pow(1f - reveal, 3f) * 0.95f);
        burst.gameObject.SetActive(reveal < 1f);
        burst.color = new Color(1f, 1f, 1f, (1f - reveal) * 0.8f);
        burst.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.4f, 2f, reveal);
    }

    /// <summary>Input 관련 상태를 설정한다.</summary>
    private void SetInput(bool enabled)
    {
        foreach (var slot in new[] { first, second })
        {
            var group = slot.GetComponent<CanvasGroup>();
            if (group != null) group.interactable = group.blocksRaycasts = enabled;
        }
        if (exitButton != null) exitButton.interactable = enabled;
    }

    /// <summary>ConfirmAndClose 동작을 처리한다.</summary>
    public void ConfirmAndClose()
    {
        if (IsBusy || !IsComplete) return;
        collection.gameObject.SetActive(false);
        if (screen != null) screen.SetActive(false);
        if (upgradesMenu != null) upgradesMenu.SetActive(false);
        Time.timeScale = 1f;
    }

    /// <summary>RestoreLayout 동작을 처리한다.</summary>
    private void RestoreLayout()
    {
        if (!captured) return;
        first.transform.localPosition = firstPosition;
        second.transform.localPosition = secondPosition;
        first.transform.localScale = firstScale;
        second.transform.localScale = secondScale;
        first.transform.localRotation = firstRotation;
        second.transform.localRotation = secondRotation;
        second.gameObject.SetActive(true);
        layout.enabled = originalLayout;
        if (firstGroup != null) firstGroup.interactable = firstGroup.blocksRaycasts = true;
        if (secondGroup != null) secondGroup.interactable = secondGroup.blocksRaycasts = true;
        captured = false;
    }

    /// <summary>비활성화될 때 이벤트와 진행 중인 동작을 정리한다.</summary>
    private void OnDisable()
    {
        UpgradeManager.InstanceChanged -= BindManager;
        LocalizationManager.InstanceChanged -= BindLocalization;
        if (first != null) first.SelectionChanged -= Refresh;
        if (second != null && second != first) second.SelectionChanged -= Refresh;
        if (subscribedManager != null) subscribedManager.UpgradesChanged -= Refresh;
        if (subscribedLocalization != null) subscribedLocalization.OnLanguageChanged -= Refresh;
        subscribedManager = null;
        subscribedLocalization = null;
        if (animationRoutine != null) StopCoroutine(animationRoutine);
        animationRoutine = null;
        RestoreLayout();
        if (normalUpgradePanel != null) normalUpgradePanel.SetActive(normalPanelWasActive);
        if (exitButton != null) exitButton.interactable = true;
        IsBusy = false;
    }

    /// <summary>Label 관련 상태를 설정한다.</summary>
    private static void SetLabel(UnityEngine.UI.Button button, string value)
    {
        var label = button.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (label != null) label.text = value;
    }
}
