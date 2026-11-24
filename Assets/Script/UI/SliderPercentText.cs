using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderPercentText : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI percentText;

    [SerializeField]
    private string suffix = "%";

    private Slider slider;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        CacheReferences();

        slider.onValueChanged.RemoveListener(UpdatePercentText);
        slider.onValueChanged.AddListener(UpdatePercentText);

        RefreshText();
    }

    private void Start()
    {
        RefreshText();
    }

    private void OnDisable()
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(UpdatePercentText);
        }
    }

    private void CacheReferences()
    {
        if (slider == null)
        {
            slider = GetComponent<Slider>();
        }

        if (percentText == null)
        {
            Transform percentTransform = transform.Find("100%");

            percentText = percentTransform != null
                ? percentTransform.GetComponent<TextMeshProUGUI>()
                : GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    public void RefreshText()
    {
        if (slider == null || percentText == null)
            return;

        int percent = Mathf.RoundToInt(
            slider.normalizedValue * 100f
        );

        percentText.text = $"{percent}{suffix}";
    }

    public void UpdatePercentText(float value)
    {
        RefreshText();
    }
}