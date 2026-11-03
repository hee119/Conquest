using System;
using System.Collections;
using UnityEngine;

/// <summary>캐릭터 정보 화면에서 선택한 캐릭터의 기본 공격을 미리 보여준다.</summary>
public sealed class CharacterDetailsPreview : MonoBehaviour
{
    [Serializable]
    public sealed class AttackPreview
    {
        public int characterId;
        public UnityEngine.UI.Image image;
        public Animator idleAnimator;
        public Sprite[] frames;
        public float[] times;
        public float duration;
    }

    [SerializeField] private UnityEngine.UI.Button playButton;
    [SerializeField] private AttackPreview[] previews;
    private int characterId = 1;
    [SerializeField, Min(0.1f)] private float attackPause = 1.2f;
    private Coroutine autoPlayback;
    private Coroutine playback;
    private AttackPreview playingPreview;
    private Sprite previousSprite;
    private bool animatorWasEnabled;
    private Vector2 previousSizeDelta;
    private Vector2 previousPosition;
    private Vector2 referenceSize;
    private bool previousPreserveAspect;


    private void OnEnable()
    {
        if (playButton != null) playButton.gameObject.SetActive(false);
        autoPlayback = StartCoroutine(AutoPlay());
    }

    private void OnDisable()
    {
        if (autoPlayback != null) StopCoroutine(autoPlayback);
        autoPlayback = null;
        StopPreview();
    }

    /// <summary>화면이 열려 있는 동안 일정 간격으로 공격 미리보기를 재생한다.</summary>
    private IEnumerator AutoPlay()
    {
        // Allow the selected character and preview visibility to initialize first.
        yield return null;
        while (true)
        {
            yield return new WaitForSecondsRealtime(attackPause);
            var preview = previews == null ? null : Array.Find(previews,
                p => p != null && p.characterId == characterId);
            if (preview == null || preview.image == null || !preview.image.gameObject.activeInHierarchy)
                continue;
            PlayAttack();
            while (playback != null) yield return null;
        }
    }

    /// <summary>미리 볼 캐릭터를 바꾸고 진행 중인 공격을 정리한다.</summary>
    public void SetCharacter(int id)
    {
        if (id != characterId) StopPreview();
        characterId = id;
    }

    /// <summary>현재 캐릭터의 공격 프레임 재생을 시작한다.</summary>
    public void PlayAttack()
    {
        if (!isActiveAndEnabled || previews == null) return;
        var preview = Array.Find(previews, p => p != null && p.characterId == characterId);
        if (preview == null || preview.image == null || preview.frames == null ||
            preview.frames.Length == 0 || preview.times == null || preview.frames.Length != preview.times.Length) return;
        StopPreview();
        playingPreview = preview;
        previousSprite = preview.image.sprite;
        var rect = preview.image.rectTransform;
        previousSizeDelta = rect.sizeDelta;
        previousPosition = rect.anchoredPosition;
        referenceSize = rect.rect.size;
        previousPreserveAspect = preview.image.preserveAspect;
        if (previousPreserveAspect && previousSprite != null)
        {
            float fit = Mathf.Min(referenceSize.x / previousSprite.rect.width,
                referenceSize.y / previousSprite.rect.height);
            referenceSize = previousSprite.rect.size * fit;
        }
        preview.image.preserveAspect = false;
        animatorWasEnabled = preview.idleAnimator != null && preview.idleAnimator.enabled;
        if (preview.idleAnimator != null) preview.idleAnimator.enabled = false;
        playback = StartCoroutine(PlayFrames(preview));
    }

    /// <summary>공격 속도와 프레임 시간에 맞춰 스프라이트를 표시한다.</summary>
    private IEnumerator PlayFrames(AttackPreview preview)
    {
        CharacterEnhancementTable.BasicAttack(characterId, CharacterProgression.Level(characterId), out float attackSpeed);
        float time = 0f;
        int frame = -1;
        while (time < Mathf.Max(0.01f, preview.duration))
        {
            int next = Mathf.Max(0, frame);
            while (next + 1 < preview.frames.Length && preview.times[next + 1] <= time) next++;
            if (frame != next) { ApplyFrame(preview, preview.frames[next]); frame = next; }
            yield return null;
            time += Time.unscaledDeltaTime * attackSpeed;
        }
        playback = null;
        RestoreIdle();
    }

    /// <summary>스프라이트의 크기와 기준점을 반영해 공격 프레임을 배치한다.</summary>
    private void ApplyFrame(AttackPreview preview, Sprite sprite)
    {
        preview.image.sprite = sprite;
        if (sprite == null || previousSprite == null) return;
        var rect = preview.image.rectTransform;
        // Match SpriteRenderer world units and sprite pivots, rather than fitting
        // every attack frame into the idle image's fixed UI rectangle.
        Vector2 units = referenceSize / (previousSprite.rect.size / previousSprite.pixelsPerUnit);
        Vector2 size = Vector2.Scale(sprite.rect.size / sprite.pixelsPerUnit, units);
        Vector2 idleOffset = Vector2.Scale(rect.pivot, referenceSize) -
            Vector2.Scale(previousSprite.pivot / previousSprite.pixelsPerUnit, units);
        Vector2 offset = Vector2.Scale(rect.pivot, size) -
            Vector2.Scale(sprite.pivot / sprite.pixelsPerUnit, units);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        rect.anchoredPosition = previousPosition + offset - idleOffset;
    }

    /// <summary>공격 재생을 중단하고 대기 화면으로 복원한다.</summary>
    private void StopPreview()
    {
        if (playback != null) StopCoroutine(playback);
        playback = null;
        RestoreIdle();
    }

    /// <summary>미리보기 이미지와 애니메이터를 공격 전 상태로 되돌린다.</summary>
    private void RestoreIdle()
    {
        if (playingPreview == null) return;
        if (playingPreview.image != null)
        {
            playingPreview.image.sprite = previousSprite;
            playingPreview.image.rectTransform.sizeDelta = previousSizeDelta;
            playingPreview.image.rectTransform.anchoredPosition = previousPosition;
            playingPreview.image.preserveAspect = previousPreserveAspect;
        }
        if (playingPreview.idleAnimator != null) playingPreview.idleAnimator.enabled = animatorWasEnabled;
        playingPreview = null;
    }
}
