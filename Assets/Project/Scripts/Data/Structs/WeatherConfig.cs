using UnityEngine;

/// <summary>
/// Config cho hệ thống thời tiết. Gán vào GameManager hoặc Resources.
/// rainChance: xác suất mưa mỗi ngày (0..1).
/// </summary>
[CreateAssetMenu(menuName = "FarmGame/Config/WeatherConfig", fileName = "WeatherConfig")]
public class WeatherConfig : ScriptableObject
{
    [Range(0f, 1f)]
    public float rainChance = 0.3f;
}
