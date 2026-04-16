using UnityEngine;

public class EquipRuntime : IModuleRuntime, IHandleEvent<OnEquipEvent>
{
    private EquipModule data;

    public EquipRuntime(EquipModule data)
    {
        this.data = data;
    }

    public EquipSlot GetEquipSlot() => data.equipSlot;

    public void Handle(OnEquipEvent e)
    {
        Debug.Log($"[EquipRuntime] Equipped to slot: {data.equipSlot}");
    }

    public ModuleSaveData ToSaveData()
    {
        return new ModuleSaveData { moduleType = "Equip", dataJson = string.Empty };
    }

    public void ApplySaveData(ModuleSaveData save) { }

    public bool Equals(IModuleRuntime other)
    {
        if (other is not EquipRuntime o) return false;
        return data.equipSlot == o.data.equipSlot;
    }
}
