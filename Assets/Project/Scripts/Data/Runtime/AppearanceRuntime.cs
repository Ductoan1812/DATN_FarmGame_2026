using Assets.HeroEditor4D.Common.Scripts.Enums;
using UnityEngine;

/// <summary>
/// Runtime của AppearanceModule — gắn trên EntityData của gear (giáp, mũ, quần...).
/// 
/// Khi entity nhận PrimaryActionEvent (từ btn_use):
///   → Chỉ forward sang EquipmentRuntime.Equip()
///   → EquipmentRuntime tự xử lý toàn bộ: check, remove khỏi inventory, swap, stats, visual
/// </summary>
public class AppearanceRuntime : IModuleRuntime, IHandleEvent<PrimaryActionEvent>
{
    private readonly AppearanceModule _data;

    public string SpriteId => _data.spriteId;
    public EquipmentPart EquipmentPart => _data.equipmentPart;

    public AppearanceRuntime(AppearanceModule data)
    {
        _data = data;
    }

    public void Handle(PrimaryActionEvent e)
    {
        if (e.actor == null || e.item == null) return;

        var equipment = e.actor.GetModule<EquipmentRuntime>();
        if (equipment == null) return;

        // Hand item (vd: cuốc, rìu...) đã được EquipHand gắn từ hotbar selection.
        // Nếu equip lại ở đây, EquipmentRuntime.Equip() sẽ coi nó là "item cũ" và
        // Pickup lại chính nó mỗi lần dùng tool → spam thông báo nhặt giả.
        if (equipment.IsEquipped(e.item)) return;

        equipment.Equip(e.item);
    }

    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }

    public bool Equals(IModuleRuntime other)
    {
        if (other is not AppearanceRuntime o) return false;
        return _data.spriteId == o._data.spriteId
            && _data.equipmentPart == o._data.equipmentPart;
    }
}
