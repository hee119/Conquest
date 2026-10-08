using TMPro;
using UnityEngine;

[RequireComponent(typeof(UnityEngine.UI.Slider))]
public class SliderPercentText : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI percentText;

    [SerializeField]
    private string suffix = "%";

    private UnityEngine.UI.Slider slider;
    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        CacheReferences();
        RegisterValueChanged();
        UpdatePercentText(GetNormalizedValue());
    }

    private void OnDisable()
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(UpdatePercentText);
        }
    }

    private void RegisterValueChanged()
    {
        if (slider == null)
        {
            return;
        }

        slider.onValueChanged.RemoveListener(UpdatePercentText);
        slider.onValueChanged.AddListener(UpdatePercentText);
    }

    private void CacheReferences()
    {
        if (slider == null)
        {
            slider = GetComponent<UnityEngine.UI.Slider>();
        }

        if (percentText == null)
        {
            Transform percentTransform = transform.Find("100%");
            percentText =
                percentTransform != null
                    ? percentTransform.GetComponent<TextMeshProUGUI>()
                    : GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    private float GetNormalizedValue()
    {
        return slider != null ? slider.normalizedValue : 0f;
    }

    private void UpdatePercentText(float normalizedValue)
    {
        if (percentText == null)
        {
            return;
        }

        int percent = Mathf.RoundToInt(Mathf.Clamp01(normalizedValue) * 100f);
        percentText.text = $"{percent}{suffix}";
    }
}
