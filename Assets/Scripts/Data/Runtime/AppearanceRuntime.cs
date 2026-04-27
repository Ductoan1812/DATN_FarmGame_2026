using Assets.HeroEditor4D.Common.Scripts.Enums;

/// <summary>
/// Runtime của AppearanceModule.
/// Chỉ giữ dữ liệu tĩnh từ module config — không có state riêng.
/// EquipmentRuntime đọc thông tin này khi equip item lên Character4D.
/// </summary>
public class AppearanceRuntime : IModuleRuntime
{
    private readonly AppearanceModule _data;

    public string SpriteId => _data.spriteId;
    public EquipmentPart EquipmentPart => _data.equipmentPart;

    public AppearanceRuntime(AppearanceModule data)
    {
        _data = data;
    }

    // Không có state → không cần save
    public ModuleSaveData ToSaveData() => null;
    public void ApplySaveData(ModuleSaveData save) { }

    public bool Equals(IModuleRuntime other)
    {
        if (other is not AppearanceRuntime o) return false;
        return _data.spriteId == o._data.spriteId
            && _data.equipmentPart == o._data.equipmentPart;
    }
}
