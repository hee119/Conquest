using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Dropdown))]
public sealed class DropdownListOccluder : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Color panelColor = Color.white;
    [SerializeField] private Color itemColor = Color.white;
    [SerializeField] private int sortingOrder = 5000;

    private TMP_Dropdown dropdown;
    private RectTransform activeList;
    private bool refreshRequested;

    private void Awake()
    {
        Cache();
        ApplyToTemplate(true);
    }

    private void OnEnable()
    {
        Cache();
        ApplyToTemplate(true);
        refreshRequested = true;
    }

    private void OnValidate()
    {
        Cache();
        ApplyToTemplate(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        refreshRequested = true;
    }

    private void LateUpdate()
    {
        if (activeList != null && activeList.gameObject.activeInHierarchy)
        {
            ApplyToList(activeList);
            return;
        }

        if (!refreshRequested)
        {
            return;
        }

        if (TryFindDropdownList(out RectTransform list))
        {
            activeList = list;
            ApplyToList(activeList);
            refreshRequested = false;
        }
    }

    private void Cache()
    {
        if (dropdown == null)
        {
            dropdown = GetComponent<TMP_Dropdown>();
        }
    }

    private void ApplyToTemplate(bool createMissingBackgrounds)
    {
        if (dropdown == null || dropdown.template == null)
        {
            return;
        }

        ApplyOpaqueBackgrounds(dropdown.template, createMissingBackgrounds);
    }

    private void ApplyToList(RectTransform list)
    {
        Canvas listCanvas = list.GetComponent<Canvas>();
        if (listCanvas == null)
        {
            listCanvas = list.gameObject.AddComponent<Canvas>();
        }

        listCanvas.overrideSorting = true;
        listCanvas.sortingOrder = sortingOrder;

        ApplyOpaqueBackgrounds(list, true);
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

    private void ApplyOpaqueBackgrounds(
        RectTransform listRoot,
        bool createMissingBackgrounds)
    {
        SetImageColor(listRoot, panelColor, false, createMissingBackgrounds);

        Transform viewport = listRoot.Find("Viewport");
        if (viewport != null)
        {
            SetImageColor(viewport, panelColor, false, createMissingBackgrounds);

            Mask mask = viewport.GetComponent<Mask>();
            if (mask != null)
            {
                mask.showMaskGraphic = true;
            }
        }

        Transform content = listRoot.Find("Viewport/Content");
        if (content == null)
        {
            return;
        }

        for (int i = 0; i < content.childCount; i++)
        {
            Transform item = content.GetChild(i);

            Image itemImage = item.GetComponent<Image>();
            if (itemImage == null && createMissingBackgrounds)
            {
                itemImage = item.gameObject.AddComponent<Image>();
            }

            if (itemImage != null)
            {
                itemImage.color = itemColor;
                itemImage.raycastTarget = true;
            }

            Transform itemBackground = item.Find("Item Background");
            if (itemBackground != null)
            {
                itemBackground.SetAsFirstSibling();
                SetImageColor(
                    itemBackground,
                    itemColor,
                    true,
                    createMissingBackgrounds
                );
            }

            Toggle toggle = item.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.transition = Selectable.Transition.None;
                if (itemImage != null)
                {
                    toggle.targetGraphic = itemImage;
                }
            }
        }
    }

    private static void SetImageColor(
        Transform target,
        Color color,
        bool keepSprite,
        bool createMissing)
    {
        Image image = target.GetComponent<Image>();
        if (image == null && createMissing)
        {
            image = target.gameObject.AddComponent<Image>();
        }

        if (image == null)
        {
            return;
        }

        image.color = new Color(color.r, color.g, color.b, 1f);
        image.raycastTarget = true;

        if (!keepSprite)
        {
            image.type = Image.Type.Simple;
        }
    }
}
