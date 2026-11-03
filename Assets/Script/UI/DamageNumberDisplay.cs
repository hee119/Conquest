using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

// Reuses the Dynamic Floating Text demo's Pop and Bounce settings on one canvas.
public sealed class DamageNumberDisplay : MonoBehaviour
{
    private static DamageNumberDisplay instance;
    private readonly Queue<DamageNumberView> available = new Queue<DamageNumberView>();
    private GameObject template;
    private DynamicTextData normalData;
    private DynamicTextData criticalData;

    public static void Show(Vector2 position, float damage, bool critical)
    {
        if (damage <= 0f || !Application.isPlaying) return;
        if (instance == null)
            instance = FindFirstObjectByType<DamageNumberDisplay>();
        if (instance == null) return;
        instance.Spawn(position, damage, critical);
    }

    private void Awake()
    {
        template = Resources.Load<GameObject>("Prefabs/Default Canvas Prefab 2D");
        normalData = Resources.Load<DynamicTextData>("DamageNumbers/Normal");
        criticalData = Resources.Load<DynamicTextData>("DamageNumbers/Critical");
        instance = this;
    }

    private void Spawn(Vector2 position, float damage, bool critical)
    {
        DynamicTextData data = critical ? criticalData : normalData;
        if (template == null || data == null) return;
        DamageNumberView view = null;
        while (available.Count > 0 && view == null) view = available.Dequeue();
        if (view == null)
        {
            var textTemplate = template.GetComponentInChildren<TextMeshProUGUI>(true);
            if (textTemplate == null) return;
            GameObject obj = Instantiate(textTemplate.gameObject, transform, false);
            obj.GetComponent<TextMeshProUGUI>().raycastTarget = false;
            view = obj.AddComponent<DamageNumberView>();
            view.Initialize(this);
        }
        view.Show(position, damage.ToString("0.##", CultureInfo.InvariantCulture), data);
    }

    internal void Recycle(DamageNumberView view)
    {
        view.gameObject.SetActive(false);
        available.Enqueue(view);
    }

    private void OnDestroy() { if (instance == this) instance = null; }
}

public sealed class DamageNumberView : MonoBehaviour
{
    private DamageNumberDisplay owner;
    private TextMeshProUGUI label;
    private DynamicTextData data;
    private Vector3 origin;
    private Vector3 labelScale;
    private float elapsed;
    private float direction;
    private bool pop;

    internal void Initialize(DamageNumberDisplay display)
    {
        owner = display;
        label = GetComponentInChildren<TextMeshProUGUI>(true);
        labelScale = label.transform.localScale;
    }

    internal void Show(Vector2 position, string value, DynamicTextData style)
    {
        data = style;
        elapsed = 0f;
        origin = position;
        direction = (Random.value - .5f) * data.maxDrift * .5f;
        pop = data.enterType != null && System.Array.IndexOf(data.enterType, EnterType.Pop) >= 0;
        transform.position = origin;
        label.text = value;
        if (data.font != null) label.font = data.font;
        label.color = data.colours != null && data.colours.Length > 0 ? data.colours[0] : Color.white;
        label.fontStyle = data.bold ? FontStyles.Bold : FontStyles.Normal;
        float size = data.sizes != null && data.sizes.Length > 0 ? data.sizes[0] : 1f;
        label.transform.localScale = pop ? Vector3.zero : labelScale * size;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float enterDuration = Mathf.Max(.001f, data.enterDuration);
        float entry = Mathf.Clamp01(elapsed / enterDuration);
        float size = data.sizes != null && data.sizes.Length > 0 ? data.sizes[0] : 1f;
        if (pop)
        {
            float multiplier = entry < 1f ? entry * data.popModifier : 1f;
            float exitStart = enterDuration + data.lifetime;
            if (elapsed >= exitStart)
                multiplier = Mathf.Lerp(data.popModifier, 0f,
                    (elapsed - exitStart) / Mathf.Max(.001f, data.exitDuration));
            label.transform.localScale = labelScale * (size * multiplier);
            if (elapsed >= exitStart + data.exitDuration) owner.Recycle(this);
        }
        else
        {
            float height = elapsed <= enterDuration ? entry :
                1f - Mathf.Clamp01((elapsed - enterDuration) / enterDuration);
            transform.position = origin + new Vector3(direction * height, data.maxHeight * height, 0f);
            if (elapsed >= enterDuration + data.lifetime) owner.Recycle(this);
        }
    }
}
