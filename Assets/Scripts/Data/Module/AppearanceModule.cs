using Assets.HeroEditor4D.Common.Scripts.Enums;

/// <summary>
/// Gắn vào EntityData của item có hiển thị sprite trên nhân vật (vũ khí, giáp, mũ...).
/// Chứa spriteId (key tra cứu SpriteCollection) + equipmentPart (vị trí gắn trên Character4D).
/// </summary>
[System.Serializable]
public class AppearanceModule : IModuleData
{
    /// <summary>
    /// Id trong SpriteCollection của HeroEditor4D.
    /// VD: "FantasyHeroes.Basic.Armor.Platemail"
    /// </summary>
    public string spriteId;

    /// <summary>
    /// Vị trí gắn sprite trên Character4D.
    /// VD: EquipmentPart.Armor, EquipmentPart.MeleeWeapon1H...
    /// </summary>
    public EquipmentPart equipmentPart;

    public override IModuleRuntime CreateRuntime()
    {
        return new AppearanceRuntime(this);
    }
}
