using UnityEngine;
using UnityEngine.UI;

/// <summary>Only the visible track, handle and explicit step buttons accept pointer input.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Slider))]
public sealed class SliderInputBounds : MonoBehaviour, ICanvasRaycastFilter
{
    private Slider slider;
    private RectTransform track;
    private RectTransform stepButtons;

    private void Awake() => Cache();

    private void Cache()
    {
        slider = GetComponent<Slider>();
        track = transform.Find("Background") as RectTransform;
        if (track == null && slider.fillRect != null)
            track = slider.fillRect.parent as RectTransform;
        stepButtons = transform.Find("Value Step Buttons") as RectTransform;
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (slider == null || track == null || stepButtons == null) Cache();
        // Step buttons retain their own click/hold handlers.
        if (stepButtons != null && stepButtons.gameObject.activeInHierarchy)
        {
            foreach (Transform child in stepButtons)
                if (child is RectTransform button && button.gameObject.activeInHierarchy &&
                    RectTransformUtility.RectangleContainsScreenPoint(button, screenPoint, eventCamera)) return true;
        }
        if (slider.handleRect != null &&
            RectTransformUtility.RectangleContainsScreenPoint(slider.handleRect, screenPoint, eventCamera)) return true;
        if (track == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(track, screenPoint, eventCamera);
    }

    public static void Attach(Slider target)
    {
        if (target != null && target.GetComponent<SliderInputBounds>() == null)
            target.gameObject.AddComponent<SliderInputBounds>();
    }
}
