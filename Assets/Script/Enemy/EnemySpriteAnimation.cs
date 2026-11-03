using System;
using UnityEngine;

// Frames and event times are extracted from the existing clips in the editor.
[DisallowMultipleComponent]
public abstract class EnemySpriteAnimation : MonoBehaviour, IEnemySpriteAnimation
{
    [Serializable] public struct Frame { public float time; public Sprite sprite; }
    [Serializable] public struct Event { public float time; public string function; }
    [Serializable] public sealed class Sequence
    {
        public string state;
        public Frame[] frames;
        public Event[] events;
        public float duration;
        public float speed = 1f;
        public bool loop;
        public string next;
    }

    [SerializeField] private EnemyAnimationData animationData;
    public Sprite[] AttackFrames => animationData != null && animationData.attack != null ? animationData.attack : Array.Empty<Sprite>();
    public Sprite[] MoveFrames => animationData != null && animationData.move != null ? animationData.move : Array.Empty<Sprite>();
    public Sprite[] HitFrames => animationData != null && animationData.hit != null ? animationData.hit : Array.Empty<Sprite>();
    private SpriteRenderer spriteRenderer;
    private Sprite initialSprite;
    private Sequence current;
    private float elapsed;
    private float speed = 1f;
    private int frameIndex;
    private int eventIndex;
    private bool finished;
    private Sequence fallbackAttack, fallbackMove, fallbackHit;

    public bool IsPlaying(string state) => current != null && current.state == state && !finished;
    public bool HasState(string state) => Find(state) != null;
    public float Speed => speed;
    public float NormalizedTime => current == null || current.duration <= 0f ? 0f : elapsed / current.duration;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) initialSprite = spriteRenderer.sprite;
    }

    private string DefaultState => animationData != null && !string.IsNullOrEmpty(animationData.defaultState)
        ? animationData.defaultState : "Move";
    private Sequence[] Sequences => animationData != null ? animationData.sequences : null;

    private void OnEnable() => Move();

    public bool Attack() => AttackFrames.Length > 0 && Play("Attack");
    public bool Move() => Play(DefaultState);
    public bool Hit() => HitFrames.Length > 0 && Play("Hit");

    private Sprite[] StateFrames(Sequence sequence) => sequence.state == "Attack" ? AttackFrames :
        sequence.state == "Hit" ? HitFrames : sequence.state == DefaultState ? MoveFrames : null;

    private int FrameCount(Sequence sequence)
    {
        var sprites = StateFrames(sequence);
        return sprites != null && sprites.Length > 0 ? sprites.Length : sequence.frames.Length;
    }

    private Sprite FrameSprite(Sequence sequence, int index)
    {
        var sprites = StateFrames(sequence);
        return sprites != null && sprites.Length > 0 ? sprites[index] : sequence.frames[index].sprite;
    }

    private float FrameTime(Sequence sequence, int index)
    {
        int count = FrameCount(sequence);
        return count == sequence.frames.Length ? sequence.frames[index].time : sequence.duration * index / count;
    }

    private Sequence MakeFallback(string state, Sprite[] sprites, bool loop)
    {
        if (sprites.Length == 0) return null;
        float duration = sprites.Length / Mathf.Max(.01f, animationData.framesPerSecond);
        var frames = new Frame[sprites.Length];
        for (int i = 0; i < frames.Length; i++) frames[i].time = duration * i / frames.Length;
        return new Sequence { state = state, frames = frames, events = Array.Empty<Event>(),
            duration = duration, speed = 1f, loop = loop, next = loop ? null : DefaultState };
    }

    private Sequence Find(string state)
    {
        if (state != null && state.StartsWith("Base Layer.")) state = state.Substring(11);
        if (Sequences != null)
            foreach (var sequence in Sequences)
                if (sequence != null && sequence.state == state)
                {
                    if (state == DefaultState && sequence.frames.Length == 0 && MoveFrames.Length > 0)
                        return fallbackMove ?? (fallbackMove = MakeFallback(state, MoveFrames, true));
                    return sequence;
                }
        if (state == "Attack") return fallbackAttack ?? (fallbackAttack = MakeFallback(state, AttackFrames, false));
        if (state == "Hit") return fallbackHit ?? (fallbackHit = MakeFallback(state, HitFrames, false));
        if (state == DefaultState) return fallbackMove ?? (fallbackMove = MakeFallback(state, MoveFrames, true));
        return null;
    }

    public bool Play(string state, float playbackSpeed = 1f)
    {
        var sequence = Find(state);
        if (sequence == null) return false;
        current = sequence;
        elapsed = 0f;
        speed = Mathf.Max(0f, playbackSpeed) * sequence.speed;
        frameIndex = eventIndex = 0;
        finished = false;
        if (spriteRenderer != null && sequence.frames != null && FrameCount(sequence) > 0)
            spriteRenderer.sprite = FrameSprite(sequence, 0);
        else if (spriteRenderer != null && state == DefaultState)
            spriteRenderer.sprite = initialSprite;
        return true;
    }

    private void Update()
    {
        if (current == null || finished || speed <= 0f) return;
        float remaining = Time.deltaTime * speed;
        while (remaining > 0f && current != null && !finished)
        {
            var sequence = current;
            float end = Mathf.Min(sequence.duration, elapsed + remaining);
            remaining -= end - elapsed;
            while (eventIndex < sequence.events.Length && sequence.events[eventIndex].time <= end)
            {
                var item = sequence.events[eventIndex++];
                Dispatch(item.function);
                if (!isActiveAndEnabled || current != sequence) return;
            }
            elapsed = end;
            int previousFrame = frameIndex;
            while (frameIndex + 1 < FrameCount(sequence) && FrameTime(sequence, frameIndex + 1) <= elapsed)
                frameIndex++;
            if (spriteRenderer != null && frameIndex != previousFrame)
                spriteRenderer.sprite = FrameSprite(sequence, frameIndex);
            if (elapsed < sequence.duration) break;
            if (sequence.loop && sequence.duration > 0f)
            {
                elapsed = 0f; frameIndex = eventIndex = 0;
                if (spriteRenderer != null && FrameCount(sequence) > 0) spriteRenderer.sprite = FrameSprite(sequence, 0);
            }
            else if (!string.IsNullOrEmpty(sequence.next) && Play(sequence.next)) { }
            else finished = true;
        }
    }

    // Only monsters with animation events override this method.
    protected virtual void Dispatch(string function) { }
}
