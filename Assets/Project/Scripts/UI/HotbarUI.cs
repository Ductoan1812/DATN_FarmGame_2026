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

    private void Start()
    {
        CacheSlots();
        CacheSelectIndicator();
        TrySubscribe();
    }

    private void OnEnable()
    {
        TrySubscribe();
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

    // ══════ Cache ══════

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
        if (_slots == null || e.index < 0 || e.index >= _slots.Length) return;

        _slots[e.index].SetIcon(e.icon);
        _slots[e.index].SetAmount(e.amount);
    }

    private void OnSelectionChanged(HotbarSelectionChangedPublish e)
    {
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

        public SlotView(Transform root, int index)
        {
            var iconT = root.Find("Icon");
            if (iconT != null) _icon = iconT.GetComponent<Image>();

            var amountT = root.Find("Amount");
            if (amountT != null) _amount = amountT.GetComponent<TextMeshProUGUI>();

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
    }
}
