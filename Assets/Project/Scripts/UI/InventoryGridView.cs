using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Shared UI renderer for inventory-like slot grids.
/// It owns only visual slot binding and click routing; gameplay actions stay in the caller.
/// </summary>
public class InventoryGridView : MonoBehaviour
{
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private Button slotTemplate;
    [SerializeField] private int visibleSlots;
    [SerializeField] private bool enableDragDrop;
    [SerializeField] private InventoryType dragInventoryType = InventoryType.Backpack;
    [SerializeField] private bool showAmountWhenOne;

    private readonly List<SlotView> views = new();
    private readonly List<GameObject> spawnedSlots = new();
    private Action<int, InventoryGridItemData> onSlotClicked;
    private Action<int, InventoryGridItemData, RectTransform> onSlotHovered;
    private Action<int> onSlotHoverExited;
    private IReadOnlyList<InventoryGridItemData> currentItems;
    private bool viewsReady;
    private int selectedIndex = -1;

    public void Configure(
        Transform container,
        Button template = null,
        int visibleSlotCount = 0,
        bool dragDropEnabled = false,
        InventoryType dragType = InventoryType.Backpack,
        bool showOneAmount = false)
    {
        slotsContainer = container;
        slotTemplate = template;
        visibleSlots = visibleSlotCount;
        enableDragDrop = dragDropEnabled;
        dragInventoryType = dragType;
        showAmountWhenOne = showOneAmount;
        viewsReady = false;
    }

    public void SetClickHandler(Action<int, InventoryGridItemData> handler)
    {
        onSlotClicked = handler;
    }

    public void SetHoverHandlers(
        Action<int, InventoryGridItemData, RectTransform> hovered,
        Action<int> exited)
    {
        onSlotHovered = hovered;
        onSlotHoverExited = exited;
    }

    public void Render(IReadOnlyList<InventoryGridItemData> items, int selected = -1, int visibleSlotOverride = -1)
    {
        currentItems = items ?? Array.Empty<InventoryGridItemData>();
        selectedIndex = selected;

        int targetSlotCount = visibleSlotOverride > 0
            ? visibleSlotOverride
            : visibleSlots > 0
                ? visibleSlots
                : currentItems.Count;

        EnsureViews(Mathf.Max(targetSlotCount, currentItems.Count));
        RefreshAll();
        ForceRebuild();
    }

    public void SetSlot(int index, InventoryGridItemData item)
    {
        if (index < 0) return;

        EnsureViews(index + 1);
        if (index >= views.Count) return;

        views[index].Set(item, index == selectedIndex);
    }

    public void SetSelectedIndex(int index)
    {
        selectedIndex = index;
        for (int i = 0; i < views.Count; i++)
            views[i].SetSelected(i == selectedIndex);
    }

    public void Clear()
    {
        currentItems = Array.Empty<InventoryGridItemData>();
        selectedIndex = -1;

        foreach (var slot in spawnedSlots)
        {
            if (slot != null) Destroy(slot);
        }

        spawnedSlots.Clear();
        views.Clear();
        viewsReady = false;
    }

    private void EnsureViews(int count)
    {
        if (slotsContainer == null) return;

        if (!viewsReady)
        {
            views.Clear();

            int existingCount = slotsContainer.childCount;
            for (int i = 0; i < existingCount; i++)
            {
                var child = slotsContainer.GetChild(i);
                if (slotTemplate != null && child == slotTemplate.transform)
                    continue;

                views.Add(CreateView(child, views.Count));
            }

            viewsReady = true;
        }

        while (views.Count < count && slotTemplate != null)
        {
            var slot = Instantiate(slotTemplate, slotsContainer);
            slot.gameObject.SetActive(true);
            spawnedSlots.Add(slot.gameObject);
            views.Add(CreateView(slot.transform, views.Count));
        }

        for (int i = 0; i < views.Count; i++)
            views[i].RebindDrag(i, enableDragDrop, dragInventoryType);
    }

    private SlotView CreateView(Transform root, int index)
    {
        return new SlotView(
            root,
            index,
            enableDragDrop,
            dragInventoryType,
            showAmountWhenOne,
            HandleSlotClicked,
            HandleSlotHovered,
            HandleSlotHoverExited);
    }

    private void RefreshAll()
    {
        for (int i = 0; i < views.Count; i++)
        {
            var item = i < currentItems.Count ? currentItems[i] : InventoryGridItemData.Empty;
            views[i].Set(item, i == selectedIndex);
        }
    }

    private void HandleSlotClicked(int index)
    {
        var item = index >= 0 && currentItems != null && index < currentItems.Count
            ? currentItems[index]
            : InventoryGridItemData.Empty;

        if (!item.Interactable) return;

        selectedIndex = index;
        SetSelectedIndex(index);
        onSlotClicked?.Invoke(index, item);
    }

    private void HandleSlotHovered(int index, RectTransform slotRect)
    {
        var item = index >= 0 && currentItems != null && index < currentItems.Count
            ? currentItems[index]
            : InventoryGridItemData.Empty;

        if (!item.Interactable)
        {
            onSlotHoverExited?.Invoke(index);
            return;
        }

        onSlotHovered?.Invoke(index, item, slotRect);
    }

    private void HandleSlotHoverExited(int index)
    {
        onSlotHoverExited?.Invoke(index);
    }

    private void ForceRebuild()
    {
        if (slotsContainer is not RectTransform rect) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    private sealed class SlotView
    {
        private const int MaxDisplayAmount = 99999;

        private readonly Image icon;
        private readonly TMP_Text amountText;
        private readonly GameObject meterRoot;
        private readonly Image meterFillImage;
        private readonly RectTransform meterFillRect;
        private readonly GameObject selectedObject;
        private readonly Toggle toggle;
        private readonly Button button;
        private readonly Transform root;
        private readonly bool showAmountWhenOne;

        public SlotView(
            Transform root,
            int index,
            bool enableDragDrop,
            InventoryType dragType,
            bool showAmountWhenOne,
            Action<int> onSelected,
            Action<int, RectTransform> onHovered,
            Action<int> onHoverExited)
        {
            this.root = root;
            this.showAmountWhenOne = showAmountWhenOne;

            var iconT = root.Find("icon") ?? root.Find("Icon") ?? root.Find("Image");
            if (iconT != null) icon = iconT.GetComponent<Image>();

            var amountT = root.Find("Amount") ?? root.Find("Quantity") ?? root.Find("AmountText");
            if (amountT != null) amountText = amountT.GetComponent<TMP_Text>();

            EnsureMeterVisual(root, out meterRoot, out meterFillImage, out meterFillRect);

            var selectedT = root.Find("Select") ?? root.Find("Selected") ?? root.Find("Highlight");
            if (selectedT != null) selectedObject = selectedT.gameObject;

            if (enableDragDrop)
            {
                var drag = root.GetComponent<DraggableSlot>();
                if (drag == null) drag = root.gameObject.AddComponent<DraggableSlot>();
                drag.Init(dragType, index, icon);
            }

            var hover = root.GetComponent<InventorySlotHoverRelay>();
            if (hover == null) hover = root.gameObject.AddComponent<InventorySlotHoverRelay>();
            hover.Configure(index, root as RectTransform, onHovered, onHoverExited);

            toggle = root.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn) onSelected?.Invoke(index);
                });
                return;
            }

            button = root.GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(() => onSelected?.Invoke(index));
        }

        public void RebindDrag(int index, bool enableDragDrop, InventoryType dragType)
        {
            if (!enableDragDrop) return;

            var drag = root.GetComponent<DraggableSlot>();
            if (drag == null) drag = root.gameObject.AddComponent<DraggableSlot>();
            drag.Init(dragType, index, icon);
        }

        public void Set(InventoryGridItemData item, bool selected)
        {
            SetIcon(item.Icon);
            SetAmount(item.Amount);
            SetMeter(item.ResourceMeter);
            SetSelected(selected);

            if (button != null)
                button.interactable = item.Interactable;

            if (toggle != null)
                toggle.interactable = item.Interactable;
        }

        public void SetSelected(bool selected)
        {
            if (toggle != null)
                toggle.SetIsOnWithoutNotify(selected);

            if (selectedObject != null)
                selectedObject.SetActive(selected);
        }

        private void SetIcon(Sprite sprite)
        {
            if (icon == null) return;

            icon.sprite = sprite;
            icon.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            icon.enabled = sprite != null;
            icon.preserveAspect = true;
        }

        private void SetAmount(int amount)
        {
            if (amountText == null) return;

            if (amount <= 0 || (!showAmountWhenOne && amount <= 1))
            {
                amountText.text = string.Empty;
                return;
            }

            amountText.text = amount > MaxDisplayAmount
                ? MaxDisplayAmount.ToString()
                : amount.ToString();
        }

        private void SetMeter(SlotResourceMeterData meter)
        {
            if (meterRoot == null || meterFillImage == null || meterFillRect == null)
                return;

            bool visible = meter.hasMeter && meter.max > 0;
            if (meterRoot.activeSelf != visible)
                meterRoot.SetActive(visible);

            if (!visible)
                return;

            float normalized = Mathf.Clamp01((float)meter.current / meter.max);
            meterFillRect.anchorMax = new Vector2(1f, normalized);
            meterFillRect.offsetMin = Vector2.zero;
            meterFillRect.offsetMax = Vector2.zero;
            meterFillImage.color = meter.fillColor.a <= 0f
                ? new Color(0.24f, 0.74f, 0.98f, 1f)
                : meter.fillColor;
        }

        private static void EnsureMeterVisual(
            Transform root,
            out GameObject meterRoot,
            out Image fillImage,
            out RectTransform fillRect)
        {
            var meter = root.Find("ResourceMeter");
            if (meter == null)
            {
                var meterObject = new GameObject("ResourceMeter", typeof(RectTransform), typeof(Image));
                meter = meterObject.transform;
                meter.SetParent(root, false);

                var meterRect = (RectTransform)meter;
                meterRect.anchorMin = new Vector2(1f, 0f);
                meterRect.anchorMax = new Vector2(1f, 1f);
                meterRect.pivot = new Vector2(1f, 0.5f);
                meterRect.sizeDelta = new Vector2(8f, -14f);
                meterRect.anchoredPosition = new Vector2(-6f, 0f);

                var background = meter.GetComponent<Image>();
                background.color = new Color(0.08f, 0.12f, 0.16f, 0.82f);
                background.raycastTarget = false;

                var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fillObject.transform.SetParent(meter, false);
                var createdFillRect = (RectTransform)fillObject.transform;
                createdFillRect.anchorMin = new Vector2(0f, 0f);
                createdFillRect.anchorMax = new Vector2(1f, 1f);
                createdFillRect.offsetMin = Vector2.zero;
                createdFillRect.offsetMax = Vector2.zero;

                var createdFillImage = fillObject.GetComponent<Image>();
                createdFillImage.color = new Color(0.24f, 0.74f, 0.98f, 1f);
                createdFillImage.raycastTarget = false;
            }

            meterRoot = meter.gameObject;
            fillRect = meter.Find("Fill") as RectTransform;
            fillImage = fillRect != null ? fillRect.GetComponent<Image>() : null;
            if (meterRoot.activeSelf)
                meterRoot.SetActive(false);
        }
    }
}

public sealed class InventorySlotHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private int index;
    private RectTransform slotRect;
    private Action<int, RectTransform> hovered;
    private Action<int> exited;

    public void Configure(
        int slotIndex,
        RectTransform rect,
        Action<int, RectTransform> onHovered,
        Action<int> onExited)
    {
        index = slotIndex;
        slotRect = rect;
        hovered = onHovered;
        exited = onExited;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered?.Invoke(index, slotRect);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        exited?.Invoke(index);
    }
}

public readonly struct InventoryGridItemData
{
    public static InventoryGridItemData Empty => new(null, 0, null, false, SlotResourceMeterData.None);

    public Sprite Icon { get; }
    public int Amount { get; }
    public object Payload { get; }
    public bool Interactable { get; }
    public SlotResourceMeterData ResourceMeter { get; }

    public InventoryGridItemData(
        Sprite icon,
        int amount,
        object payload = null,
        bool interactable = true,
        SlotResourceMeterData resourceMeter = default)
    {
        Icon = icon;
        Amount = amount;
        Payload = payload;
        Interactable = interactable;
        ResourceMeter = resourceMeter;
    }
}
