using UnityEngine;

/// <summary>
/// Runtime của PlacementModule.
/// 
/// Flow (animation-driven):
///   1. PrimaryActionEvent → Validate (check ô trống, placementRule)
///   2. Nếu OK → ToolActionBridge.Request() → play animation
///   3. AnimStrikeEvent → Execute (spawn entity xuống world)
/// </summary>
public class PlacementRuntime : IModuleRuntime, IHandleEvent<PrimaryActionEvent>, IHandleEvent<AnimStrikeEvent>
{
    private readonly PlacementModule _data;

    public PlacementRuntime(PlacementModule data)
    {
        _data = data;
    }

    // ── PrimaryAction: Validate → request animation ───────────────────────────

    public void Handle(PrimaryActionEvent e)
    {
        if (e.actor == null) return;

        var actorGO = e.actor.Owner?.GameObject;
        if (actorGO == null) return;

        // ── Validate ──────────────────────────────────────────────────────────
        var targetCell = GridSystem.GetCellInFrontOf(actorGO);
        var cell2d = new Vector2Int(targetCell.x, targetCell.y);

        var ws = GameManager.Instance?.WorldService;
        if (ws == null)
        {
            Debug.LogWarning("[PlacementRuntime] WorldService null.");
            return;
        }

        // Lấy PlacementRule từ EntityData của item (hạt giống/đồ vật đang cầm)
        var itemData = e.item?.entityData;
        var placedData = _data.placedEntityData != null ? _data.placedEntityData : itemData;
        if (itemData == null)
        {
            Debug.LogWarning("[PlacementRuntime] item.entityData null.");
            return;
        }

        if (!CanPlaceInCurrentSeason(e.item, placedData, out var seasonReason))
        {
            Debug.Log($"[PlacementRuntime] Không thể đặt tại mùa hiện tại: {seasonReason}");
            return;
        }

        if (!ws.CanPlaceAt(placedData.placementRule, cell2d, out var reason))
        {
            Debug.Log($"[PlacementRuntime] Không thể đặt tại {cell2d}: {reason}");
            return;
        }

        // ── Request animation ─────────────────────────────────────────────────
        var bridge = actorGO.GetComponent<ToolActionBridge>();
        if (bridge == null || bridge.IsBusy)
            return;

        var trigger = _data.animTrigger;
        if (string.IsNullOrEmpty(trigger)) trigger = "Sow";

        bridge.Request(e.actor, e.item, trigger);
    }

    // ── AnimStrike: animation đến frame Strike → spawn entity ─────────────────

    public void Handle(AnimStrikeEvent e)
    {
        if (e.actor == null) return;

        var actorGO = e.actor.Owner?.GameObject;
        if (actorGO == null) return;

        var targetCell = GridSystem.GetCellInFrontOf(actorGO);
        var worldPos = new Vector2(targetCell.x, targetCell.y);

        var gm = GameManager.Instance;
        if (gm == null) return;

        if (_data.centerTile) worldPos += new Vector2(0.5f, 0.5f);

        if (_data.placedEntityData == null)
        {
            gm.EventBus.Publish(new SpawnRequestPublish(worldPos, _data.objectTypeToSpawn, e.item, splitOnSpawn: true));
            return;
        }

        if (gm.InventoryService == null || !gm.InventoryService.Consume(e.item, e.actor, 1))
            return;

        gm.EventBus.Publish(new SpawnRequestPublish(
            worldPos,
            _data.objectTypeToSpawn,
            _data.placedEntityData,
            spawnAmount: 1));
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }
    public bool Equals(IModuleRuntime other) => other is PlacementRuntime;

    private static bool CanPlaceInCurrentSeason(EntityRuntime itemRuntime, EntityData placedData, out string reason)
    {
        reason = string.Empty;
        var timeManager = GameManager.Instance?.TimeManager;
        if (timeManager == null)
            return true;

        var runtimeRule = itemRuntime?.GetModule<SeasonRuleRuntime>();
        if (runtimeRule != null && runtimeRule.BlocksPlacementOutOfSeason && !runtimeRule.AllowsSeason(timeManager.Season))
        {
            reason = $"item '{itemRuntime.entityData?.id}' không hợp mùa {timeManager.Season}.";
            return false;
        }

        var dataRule = FindSeasonRuleData(placedData);
        if (dataRule != null && dataRule.blockPlacementOutOfSeason && !dataRule.AllowsSeason(timeManager.Season))
        {
            reason = $"entity '{placedData?.id}' không hợp mùa {timeManager.Season}.";
            return false;
        }

        return true;
    }

    private static SeasonRuleModule FindSeasonRuleData(EntityData data)
    {
        if (data?.modules == null)
            return null;

        for (int i = 0; i < data.modules.Count; i++)
        {
            if (data.modules[i] is SeasonRuleModule seasonRule)
                return seasonRule;
        }

        return null;
    }
}
