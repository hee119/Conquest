using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class StartupLoading : MonoBehaviour
{
    [SerializeField] private ExpBar progressBar;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text tipText;
    [SerializeField] private TMP_Text touchToStartText;
    [SerializeField, Min(0.5f)] private float blinkPeriod = 2.8f;
    [SerializeField] private CanvasGroup transitionFade;
    [SerializeField, Min(0.1f)] private float fadeDuration = 0.8f;
    [SerializeField, Min(0f)] private float completedDisplayDuration = 1f;
    [SerializeField, Min(0.05f)] private float progressSmoothTime = 0.25f;
    [SerializeField, Min(1f)] private float tipInterval = 4f;
    [SerializeField] private string nextScene = "CutSceen";
    private Coroutine tipCoroutine;
    private Coroutine blinkCoroutine;
    [System.NonSerialized] private InputAction startAction;
    private bool waitingForStart;
    private float targetProgress;
    private float displayedProgress;
    private float progressVelocity;
    private static readonly string[] TipKeys = { "loading_tip_agents", "loading_tip_fusion", "loading_tip_recommended_augments" };

    private void Awake()
    {
        GameSceneManager.TransitionFadeSeconds = Mathf.Max(0.1f, fadeDuration);
        if (LocalizationManager.Instance == null)
            new GameObject("LocalizationManager").AddComponent<LocalizationManager>();
    }

    private void OnEnable()
    {
        waitingForStart = false;
        if (transitionFade != null)
        {
            transitionFade.alpha = 0f;
            transitionFade.blocksRaycasts = false;
        }
        if (touchToStartText != null) touchToStartText.gameObject.SetActive(false);
        targetProgress = displayedProgress = progressVelocity = 0f;
        RenderProgress(0f);
        if (tipText != null && TipKeys.Length > 0)
            tipCoroutine = StartCoroutine(RotateTips());
    }

    private void OnDisable()
    {
        if (tipCoroutine != null) StopCoroutine(tipCoroutine);
        tipCoroutine = null;
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = null;
        waitingForStart = false;
        if (startAction != null)
        {
            startAction.performed -= OnStartPressed;
            startAction.Disable();
        }
    }

    private void OnDestroy() => startAction?.Dispose();

    private IEnumerator RotateTips()
    {
        int count = TipKeys.Length;
        int index = Random.Range(0, count);
        do
        {
            string key = TipKeys[index];
            tipText.text = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetTranslation(key) : key;
            yield return new WaitForSecondsRealtime(Mathf.Max(1f, tipInterval));
            index = (index + 1) % count;
        } while (count > 1);
        tipCoroutine = null;
    }

    private IEnumerator Start()
    {
        yield return null;
        // CSV values must reach prefabs before their pooled copies are made.
        if (DataManager.Instance == null)
            new GameObject("DataManager").AddComponent<DataManager>();
        yield return null;
        float sceneProgress = 0f;
        float poolProgress = 0f;
        StartCoroutine(GameSceneManager.PrepareScenes(value =>
        {
            sceneProgress = value;
            ShowProgress(poolProgress * 0.9f + sceneProgress * 0.1f);
        }));
        yield return PoolManager.Instance.Prewarm(value =>
        {
            poolProgress = value;
            ShowProgress(poolProgress * 0.9f + sceneProgress * 0.1f);
        });
        while (!GameSceneManager.AreScenesPrepared) yield return null;
        ShowProgress(1f);
        while (displayedProgress < 1f) yield return null;
        yield return new WaitForSecondsRealtime(completedDisplayDuration);
        yield return FadeOutLoadingDisplay();
        ShowStartPrompt();
    }

    private IEnumerator FadeOutLoadingDisplay()
    {
        if (tipCoroutine != null) StopCoroutine(tipCoroutine);
        tipCoroutine = null;
        var graphics = new System.Collections.Generic.HashSet<UnityEngine.UI.Graphic>();
        if (progressBar != null)
            foreach (var graphic in progressBar.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                graphics.Add(graphic);
        if (progressText != null) graphics.Add(progressText);
        if (tipText != null) graphics.Add(tipText);
        var colors = new System.Collections.Generic.Dictionary<UnityEngine.UI.Graphic, Color>();
        foreach (var graphic in graphics) colors[graphic] = graphic.color;
        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, fadeDuration);
        while (elapsed < duration)
        {
            float opacity = 1f - Mathf.SmoothStep(0f, 1f, elapsed / duration);
            foreach (var entry in colors)
            {
                if (entry.Key == null) continue;
                Color color = entry.Value;
                color.a *= opacity;
                entry.Key.color = color;
            }
            yield return null;
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        }
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (tipText != null) tipText.gameObject.SetActive(false);
        if (progressText != null) progressText.gameObject.SetActive(false);
        foreach (var entry in colors)
            if (entry.Key != null) entry.Key.color = entry.Value;
    }

    private void ShowStartPrompt()
    {
        if (tipCoroutine != null) StopCoroutine(tipCoroutine);
        tipCoroutine = null;
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (tipText != null) tipText.gameObject.SetActive(false);
        if (progressText != null) progressText.gameObject.SetActive(false);
        if (touchToStartText != null)
        {
            touchToStartText.text = LocalizationManager.Instance.GetTranslation("loading_touch_to_start");
            touchToStartText.alpha = 0f;
            touchToStartText.gameObject.SetActive(true);
            blinkCoroutine = StartCoroutine(BlinkStartPrompt());
        }
        waitingForStart = true;
        startAction?.Dispose();
        startAction = new InputAction("TouchToStart", InputActionType.Button, "<Pointer>/press");
        startAction.performed += OnStartPressed;
        // Button actions do not perform an initial held-state check: a new press is required.
        startAction.Enable();
    }

    private IEnumerator BlinkStartPrompt()
    {
        float duration = Mathf.Max(0.1f, fadeDuration);
        float fadeElapsed = 0f;
        while (fadeElapsed < duration)
        {
            touchToStartText.alpha = Mathf.SmoothStep(0f, 1f, fadeElapsed / duration);
            yield return null;
            fadeElapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        }
        touchToStartText.alpha = 1f;
        float elapsed = 0f;
        while (true)
        {
            touchToStartText.alpha = Mathf.Lerp(0.35f, 1f,
                (Mathf.Cos(elapsed * Mathf.PI * 2f / Mathf.Max(0.5f, blinkPeriod)) + 1f) * 0.5f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void OnStartPressed(InputAction.CallbackContext context)
    {
        if (!waitingForStart || !GameSceneManager.AreScenesPrepared) return;
        waitingForStart = false;
        startAction.Disable();
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = null;
        StartCoroutine(FadeIntoCutscene());
    }

    private IEnumerator FadeIntoCutscene()
    {
        if (transitionFade != null)
        {
            transitionFade.blocksRaycasts = true;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                transitionFade.alpha = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / Mathf.Max(0.1f, fadeDuration)));
                yield return null;
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            }
            transitionFade.alpha = 1f;
            // Present a fully black frame before activating the already loaded cutscene.
            yield return null;
        }
        GameSceneManager.LoadSceneAsync(nextScene);
    }

    private void ShowProgress(float value)
    {
        targetProgress = Mathf.Max(targetProgress, Mathf.Clamp01(value));
    }

    private void Update()
    {
        if (displayedProgress >= targetProgress) return;

        // Keep long loading frames from becoming large visible jumps.
        displayedProgress = Mathf.SmoothDamp(displayedProgress, targetProgress,
            ref progressVelocity, Mathf.Max(0.05f, progressSmoothTime),
            Mathf.Infinity, Mathf.Min(Time.unscaledDeltaTime, 0.05f));
        if (targetProgress - displayedProgress < 0.00001f)
        {
            displayedProgress = targetProgress;
            progressVelocity = 0f;
        }
        RenderProgress(displayedProgress);
    }

    private void RenderProgress(float value)
    {
        value = Mathf.Clamp01(value);
        if (progressBar != null) progressBar.SetLoadingProgress(value);
        if (progressText != null)
        {
            progressText.text = (value * 100f).ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "%";
        }
    }
}
