using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EntityService
{
    private EntityRegistry registry;

    public EntityService(EntityRegistry registry)
    {
        this.registry = registry;
    }

    // ══════════════════════════════════════════════════════════
    //  Amount mutation — single source of truth
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Set Amount tuyệt đối (đã clamp về [0, MaxStack]).
    /// Nếu kết quả = 0 → tự Unregister khỏi Registry.
    /// Trả về true nếu entity bị depleted (Amount = 0) sau khi set.
    /// MỌI thay đổi Amount trong game PHẢI đi qua hàm này.
    /// </summary>
    public bool SetAmount(EntityRuntime entity, int value)
    {
        if (entity == null) return false;
        int clamped = Mathf.Clamp(value, 0, entity.MaxStack);
        entity.Amount = clamped;
        if (clamped <= 0)
        {
            registry.Unregister(entity);
            return true;
        }
        return false;
    }

    /// <summary>Cộng/trừ Amount theo delta. Trả về true nếu depleted.</summary>
    public bool AddAmount(EntityRuntime entity, int delta)
    {
        if (entity == null) return false;
        return SetAmount(entity, entity.Amount + delta);
    }

    // ══════════════════════════════════════════════════════════
    //  Stack rules — pure function, không mutate
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Hai entity có thể stack vào chung 1 slot hay không.
    /// Pure function — không cần instance service state.
    /// </summary>
    public static bool CanStack(EntityRuntime a, EntityRuntime b)
    {
        if (a == null || b == null) return false;
        if (a.entityData == null || b.entityData == null) return false;
        if (a.entityData.maxStack <= 1) return false;
        if (a.entityData.id != b.entityData.id) return false;
        if (!a.stats.Equals(b.stats)) return false;
        if (a.modules.Count != b.modules.Count) return false;
        foreach (var ma in a.modules)
        {
            var t = ma.GetType();
            var mb = b.modules.Find(m => m.GetType() == t);
            if (mb == null) return false;
            if (!ma.Equals(mb)) return false;
        }
        return true;
    }

    // ══════════════════════════════════════════════════════════
    //  Create / Clone / Split / Merge / Consume / Destroy
    // ══════════════════════════════════════════════════════════

    public EntityRuntime Create(EntityData data, int amount = 1)
    {
        var entity = new EntityRuntime(data, amount);
        registry.Register(entity);
        return entity;
    }

    public EntityRuntime Clone(EntityRuntime source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var save = source.ToSaveData();
        save.id = null;
        var clone = EntityRuntime.LoadFromSave(save, id => source.entityData);
        registry.Register(clone);
        return clone;
    }

    /// <summary>Tách splitAmount khỏi source, tạo entity mới cùng data.</summary>
    public EntityRuntime Split(EntityRuntime source, int splitAmount)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (splitAmount <= 0 || splitAmount > source.Amount) return null;

        AddAmount(source, -splitAmount); // có thể Unregister source nếu về 0

        var newEntity = new EntityRuntime(source.entityData, splitAmount);
        registry.Register(newEntity);
        return newEntity;
    }

    /// <summary>
    /// Dồn src vào dst (nếu CanStack). Trả về số lượng đã dồn.
    /// Nếu src hết sạch → tự Unregister (do SetAmount xử lý).
    /// </summary>
    public int Merge(EntityRuntime dst, EntityRuntime src)
    {
        if (dst == null || src == null) return 0;
        if (!CanStack(dst, src)) return 0;

        int take = Mathf.Min(src.Amount, dst.FreeSpace);
        if (take <= 0) return 0;

        AddAmount(dst, take);
        AddAmount(src, -take); // nếu src về 0 → Unregister
        return take;
    }

    /// <summary>
    /// Tiêu thụ amount của entity. Trả về true nếu entity bị depleted.
    /// </summary>
    public bool TryConsume(EntityRuntime entity, int amount)
    {
        if (entity == null || amount <= 0) return false;
        return AddAmount(entity, -amount);
    }

    public void Destroy(EntityRuntime entity)
    {
        if (entity == null) return;
        entity.Owner?.Remove(entity);
        registry.Unregister(entity);
    }

    // ══════════════════════════════════════════════════════════
    //  Move / Query
    // ══════════════════════════════════════════════════════════

    public void Move(EntityRuntime entity, IEntityContainer target)
    {
        entity.Owner?.Remove(entity);
        target.Add(entity);
    }

    public EntityRuntime Get(string id) => registry.Get(id);

    public IEnumerable<EntityRuntime> GetAll() => registry.GetAll();

    // ══════════════════════════════════════════════════════════
    //  Save / Load
    // ══════════════════════════════════════════════════════════

    [Serializable]
    public class EntitySaveContainer
    {
        public EntitySaveData[] entities;
    }

    public void SaveData(string filename, bool saveToFile = true)
    {
        var list = new List<EntitySaveData>();
        foreach (var entity in registry.GetAll())
        {
            if (entity == null) continue;
            list.Add(entity.ToSaveData());
        }

        var container = new EntitySaveContainer { entities = list.ToArray() };
        var json = JsonUtility.ToJson(container, true);

        if (saveToFile)
        {
            var path = Path.Combine(Application.persistentDataPath, filename);
            File.WriteAllText(path, json);
            Debug.Log($"Saved {list.Count} entities to {path}");
        }
        else
        {
            PlayerPrefs.SetString("entities_save", json);
            PlayerPrefs.Save();
            Debug.Log($"Saved {list.Count} entities to PlayerPrefs");
        }
    }

    public void LoadData(Func<string, EntityData> resolver, string filename, bool fromFile = true)
    {
        if (resolver == null) throw new ArgumentNullException(nameof(resolver));

        string json;
        if (fromFile)
        {
            var path = Path.Combine(Application.persistentDataPath, filename);
            if (!File.Exists(path)) { Debug.LogWarning("Save file not found: " + path); return; }
            json = File.ReadAllText(path);
        }
        else
        {
            json = PlayerPrefs.GetString("entities_save");
            if (string.IsNullOrEmpty(json)) { Debug.LogWarning("No save in PlayerPrefs"); return; }
        }

        EntitySaveContainer container;
        try { container = JsonUtility.FromJson<EntitySaveContainer>(json); }
        catch (Exception ex) { Debug.LogError("Failed to parse entities save JSON: " + ex.Message); return; }

        if (container?.entities == null) { Debug.LogWarning("No entities in save"); return; }

        registry.Clear();

        int loaded = 0;
        foreach (var save in container.entities)
        {
            try
            {
                var runtime = EntityRuntime.LoadFromSave(save, resolver);
                if (runtime != null) { registry.Register(runtime); loaded++; }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load entity id={save?.entityDataId}: {ex.Message}");
            }
        }

        Debug.Log($"Loaded {loaded}/{container.entities.Length} entities into registry");
    }

    /// <summary>
    /// Restore inventory slots sau khi tất cả GameObject đã spawn (Container đã được set).
    /// Gọi từ SaveLoadManager sau khi WorldEntityService.Load xong.
    /// </summary>
    public void RestoreAllInventories()
    {
        int count = 0;
        foreach (var entity in registry.GetAll())
            foreach (var inv in entity.GetModules<InventoryRuntime>())
            {
                // Entity và inventory cùng chủ sở hữu → set Container trước khi RestoreSlots
                if (inv.Container == null && entity.Owner != null)
                    inv.Container = entity.Owner;

                inv.RestoreSlots(registry);
                count++;
            }
        Debug.Log($"[EntityService] RestoreAllInventories: {count} inventory(s) restored.");
    }
}
