using UnityEngine;
namespace Assets.HeroEditor4D.Common.Scripts.Enums
{
    public enum EquipmentPart
    {
        [InspectorName("Áo toàn thân")]
        Armor,
        [InspectorName("Mũ")]
        Helmet,
        [InspectorName("Áo")]
        Vest,
        [InspectorName("giáp Tay")]
        Bracers,
        [InspectorName("Quần")]
        Leggings,
        [InspectorName("Vũ Khí Đơn")]
        MeleeWeapon1H,
        [InspectorName("Vũ Khí 2 tay")]
        MeleeWeapon2H,
        [InspectorName("Cung")]
        Bow,
        [InspectorName("Súng")]
        Crossbow,
        [InspectorName("Vũ Khí Phụ 1 tay")]
        SecondaryMelee1H,
        [InspectorName("Súng Phụ 1 tay")]
        SecondaryFirearm1H,
        [InspectorName("Khiên")]
        Shield,
        [InspectorName("Bông Tai")]
        Earrings,
        [InspectorName("Áo Choàng")]
        Cape,
        [InspectorName("Dây Chuyền")]
        Quiver,
        [InspectorName("Ba Lô")]
        Back,
        [InspectorName("Mặt Nạ")]
        Mask,
        [InspectorName("Vũ Khí Phụ 2 tay")]
        Firearm1H,
        [InspectorName("Súng Phụ 2 tay")]
        Firearm2H,
        [InspectorName("Cánh")]
        Wings
    }
}