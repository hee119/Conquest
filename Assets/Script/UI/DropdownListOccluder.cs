using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Dropdown))]
public sealed class DropdownListOccluder : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private int sortingOrder = 5000;

    private RectTransform activeList;
    private bool refreshRequested;

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        refreshRequested = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        refreshRequested = true;
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
            return;

        if (activeList != null && activeList.gameObject.activeInHierarchy)
        {
            if (refreshRequested)
            {
                ApplySorting(activeList);
                refreshRequested = false;
            }
            return;
        }

        if (!refreshRequested)
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
        Canvas listCanvas = list.GetComponent<Canvas>();
        if (listCanvas == null)
        {
            listCanvas = list.gameObject.AddComponent<Canvas>();
        }

        listCanvas.overrideSorting = true;
        listCanvas.sortingOrder = sortingOrder;
    }

    private bool TryFindDropdownList(out RectTransform list)
    {
        list = null;

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
