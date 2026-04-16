using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class EntityRuntime
{
    public string Id { get; private set; }
    public EntityData entityData { get; private set; }
    public IEntityContainer Owner { get; set; }
    public StatsRuntime stats = new();
    private List<IModuleRuntime> modules = new();

    // ══════ Số lượng ══════
    public int Amount { get; private set; } = 1;
    public int MaxStack => entityData != null ? entityData.maxStack : 1;
    public int FreeSpace => MaxStack - Amount;
    public bool IsEmpty => Amount <= 0;
    public bool IsFull => Amount >= MaxStack;

    public EntityRuntime(EntityData data, int amount = 1)
    {
        Id = Guid.NewGuid().ToString("N").Substring(0, 8);
        entityData = data;
        Amount = Mathf.Clamp(amount, 0, data != null ? data.maxStack : 1);
        Initialize();
    }

    public void AddAmount(int delta)
    {
        Amount = Mathf.Clamp(Amount + delta, 0, MaxStack);
    }

    public int MergeFrom(EntityRuntime other)
    {
        if (other == null || !CanStackWith(other)) return 0;
        int take = Mathf.Min(other.Amount, FreeSpace);
        if (take <= 0) return 0;
        Amount += take;
        other.Amount -= take;
        return take;
    }

    // ══════ Save / Load ══════

    public EntitySaveData ToSaveData()
    {
        var save = new EntitySaveData();
        save.id = Id;
        save.entityDataId = entityData?.id;
        save.amount = Amount;
        save.stats = stats?.ToSaveData();

        var list = new List<ModuleSaveData>();
        foreach (var m in modules)
            list.Add(m?.ToSaveData() ?? new ModuleSaveData());
        save.modules = list.ToArray();
        return save;
    }

    public static EntityRuntime LoadFromSave(EntitySaveData save, Func<string, EntityData> resolver)
    {
        if (save == null) return null;
        if (resolver == null) throw new ArgumentNullException(nameof(resolver));

        var entityData = resolver(save.entityDataId);
        if (entityData == null) throw new ArgumentException($"EntityData not found for id={save.entityDataId}");

        var runtime = new EntityRuntime(entityData, save.amount);
        runtime.Id = save.id ?? Guid.NewGuid().ToString("N").Substring(0, 8);

        if (save.stats != null)
            runtime.stats = StatsRuntime.FromSaveData(save.stats);

        if (save.modules != null)
        {
            foreach (var mSave in save.modules)
            {
                if (mSave == null) continue;
                var targetRuntimeName = (mSave.moduleType ?? string.Empty) + "Runtime";

                // Module tự match nếu có MatchesSave, fallback về class name
                var runtimeModule = runtime.modules.Find(m => m.MatchesSave(mSave))
                                 ?? runtime.modules.Find(m => m.GetType().Name == targetRuntimeName);

                if (runtimeModule != null)
                    runtimeModule.ApplySaveData(mSave);
                else
                    Debug.LogWarning($"No runtime module found for saved moduleType={mSave.moduleType}");
            }
        }
        return runtime;
    }

    // ══════ Khởi tạo ══════

    private void Initialize()
    {
        if (entityData != null && entityData.baseStats != null)
            stats.Init(entityData.baseStats);

        modules.Clear();
        if (entityData?.modules != null)
        {
            foreach (var m in entityData.modules)
                if (m != null) modules.Add(m.CreateRuntime());
        }
    }

    // ══════ Module ══════

    public T GetModule<T>() where T : class, IModuleRuntime
    {
        if (modules == null) return null;
        foreach (var m in modules)
            if (m is T t) return t;
        return null;
    }

    /// <summary>Lấy tất cả module cùng type (ví dụ: nhiều InventoryRuntime).</summary>
    public List<T> GetModules<T>() where T : class, IModuleRuntime
    {
        var result = new List<T>();
        if (modules == null) return result;
        foreach (var m in modules)
            if (m is T t) result.Add(t);
        return result;
    }

    // ══════ Stack ══════

    public bool CanStackWith(EntityRuntime other)
    {
        if (other == null) return false;
        if (entityData.maxStack <= 1) return false;
        if (entityData.id != other.entityData.id) return false;
        if (!stats.Equals(other.stats)) return false;
        if (modules.Count != other.modules.Count) return false;
        foreach (var myModule in modules)
        {
            var myType = myModule.GetType();
            var otherModule = other.modules.Find(m => m.GetType() == myType);
            if (otherModule == null) return false;
            if (!myModule.Equals(otherModule)) return false;
        }
        return true;
    }

    // ══════ Event ══════

    public void TriggerEvent<T>(T e) where T : IGameEvent
    {
        foreach (var m in modules)
            if (m is IHandleEvent<T> handler)
                handler.Handle(e);
    }
}
