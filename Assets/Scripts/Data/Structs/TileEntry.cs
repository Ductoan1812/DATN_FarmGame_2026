using System.Collections.Generic;

/// <summary>
/// Dữ liệu 1 cell trong spatial map.
/// Chỉ chứa dictionary entity theo layer.
/// Ground/tile data thuộc TileRegistry.
/// </summary>
public class TileEntry
{
    /// <summary>Key = EntityLayer, Value = idRuntime của entity chiếm layer đó.</summary>
    public Dictionary<EntityLayer, string> layers = new();

    // ── Backward compat: trả về tất cả entity ids ──
    public List<string> entityIds
    {
        get
        {
            var list = new List<string>(layers.Count);
            foreach (var kv in layers) list.Add(kv.Value);
            return list;
        }
    }

    public bool HasEntityAt(EntityLayer layer) => layers.ContainsKey(layer);

    public string GetEntityAt(EntityLayer layer)
    {
        layers.TryGetValue(layer, out var id);
        return id;
    }

    public bool TryAdd(EntityLayer layer, string idRuntime)
    {
        if (layers.ContainsKey(layer)) return false;
        layers[layer] = idRuntime;
        return true;
    }

    public bool Remove(EntityLayer layer, string idRuntime)
    {
        if (!layers.TryGetValue(layer, out var existing)) return false;
        if (existing != idRuntime) return false;
        layers.Remove(layer);
        return true;
    }

    public void UpdateEntityId(EntityLayer layer, string oldId, string newId)
    {
        if (layers.TryGetValue(layer, out var existing) && existing == oldId)
            layers[layer] = newId;
    }
}
