using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Dropdown))]
public sealed class DropdownListOccluder : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
{

    private RectTransform activeList;
    private bool refreshRequested;

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        refreshRequested = true;
        PrepareTemplate();
    }

    public void OnPointerDown(PointerEventData eventData) => PrepareTemplate();

    private void PrepareTemplate()
    {
        var dropdown = GetComponent<TMP_Dropdown>();
        if (dropdown.template == null || dropdown.template.parent != transform) return;
        var template = dropdown.template;
        template.anchorMin = new Vector2(0f, 0f);
        template.anchorMax = new Vector2(1f, 0f);
        template.pivot = new Vector2(.5f, 1f);
        template.anchoredPosition = new Vector2(0f, -2f);
        template.sizeDelta = new Vector2(0f, template.sizeDelta.y);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (Application.isPlaying && activeList != null) FitBelowDropdown(activeList);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        refreshRequested = true;
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
            return;

        var shared = GetComponent<SharedCanvasDropdown>();
        if (shared != null && shared.UsesSharedPopup)
        {
            if (shared.IsExpanded) FitBelowDropdown(shared.template);
            return;
        }
        if (activeList != null && activeList.gameObject.activeInHierarchy)
        {
            if (refreshRequested)
            {
                ApplySorting(activeList);
                refreshRequested = false;
            }
            return;
        }

        if (!refreshRequested && !GetComponent<TMP_Dropdown>().IsExpanded)
        {
            return;
        }

        if (TryFindDropdownList(out RectTransform list))
        {
            activeList = list;
            ApplySorting(activeList);
            refreshRequested = false;
        }
    }

    private void ApplySorting(RectTransform list)
    {
        FitBelowDropdown(list);
    }

    private void FitBelowDropdown(RectTransform list)
    {
        var parent = list.parent as RectTransform;
        var button = transform as RectTransform;
        if (parent == null || button == null) return;
        var corners = new Vector3[4];
        button.GetWorldCorners(corners);
        Vector3 left = parent.InverseTransformPoint(corners[0]);
        Vector3 right = parent.InverseTransformPoint(corners[3]);
        list.anchorMin = list.anchorMax = parent.pivot;
        list.pivot = new Vector2(.5f, 1f);
        // as the dropdown; measure its local width without applying that factor twice.
        float scaleX = Mathf.Max(.001f, Mathf.Abs(list.localScale.x));
        list.localRotation = Quaternion.identity;
        list.sizeDelta = new Vector2(Mathf.Abs(right.x - left.x) / scaleX, list.rect.height);
        list.anchoredPosition = new Vector2((left.x + right.x) * .5f, left.y - 2f);
    }

    private bool TryFindDropdownList(out RectTransform list)
    {
        list = null;

        var dropdown = GetComponent<TMP_Dropdown>();
        if (dropdown.template != null && dropdown.template.parent != null)
        {
            var direct = dropdown.template.parent.Find("Dropdown List") as RectTransform;
            if (direct != null && direct.gameObject.activeInHierarchy) { list = direct; return true; }
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        Transform searchRoot = canvas != null ? canvas.transform : transform.root;
        RectTransform[] rects = searchRoot.GetComponentsInChildren<RectTransform>(true);

        foreach (RectTransform rect in rects)
        {
            if (rect.name == "Dropdown List" && rect.gameObject.activeInHierarchy)
            {
                list = rect;
                return true;
            }
        }

        return false;
    }

}
