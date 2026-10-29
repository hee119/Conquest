using DG.Tweening;
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
    [SerializeField, Min(0.01f)] private float fadeInSeconds = 1.2f;
    [SerializeField, Min(0f)] private float visibleSeconds = 3f;
    [SerializeField, Min(0.01f)] private float fadeOutSeconds = 0.8f;
    [SerializeField] private Color darkTint = new Color(0.06f, 0.06f, 0.06f, 1f);

    private int currentFrame;
    private bool loadingStage;
    private bool transitioning;
    private bool hasStarted;
    private Tween playbackTween;
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
        fadeOverlay.color = darkTint;
        fadeOverlay.raycastTarget = false;
    }

    private void OnEnable()
    {
        SubscribeToLocalization();
        RefreshLocalizedCaption();
    }

    private void Start()
    {
        SubscribeToLocalization();
        hasStarted = true;

        SoundManager.Instance?.Play(SoundIds.CutsceneMusic, loop: true, bgm: true, restartIfPlaying: true);

        ShowFrame(currentFrame);
    }

    private void ShowFrame(int index)
    {
        currentFrame = index;
        displayImage.sprite = frames[index];
        displayImage.color = Color.white;
        fadeOverlay.color = darkTint;
        transitioning = false;

        ShowCaption(index);

        playbackTween = DOTween.Sequence()
            .SetUpdate(true)
            .Append(fadeOverlay.DOFade(0f, fadeInSeconds))
            .AppendInterval(visibleSeconds)
            .Append(fadeOverlay.DOFade(darkTint.a, fadeOutSeconds))
            .OnComplete(Advance);
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
        playbackTween?.Kill();
        captionTypewriter?.StopShowingText();
        playbackTween = fadeOverlay.DOFade(darkTint.a, fadeOutSeconds)
            .SetUpdate(true)
            .OnComplete(Advance);
    }

    public void SkipCutscene()
    {
        LoadStage();
    }

    private void Advance()
    {
        playbackTween = null;
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
        playbackTween?.Kill();
        playbackTween = null;
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

        playbackTween?.Kill();
        playbackTween = null;
        captionTypewriter?.StopShowingText();
        SoundManager.Instance?.StopBgm();
    }
}
