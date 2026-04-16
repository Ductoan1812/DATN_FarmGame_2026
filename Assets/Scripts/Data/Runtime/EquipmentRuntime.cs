using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Container chứa item đang trang bị trên Player/NPC.
/// Mỗi EquipSlot chứa 1 entity.
/// Hand slot đặc biệt: giữ refs chung với Hotbar (không xóa khỏi Inventory).
/// </summary>
public class EquipmentRuntime : IModuleRuntime
{
    private readonly Dictionary<EquipSlot, EntityRuntime> _slots = new();

    /// <summary>StatsRuntime của owner (player) — set từ bên ngoài khi init.</summary>
    public StatsRuntime OwnerStats { get; set; }

    // ══════ Equip ══════

    /// <summary>
    /// Trang bị entity vào slot tương ứng.
    /// Tự đọc slot từ EquipRuntime. Apply stats vào OwnerStats.
    /// Trả về entity cũ nếu slot đã có (swap).
    /// </summary>
    public EntityRuntime Equip(EntityRuntime entity)
    {
        if (entity == null) return null;

        var equipInfo = entity.GetModule<EquipRuntime>();
        if (equipInfo == null)
        {
            Debug.LogWarning($"[EquipmentRuntime] '{entity.entityData.keyName}' không có EquipModule.");
            return null;
        }

        var slot = equipInfo.GetEquipSlot();
        if (slot == EquipSlot.None) return null;

        // Remove stats của item cũ nếu có
        EntityRuntime previous = null;
        if (_slots.TryGetValue(slot, out var existing))
        {
            RemoveStats(existing);
            previous = existing;
        }

        _slots[slot] = entity;
        ApplyStats(entity);
        return previous;
    }

    // ══════ Unequip ══════

    /// <summary>Tháo item khỏi slot. Remove stats. Trả về entity.</summary>
    public EntityRuntime Unequip(EquipSlot slot)
    {
        if (!_slots.TryGetValue(slot, out var entity)) return null;
        RemoveStats(entity);
        _slots.Remove(slot);
        return entity;
    }

    /// <summary>Clear Hand slot (khi đổi hotbar sang item không phải Hand).</summary>
    public void ClearHand()
    {
        if (!_slots.TryGetValue(EquipSlot.Hand, out var entity)) return;
        RemoveStats(entity);
        _slots.Remove(EquipSlot.Hand);
    }

    // ══════ Query ══════

    public EntityRuntime Get(EquipSlot slot)
    {
        _slots.TryGetValue(slot, out var entity);
        return entity;
    }

    public bool HasItem(EquipSlot slot) => _slots.ContainsKey(slot);

    public IEnumerable<KeyValuePair<EquipSlot, EntityRuntime>> GetAll() => _slots;

    // ══════ Stats ══════

    private void ApplyStats(EntityRuntime entity)
    {
        if (OwnerStats == null || entity?.stats == null) return;
        foreach (var entry in entity.entityData.baseStats.baseStats)
            OwnerStats.AddFlat(entry.statType, entry.value);
    }

    private void RemoveStats(EntityRuntime entity)
    {
        if (OwnerStats == null || entity?.stats == null) return;
        foreach (var entry in entity.entityData.baseStats.baseStats)
            OwnerStats.AddFlat(entry.statType, -entry.value);
    }

    // ══════ Save / Load ══════

    public ModuleSaveData ToSaveData()
    {
        var entries = new List<EquipSlotSave>();
        foreach (var kv in _slots)
        {
            // Hand không save — sẽ restore từ hotbar selection
            if (kv.Key == EquipSlot.Hand) continue;
            entries.Add(new EquipSlotSave { slot = (int)kv.Key, entityId = kv.Value.Id });
        }

        var data = new EquipmentSaveData { entries = entries.ToArray() };
        return new ModuleSaveData
        {
            moduleType = "Equipment",
            dataJson = JsonUtility.ToJson(data)
        };
    }

    public void ApplySaveData(ModuleSaveData save)
    {
        if (save == null || string.IsNullOrEmpty(save.dataJson)) return;
        var data = JsonUtility.FromJson<EquipmentSaveData>(save.dataJson);
        if (data?.entries == null) return;
        _pendingSlots = data.entries;
    }

    public void RestoreSlots(EntityRegistry registry)
    {
        if (_pendingSlots == null) return;
        foreach (var s in _pendingSlots)
        {
            var entity = registry.Get(s.entityId);
            if (entity == null)
            {
                Debug.LogWarning($"[EquipmentRuntime] Không tìm thấy entity id={s.entityId}");
                continue;
            }
            _slots[(EquipSlot)s.slot] = entity;
            ApplyStats(entity);
        }
        _pendingSlots = null;
    }

    private EquipSlotSave[] _pendingSlots;

    public bool Equals(IModuleRuntime other) => other is EquipmentRuntime;

    [Serializable] private class EquipmentSaveData { public EquipSlotSave[] entries; }
    [Serializable] private class EquipSlotSave { public int slot; public string entityId; }
}
