using UnityEngine;

/// <summary>
/// Tool WateringCan: tưới nước → đặt wateredTile lên tmWatered tilemap.
/// Có dung lượng nước (charges). Hết nước → phải lấy nước từ giếng/sông.
/// Validate: ô trước mặt phải là plowed (hoặc có plant) + chưa tưới + còn nước + đủ stamina.
/// Execute: WateredTileTracker.SetWatered(cell) + trừ stamina + trừ 1 charge.
/// </summary>
public class WateringCanRuntime : ToolRuntime
{
    private enum PendingWateringAction
    {
        None,
        WaterSoil,
        RefillFromSource
    }

    /// <summary>Số lần tưới còn lại. Mặc định = maxCharges.</summary>
    public int CurrentCharges { get; private set; }

    /// <summary>Dung lượng tối đa (lấy từ item stats Range, default 20).</summary>
    public int MaxCharges { get; private set; }

    private EntityRuntime _itemEntity;
    private PendingWateringAction _pendingAction;
    private bool _hasExplicitChargeState;

    public WateringCanRuntime(ToolModule data) : base(data) { }

    /// <summary>Gọi khi item entity được init (từ PrimaryActionEvent lần đầu).</summary>
    public void EnsureInitialized(EntityRuntime item)
    {
        if (item == null) return;

        if (_itemEntity == item && MaxCharges > 0)
            return;

        _itemEntity = item;

        // MaxCharges lấy từ item stat "Range" (reuse stat, hoặc có thể dùng stat riêng)
        // Default = 20 nếu không config
        float configCharges = item?.stats?.Get(StatType.Range) ?? 0f;
        int configuredMax = configCharges > 0 ? Mathf.RoundToInt(configCharges) : 20;
        MaxCharges = Mathf.Max(1, configuredMax);

        // Chỉ fill full khi chưa từng load/save state.
        if (!_hasExplicitChargeState)
            CurrentCharges = MaxCharges;
        else
            CurrentCharges = Mathf.Clamp(CurrentCharges, 0, MaxCharges);
    }

    protected override bool Validate(GameObject actorGO, PrimaryActionEvent e)
    {
        EnsureInitialized(e.item);
        _pendingAction = PendingWateringAction.None;

        var targetCell = GridSystem.GetCellInFrontOf(actorGO);
        var cell2d = new Vector2Int(targetCell.x, targetCell.y);

        var gm = GameManager.Instance;
        if (gm == null) return false;

        // Debug: log actor stats
        if (e.actor?.stats != null)
        {
            float dbgStamina = e.actor.stats.Get(StatType.Stamina);
            float dbgMax = e.actor.stats.Get(StatType.MaxStamina);
            Debug.Log($"[WateringCanRuntime] Validate: Stamina={dbgStamina}/{dbgMax}, Charges={CurrentCharges}/{MaxCharges}, cell={cell2d}");
        }

        var tracker = gm.WateredTileTracker;
        if (tracker == null)
        {
            Debug.LogWarning("[WateringCanRuntime] WateredTileTracker chưa khởi tạo!");
            return false;
        }

        var ws = gm.WorldService;
        if (ws == null) return false;

        if (ws.IsWaterSource(cell2d))
        {
            if (CurrentCharges >= MaxCharges)
            {
                Debug.Log("[WateringCanRuntime] Bình tưới đã đầy nước.");
                return false;
            }

            _pendingAction = PendingWateringAction.RefillFromSource;
            return true;
        }

        // Check còn nước không
        if (CurrentCharges <= 0)
        {
            Debug.Log("[WateringCanRuntime] Hết nước! Cần lấy nước từ giếng.");
            return false;
        }

        // Đã tưới rồi → không tưới lại
        if (tracker.IsWatered(cell2d))
        {
            Debug.Log("[WateringCanRuntime] Ô này đã tưới hôm nay.");
            return false;
        }

        // Chỉ cho phép tưới trên ô đã cuốc.
        if (!ws.IsPlowed(cell2d))
        {
            Debug.Log("[WateringCanRuntime] Ô này chưa được cuốc.");
            return false;
        }

        // Check stamina
        if (e.actor?.stats != null)
        {
            float maxStamina = e.actor.stats.Get(StatType.MaxStamina);
            if (maxStamina > 0f)
            {
                float stamina = e.actor.stats.Get(StatType.Stamina);
                if (stamina < 2f)
                {
                    Debug.Log($"[WateringCanRuntime] Không đủ thể lực. Stamina={stamina}/{maxStamina}");
                    return false;
                }
            }
        }

        _pendingAction = PendingWateringAction.WaterSoil;
        return true;
    }

    protected override string GetAnimationTrigger(PrimaryActionEvent e)
    {
        return _pendingAction == PendingWateringAction.RefillFromSource
            ? _data.GetRefillAnimTrigger()
            : base.GetAnimationTrigger(e);
    }

    protected override void Execute(GameObject actorGO, EntityRuntime actor, EntityRuntime item)
    {
        var targetCell = GridSystem.GetCellInFrontOf(actorGO);
        var cell2d = new Vector2Int(targetCell.x, targetCell.y);

        if (_pendingAction == PendingWateringAction.RefillFromSource)
        {
            Refill();
            _pendingAction = PendingWateringAction.None;
            GameManager.Instance?.EventBus?.Publish(new InventoryVisualRefreshRequestPublish());
            return;
        }

        if (_pendingAction != PendingWateringAction.WaterSoil)
            return;

        var tracker = GameManager.Instance?.WateredTileTracker;
        if (tracker == null) return;

        // Tưới: đặt tile lên tmWatered
        tracker.SetWatered(cell2d);

        // Trừ 1 charge
        CurrentCharges--;

        // Trừ stamina
        if (actor?.stats != null)
        {
            float current = actor.stats.Get(StatType.Stamina);
            actor.stats.Set(StatType.Stamina, Mathf.Max(0f, current - 2f));
        }

        Debug.Log($"[WateringCanRuntime] Tưới tại {cell2d}. Nước còn: {CurrentCharges}/{MaxCharges}");
        _pendingAction = PendingWateringAction.None;
        GameManager.Instance?.EventBus?.Publish(new InventoryVisualRefreshRequestPublish());
    }

    /// <summary>Lấy nước đầy (gọi khi interact giếng/sông).</summary>
    public void Refill()
    {
        CurrentCharges = MaxCharges;
        Debug.Log($"[WateringCanRuntime] Đã lấy nước đầy! {CurrentCharges}/{MaxCharges}");
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    public override ModuleSaveData ToSaveData()
    {
        var json = JsonUtility.ToJson(new WateringCanSave { charges = CurrentCharges, maxCharges = MaxCharges });
        return new ModuleSaveData { moduleType = "WateringCan", dataJson = json };
    }

    public override void ApplySaveData(ModuleSaveData save)
    {
        if (save == null || string.IsNullOrEmpty(save.dataJson)) return;
        var s = JsonUtility.FromJson<WateringCanSave>(save.dataJson);
        CurrentCharges = s.charges;
        MaxCharges = s.maxCharges > 0 ? s.maxCharges : 20;
        _hasExplicitChargeState = true;
        CurrentCharges = Mathf.Clamp(CurrentCharges, 0, MaxCharges);
    }

    [System.Serializable]
    private class WateringCanSave
    {
        public int charges;
        public int maxCharges;
    }
}
