using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Registry chứa toàn bộ EntityData (ScriptableObject).
/// Dùng để tra cứu EntityData theo id hoặc keyName.
/// </summary>
public class EntityDataRegistry
{
    private readonly Dictionary<string, EntityData> byId = new();
    private readonly Dictionary<string, EntityData> byKey = new();
    private readonly List<EntityData> all = new();

    /// <summary>Đăng ký 1 EntityData.</summary>
    public void Register(EntityData data)
    {
        if (data == null) return;

        if (!string.IsNullOrEmpty(data.id))
        {
            if (byId.ContainsKey(data.id))
                Debug.LogWarning($"[EntityDataRegistry] Duplicate id: {data.id} ({data.keyName})");
            byId[data.id] = data;
        }

        if (!string.IsNullOrEmpty(data.keyName))
        {
            var key = data.keyName.ToLowerInvariant();
            if (byKey.ContainsKey(key))
                Debug.LogWarning($"[EntityDataRegistry] Duplicate keyName: {data.keyName}");
            byKey[key] = data;
        }

        all.Add(data);
    }

    /// <summary>Đăng ký nhiều EntityData cùng lúc.</summary>
    public void RegisterAll(IEnumerable<EntityData> dataList)
    {
        foreach (var d in dataList) Register(d);
    }

    /// <summary>Tìm theo id chính xác.</summary>
    public EntityData GetById(string id)
    {
        return !string.IsNullOrEmpty(id) && byId.TryGetValue(id, out var data) ? data : null;
    }

    /// <summary>Tìm theo keyName (case-insensitive).</summary>
    public EntityData GetByKey(string keyName)
    {
        if (string.IsNullOrEmpty(keyName)) return null;
        return byKey.TryGetValue(keyName.ToLowerInvariant(), out var data) ? data : null;
    }

    /// <summary>Tìm theo id chính xác (alias cho GetById).</summary>
    public EntityData FindById(string id) => GetById(id);

    /// <summary>Tìm theo id hoặc keyName.</summary>
    public EntityData Find(string idOrKey)
    {
        return GetById(idOrKey) ?? GetByKey(idOrKey);
    }

    /// <summary>Tìm kiếm gợi ý — trả về danh sách EntityData có id hoặc keyName chứa query.</summary>
    public List<EntityData> Search(string query, int maxResults = 10)
    {
        if (string.IsNullOrEmpty(query)) return all.Take(maxResults).ToList();

        var q = query.ToLowerInvariant();
        return all
            .Where(d => (!string.IsNullOrEmpty(d.id) && d.id.ToLowerInvariant().Contains(q))
                     || (!string.IsNullOrEmpty(d.keyName) && d.keyName.ToLowerInvariant().Contains(q)))
            .Take(maxResults)
            .ToList();
    }

    /// <summary>Trả về toàn bộ EntityData đã đăng ký.</summary>
    public IReadOnlyList<EntityData> GetAll() => all;

    public int Count => all.Count;
}
