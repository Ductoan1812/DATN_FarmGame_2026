using UnityEngine;

/// <summary>
/// Tool WateringCan: tưới nước → đặt wateredTile lên tmWatered tilemap.
/// Có dung lượng nước (charges). Hết nước → phải lấy nước từ giếng/sông.
/// Validate: ô trước mặt phải là plowed (hoặc có plant) + chưa tưới + còn nước + đủ stamina.
/// Execute: WateredTileTracker.SetWatered(cell) + trừ stamina + trừ 1 charge.
/// </summary>
public class WateringCanRuntime : ToolRuntime
{
    /// <summary>Số lần tưới còn lại. Mặc định = maxCharges.</summary>
    public int CurrentCharges { get; private set; }

    /// <summary>Dung lượng tối đa (lấy từ item stats Range, default 20).</summary>
    public int MaxCharges { get; private set; }

    private EntityRuntime _itemEntity;

    public WateringCanRuntime(ToolModule data) : base(data) { }

    /// <summary>Gọi khi item entity được init (từ PrimaryActionEvent lần đầu).</summary>
    private void EnsureInit(EntityRuntime item)
    {
        if (_itemEntity == item) return;
        _itemEntity = item;

        // MaxCharges lấy từ item stat "Range" (reuse stat, hoặc có thể dùng stat riêng)
        // Default = 20 nếu không config
        float configCharges = item?.stats?.Get(StatType.Range) ?? 0f;
        MaxCharges = configCharges > 0 ? Mathf.RoundToInt(configCharges) : 20;

        // Nếu chưa có CurrentCharges (lần đầu) → full
        if (CurrentCharges <= 0)
            CurrentCharges = MaxCharges;
    }

    protected override bool Validate(GameObject actorGO, PrimaryActionEvent e)
    {
        EnsureInit(e.item);

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

        // Check ô phải là plowed HOẶC có plant entity
        var ws = gm.WorldService;
        if (ws == null) return false;

        var tileData = gm.TileData;
        if (tileData?.plowedTile == null) return false;

        bool isPlowed = false;
        var groundTile = ws.GetGround(cell2d);
        if (groundTile == tileData.plowedTile)
            isPlowed = true;

        bool hasPlant = ws.HasBlockerAt(cell2d, EntityLayer.Plant);

        if (!isPlowed && !hasPlant)
        {
            Debug.Log("[WateringCanRuntime] Ô không phải đất cày và không có cây.");
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

        return true;
    }

    protected override void Execute(GameObject actorGO, EntityRuntime actor, EntityRuntime item)
    {
        var targetCell = GridSystem.GetCellInFrontOf(actorGO);
        var cell2d = new Vector2Int(targetCell.x, targetCell.y);

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
    }

    [System.Serializable]
    private class WateringCanSave
    {
        public int charges;
        public int maxCharges;
    }
}
