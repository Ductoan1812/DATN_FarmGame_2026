using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Query helper của 1 túi đồ.
/// KHÔNG tác động entity — chỉ cung cấp thông tin slot và set/clear slot ref.
/// Mọi mutation entity đi qua EntityService (via InventoryManager).
/// </summary>
public class InventoryRuntime : IModuleRuntime
{
    public InventoryType Type     { get; private set; }
    public int           MaxSlots { get; private set; }

    // Container sở hữu túi này (dùng để set Owner của entity)
    public IEntityContainer Container { get; set; }

    // ══════ Selection (chỉ có ý nghĩa với Hotbar) ══════

    /// <summary>Index slot đang được chọn. Mặc định 0.</summary>
    public int SelectedIndex { get; private set; }

    /// <summary>Entity tại slot đang chọn (null nếu slot trống).</summary>
    public EntityRuntime SelectedEntity => GetSlot(SelectedIndex)?.entity;

    /// <summary>Fired khi SelectedIndex thay đổi.</summary>
    public event Action<int> OnSelectionChanged;

    /// <summary>Chọn slot theo index. Clamp trong [0, MaxSlots).</summary>
    public void SelectSlot(int index)
    {
        if (index < 0 || index >= MaxSlots) return;
        SelectedIndex = index;
        OnSelectionChanged?.Invoke(index);
    }

    /// <summary>Cycle selection theo delta (-1 / +1).</summary>
    public void CycleSelection(int delta)
    {
        int next = (SelectedIndex + delta + MaxSlots) % MaxSlots;
        SelectSlot(next);
    }

    private List<InventorySlot> slots;

    public InventoryRuntime(InventoryModule data)
    {
        Type     = data.inventoryType;
        MaxSlots = data.size;
        slots    = new List<InventorySlot>(data.size);
        for (int i = 0; i < data.size; i++)
            slots.Add(new InventorySlot());
    }

    // ══════ Slot access (raw — chỉ InventoryManager gọi) ══════

    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return null;
        return slots[index];
    }

    /// <summary>Đặt entity vào slot và cập nhật Owner.</summary>
    public void SetSlot(int index, EntityRuntime entity)
    {
        if (index < 0 || index >= slots.Count) return;
        slots[index].entity = entity;
        if (entity != null) entity.Owner = Container;
    }

    /// <summary>Xóa slot và clear Owner của entity.</summary>
    public void ClearSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return;
        var entity = slots[index].entity;
        if (entity != null) entity.Owner = null;
        slots[index].Clear();
    }

    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= slots.Count) return;
        if (indexB < 0 || indexB >= slots.Count) return;
        var temp = slots[indexA].entity;
        slots[indexA].entity = slots[indexB].entity;
        slots[indexB].entity = temp;
        // Cập nhật Owner sau swap
        if (slots[indexA].entity != null) slots[indexA].entity.Owner = Container;
        if (slots[indexB].entity != null) slots[indexB].entity.Owner = Container;
    }

    // ══════ Query ══════

    /// <summary>Trả về số lượng entity có thể nhận vào túi này.</summary>
    public int CanReceive(EntityRuntime entity)
    {
        if (entity == null || entity.IsEmpty) return 0;
        int canReceive = 0;

        // Cộng free space từ các stack cùng loại
        for (int i = 0; i < slots.Count; i++)
            if (!slots[i].IsEmpty && EntityService.CanStack(slots[i].entity, entity))
                canReceive += slots[i].entity.FreeSpace;

        // Cộng slot trống
        for (int i = 0; i < slots.Count; i++)
            if (slots[i].IsEmpty)
                canReceive += entity.MaxStack;

        return Mathf.Min(canReceive, entity.Amount);
    }

    public bool Contains(EntityRuntime entity)
    {
        if (entity == null) return false;
        foreach (var slot in slots)
            if (!slot.IsEmpty && slot.entity == entity) return true;
        return false;
    }

    public int FindSlotOf(EntityRuntime entity)
    {
        for (int i = 0; i < slots.Count; i++)
            if (!slots[i].IsEmpty && slots[i].entity == entity) return i;
        return -1;
    }

    public int FindStackableSlot(EntityRuntime entity)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsEmpty) continue;
            if (EntityService.CanStack(slots[i].entity, entity) && !slots[i].entity.IsFull)
                return i;
        }
        return -1;
    }

    public int FindEmptySlot()
    {
        for (int i = 0; i < slots.Count; i++)
            if (slots[i].IsEmpty) return i;
        return -1;
    }

    public int FindSlotByEntityId(string entityDataId)
    {
        for (int i = 0; i < slots.Count; i++)
            if (!slots[i].IsEmpty && slots[i].entity.entityData.id == entityDataId)
                return i;
        return -1;
    }

    public int CountEntity(string entityDataId)
    {
        int total = 0;
        foreach (var slot in slots)
            if (!slot.IsEmpty && slot.entity.entityData.id == entityDataId)
                total += slot.entity.Amount;
        return total;
    }

    public IEnumerable<EntityRuntime> GetAll()
    {
        foreach (var slot in slots)
            if (!slot.IsEmpty) yield return slot.entity;
    }

    // ══════ Save / Load ══════

    public ModuleSaveData ToSaveData()
    {
        var slotSaves = new List<SlotSave>();
        for (int i = 0; i < slots.Count; i++)
            if (!slots[i].IsEmpty)
                slotSaves.Add(new SlotSave { index = i, entityId = slots[i].entity.id });

        var data = new InventorySaveData { type = Type, slots = slotSaves.ToArray(), selectedIndex = SelectedIndex };
        return new ModuleSaveData { moduleType = "Inventory", dataJson = JsonUtility.ToJson(data) };
    }

    public void ApplySaveData(ModuleSaveData save)
    {
        if (save == null || string.IsNullOrEmpty(save.dataJson)) return;
        var data = JsonUtility.FromJson<InventorySaveData>(save.dataJson);
        if (data == null) return;
        _pendingSlots = data.slots;
        SelectedIndex = Mathf.Clamp(data.selectedIndex, 0, MaxSlots - 1);
    }

    public void RestoreSlots(EntityRegistry registry)
    {
        if (_pendingSlots == null) return;
        foreach (var s in _pendingSlots)
        {
            var entity = registry.Get(s.entityId);
            if (entity == null) { Debug.LogWarning($"[InventoryRuntime] Không tìm thấy entity id={s.entityId}"); continue; }
            SetSlot(s.index, entity);
        }
        _pendingSlots = null;
    }

    private SlotSave[] _pendingSlots;

    [Serializable] private class InventorySaveData { public InventoryType type; public SlotSave[] slots; public int selectedIndex; }
    [Serializable] private class SlotSave { public int index; public string entityId; }

    public bool Equals(IModuleRuntime other)
    {
        if (other is not InventoryRuntime o) return false;
        return Type == o.Type && MaxSlots == o.MaxSlots;
    }

    public bool MatchesSave(ModuleSaveData save)
    {
        if (save?.moduleType != "Inventory" || string.IsNullOrEmpty(save.dataJson)) return false;
        var hint = JsonUtility.FromJson<InventoryTypeHint>(save.dataJson);
        return (int)Type == hint.type;
    }

    [System.Serializable]
    private class InventoryTypeHint
    {
        // Giá trị này được JsonUtility điền từ save JSON.
        public int type = -1;
    }
}
