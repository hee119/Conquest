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

    private GameObject gameOverOverlay;
    private TMP_Text gameOverTitle;
    private TMP_Text gameOverMessage;
    private UnityEngine.UI.Button gameOverRetry;
    private UnityEngine.UI.Button gameOverLobby;
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
        gameOverOverlay = overlay;
        gameOverTitle = titleText;
        gameOverMessage = messageText;
        gameOverRetry = retryButton;
        gameOverLobby = lobbyButton;
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



        SubscribeToLocalization();
    }

    private void OnEnable()
    {
        SubscribeToLocalization();
        if (!isShowing) return;
        if (clearTitleTypewriter != null)
            clearTitleTypewriter.onTextShowed.RemoveListener(ShowClearMessage);
        if (clearMessageTypewriter != null)
            clearMessageTypewriter.onTextShowed.RemoveListener(ShowClearButtons);
        clearTitleTypewriter = clearMessageTypewriter = null;
        overlay = gameOverOverlay;
        titleText = gameOverTitle;
        messageText = gameOverMessage;
        retryButton = gameOverRetry;
        lobbyButton = gameOverLobby;
        retryButtonText = retryButton != null ? retryButton.GetComponentInChildren<TMP_Text>(true) : null;
        defaultTitleColor = titleText != null ? titleText.color : Color.white;
        isShowing = isClearShowing = false;
        foreach (var material in revealMaterials) if (material != null) Destroy(material);
        revealMaterials.Clear();
        if (overlay != null) { PrepareReveal(); overlay.SetActive(false); }
        if (demoClearOverlay != null) demoClearOverlay.SetActive(false);
    }

    private void OnDisable()
    {
        revealSequence?.Kill();
        revealSequence = null;
        if (contentGroups != null)
            foreach (var group in contentGroups) if (group != null) group.DOKill();
        clearTitleTypewriter?.StopShowingText();
        clearMessageTypewriter?.StopShowingText();
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

    private static GameEndScreenController GetOrActivateInstance()
    {
        if (instance != null && instance.isActiveAndEnabled)
            return instance;

        var controller = FindFirstObjectByType<GameEndScreenController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            Debug.LogError("Game end screen controller is missing.");
            return null;
        }

        controller.gameObject.SetActive(true);
        return controller;
    }

    public static void ShowGameOver()
    {
        var controller = GetOrActivateInstance();
        if (controller != null)
            controller.Show("gameover_title", "gameover_message", false);
    }

    public static void ShowDemoClear()
    {
        var controller = GetOrActivateInstance();
        if (controller != null)
        {
            controller.SelectDemoClearOverlay();
            controller.Show("demo_clear_title", "demo_clear_message", true);
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
        Transform buttonRow = panel.Find("GameObject") ?? panel;
        retryButton = buttonRow.Find("DemoClearReplayButton")?.GetComponent<Button>();
        lobbyButton = buttonRow.Find("DemoClearStageButton")?.GetComponent<Button>();
        retryButtonText = retryButton != null
            ? retryButton.GetComponentInChildren<TMP_Text>(true)
            : null;
        defaultTitleColor = titleText != null ? titleText.color : Color.white;

        PrepareReveal();
    }

    private void Show(string titleKey, string messageKey, bool isClear)
    {
        if (isShowing || overlay == null)
            return;

        isShowing = true;
        isClearShowing = isClear;
        RunReward reward = CharacterProgression.GrantResult(isClear);
        RunRewardView rewardView = overlay.GetComponentInChildren<RunRewardView>(true);
        if (rewardView != null) rewardView.Bind(reward);
        SoundManager.Instance?.StopBgm();
        if (isClear)
        {
            SoundManager.Instance?.PlayOneShotIgnoringPause(SoundIds.GameClear);
        }
        else
            SoundManager.Instance?.PlayOneShotIgnoringPause(SoundIds.GameOver);
        SubscribeToLocalization();
        if (titleText != null)
        {
            titleText.text = Translate(titleKey);
            titleText.color = defaultTitleColor;
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
            EnsureGroup(lobbyButton),
            EnsureGroup(panel.GetComponentInChildren<RunRewardView>(true))
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
        overlay.GetComponentInChildren<RunRewardView>(true)?.PlayReveal();
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
        overlay.GetComponentInChildren<RunRewardView>(true)?.PlayReveal();
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
        if (!isShowing)
            return;
        if (!isClearShowing)
        {
            if (titleText != null) titleText.text = Translate("gameover_title");
            if (messageText != null) messageText.text = Translate("gameover_message");
            if (retryButtonText != null) retryButtonText.text = Translate("end_retry");
            return;
        }
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
        if (GameSceneManager.IsLoading)
            return;

        Time.timeScale = 1f;
        if (StageManager.Instance != null)
            StageManager.Instance.StopRun();
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.StopRun();
        GameSceneManager.SceneChanged += ReinitializeRetriedStage;
        GameSceneManager.LoadSceneAsync(stageSceneName);
    }

    private static void ReinitializeRetriedStage(Scene scene, LoadSceneMode mode)
    {
        GameSceneManager.SceneChanged -= ReinitializeRetriedStage;
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
        if (GameSceneManager.IsLoading)
            return;

        Time.timeScale = 1f;
        if (StageManager.Instance != null)
            StageManager.Instance.StopRun();
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.StopRun();
        LobbyController.OpenStageSelectOnEntry = true;
        GameSceneManager.LoadSceneAsync(lobbySceneName);
    }
}
