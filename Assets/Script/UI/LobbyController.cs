using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Handles the static lobby scene's button navigation.</summary>
public sealed class LobbyController : MonoBehaviour
{
    [SerializeField] private string stageSceneName = "Stage1";
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject stagePanel;
    [SerializeField] private GameObject characterPanel;

    private void Awake()
    {
        Bind("MainMenu/StageSelect", OpenStageSelect);
        Bind("MainMenu/CharacterSelect", OpenCharacterSelect);
        Bind("MainMenu/Quit", QuitGame);
        BindStageTiles();
        Bind("StagePanel/Back", ShowMainMenu);
        Bind("StagePanel/Close", ShowMainMenu);
        Bind("CharacterPanel/Swordman", SelectSwordman);
        Bind("CharacterPanel/Back", ShowMainMenu);
        Bind("CharacterPanel/Close", ShowMainMenu);
    }

    private void Bind(string path, UnityEngine.Events.UnityAction action)
    {
        Transform target = transform.Find(path);
        if (target == null)
        {
            Debug.LogError($"Lobby button '{path}' is missing.", this);
            return;
        }

        Button button = target.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"Lobby button '{path}' has no Button component.", target);
            return;
        }

        button.onClick.AddListener(action);
    }

    private void BindStageTiles()
    {
        Transform grid = transform.Find("StagePanel/StageGrid");
        if (grid == null)
        {
            Debug.LogError("Lobby stage grid is missing.", this);
            return;
        }

        foreach (Transform tile in grid)
        {
            Button button = tile.GetComponent<Button>();
            if (button == null)
                continue;

            if (tile.name == "StageTile_01")
                button.onClick.AddListener(StartStage);
            else
                button.interactable = false;
        }
    }

    public void OpenStageSelect()
    {
        SetView(stagePanel);
    }

    public void OpenCharacterSelect()
    {
        SetView(characterPanel);
    }

    public void ShowMainMenu()
    {
        SetView(mainMenu);
    }

    public void SelectSwordman()
    {
        // Swordman is the only playable character in the current project.
        PlayerPrefs.SetInt("SelectedCharacter", 1);
        PlayerPrefs.Save();
        ShowMainMenu();
    }

    public void StartStage()
    {
        if (!string.IsNullOrWhiteSpace(stageSceneName))
            SceneManager.LoadSceneAsync(stageSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void SetView(GameObject activeView)
    {
        if (mainMenu != null) mainMenu.SetActive(activeView == mainMenu);
        if (stagePanel != null) stagePanel.SetActive(activeView == stagePanel);
        if (characterPanel != null) characterPanel.SetActive(activeView == characterPanel);
    }
}
