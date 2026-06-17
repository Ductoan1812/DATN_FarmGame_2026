using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// FE Hotbar — subscribe HotbarSlotChangedPublish + HotbarSelectionChangedPublish.
/// Gắn vào GameObject "Hotbar" trong HUD_Canvas.
/// Children theo thứ tự = slot index (child 0 = index 0, ...).
/// Mỗi child cần: "Icon" (Image), "Amount" (TMP), slot đầu tiên chứa "Select" (Image).
/// </summary>
public class HotbarUI : MonoBehaviour
{
    private const int MaxDisplayAmount = 99999;

    // ── Cache ──
    private SlotView[] _slots;
    private Transform _selectIndicator;
    private int _currentSelected = -1;
    private bool _subscribed;

    // ══════ Lifecycle ══════

    private void Awake()
    {
        CacheSlots();
        CacheSelectIndicator();
    }

    private void Start()
    {
        EnsureCached();
        TrySubscribe();
        PublishVisualRefreshRequest();
    }

    private void OnEnable()
    {
        EnsureCached();
        TrySubscribe();
        PublishVisualRefreshRequest();
    }

    private void OnDestroy()
    {
        if (!_subscribed) return;
        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;
        bus.Unsubscribe<HotbarSlotChangedPublish>(OnSlotChanged);
        bus.Unsubscribe<HotbarSelectionChangedPublish>(OnSelectionChanged);
        _subscribed = false;
    }

    private void TrySubscribe()
    {
        if (_subscribed) return;
        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;
        bus.Subscribe<HotbarSlotChangedPublish>(OnSlotChanged);
        bus.Subscribe<HotbarSelectionChangedPublish>(OnSelectionChanged);
        _subscribed = true;
    }

    private void PublishVisualRefreshRequest()
    {
        GameManager.Instance?.EventBus?.Publish(new InventoryVisualRefreshRequestPublish());
    }

    // ══════ Cache ══════

    private void EnsureCached()
    {
        if (_slots == null || _slots.Length != transform.childCount)
            CacheSlots();

        if (_selectIndicator == null)
            CacheSelectIndicator();
    }

    private void CacheSlots()
    {
        int count = transform.childCount;
        _slots = new SlotView[count];
        for (int i = 0; i < count; i++)
            _slots[i] = new SlotView(transform.GetChild(i), i);
    }

    private void CacheSelectIndicator()
    {
        // Tìm "Select" trong bất kỳ slot con nào (mặc định nằm ở slot đầu)
        for (int i = 0; i < transform.childCount; i++)
        {
            var sel = transform.GetChild(i).Find("Select");
            if (sel != null)
            {
                _selectIndicator = sel;
                _currentSelected = i;
                return;
            }
        }
    }

    // ══════ Event handlers ══════

    private void OnSlotChanged(HotbarSlotChangedPublish e)
    {
        EnsureCached();
        if (_slots == null || e.index < 0 || e.index >= _slots.Length) return;

        _slots[e.index].SetIcon(e.icon);
        _slots[e.index].SetAmount(e.amount);
        _slots[e.index].SetMeter(e.meter);
    }

    private void OnSelectionChanged(HotbarSelectionChangedPublish e)
    {
        EnsureCached();
        if (e.selectedIndex != _currentSelected)
            MoveSelect(e.selectedIndex);
    }

    // ══════ Select indicator ══════

    private void MoveSelect(int newIndex)
    {
        if (_selectIndicator == null) return;
        if (newIndex < 0 || newIndex >= _slots.Length) return;

        _selectIndicator.SetParent(transform.GetChild(newIndex), false);
        _currentSelected = newIndex;
    }

    // ══════ SlotView ══════

    private class SlotView
    {
        private readonly Image _icon;
        private readonly TextMeshProUGUI _amount;
        private readonly RectTransform _meterFill;
        private readonly Image _meterFillImage;
        private readonly GameObject _meterRoot;

        public SlotView(Transform root, int index)
        {
            var iconT = root.Find("Icon");
            if (iconT != null) _icon = iconT.GetComponent<Image>();

            var amountT = root.Find("Amount");
            if (amountT != null) _amount = amountT.GetComponent<TextMeshProUGUI>();

            EnsureMeterVisual(root, out _meterRoot, out _meterFillImage, out _meterFill);

            // Drag & drop
            var drag = root.GetComponent<DraggableSlot>();
            if (drag == null) drag = root.gameObject.AddComponent<DraggableSlot>();
            drag.Init(InventoryType.Hotbar, index, _icon);
        }

        public void SetIcon(Sprite sprite)
        {
            if (_icon == null) return;
            _icon.sprite = sprite;
            _icon.color = sprite != null ? Color.white : new Color(1, 1, 1, 0);
        }

        public void SetAmount(int amount)
        {
            if (_amount == null) return;

            if (amount <= 1)
            {
                _amount.text = "";
                return;
            }

            _amount.text = amount > MaxDisplayAmount
                ? MaxDisplayAmount.ToString()
                : amount.ToString();
        }

        public void SetMeter(SlotResourceMeterData meter)
        {
            if (_meterRoot == null || _meterFill == null || _meterFillImage == null)
                return;

            bool visible = meter.hasMeter && meter.max > 0;
            if (_meterRoot.activeSelf != visible)
                _meterRoot.SetActive(visible);

            if (!visible)
                return;

            float normalized = Mathf.Clamp01((float)meter.current / meter.max);
            _meterFill.anchorMax = new Vector2(1f, normalized);
            _meterFill.offsetMin = Vector2.zero;
            _meterFill.offsetMax = Vector2.zero;
            _meterFillImage.color = meter.fillColor.a <= 0f
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
                meterRect.sizeDelta = new Vector2(8f, -18f);
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
