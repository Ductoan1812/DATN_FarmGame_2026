using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class EntityRuntime
{
    public string id { get; internal set; }
    public EntityData entityData { get; private set; }
    public IEntityContainer Owner { get; set; }
<<<<<<< HEAD:Assets/Scripts/Data/Entitys/EntityRuntime.cs
  
    public int Amount { get; private set; } = 1;
=======
    public StatsRuntime stats = new();
    internal List<IModuleRuntime> modules = new();

    // ══════ Số lượng ══════
    // NOTE: Setter là `internal` — CHỈ EntityService được phép set (qua SetAmount).
    //       Không set trực tiếp ở bất kỳ nơi nào khác.
    public int Amount { get; internal set; } = 1;
>>>>>>> BranchFixCrash:Assets/Project/Scripts/Data/Entitys/EntityRuntime.cs
    public int MaxStack => entityData != null ? entityData.maxStack : 1;
    public int FreeSpace => MaxStack - Amount;
    public bool IsEmpty => Amount <= 0;
    public bool IsFull => Amount >= MaxStack;
    public StatsRuntime stats = new();
    private List<IModuleRuntime> modules = new();
    public EntityRuntime(EntityData data, int amount = 1)
    {
        id = Guid.NewGuid().ToString("N").Substring(0, 8);
        entityData = data;
        Amount = Mathf.Clamp(amount, 0, data != null ? data.maxStack : 1);
        Initialize();
    }

    // ══════ Save / Load ══════

    public EntitySaveData ToSaveData()
    {
        var save = new EntitySaveData();
        save.id = id;
        save.entityDataId = entityData?.id;
        save.amount = Amount;
        save.stats = stats?.ToSaveData();

        // Module nào trả null từ ToSaveData() = không cần save → skip.
        var list = new List<ModuleSaveData>();
        foreach (var m in modules)
        {
            if (m == null) continue;
            var saveData = m.ToSaveData();
            if (saveData == null) continue;
            list.Add(saveData);
        }
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
        runtime.id = save.id ?? Guid.NewGuid().ToString("N").Substring(0, 8);

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

    // ══════ Event ══════

    public void TriggerEvent<T>(T e) where T : IGameEvent
    {
        foreach (var m in modules)
            if (m is IHandleEvent<T> handler)
                handler.Handle(e);
    }
}
