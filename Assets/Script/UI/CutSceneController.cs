using System.Collections;
using Febucci.TextAnimatorForUnity;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(UnityEngine.UI.Image))]
public sealed class CutSceneController : MonoBehaviour, IPointerClickHandler
{
    private enum PreviewLanguage { Korean, English, Japanese }

    [SerializeField] private UnityEngine.UI.Image displayImage;
    [SerializeField] private UnityEngine.UI.Image fadeOverlay;
    [SerializeField] private TMP_Text captionText;
    [SerializeField] private TypewriterComponent captionTypewriter;
    [SerializeField] private TextAnimator_TMP captionAnimator;
    [SerializeField] private Sprite[] frames;
    [Header("장면별 자막 키 — 번역은 Google Sheet에서 관리합니다")]
    [SerializeField] private string[] captionKeys;
    [Header("편집 모드 미리보기")]
    [Tooltip("0은 첫 번째 장면, 1은 두 번째 장면입니다.")]
    [SerializeField, Min(0)] private int previewFrame;
    [SerializeField] private PreviewLanguage previewLanguage;
    [SerializeField] private string nextSceneName = "Lobby";
    [SerializeField, Min(0f)] private float initialBlackSeconds = 0.15f;
    [SerializeField, Min(0f)] private float visibleSeconds = 3f;

    private int currentFrame;
    private bool loadingStage;
    private bool transitioning;
    private bool hasStarted;
    private Coroutine playbackCoroutine;
    private LocalizationManager subscribedLocalization;

    private void Awake()
    {
        if (displayImage == null)
            displayImage = GetComponent<UnityEngine.UI.Image>();
        if (fadeOverlay == null)
            fadeOverlay = displayImage != null
                ? displayImage.transform.parent?.Find("Fade Overlay")?.GetComponent<UnityEngine.UI.Image>()
                : null;

        if (frames == null || frames.Length == 0 || displayImage == null || fadeOverlay == null)
        {
            Debug.LogError("CutSceen: 컷신 이미지 또는 페이드 이미지가 설정되지 않았습니다.", this);
            enabled = false;
            return;
        }

        currentFrame = 0;
        displayImage.sprite = frames[currentFrame];
        displayImage.color = Color.white;
        displayImage.raycastTarget = true;
        fadeOverlay.color = Color.black;
        fadeOverlay.raycastTarget = false;
        transitioning = true;
    }

    private void OnEnable()
    {
        SubscribeToLocalization();
        RefreshLocalizedCaption();
        if (hasStarted)
        {
            currentFrame = 0;
            loadingStage = false;
            transitioning = true;
            fadeOverlay.color = Color.black;
            StartCoroutine(BeginPlayback());
        }
    }

    private IEnumerator Start()
    {
        hasStarted = true;
        yield return BeginPlayback();
    }

    private IEnumerator BeginPlayback()
    {
        SubscribeToLocalization();

        SoundManager.Instance?.Play(SoundIds.CutsceneMusic, loop: true, bgm: true, restartIfPlaying: true);

        // Present a fully opaque black frame before the first reveal. Scene
        // activation and the first UI rebuild finish while the image is covered.
        Canvas.ForceUpdateCanvases();
        yield return null;
        yield return new WaitForSecondsRealtime(initialBlackSeconds);
        // Direct Editor entry can still be finishing a normal scene load.
        while (GameSceneManager.IsLoading)
            yield return null;
        if (!loadingStage)
        {
            ShowFrame(currentFrame);
        }
    }

    private void ShowFrame(int index)
    {
        currentFrame = index;
        displayImage.sprite = frames[index];
        displayImage.color = Color.white;
        fadeOverlay.color = Color.black;
        transitioning = true;
        ShowCaption(index);
        playbackCoroutine = StartCoroutine(PlayFrame());
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        float from = fadeOverlay.color.a;
        float duration = Mathf.Max(0.1f, GameSceneManager.TransitionFadeSeconds);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float alpha = Mathf.SmoothStep(from, targetAlpha, elapsed / duration);
            fadeOverlay.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
            // Match the loading screen fade and avoid skipping a reveal on a
            // long scene activation or first-render frame.
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        }
        fadeOverlay.color = new Color(0f, 0f, 0f, targetAlpha);
    }

    private IEnumerator PlayFrame()
    {
        yield return FadeTo(0f);
        transitioning = false;
        yield return new WaitForSecondsRealtime(visibleSeconds);
        transitioning = true;
        yield return FadeTo(1f);
        playbackCoroutine = null;
        Advance();
    }

    private IEnumerator FadeOutAndAdvance()
    {
        yield return FadeTo(1f);
        playbackCoroutine = null;
        Advance();
    }

    private void SubscribeToLocalization()
    {
        LocalizationManager manager = LocalizationManager.Instance;
        if (manager == null || manager == subscribedLocalization)
            return;

        if (subscribedLocalization != null)
            subscribedLocalization.OnLanguageChanged -= RefreshLocalizedCaption;

        subscribedLocalization = manager;
        subscribedLocalization.OnLanguageChanged += RefreshLocalizedCaption;
    }

    private void RefreshLocalizedCaption()
    {
        if (!hasStarted || loadingStage)
            return;

        ShowCaption(currentFrame);
    }

    private void ShowCaption(int index)
    {
        if (captionText == null)
            return;

        captionText.GetComponent<LocalizedFont>()?.RefreshFont();
        string caption = GetCaption(index);
        if (captionTypewriter != null)
        {
            captionTypewriter.StopShowingText();
            captionTypewriter.ShowText(caption);
        }
        else if (captionAnimator != null)
            captionAnimator.SetText(caption, true);
        else
            captionText.text = caption;
    }

    private string GetCaption(int index)
    {
        LocalizationManager localization = subscribedLocalization != null
            ? subscribedLocalization
            : LocalizationManager.Instance;
        if (localization != null && captionKeys != null && index >= 0 && index < captionKeys.Length)
        {
            string key = captionKeys[index];
            if (!string.IsNullOrEmpty(key))
                return localization.GetTranslation(key);
        }

        return string.Empty;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        UnityEditor.EditorApplication.delayCall -= RefreshEditorPreview;
        UnityEditor.EditorApplication.delayCall += RefreshEditorPreview;
    }

    private void RefreshEditorPreview()
    {
        if (this == null || Application.isPlaying || !gameObject.scene.IsValid() ||
            frames == null || frames.Length == 0)
            return;

        int index = Mathf.Clamp(previewFrame, 0, frames.Length - 1);
        if (displayImage != null && frames[index] != null && displayImage.sprite != frames[index])
        {
            displayImage.sprite = frames[index];
            UnityEditor.EditorUtility.SetDirty(displayImage);
        }

        if (captionText == null)
            return;

        string key = captionKeys != null && index < captionKeys.Length ? captionKeys[index] : string.Empty;
        string caption = LocalizationManager.Instance != null && !string.IsNullOrEmpty(key)
            ? LocalizationManager.Instance.GetTranslation(key)
            : key;
        // Validation can run again after saving or reloading scripts. An unchanged
        // preview must not mark the scene dirty again.
        if (captionText.text != caption)
        {
            captionText.text = caption;
            UnityEditor.EditorUtility.SetDirty(captionText);
        }
    }
#endif

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!enabled || loadingStage || transitioning ||
            eventData.button != PointerEventData.InputButton.Left)
            return;

        // A click skips the remaining bright display time, then fades out normally.
        transitioning = true;
        if (playbackCoroutine != null) StopCoroutine(playbackCoroutine);
        captionTypewriter?.StopShowingText();
        playbackCoroutine = StartCoroutine(FadeOutAndAdvance());
    }

    public void SkipCutscene()
    {
        LoadStage();
    }

    private void Advance()
    {
        playbackCoroutine = null;
        int nextFrame = currentFrame + 1;
        if (nextFrame < frames.Length)
        {
            ShowFrame(nextFrame);
            return;
        }

        LoadStage();
    }

    private void LoadStage()
    {
        if (loadingStage)
            return;

        loadingStage = true;
        transitioning = true;
        if (playbackCoroutine != null) StopCoroutine(playbackCoroutine);
        captionTypewriter?.StopShowingText();
        playbackCoroutine = StartCoroutine(FinishCutscene());
    }

    private IEnumerator FinishCutscene()
    {
        if (fadeOverlay.color.a < 1f)
            yield return FadeTo(1f);
        yield return null;
        SoundManager.Instance?.StopBgm();
        GameSceneManager.LoadSceneAsync(nextSceneName);
    }

    private void OnDisable()
    {
        if (subscribedLocalization != null)
        {
            subscribedLocalization.OnLanguageChanged -= RefreshLocalizedCaption;
            subscribedLocalization = null;
        }

        if (playbackCoroutine != null) StopCoroutine(playbackCoroutine);
        playbackCoroutine = null;
        captionTypewriter?.StopShowingText();
        SoundManager.Instance?.StopBgm();
    }
}
