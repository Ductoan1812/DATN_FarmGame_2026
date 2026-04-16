/// <summary>
/// Gắn vào EntityData của Player/NPC.
/// Tạo EquipmentRuntime — container chứa item đang trang bị.
/// </summary>
public class EquipmentModule : IModuleData
{
    public override IModuleRuntime CreateRuntime()
    {
        return new EquipmentRuntime();
    }
}
