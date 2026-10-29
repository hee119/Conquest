using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class StagePlanetMenu : MonoBehaviour
{
    [SerializeField] private StagePlanetButton[] planets;
    [SerializeField] private UnityEngine.UI.Button playButton;
    [SerializeField] private TMP_Text selectedStageLabel;
    [SerializeField] private UnityEngine.UI.ScrollRect scrollRect;
    [SerializeField] private string playableSceneName = "Stage1";
    [SerializeField, Range(0f, 3f)] private float selectedGlowIntensity = 1f;

    private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");
    private static readonly int OutlineColorId = Shader.PropertyToID("_OuterOutlineColor");
    private int selectedIndex;
    private Material runtimeGlowMaterial;
    private Color originalOutlineColor;

    private void Awake()
    {
        ConfigureSelectionGlow();

        for (int i = 0; i < planets.Length; i++)
        {
            int index = i;
            planets[i].GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => Select(index));
        }

        if (playButton != null)
            playButton.onClick.AddListener(PlaySelectedStage);
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            SetSelectedGlowIntensity(selectedGlowIntensity);
    }

    private void OnDestroy()
    {
        if (runtimeGlowMaterial != null)
            Destroy(runtimeGlowMaterial);
    }

    private void ConfigureSelectionGlow()
    {
        if (planets == null || planets.Length == 0)
            return;

        Material source = null;
        foreach (StagePlanetButton planet in planets)
        {
            if (planet == null)
                continue;
            source = planet.GetSelectionGlowMaterial();
            if (source != null)
                break;
        }
        if (source == null)
            return;

        runtimeGlowMaterial = new Material(source);
        if (source.HasProperty(OutlineColorId))
            originalOutlineColor = source.GetColor(OutlineColorId);
        foreach (StagePlanetButton planet in planets)
            if (planet != null)
                planet.SetSelectionGlowMaterial(runtimeGlowMaterial);
        SetSelectedGlowIntensity(selectedGlowIntensity);
    }

    public void SetSelectedGlowIntensity(float intensity)
    {
        selectedGlowIntensity = Mathf.Clamp(intensity, 0f, 3f);
        if (runtimeGlowMaterial != null)
        {
            if (runtimeGlowMaterial.HasProperty(GlowIntensityId))
                runtimeGlowMaterial.SetFloat(GlowIntensityId, selectedGlowIntensity);
            if (runtimeGlowMaterial.HasProperty(OutlineColorId))
                runtimeGlowMaterial.SetColor(OutlineColorId, originalOutlineColor * selectedGlowIntensity);
        }
    }

    private void OnEnable()
    {
        Select(0);
        if (scrollRect != null)
            scrollRect.horizontalNormalizedPosition = 0f;
    }

    public void Select(int index)
    {
        if (planets == null || index < 0 || index >= planets.Length)
            return;

        selectedIndex = index;
        for (int i = 0; i < planets.Length; i++)
            planets[i].SetSelected(i == selectedIndex);

        if (selectedStageLabel != null)
            selectedStageLabel.text = $"STAGE {selectedIndex + 1:00}";

        if (playButton != null)
            playButton.interactable = selectedIndex == 0;
    }

    public bool IsStartButton(UnityEngine.UI.Button button) => button == playButton;

    public void PlaySelectedStage()
    {
        if (selectedIndex == 0 && !string.IsNullOrEmpty(playableSceneName))
        {
            SoundManager.Instance?.PlayOneShotIgnoringPause(SoundIds.CharacterConfirm);
            // Existing managers survive the lobby, so a returning run needs
            // its own initialization after the new Stage1 scene is loaded.
            if (StageManager.Instance != null)
                GameSceneManager.SceneChanged += ReinitializeReturningStage;
            GameSceneManager.LoadScene(playableSceneName);
        }
    }

    private static void ReinitializeReturningStage(Scene scene, LoadSceneMode mode)
    {
        GameSceneManager.SceneChanged -= ReinitializeReturningStage;
        if (scene.name != "Stage1" || StageManager.Instance == null)
            return;

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.ResetForNewRun();
        if (LevelManager.Instance != null)
            LevelManager.Instance.ResetForNewRun();
        StageManager.Instance.StageInit();
    }
}
