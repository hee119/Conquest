using UnityEngine;

public class WaringSign : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private Color startColor = new Color(1f, 0.75f, 0.85f);
    [SerializeField] private Color endColor = Color.red;

    private Material material;

    private static readonly int Progress = Shader.PropertyToID("_Progress");

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        material = spriteRenderer.material;
    }

    private void OnEnable()
    {
        SetProgress(0f);
    }

    public void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);

        material.SetFloat(Progress, progress);

        spriteRenderer.color = Color.Lerp(
            startColor,
            endColor,
            progress
        );
    }
}