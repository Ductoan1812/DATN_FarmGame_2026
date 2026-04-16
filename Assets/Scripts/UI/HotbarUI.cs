using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bind Hotbar data từ PlayerInventory → UI.
/// Đợi WorldReady trước khi bind.
/// </summary>
public class HotbarUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TextMeshProUGUI slotLabel;

    private PlayerInventory _inventory;
    private SlotUI[] _slots;
    private bool _ready;

    private void OnEnable()
    {
        var bus = GameManager.Instance?.EventBus;
        if (bus != null) bus.Subscribe<WorldReady>(OnWorldReady);
    }

    private void OnDisable()
    {
        if (_inventory != null)
        {
            _inventory.OnInventoryChanged -= Refresh;
            _inventory.OnHotbarSelectionChanged -= OnSelectionChanged;
        }
        var bus = GameManager.Instance?.EventBus;
        if (bus != null) bus.Unsubscribe<WorldReady>(OnWorldReady);
    }

    private void OnWorldReady(WorldReady _)
    {
        var player = FindAnyObjectByType<PlayerInventory>();
        if (player == null) { Debug.LogWarning("[HotbarUI] PlayerInventory not found."); return; }
        _inventory = player;

        // Cache slot UI
        _slots = new SlotUI[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            _slots[i] = new SlotUI(transform.GetChild(i));

        // Subscribe events
        _inventory.OnInventoryChanged += Refresh;
        _inventory.OnHotbarSelectionChanged += OnSelectionChanged;

        _ready = true;
        Refresh();
        Debug.Log("[HotbarUI] Ready.");
    }

    // ══════ Refresh ══════

    private void Refresh()
    {
        if (!_ready || _inventory == null) return;
        var hotbar = _inventory.GetInventory(InventoryType.Hotbar);
        if (hotbar == null || _slots == null) return;

        for (int i = 0; i < _slots.Length; i++)
        {
            var slot = hotbar.GetSlot(i);
            bool isEmpty = slot == null || slot.IsEmpty;

            _slots[i].SetIcon(isEmpty ? null : slot.entity.entityData.icon);
            _slots[i].SetAmount(isEmpty ? 0 : slot.entity.Amount);
            _slots[i].SetSelected(i == _inventory.SelectedHotbarIndex);
        }

        UpdateLabel();
    }

    private void OnSelectionChanged(int index)
    {
        if (_slots == null) return;
        for (int i = 0; i < _slots.Length; i++)
            _slots[i].SetSelected(i == index);
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (slotLabel == null) return;
        var item = _inventory?.SelectedItem;
        slotLabel.text = item != null ? item.entityData.keyName : "";
    }

    // ══════ SlotUI helper ══════

    private class SlotUI
    {
        private readonly Image _icon;
        private readonly TextMeshProUGUI _amount;
        private readonly GameObject _highlight;

        public SlotUI(Transform root)
        {
            var iconT = root.Find("Icon");
            if (iconT != null) _icon = iconT.GetComponent<Image>();

            var amountT = root.Find("Amount");
            if (amountT != null) _amount = amountT.GetComponent<TextMeshProUGUI>();

            var hlT = root.Find("Highlight");
            if (hlT != null) _highlight = hlT.gameObject;
        }

        public void SetIcon(Sprite sprite)
        {
            if (_icon == null) return;
            _icon.sprite = sprite;
            _icon.color = sprite != null ? Color.white : new Color(1, 1, 1, 0.15f);
        }

        public void SetAmount(int amount)
        {
            if (_amount == null) return;
            _amount.text = amount > 1 ? amount.ToString() : "";
        }

        public void SetSelected(bool selected)
        {
            if (_highlight != null) _highlight.SetActive(selected);
        }
    }
}
