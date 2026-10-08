using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class ScreenHitFlash : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image overlay;
    [SerializeField] private Color hitColor = new Color(1f, 0.04f, 0.02f, 0.38f);
    [SerializeField, Min(0.01f)] private float duration = 0.28f;

    private Coroutine flashCoroutine;

    private void Awake()
    {
        if (overlay == null)
            overlay = GetComponent<UnityEngine.UI.Image>();

        if (overlay != null)
        {
            overlay.raycastTarget = false;
            SetAlpha(0f);
        }
    }

    public void Play()
    {
        if (overlay == null)
            overlay = GetComponent<UnityEngine.UI.Image>();

        if (overlay == null || !isActiveAndEnabled)
            return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashCoroutine());
    }

    private IEnumerator FlashCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(hitColor.a * (1f - t) * (1f - t));
            yield return null;
        }

        SetAlpha(0f);
        flashCoroutine = null;
    }

    private void SetAlpha(float alpha)
    {
        Color color = hitColor;
        color.a = alpha;
        overlay.color = color;
    }
}
