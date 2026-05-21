using UnityEngine;

/// <summary>
/// Deterministic AI assistant that provides farming tips based on game state.
/// </summary>
public class AIAssistantService
{
    private readonly WateredTileTracker _wateredTileTracker;
    private readonly WeatherSystem _weatherSystem;
    private readonly TimeManager _timeManager;

    public AIAssistantService(
        WateredTileTracker wateredTileTracker,
        WeatherSystem weatherSystem,
        TimeManager timeManager)
    {
        _wateredTileTracker = wateredTileTracker;
        _weatherSystem = weatherSystem;
        _timeManager = timeManager;
    }

    /// <summary>Returns the primary tip key based on current game state.</summary>
    public string GetPrimaryTipKey()
    {
        var currentWeather = _weatherSystem != null ? _weatherSystem.CurrentWeather : WeatherType.Sunny;
        var wateredCount = _wateredTileTracker != null ? _wateredTileTracker.GetWateredCount() : 0;
        var currentDay = _timeManager != null ? _timeManager.Day : 1;

        // Priority order:
        // 1. Sunny + no watered tiles => suggest watering
        if (currentWeather == WeatherType.Sunny && wateredCount == 0)
            return "s5.ai.tip.water_crops";

        // 2. Rainy => inform rain waters crops
        if (currentWeather == WeatherType.Rainy)
            return "s5.ai.tip.rain_waters_crops";

        // 3. Day >= 7 => suggest mining
        if (currentDay >= 7)
            return "s5.ai.tip.visit_mine_for_materials";

        // 4. Fallback
        return "s5.ai.tip.keep_progressing";
    }
}
