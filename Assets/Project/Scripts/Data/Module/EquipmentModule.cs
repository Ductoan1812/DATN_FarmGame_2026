using System.Collections.Generic;
using Assets.HeroEditor4D.Common.Scripts.Enums;

/// <summary>
/// Gắn vào EntityData của Player/NPC.
/// Khai báo danh sách EquipmentPart mà entity này hỗ trợ.
/// Tạo EquipmentRuntime — container chứa item đang trang bị.
/// </summary>
[System.Serializable]
public class EquipmentModule : IModuleData
{
    /// <summary>
    /// Danh sách slot trang bị mà entity hỗ trợ.
    /// VD: Player có Helmet, Armor, Vest, Leggings, MeleeWeapon1H...
    /// </summary>
    public List<EquipmentPart> supportedParts = new();

    public override IModuleRuntime CreateRuntime()
    {
        return new EquipmentRuntime(this);
    }
}
