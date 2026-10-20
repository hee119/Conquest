using System.Collections.Generic;
using DG.Tweening;
using Febucci.TextAnimatorForUnity;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Controls the saved in-scene game-over and demo-clear UI.</summary>
public sealed class GameEndScreenController : MonoBehaviour
{
    private static GameEndScreenController instance;

    [SerializeField] private GameObject overlay;
    [SerializeField] private GameObject demoClearOverlay;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private UnityEngine.UI.Button retryButton;
    [SerializeField] private UnityEngine.UI.Button lobbyButton;
    [SerializeField] private string lobbySceneName = "Lobby";

    private string stageSceneName;
    private bool isShowing;
    private CanvasGroup[] contentGroups;
    private Image overlayImage;
    private Image panelImage;
    private Material overlayRevealMaterial;
    private Material panelRevealMaterial;
    private float revealProgress;
    private Sequence revealSequence;
    private Color defaultTitleColor;
    private TMP_Text retryButtonText;
    private TypewriterComponent clearTitleTypewriter;
    private TypewriterComponent clearMessageTypewriter;
    private bool isClearShowing;
    private LocalizationManager subscribedLocalization;
    private readonly List<Material> revealMaterials = new List<Material>();

    private void Awake()
    {
        instance = this;
        stageSceneName = SceneManager.GetActiveScene().name;
        if (titleText != null)
            defaultTitleColor = titleText.color;
        if (retryButton != null)
            retryButtonText = retryButton.GetComponentInChildren<TMP_Text>(true);

        if (overlay != null)
            PrepareReveal();

        if (overlay != null)
            overlay.SetActive(false);
        if (demoClearOverlay == null)
        {
            Transform clearTransform = transform.Find("DemoClearOverlay");
            if (clearTransform != null)
                demoClearOverlay = clearTransform.gameObject;
        }
        if (demoClearOverlay != null)
            demoClearOverlay.SetActive(false);

        if (retryButton != null)
            retryButton.onClick.AddListener(RetryStage);

        if (lobbyButton != null)
            lobbyButton.onClick.AddListener(ReturnToLobby);

        SubscribeToLocalization();
    }

    private void Start() => SubscribeToLocalization();

    private void OnDestroy()
    {
        revealSequence?.Kill();
        if (subscribedLocalization != null)
            subscribedLocalization.OnLanguageChanged -= RefreshLocalizedText;
        if (clearTitleTypewriter != null)
            clearTitleTypewriter.onTextShowed.RemoveListener(ShowClearMessage);
        if (clearMessageTypewriter != null)
            clearMessageTypewriter.onTextShowed.RemoveListener(ShowClearButtons);
        foreach (Material material in revealMaterials)
            if (material != null)
                Destroy(material);
        if (instance == this)
            instance = null;
    }

    public static void ShowGameOver()
    {
        if (instance != null)
            instance.Show("gameover_title", "gameover_message", false);
    }

    public static void ShowDemoClear()
    {
        if (instance != null)
        {
            instance.SelectDemoClearOverlay();
            instance.Show("demo_clear_title", "demo_clear_message", true);
        }
    }

    private void SelectDemoClearOverlay()
    {
        if (isShowing || demoClearOverlay == null)
            return;

        Transform panel = demoClearOverlay.transform.Find("EndPanel");
        if (panel == null)
            return;

        overlay = demoClearOverlay;
        titleText = panel.Find("Title")?.GetComponent<TMP_Text>();
        messageText = panel.Find("Message")?.GetComponent<TMP_Text>();
        clearTitleTypewriter = titleText != null ? titleText.GetComponent<TypewriterComponent>() : null;
        clearMessageTypewriter = messageText != null ? messageText.GetComponent<TypewriterComponent>() : null;
        retryButton = panel.Find("DemoClearReplayButton")?.GetComponent<Button>();
        lobbyButton = panel.Find("DemoClearStageButton")?.GetComponent<Button>();
        retryButtonText = retryButton != null
            ? retryButton.GetComponentInChildren<TMP_Text>(true)
            : null;
        defaultTitleColor = titleText != null ? titleText.color : Color.white;

        PrepareReveal();
        if (retryButton != null)
            retryButton.onClick.AddListener(RetryStage);
        if (lobbyButton != null)
            lobbyButton.onClick.AddListener(ReturnToLobby);
    }

    private void Show(string titleKey, string messageKey, bool isClear)
    {
        if (isShowing || overlay == null)
            return;

        isShowing = true;
        isClearShowing = isClear;
        SubscribeToLocalization();
        if (titleText != null)
        {
            titleText.text = Translate(titleKey);
            titleText.color = isClear ? new Color(1f, 0.82f, 0.35f) : defaultTitleColor;
        }
        if (messageText != null)
        {
            messageText.text = Translate(messageKey);
            messageText.gameObject.SetActive(!string.IsNullOrEmpty(messageText.text));
        }
        if (retryButtonText != null)
        {
            string retryKey = isClear ? "end_play_again" : "end_retry";
            LocalizedText localizedRetry = retryButtonText.GetComponent<LocalizedText>();
            if (localizedRetry != null)
                localizedRetry.key = retryKey;
        }

        SetRevealProgress(0f);
        SetContentAlpha(0f);
        if (retryButton != null) retryButton.interactable = false;
        if (lobbyButton != null) lobbyButton.interactable = false;

        overlay.SetActive(true);
        if (isClear)
        {
            if (clearTitleTypewriter != null)
            {
                clearTitleTypewriter.onTextShowed.RemoveListener(ShowClearMessage);
                clearTitleTypewriter.onTextShowed.AddListener(ShowClearMessage);
            }
            if (clearMessageTypewriter != null)
            {
                clearMessageTypewriter.onTextShowed.RemoveListener(ShowClearButtons);
                clearMessageTypewriter.onTextShowed.AddListener(ShowClearButtons);
            }
        }
        if (retryButtonText != null)
            retryButtonText.text = isClear
                ? Translate("end_play_again")
                : Translate("end_retry");
        Time.timeScale = 0f;

        if (overlayRevealMaterial == null || panelRevealMaterial == null)
        {
            FinishReveal();
            return;
        }

        revealSequence = DOTween.Sequence().SetUpdate(true);
        revealSequence.Join(DOTween.To(
            () => revealProgress,
            SetRevealProgress,
            1.12f, 1.3f).SetEase(Ease.InOutSine));
        if (!isClear && contentGroups != null)
        {
            foreach (CanvasGroup group in contentGroups)
            {
                if (group != null)
                    revealSequence.Join(group.DOFade(1f, 0.55f).SetDelay(0.6f));
            }
        }
        revealSequence.OnComplete(FinishReveal);
    }

    private void PrepareReveal()
    {
        overlayImage = overlay.GetComponent<Image>();
        RectTransform panel = overlay.transform.Find("EndPanel") as RectTransform;
        if (panel == null)
        {
            Debug.LogError("Game-over EndPanel is missing.", overlay);
            return;
        }

        panelImage = panel.GetComponent<Image>();
        Shader shader = Resources.Load<Shader>("Shaders/CenterOutFade");
        if (shader == null || overlayImage == null || panelImage == null)
        {
            Debug.LogError("Game-over center-out shader or images are missing.", overlay);
            return;
        }

        overlayRevealMaterial = new Material(shader);
        panelRevealMaterial = new Material(shader);
        revealMaterials.Add(overlayRevealMaterial);
        revealMaterials.Add(panelRevealMaterial);
        overlayImage.material = overlayRevealMaterial;
        panelImage.material = panelRevealMaterial;

        contentGroups = new[]
        {
            EnsureGroup(titleText),
            EnsureGroup(messageText),
            EnsureGroup(retryButton),
            EnsureGroup(lobbyButton)
        };
    }

    private void FinishReveal()
    {
        SetRevealProgress(1.12f);
        if (isClearShowing)
        {
            ShowClearTitle();
            return;
        }
        SetContentAlpha(1f);
        if (retryButton != null) retryButton.interactable = true;
        if (lobbyButton != null) lobbyButton.interactable = true;
    }

    private void ShowClearTitle()
    {
        SetGroupAlpha(0, 1f);
        string title = Translate("demo_clear_title");
        if (clearTitleTypewriter != null)
            clearTitleTypewriter.ShowText(title);
        else
        {
            if (titleText != null) titleText.text = title;
            ShowClearMessage();
        }
    }

    private void ShowClearMessage()
    {
        SetGroupAlpha(1, 1f);
        string message = Translate("demo_clear_message");
        if (clearMessageTypewriter != null)
            clearMessageTypewriter.ShowText(message);
        else
        {
            if (messageText != null) messageText.text = message;
            ShowClearButtons();
        }
    }

    private void ShowClearButtons()
    {
        if (contentGroups != null)
        {
            for (int i = 2; i < contentGroups.Length; i++)
            {
                CanvasGroup group = contentGroups[i];
                if (group == null) continue;
                group.DOFade(1f, 0.35f).SetUpdate(true).OnComplete(() =>
                {
                    group.interactable = true;
                    group.blocksRaycasts = true;
                });
            }
        }
        if (retryButton != null) retryButton.interactable = true;
        if (lobbyButton != null) lobbyButton.interactable = true;
    }

    private void SetGroupAlpha(int index, float alpha)
    {
        if (contentGroups == null || index >= contentGroups.Length || contentGroups[index] == null)
            return;
        contentGroups[index].alpha = alpha;
    }

    private void SubscribeToLocalization()
    {
        LocalizationManager manager = LocalizationManager.Instance;
        if (manager == null || manager == subscribedLocalization)
            return;
        if (subscribedLocalization != null)
            subscribedLocalization.OnLanguageChanged -= RefreshLocalizedText;
        subscribedLocalization = manager;
        subscribedLocalization.OnLanguageChanged += RefreshLocalizedText;
    }

    private void RefreshLocalizedText()
    {
        if (!isShowing || !isClearShowing)
            return;
        if (clearTitleTypewriter != null)
            clearTitleTypewriter.StopShowingText();
        if (clearMessageTypewriter != null)
            clearMessageTypewriter.StopShowingText();
        if (contentGroups != null && contentGroups.Length > 0 && contentGroups[0] != null && contentGroups[0].alpha > 0f)
            ShowClearTitle();
        if (retryButtonText != null)
            retryButtonText.text = Translate("end_play_again");
    }

    private void SetRevealProgress(float progress)
    {
        revealProgress = progress;
        if (overlayRevealMaterial != null)
            overlayRevealMaterial.SetFloat("_Progress", progress);
        if (panelRevealMaterial != null)
            panelRevealMaterial.SetFloat("_Progress", progress);
    }

    private void SetContentAlpha(float alpha)
    {
        if (contentGroups == null)
            return;

        foreach (CanvasGroup group in contentGroups)
        {
            if (group != null)
            {
                group.alpha = alpha;
                group.interactable = alpha >= 1f;
                group.blocksRaycasts = alpha >= 1f;
            }
        }
    }

    private static CanvasGroup EnsureGroup(Component component)
    {
        if (component == null)
            return null;

        CanvasGroup group = component.GetComponent<CanvasGroup>();
        return group != null ? group : component.gameObject.AddComponent<CanvasGroup>();
    }

    private static string Translate(string key)
    {
        LocalizationManager localization = LocalizationManager.Instance;
        if (localization == null)
            return key;

        return localization.GetTranslation(key);
    }

    public void RetryStage()
    {
        Time.timeScale = 1f;
        if (StageManager.Instance != null)
            StageManager.Instance.StopRun();
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.StopRun();
        SceneManager.sceneLoaded += ReinitializeRetriedStage;
        SceneManager.LoadScene(stageSceneName);
    }

    private static void ReinitializeRetriedStage(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= ReinitializeRetriedStage;
        if (scene.name == "Stage1" && StageManager.Instance != null)
        {
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.ResetForNewRun();
            if (LevelManager.Instance != null)
                LevelManager.Instance.ResetForNewRun();
            StageManager.Instance.StageInit();
        }
    }

    public void ReturnToLobby()
    {
        Time.timeScale = 1f;
        if (StageManager.Instance != null)
            StageManager.Instance.StopRun();
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.StopRun();
        if (PoolManager.Instance != null)
            PoolManager.Instance.ResetActiveObjects();
        LobbyController.OpenStageSelectOnEntry = true;
        SceneManager.LoadScene(lobbySceneName);
    }
}
