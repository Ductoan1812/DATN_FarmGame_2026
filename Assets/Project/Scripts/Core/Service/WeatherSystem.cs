using UnityEngine;

/// <summary>
/// Owns current weather state and generates next-day weather.
/// </summary>
public class WeatherSystem
{
    private readonly EventBus _eventBus;
    private WeatherConfig _config;

    public WeatherType CurrentWeather { get; private set; } = WeatherType.Sunny;

    public WeatherSystem(EventBus eventBus, WeatherConfig config)
    {
        _eventBus = eventBus;
        _config = config;
    }

    /// <summary>Force-set weather and publish change event.</summary>
    public void SetWeather(WeatherType weather)
    {
        CurrentWeather = weather;
        _eventBus?.Publish(new WeatherChangedPublish(weather));
        Debug.Log($"[WeatherSystem] Weather set to {weather}.");
    }

    /// <summary>
    /// Roll next-day weather. Called at end of day (before AdvanceDay publishes events).
    /// Does NOT apply rain — rain is applied after reset via ApplyRainForNewDay().
    /// </summary>
    public void RollNextDayWeather()
    {
        float chance = _config != null ? _config.rainChance : 0.3f;
        var next = Random.value < chance ? WeatherType.Rainy : WeatherType.Sunny;
        SetWeather(next);
    }

}
