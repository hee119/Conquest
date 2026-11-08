using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The popup and blocker are authored in the scene beneath its shared canvas.
// Only option rows are reused as the option count changes.
public sealed class SharedCanvasDropdown : TMP_Dropdown
{
    [SerializeField] private RectTransform popupRoot;
    [SerializeField] private Button blocker;
    private readonly List<Toggle> rows = new List<Toggle>();
    private Toggle itemTemplate;
    private float rowHeight;
    private float popupHeight;
    public new bool IsExpanded => popupRoot != null ? popupRoot.gameObject.activeSelf : base.IsExpanded;
    public bool UsesSharedPopup => popupRoot != null;

    protected override void Awake()
    {
        base.Awake();
        if (template != null)
        {
            itemTemplate = template.GetComponentInChildren<Toggle>(true);
            if (itemTemplate != null) rowHeight = ((RectTransform)itemTemplate.transform).rect.height;
            popupHeight = template.rect.height;
        }
        Hide();
    }

    public override void OnPointerClick(PointerEventData eventData) => Show();
    public override void OnSubmit(BaseEventData eventData) => Show();
    public override void OnCancel(BaseEventData eventData) => Hide();
    protected override void OnDisable() { Hide(); base.OnDisable(); }

    public new void Show()
    {
        if (popupRoot == null) { base.Show(); return; }
        if (!IsActive() || !IsInteractable() || IsExpanded || popupRoot == null ||
            template == null || itemTemplate == null || options.Count == 0) return;
        var content = (RectTransform)itemTemplate.transform.parent;
        itemTemplate.gameObject.SetActive(false);
        while (rows.Count < options.Count)
        {
            int index = rows.Count;
            var row = Instantiate(itemTemplate, content, false);
            row.name = "Option " + index;
            row.onValueChanged.AddListener(_ => SelectOption(index));
            rows.Add(row);
        }
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            row.gameObject.SetActive(false);
            if (i >= options.Count) continue;
            var rect = (RectTransform)row.transform;
            rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, 1f);
            rect.sizeDelta = new Vector2(0f, rowHeight);
            rect.anchoredPosition = new Vector2(0f, -i * rowHeight);
            var label = row.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = options[i].text;
            row.SetIsOnWithoutNotify(i == value);
            row.gameObject.SetActive(true);
        }
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rowHeight * options.Count);
        template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            Mathf.Min(popupHeight, rowHeight * options.Count + 2f));
        content.anchoredPosition = Vector2.zero;
        var group = template.GetComponent<CanvasGroup>();
        if (group != null) { group.alpha = 1f; group.blocksRaycasts = true; group.interactable = true; }
        popupRoot.SetAsLastSibling();
        popupRoot.gameObject.SetActive(true);
        template.gameObject.SetActive(true);
        if (blocker != null) blocker.gameObject.SetActive(true);
        if (value < rows.Count) rows[value].Select();
    }

    public new void Hide()
    {
        if (popupRoot != null) popupRoot.gameObject.SetActive(false);
        else base.Hide();
    }

    private void SelectOption(int index)
    {
        value = index;
        RefreshShownValue();
        Hide();
        Select();
    }
}
