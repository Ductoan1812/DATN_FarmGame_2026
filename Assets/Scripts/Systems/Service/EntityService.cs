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

    // ===== Create =====

    public EntityRuntime Create(EntityData data, int amount = 1)
    {
        var entity = new EntityRuntime(data, amount);
        registry.Register(entity);
        return entity;
    }

    // ===== Clone =====

    public EntityRuntime Clone(EntityRuntime source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var save = source.ToSaveData();
        save.id = null;
        var clone = EntityRuntime.LoadFromSave(save, id => source.entityData);
        registry.Register(clone);
        return clone;
    }

    // ===== Split =====

    public EntityRuntime Split(EntityRuntime source, int splitAmount)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (splitAmount <= 0 || splitAmount > source.Amount) return null;
        source.AddAmount(-splitAmount);
        var newEntity = new EntityRuntime(source.entityData, splitAmount);
        registry.Register(newEntity);
        return newEntity;
    }

    // ===== Consume =====

    /// <summary>
    /// Tiêu thụ amount của entity. Trả về true nếu entity bị depleted (Amount = 0).
    /// Nếu depleted → Unregister khỏi Registry.
    /// </summary>
    public bool TryConsume(EntityRuntime entity, int amount)
    {
        if (entity == null || amount <= 0) return false;
        entity.AddAmount(-amount);
        if (entity.IsEmpty)
        {
            registry.Unregister(entity);
            return true;
        }
        return false;
    }

    // ===== Destroy =====

    public void Destroy(EntityRuntime entity)
    {
        entity.Owner?.Remove(entity);
        registry.Unregister(entity);
    }

    // ===== Move =====

    public void Move(EntityRuntime entity, IEntityContainer target)
    {
        entity.Owner?.Remove(entity);
        target.Add(entity);
    }

    // ===== Query =====

    public EntityRuntime Get(string id) => registry.Get(id);

    public IEnumerable<EntityRuntime> GetAll() => registry.GetAll();

    // ===== Save / Load =====

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
