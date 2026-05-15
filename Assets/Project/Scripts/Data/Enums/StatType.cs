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
    [InspectorName("Thể lực")]
    Stamina,
    [InspectorName("Thể lực tối đa")]
    MaxStamina,
    [InspectorName("Tỷ lệ chí mạng")]
    CritChance,
    [InspectorName("Sát thương chí mạng")]
    CritDamage,
    [InspectorName("Chiều rộng vùng chiếm (ô)")]
    AreaX,
    [InspectorName("Chiều cao vùng chiếm (ô)")]
    AreaY,
    [InspectorName("Hồi chiêu")]
    CoolDown,
    [InspectorName("Tầm đánh")]
    Range,
    [InspectorName("Tiền")]
    Money,
    [InspectorName("Năng lượng")]
    Mp,
    [InspectorName("Năng lượng tối đa")]
    MaxMp,
    [InspectorName("Kinh nghiệm")]
    Exp,
    [InspectorName("Kinh nghiệm tối đa")]
    MaxExp,
    [InspectorName("Cấp độ")]
    Level
}
