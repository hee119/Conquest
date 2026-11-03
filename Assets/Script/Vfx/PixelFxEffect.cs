using UnityEngine;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public sealed class PixelFxEffect : MonoBehaviour
{
    [SerializeField] private AnimationClip clip;
    [SerializeField] private bool loop;
    private Animator animator;
    private float elapsed;

    private void Awake() => animator = GetComponent<Animator>();

    private void OnEnable()
    {
        elapsed = 0f;
        if (animator == null) animator = GetComponent<Animator>();
        if (clip != null && animator.runtimeAnimatorController != null)
            animator.Play(clip.name, 0, 0f);
    }

    private void Update()
    {
        if (loop || clip == null) return;
        elapsed += Time.deltaTime;
        if (elapsed < clip.length) return;
        var pooled = GetComponent<PooledObject>();
        if (pooled != null) pooled.DestroyObj();
        else gameObject.SetActive(false);
    }
}
