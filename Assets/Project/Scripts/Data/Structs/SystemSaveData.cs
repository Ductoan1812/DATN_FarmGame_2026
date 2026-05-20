/// <summary>
/// Save data cho các system-level state (không phải entity).
/// Mở rộng thêm field khi cần (weather, quest progress, NPC friendship...).
/// </summary>
[System.Serializable]
public class SystemSaveData
{
    public TimeState time;
    public System.Collections.Generic.List<WateredCellDto> wateredCells = new System.Collections.Generic.List<WateredCellDto>();
    public WeatherType currentWeather = WeatherType.Sunny;
}

[System.Serializable]
public struct WateredCellDto
{
    public int x;
    public int y;
}
