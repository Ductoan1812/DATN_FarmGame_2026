using UnityEngine;
public enum StatType
{
    [InspectorName("Máu tối đa")]
    MaxHp,
    [InspectorName("Máu")]
    Hp,
    [InspectorName("Tấn công")]
    Attack,
    [InspectorName("Phòng thủ")]
    Defense,
    [InspectorName("Tốc độ")]
    Speed,
    [InspectorName("Thể lực tối đa")]
    MaxStamina,
    [InspectorName("Tỷ lệ chí mạng")]
    CritChance,
    [InspectorName("Sát thương chí mạng")]
    CritDamage,
    [InspectorName("Chiều rộng vùng chiếm (ô)")]
    AreaX,
    [InspectorName("Chiều cao vùng chiếm (ô)")]
    AreaY
}