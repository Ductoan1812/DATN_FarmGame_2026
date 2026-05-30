using UnityEngine;

/// <summary>
/// Save data cho các system-level state (không phải entity).
/// Mở rộng thêm field khi cần (weather, quest progress, NPC friendship...).
/// </summary>
[System.Serializable]
public class SystemSaveData
{
    public TimeState time;
    public System.Collections.Generic.List<WateredCellDto> wateredCells = new System.Collections.Generic.List<WateredCellDto>();
    public System.Collections.Generic.List<SoilCellDto> soilCells = new System.Collections.Generic.List<SoilCellDto>();
    public System.Collections.Generic.List<ClearZoneDto> clearZones = new System.Collections.Generic.List<ClearZoneDto>();
    public WeatherType currentWeather = WeatherType.Sunny;
    public System.Collections.Generic.List<string> triggeredStoryEventIds = new System.Collections.Generic.List<string>();
    public System.Collections.Generic.List<string> unlockedResearch = new System.Collections.Generic.List<string>();

    [Header("Saved Player Position")]
    public string lastActiveSceneName;
    public float playerPosX;
    public float playerPosY;
    public bool hasSavedPlayerPosition;
}

[System.Serializable]
public struct WateredCellDto
{
    public int x;
    public int y;
}

[System.Serializable]
public struct SoilCellDto
{
    public int x;
    public int y;
    public int quality;
}

[System.Serializable]
public class ClearZoneDto
{
    public string zoneId;
    public System.Collections.Generic.List<CellDto> cells = new System.Collections.Generic.List<CellDto>();
}

[System.Serializable]
public struct CellDto
{
    public int x;
    public int y;
}
