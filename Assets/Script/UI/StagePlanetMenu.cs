using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>스테이지 행성 선택과 선택한 스테이지 시작을 관리한다.</summary>
public sealed class StagePlanetMenu : MonoBehaviour
{
    [SerializeField] private StagePlanetButton[] planets;
    [SerializeField] private UnityEngine.UI.Button playButton;
    [SerializeField] private TMP_Text[] planetLabels;
    [SerializeField] private UnityEngine.UI.ScrollRect scrollRect;
    [SerializeField] private string playableSceneName = "Stage1";
    [SerializeField, Range(0f, 3f)] private float selectedGlowIntensity = 1f;

    private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");
    private static readonly int OutlineColorId = Shader.PropertyToID("_OuterOutlineColor");
    private int selectedIndex;
    private Material runtimeGlowMaterial;
    private Color originalOutlineColor;

    /// <summary>선택 행성의 강조 효과를 준비한다.</summary>
    private void Awake()
    {
        UiSoundEvents.UseDedicatedClick(playButton);
        ConfigureSelectionGlow();


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

    /// <summary>행성들이 함께 사용할 선택 강조 재질을 준비한다.</summary>
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

    /// <summary>선택된 행성의 강조 효과 강도를 변경한다.</summary>
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

    /// <summary>행성 이름을 현지화하고 기본 선택 상태를 설정한다.</summary>
    private void OnEnable()
    {
        if (planetLabels != null)
            for (int i = 0; i < planetLabels.Length; i++)
            {
                var label = planetLabels[i];
                if (label == null) continue;
                var localized = label.GetComponent<LocalizedText>();
                if (localized != null) localized.SetArguments((i + 1).ToString("00"));
            }
        Select(0);
        if (scrollRect != null)
            scrollRect.horizontalNormalizedPosition = 0f;
    }

    /// <summary>선택한 행성을 강조하고 시작 버튼의 사용 가능 여부를 갱신한다.</summary>
    public void Select(int index)
    {
        if (planets == null || index < 0 || index >= planets.Length)
            return;

        selectedIndex = index;
        for (int i = 0; i < planets.Length; i++)
            planets[i].SetSelected(i == selectedIndex);

        if (playButton != null)
            playButton.interactable = selectedIndex == 0;
    }

    public bool IsStartButton(UnityEngine.UI.Button button) => button == playButton;

    /// <summary>플레이할 수 있는 스테이지를 불러온다.</summary>
    public void PlaySelectedStage()
    {
        if (GameSceneManager.IsLoading)
            return;

        if (selectedIndex == 0 && !string.IsNullOrEmpty(playableSceneName))
        {
            UiSoundEvents.Request(UiSoundCue.Confirm);
            // Existing managers survive the lobby, so a returning run needs
            // its own initialization after the new Stage1 scene is loaded.
            if (StageManager.Instance != null)
                GameSceneManager.SceneChanged += ReinitializeReturningStage;
            GameSceneManager.LoadSceneAsync(playableSceneName);
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
