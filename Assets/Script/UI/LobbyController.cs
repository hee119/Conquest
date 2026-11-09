using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Handles the static lobby scene's button navigation.</summary>
public sealed class LobbyController : MonoBehaviour
{
    public static bool OpenStageSelectOnEntry { get; set; }

    [SerializeField] private string stageSceneName = "Stage1";
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject characterPanel;
    [SerializeField] private LocalizedText characterName;
    [SerializeField] private LocalizedText characterDescription;
    [SerializeField] private GameObject swordmanIdlePreview;
    [SerializeField] private GameObject mageIdlePreview;
    [SerializeField] private CharacterCardVisual swordmanCard;
    [SerializeField] private CharacterCardVisual mageCard;
    [SerializeField] private CharacterProgressionPanel progressionPanel;
    [SerializeField] private CharacterDetailsPreview detailsPreview;
    [SerializeField] private CharacterSummaryStats summaryStats;

    private int previewCharacterId = 1;
    private bool started;
    private AudioSource lobbyMusicSource;
    private AudioClip lobbyMusicClip;
    [SerializeField] private QuitConfirmationDialog quitDialog;

    private void OnEnable()
    {
        if (started) InitializeLobby();
    }

    private void Start()
    {
        started = true;
        InitializeLobby();
    }

    private void InitializeLobby()
    {
        if (SoundManager.Instance != null)
        {
            lobbyMusicSource = SoundManager.Instance.Play(SoundIds.LobbyMusic, loop: true, bgm: true);
            if (lobbyMusicSource != null)
                lobbyMusicClip = lobbyMusicSource.clip;
        }

        previewCharacterId = PlayerPrefs.GetInt("SelectedCharacter", 1) == 2 ? 2 : 1;
        UpdateCharacterPreview();
    }

    private void OnDisable()
    {
        if (quitDialog != null) quitDialog.Hide();
        // A new scene may already have replaced the shared BGM source's clip.
        if (lobbyMusicSource != null && lobbyMusicSource.clip == lobbyMusicClip &&
            SoundManager.Instance != null)
            SoundManager.Instance.Stop(lobbyMusicSource);
    }

    public void ShowMainMenu()
    {
        SetView(mainMenu);
    }

    public void SelectSwordman()
    {
        SelectCharacter(1);
    }

    public void SelectMage()
    {
        SelectCharacter(2);
    }

    public void PreviewSwordman()
    {
        PreviewCharacter(1);
    }

    public void PreviewMage()
    {
        PreviewCharacter(2);
    }

    public void ConfirmCharacter()
    {
        SelectCharacter(previewCharacterId);
        UiSoundEvents.Request(UiSoundCue.Confirm);
        if (characterPanel != null)
            characterPanel.SetActive(false);
        if (mainMenu != null)
            mainMenu.SetActive(true);
    }

    private void PreviewCharacter(int characterId)
    {
        previewCharacterId = characterId;
        SelectCharacter(characterId);
    }

    private void UpdateCharacterPreview()
    {
        foreach (var wallet in GetComponentsInChildren<ProgressionWalletView>(true))
            wallet.SetCharacter(previewCharacterId);
        if (summaryStats != null) summaryStats.SetCharacter(previewCharacterId);
        if (detailsPreview != null)
            detailsPreview.SetCharacter(previewCharacterId);
        if (progressionPanel != null)
            progressionPanel.SetCharacter(previewCharacterId);
        bool mageSelected = previewCharacterId == 2;

        if (characterName != null)
        {
            characterName.key = mageSelected ? "lobby_character_mage" : "lobby_character_swordman";
            characterName.ForceUpdateText();
        }

        if (characterDescription != null)
        {
            characterDescription.key = mageSelected
                ? "lobby_character_mage_desc"
                : "lobby_character_swordman_desc";
            characterDescription.ForceUpdateText();
        }

        if (swordmanIdlePreview != null)
            swordmanIdlePreview.SetActive(!mageSelected);
        if (mageIdlePreview != null)
            mageIdlePreview.SetActive(mageSelected);
        if (swordmanCard != null)
            swordmanCard.SetSelected(!mageSelected);
        if (mageCard != null)
            mageCard.SetSelected(mageSelected);
    }

    private void SelectCharacter(int characterId)
    {
        previewCharacterId = characterId;
        PlayerPrefs.SetInt("SelectedCharacter", characterId);
        PlayerPrefs.Save();

        if (CharacterManager.Instance != null)
            CharacterManager.Instance.currentCharacterId = characterId;
        UpdateCharacterPreview();
    }

    private void SetView(GameObject activeView)
    {
        if (mainMenu != null) mainMenu.SetActive(activeView == mainMenu);
    }

    public void QuitGame()
    {
        if (quitDialog != null) quitDialog.Show();
    }
}
