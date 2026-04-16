using UnityEngine;

/// <summary>
/// Kiểm tra entity có được đặt tại cell hay không.
/// Logic đơn giản, 1 hàm duy nhất, dễ đọc dễ debug.
/// </summary>
public class PlacementValidator
{
    private readonly WorldEntityRegistry _registry;
    private readonly TileData _tileData;

    public PlacementValidator(WorldEntityRegistry registry, TileData tileData)
    {
        _registry = registry;
        _tileData = tileData;
    }

    /// <summary>
    /// Kiểm tra entity có đặt được tại cell không.
    /// Trả về SpawnResult + reason string để debug.
    /// </summary>
    public SpawnResult CanPlace(PlacementRule rule, Vector2Int cell, out string reason)
    {
        var entry = _registry.GetTileEntry(cell);

        // ── 1. Layer đã bị chiếm? ──
        if (entry != null && entry.HasEntityAt(rule.occupyLayer))
        {
            reason = $"Layer {rule.occupyLayer} đã có entity tại {cell}";
            return SpawnResult.CellBlocked;
        }

        // ── 2. Có entity nào đang block layer này? ──
        if (entry != null)
        {
            foreach (var kv in entry.layers)
            {
                var otherEp = _registry.GetEntity(kv.Value);
                if (otherEp == null) continue;

                var otherData = ResolveEntityData(otherEp);
                if (otherData == null) continue;

                if (IsBlocking(otherData.placementRule, rule.occupyLayer))
                {
                    reason = $"Entity '{otherEp.idPrefab}' tại layer {kv.Key} block layer {rule.occupyLayer}";
                    return SpawnResult.CellBlocked;
                }
            }
        }

        // ── 3. Kiểm tra requireTags ──
        if (rule.requireTags != PlacementTag.None)
        {
            var availableTags = GatherTags(cell, entry);

            if ((availableTags & rule.requireTags) == 0)
            {
                reason = $"Thiếu tag: cần [{rule.requireTags}], có [{availableTags}] tại {cell}";
                return SpawnResult.ConditionFailed;
            }
        }

        reason = null;
        return SpawnResult.Success;
    }

    // ── Private ──────────────────────────────────────────

    /// <summary>Gộp tất cả tags tại cell: từ Tilemap ground + từ entity provideTags.</summary>
    private PlacementTag GatherTags(Vector2Int cell, TileEntry entry)
    {
        var tags = PlacementTag.None;

        // Tags từ ground tile (Tilemap)
        var groundTile = _registry.GetGround(cell);
        if (_tileData != null && groundTile != null)
            tags |= _tileData.GetTags(groundTile);

        // Tags từ các entity đã có tại cell
        if (entry != null)
        {
            foreach (var kv in entry.layers)
            {
                var ep = _registry.GetEntity(kv.Value);
                if (ep == null) continue;

                var data = ResolveEntityData(ep);
                if (data != null)
                    tags |= data.placementRule.provideTags;
            }
        }

        return tags;
    }

    /// <summary>Kiểm tra rule có block layer target không.</summary>
    private static bool IsBlocking(PlacementRule rule, EntityLayer targetLayer)
    {
        if (rule.blockLayers == null) return false;
        foreach (var blocked in rule.blockLayers)
            if (blocked == targetLayer) return true;
        return false;
    }

    /// <summary>Resolve EntityData từ EntityPosition thông qua EntityRegistry.</summary>
    private static EntityData ResolveEntityData(EntityPosition ep)
    {
        // Lấy từ EntityService thông qua GameManager
        var runtime = GameManager.Instance?.EntityService?.Get(ep.idRuntime);
        return runtime?.entityData;
    }
}
