using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// FE Backpack — gắn vào UIManager (luôn active, không bao giờ miss event).
/// Cache data từ BackpackSlotChangedPublish.
/// Khi slotsContainer (Backpack_panel) active → apply data lên UI.
/// </summary>
public class BackpackUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private Button btnSort;

    private const int MaxDisplayAmount = 99999;

    // ── Cache data (luôn cập nhật dù UI ẩn) ──
    private struct SlotData
    {
        public Sprite icon;
        public int amount;
    }

    private SlotData[] _cache;
    private SlotView[] _views;
    private bool _viewsReady;
    private bool _wasActive;   // track trạng thái frame trước
    private bool _dirty;       // có data mới cần refresh
    private bool _subscribed;  // đã subscribe EventBus chưa

    // ══════ Lifecycle ══════

    private void Start()
    {
        TrySubscribe();
    }

    private void OnEnable()
    {
        TrySubscribe();

        if (btnSort != null)
            btnSort.onClick.AddListener(OnSortClicked);
    }

    private void OnDisable()
    {
        if (_subscribed)
        {
            var bus = GameManager.Instance?.EventBus;
            if (bus != null)
                bus.Unsubscribe<BackpackSlotChangedPublish>(OnSlotChanged);
            _subscribed = false;
        }

        if (btnSort != null)
            btnSort.onClick.RemoveListener(OnSortClicked);
    }

    private void TrySubscribe()
    {
        if (_subscribed) return;

        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;

        bus.Subscribe<BackpackSlotChangedPublish>(OnSlotChanged);
        _subscribed = true;
    }

    // ══════ Event handler — cache data ══════

    private void OnSlotChanged(BackpackSlotChangedPublish e)
    {
        EnsureCache(e.index + 1);
        _cache[e.index] = new SlotData { icon = e.icon, amount = e.amount };
        _dirty = true;

        // Nếu UI đang hiển thị → apply ngay
        if (IsContainerActive())
        {
            EnsureViews();
            ApplySlot(e.index);
        }
    }

    // ══════ Detect panel vừa bật → refresh toàn bộ ══════

    private void LateUpdate()
    {
        // Retry subscribe nếu chưa thành công (EventBus chưa sẵn sàng lúc OnEnable/Start)
        if (!_subscribed)
            TrySubscribe();

        bool active = IsContainerActive();

        // Vừa chuyển từ inactive → active: refresh toàn bộ
        if (active && (!_wasActive || _dirty))
        {
            EnsureViews();
            RefreshAll();
            _dirty = false;
        }

        _wasActive = active;
    }

    private void RefreshAll()
    {
        if (_cache == null || _views == null) return;
        for (int i = 0; i < _cache.Length && i < _views.Length; i++)
            ApplySlot(i);
    }

    // ══════ Sort ══════

    private void OnSortClicked()
    {
        var bridge = FindAnyObjectByType<PlayerBridge>();
        if (bridge != null) bridge.BridgeSort();
    }

    // ══════ Internal ══════

    private bool IsContainerActive()
    {
        return slotsContainer != null && slotsContainer.gameObject.activeInHierarchy;
    }

    private void EnsureCache(int minSize)
    {
        if (_cache == null)
        {
            _cache = new SlotData[minSize];
            return;
        }
        if (_cache.Length < minSize)
        {
            var old = _cache;
            _cache = new SlotData[minSize];
            old.CopyTo(_cache, 0);
        }
    }

    private void EnsureViews()
    {
        if (_viewsReady || slotsContainer == null) return;

        int count = slotsContainer.childCount;
        _views = new SlotView[count];
        for (int i = 0; i < count; i++)
            _views[i] = new SlotView(slotsContainer.GetChild(i));

        _viewsReady = true;
    }

    private void ApplySlot(int index)
    {
        if (_views == null || index < 0 || index >= _views.Length) return;
        if (_cache == null || index >= _cache.Length) return;

        _views[index].SetIcon(_cache[index].icon);
        _views[index].SetAmount(_cache[index].amount);
    }

    // ══════ SlotView ══════

    private class SlotView
    {
        private readonly Image _icon;
        private readonly TextMeshProUGUI _amount;

        public SlotView(Transform root)
        {
            var iconT = root.Find("icon");
            if (iconT != null) _icon = iconT.GetComponent<Image>();

            var amountT = root.Find("Amount");
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

            if (amount <= 0)
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
