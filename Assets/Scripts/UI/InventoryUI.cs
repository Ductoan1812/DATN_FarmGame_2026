using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bind Inventory (Backpack) data từ PlayerInventory → UI.
/// Đợi WorldReady trước khi bind.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform slotsGrid;

    private PlayerInventory _inventory;
    private SlotUI[] _slots;
    private bool _isOpen;
    private bool _ready;

    private void OnEnable()
    {
        var bus = GameManager.Instance?.EventBus;
        if (bus != null) bus.Subscribe<WorldReady>(OnWorldReady);
    }

    private void OnDisable()
    {
        if (_inventory != null)
            _inventory.OnInventoryChanged -= Refresh;
        var bus = GameManager.Instance?.EventBus;
        if (bus != null) bus.Unsubscribe<WorldReady>(OnWorldReady);
    }

    private void OnWorldReady(WorldReady _)
    {
        var player = FindAnyObjectByType<PlayerInventory>();
        if (player == null) { Debug.LogWarning("[InventoryUI] PlayerInventory not found."); return; }
        _inventory = player;

        // Cache slot UI
        if (slotsGrid != null)
        {
            _slots = new SlotUI[slotsGrid.childCount];
            for (int i = 0; i < slotsGrid.childCount; i++)
                _slots[i] = new SlotUI(slotsGrid.GetChild(i));
        }

        // Buttons
        if (openButton != null) openButton.onClick.AddListener(Toggle);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        // Subscribe
        _inventory.OnInventoryChanged += Refresh;

        _ready = true;

        // Bắt đầu ẩn
        gameObject.SetActive(false);
        _isOpen = false;

        Debug.Log("[InventoryUI] Ready.");
    }

    // ══════ Toggle ══════

    public void Toggle()
    {
        _isOpen = !_isOpen;
        gameObject.SetActive(_isOpen);
        if (_isOpen && _ready) Refresh();
    }

    public void Close()
    {
        _isOpen = false;
        gameObject.SetActive(false);
    }

    // ══════ Refresh ══════

    private void Refresh()
    {
        if (!_ready || _inventory == null) return;
        var backpack = _inventory.GetInventory(InventoryType.Backpack);
        if (backpack == null || _slots == null) return;

        for (int i = 0; i < _slots.Length; i++)
        {
            var slot = backpack.GetSlot(i);
            bool isEmpty = slot == null || slot.IsEmpty;

            _slots[i].SetIcon(isEmpty ? null : slot.entity.entityData.icon);
            _slots[i].SetAmount(isEmpty ? 0 : slot.entity.Amount);
        }
    }

    // ══════ SlotUI helper ══════

    private class SlotUI
    {
        private readonly Image _icon;
        private readonly TextMeshProUGUI _amount;

        public SlotUI(Transform root)
        {
            var iconT = root.Find("Icon");
            if (iconT != null) _icon = iconT.GetComponent<Image>();

            var amountT = root.Find("Amount");
            if (amountT != null) _amount = amountT.GetComponent<TextMeshProUGUI>();
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
    }
}
