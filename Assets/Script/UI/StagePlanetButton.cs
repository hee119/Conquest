using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UnityEngine.UI.Button), typeof(UnityEngine.UI.Image))]
public sealed class StagePlanetButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private UnityEngine.UI.Image planetImage;
    [SerializeField] private Material hoverOutlineMaterial;
    [SerializeField] private Material selectionGlowMaterial;

    private bool selected;
    private bool hovered;
    private Vector3 baseScale;
    private bool initialized;

    private void Awake()
    {
        EnsureInitialized();
        RefreshVisual();
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;
        baseScale = transform.localScale;
        if (planetImage == null)
            planetImage = transform.Find("Planet")?.GetComponent<UnityEngine.UI.Image>();
    }

    private void OnEnable()
    {
        RefreshVisual();
    }

    private void OnDisable()
    {
        hovered = false;
        if (planetImage != null)
            planetImage.material = null;
    }

    public void SetSelected(bool value)
    {
        selected = value;
        RefreshVisual();
    }

    public Material GetSelectionGlowMaterial()
    {
        EnsureInitialized();
        return selectionGlowMaterial;
    }

    public void SetSelectionGlowMaterial(Material material)
    {
        EnsureInitialized();
        selectionGlowMaterial = material;
        RefreshVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        RefreshVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        EnsureInitialized();

        if (planetImage != null)
            planetImage.material = selected ? selectionGlowMaterial : hovered ? hoverOutlineMaterial : null;

        transform.localScale = hovered ? baseScale * 1.06f : baseScale;
    }
}
