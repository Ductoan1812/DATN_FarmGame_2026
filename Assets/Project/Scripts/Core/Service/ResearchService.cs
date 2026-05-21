using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ResearchService
{
    private readonly Dictionary<string, ResearchData> _registry = new();
    private readonly HashSet<string> _unlocked = new();
    private readonly EventBus _eventBus;
    private readonly TimeManager _timeManager;

    public ResearchService(EventBus eventBus, TimeManager timeManager, IEnumerable<ResearchData> research)
    {
        _eventBus = eventBus;
        _timeManager = timeManager;
        foreach (var r in research ?? Enumerable.Empty<ResearchData>())
        {
            if (r != null && !string.IsNullOrEmpty(r.id))
                _registry[r.id] = r;
        }

        _eventBus?.Subscribe<DayChangedPublish>(OnDayChanged);
        CheckUnlocks();
    }

    private void OnDayChanged(DayChangedPublish evt)
    {
        CheckUnlocks();
    }

    private void CheckUnlocks()
    {
        int totalDays = CalculateTotalDays();
        foreach (var kvp in _registry)
        {
            if (!_unlocked.Contains(kvp.Key) && totalDays >= kvp.Value.unlockDay)
            {
                _unlocked.Add(kvp.Key);
                _eventBus?.Publish(new ResearchUnlockedPublish(kvp.Value));
            }
        }
    }

    private int CalculateTotalDays()
    {
        if (_timeManager == null)
            return 1;

        int daysPerSeason = _timeManager.DaysPerSeason;
        int completedYears = Mathf.Max(0, _timeManager.Year - 1);
        int completedSeasons = (int)_timeManager.Season;
        return completedYears * 4 * daysPerSeason + completedSeasons * daysPerSeason + _timeManager.Day;
    }

    public bool IsUnlocked(string id) => _unlocked.Contains(id);

    public List<ResearchData> GetUnlockedResearch()
    {
        return _unlocked.Select(id => _registry.ContainsKey(id) ? _registry[id] : null)
            .Where(r => r != null).ToList();
    }

    public List<string> ExportUnlocked() => _unlocked.ToList();

    public void ImportUnlocked(List<string> ids)
    {
        _unlocked.Clear();
        if (ids != null)
        {
            foreach (var id in ids)
            {
                _unlocked.Add(id);
            }
        }

        CheckUnlocks();
    }

    public void Shutdown()
    {
        _eventBus?.Unsubscribe<DayChangedPublish>(OnDayChanged);
    }
}
