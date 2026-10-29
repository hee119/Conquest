using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PersistentTMPDropdownList : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private RectTransform listRoot;

    private bool isOpen;
    private bool itemsBound;

    private void Awake()
    {
        Setup();
        SetListActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SoundManager.Instance?.PlayUiClick();
        ToggleList();
    }

    public void ToggleList()
    {
        Setup();
        SetListActive(!isOpen);
    }

    public void CloseList()
    {
        SetListActive(false);
    }

    public void SelectOption(int index)
    {
        if (dropdown == null ||
            index < 0 ||
            index >= dropdown.options.Count)
        {
            return;
        }

        dropdown.value = index;
        dropdown.RefreshShownValue();
        SoundManager.Instance?.PlayUiConfirm();
        SetListActive(false);
    }

    private void Setup()
    {
        if (dropdown == null)
        {
            dropdown = GetComponent<TMP_Dropdown>();
        }

        if (dropdown == null)
        {
            return;
        }

        if (listRoot == null)
        {
            Transform dropdownList =
                transform.Find("Dropdown List");

            if (dropdownList != null)
            {
                listRoot =
                    dropdownList.GetComponent<RectTransform>();
            }
        }

        if (listRoot == null)
        {
            listRoot = dropdown.template;
        }

        if (listRoot == null ||
            itemsBound)
        {
            return;
        }

        BindExistingItems();
    }

    private void BindExistingItems()
    {
        Transform content =
            listRoot.Find("Viewport/Content");

        if (content == null)
        {
            return;
        }

        int optionCount =
            dropdown.options.Count;

        float itemHeight = 28f;

        for (int i = 0; i < content.childCount; i++)
        {
            Transform child =
                content.GetChild(i);

            RectTransform item =
                child.GetComponent<RectTransform>();

            if (item == null)
            {
                continue;
            }

            bool hasOption =
                i < optionCount;

            child.gameObject.SetActive(hasOption);

            if (!hasOption)
            {
                continue;
            }

            item.anchorMin = new Vector2(0f, 1f);
            item.anchorMax = new Vector2(1f, 1f);
            item.pivot = new Vector2(0.5f, 1f);
            item.sizeDelta = new Vector2(0f, itemHeight);
            item.anchoredPosition =
                new Vector2(0f, -itemHeight * i);

            TextMeshProUGUI label =
                child.GetComponentInChildren<TextMeshProUGUI>(true);

            if (label != null)
            {
                label.text = dropdown.options[i].text;
                label.color = Color.black;
                label.raycastTarget = false;
            }

            Toggle toggle =
                child.GetComponent<Toggle>();

            if (toggle != null)
            {
                toggle.enabled = false;
            }

            Button button =
                child.GetComponent<Button>();

            if (button != null)
            {
                button.enabled = false;
            }

            Image image =
                child.GetComponent<Image>();

            if (image != null)
            {
                image.raycastTarget = true;
            }

            PersistentTMPDropdownOption option =
                child.GetComponent<PersistentTMPDropdownOption>();

            if (option == null)
            {
                option = child.gameObject.AddComponent<PersistentTMPDropdownOption>();
            }

            option.Initialize(this, i);
        }

        RectTransform contentRect =
            content.GetComponent<RectTransform>();

        if (contentRect != null)
        {
            contentRect.sizeDelta =
                new Vector2(
                    contentRect.sizeDelta.x,
                    itemHeight * optionCount
                );
        }

        listRoot.sizeDelta =
            new Vector2(
                listRoot.sizeDelta.x,
                itemHeight * optionCount
            );

        itemsBound = true;
    }

    private void SetListActive(bool active)
    {
        isOpen = active;

        if (listRoot != null)
        {
            listRoot.gameObject.SetActive(active);
        }
    }
}

public class PersistentTMPDropdownOption : MonoBehaviour, IPointerClickHandler
{
    private PersistentTMPDropdownList owner;
    private int index;

    public void Initialize(
        PersistentTMPDropdownList owner,
        int index)
    {
        this.owner = owner;
        this.index = index;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (owner != null)
        {
            owner.SelectOption(index);
        }
    }
}
