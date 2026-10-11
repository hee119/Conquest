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
    [SerializeField] private UnityEngine.UI.Image displayImage;
    [SerializeField] private TMP_Text captionText;
    [SerializeField] private TypewriterComponent captionTypewriter;
    [SerializeField] private TextAnimator_TMP captionAnimator;
    [SerializeField] private Sprite[] frames;
    [SerializeField, TextArea(2, 4)] private string[] captions;
    [SerializeField] private string nextSceneName = "Lobby";
    [SerializeField, Min(0.01f)] private float fadeInSeconds = 1.2f;
    [SerializeField, Min(0f)] private float visibleSeconds = 3f;
    [SerializeField, Min(0.01f)] private float fadeOutSeconds = 0.8f;
    [SerializeField] private Color darkTint = new Color(0.06f, 0.06f, 0.06f, 1f);

    private int currentFrame;
    private bool loadingStage;
    private bool transitioning;
    private Tween playbackTween;

    private void Awake()
    {
        if (displayImage == null)
            displayImage = GetComponent<UnityEngine.UI.Image>();

        if (frames == null || frames.Length == 0 || displayImage == null)
        {
            Debug.LogError("CutSceen: 컷신 이미지가 설정되지 않았습니다.", this);
            enabled = false;
            return;
        }

        currentFrame = 0;
        displayImage.sprite = frames[currentFrame];
        displayImage.color = darkTint;
        displayImage.raycastTarget = true;
    }

    private void Start()
    {
        ShowFrame(currentFrame);
    }

    private void ShowFrame(int index)
    {
        currentFrame = index;
        displayImage.sprite = frames[index];
        displayImage.color = darkTint;
        transitioning = false;

        if (captionText != null)
        {
            string caption = captions != null && index < captions.Length
                ? captions[index]
                : string.Empty;

            if (captionTypewriter != null)
                captionTypewriter.ShowText(caption);
            else if (captionAnimator != null)
                captionAnimator.SetText(caption, true);
            else
                captionText.text = caption;
        }

        playbackTween = DOTween.Sequence()
            .SetUpdate(true)
            .Append(displayImage.DOColor(Color.white, fadeInSeconds))
            .AppendInterval(visibleSeconds)
            .Append(displayImage.DOColor(darkTint, fadeOutSeconds))
            .OnComplete(Advance);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!enabled || loadingStage || transitioning ||
            eventData.button != PointerEventData.InputButton.Left)
            return;

        // A click skips the remaining bright display time, then fades out normally.
        transitioning = true;
        playbackTween?.Kill();
        captionTypewriter?.StopShowingText();
        playbackTween = displayImage.DOColor(darkTint, fadeOutSeconds)
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
        SceneManager.LoadSceneAsync(nextSceneName);
    }

    private void OnDisable()
    {
        playbackTween?.Kill();
        playbackTween = null;
        captionTypewriter?.StopShowingText();
    }
}
