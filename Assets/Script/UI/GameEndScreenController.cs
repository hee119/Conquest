using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Controls the saved in-scene game-over and demo-clear UI.</summary>
public sealed class GameEndScreenController : MonoBehaviour
{
    private static GameEndScreenController instance;

    [SerializeField] private GameObject overlay;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button lobbyButton;
    [SerializeField] private string lobbySceneName = "Lobby";

    private string stageSceneName;
    private bool isShowing;

    private void Awake()
    {
        instance = this;
        stageSceneName = SceneManager.GetActiveScene().name;

        if (overlay != null)
            overlay.SetActive(false);

        if (retryButton != null)
            retryButton.onClick.AddListener(RetryStage);

        if (lobbyButton != null)
            lobbyButton.onClick.AddListener(ReturnToLobby);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public static void ShowGameOver()
    {
        if (instance != null)
            instance.Show("gameover_title", "gameover_message", "Game Over", "이번 도전은 여기까지입니다.");
    }

    public static void ShowDemoClear()
    {
        if (instance != null)
            instance.Show("demo_clear_title", "demo_clear_message", "DEMO CLEAR", "행성 개척의 첫걸음을 내디뎠습니다.");
    }

    private void Show(string titleKey, string messageKey, string fallbackTitle, string fallbackMessage)
    {
        if (isShowing || overlay == null)
            return;

        isShowing = true;
        if (titleText != null)
            titleText.text = Translate(titleKey, fallbackTitle);
        if (messageText != null)
            messageText.text = Translate(messageKey, fallbackMessage);

        overlay.SetActive(true);
        Time.timeScale = 0f;
    }

    private static string Translate(string key, string fallback)
    {
        LocalizationManager localization = LocalizationManager.Instance;
        if (localization == null)
            return fallback;

        string translated = localization.GetTranslation(key);
        return translated == key ? fallback : translated;
    }

    public void RetryStage()
    {
        Time.timeScale = 1f;
        SceneManager.sceneLoaded += ReinitializeRetriedStage;
        SceneManager.LoadScene(stageSceneName);
    }

    private static void ReinitializeRetriedStage(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= ReinitializeRetriedStage;
        if (scene.name == "Stage1" && StageManager.Instance != null)
            StageManager.Instance.StageInit();
    }

    public void ReturnToLobby()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(lobbySceneName);
    }
}
