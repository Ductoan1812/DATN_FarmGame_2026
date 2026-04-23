using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bind Inventory (Backpack) data → UI.
/// Subscribe EventBus: InventoryChangedPublish.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform slotsGrid;

    private EntityRuntime _playerEntity;
    private InventoryRuntime _backpack;
    private SlotUI[] _slots;
    private bool _ready;

    private void OnEnable()
    {
        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;
        bus.Subscribe<WorldReadyPublish>(OnWorldReady);
        bus.Subscribe<InventoryChangedPublish>(OnInventoryChanged);
    }

    private void OnDisable()
    {
        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;
        bus.Unsubscribe<WorldReadyPublish>(OnWorldReady);
        bus.Unsubscribe<InventoryChangedPublish>(OnInventoryChanged);
    }

    private void OnWorldReady(WorldReadyPublish _)
    {
        var playerRoot = FindAnyObjectByType<PlayerInventory>()?.GetComponent<EntityRoot>();
        if (playerRoot == null) return;

        _playerEntity = playerRoot.GetEntity();
        if (_playerEntity == null) return;

        _backpack = _playerEntity.GetModules<InventoryRuntime>()
                                 .Find(i => i.Type == InventoryType.Backpack);

        // Cache slot UI
        if (slotsGrid != null)
        {
            _slots = new SlotUI[slotsGrid.childCount];
            for (int i = 0; i < slotsGrid.childCount; i++)
                _slots[i] = new SlotUI(slotsGrid.GetChild(i));
        }

        _ready = true;
        Refresh();
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnInventoryChanged(InventoryChangedPublish e)
    {
        if (e.entityId != _playerEntity?.id) return;
        if (e.inventoryType != InventoryType.Backpack) return;
        Refresh();
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    private void Refresh()
    {
        if (!_ready || _backpack == null || _slots == null) return;

        for (int i = 0; i < _slots.Length; i++)
        {
            var slot = _backpack.GetSlot(i);
            bool isEmpty = slot == null || slot.IsEmpty;

            _slots[i].SetIcon(isEmpty ? null : slot.entity.entityData.icon);
            _slots[i].SetAmount(isEmpty ? 0 : slot.entity.Amount);
        }
    }

    // ── SlotUI helper ─────────────────────────────────────────────────────────

    private class SlotUI
    {
        private readonly Image _icon;
        private readonly TextMeshProUGUI _amount;

        public SlotUI(Transform root)
        {
            var iconT = root.Find("Icon") ?? root.Find("ImgItem");
            if (iconT != null) _icon = iconT.GetComponent<Image>();

            var amountT = root.Find("Amount") ?? root.Find("Quantity");
            if (amountT != null) _amount = amountT.GetComponent<TextMeshProUGUI>();
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
            _amount.text = amount > 1 ? amount.ToString() : "";
        }
    }
}
