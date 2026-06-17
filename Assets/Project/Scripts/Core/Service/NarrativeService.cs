using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Service quản lý story events unlock theo ngày.
/// Subscribe DayChangedPublish → unlock events → publish StoryEventUnlockedPublish.
/// </summary>
public class NarrativeService
{
    private readonly EventBus _eventBus;
    private readonly Dictionary<string, StoryEventData> _events = new Dictionary<string, StoryEventData>();
    private readonly HashSet<string> _triggered = new HashSet<string>();

    public NarrativeService(EventBus eventBus, IEnumerable<StoryEventData> events = null)
    {
        _eventBus = eventBus;
        if (events != null)
            Register(events);
        _eventBus.Subscribe<DayChangedPublish>(OnDayChanged);
    }

    public void Register(IEnumerable<StoryEventData> events)
    {
        foreach (var e in events)
        {
            if (e == null || string.IsNullOrEmpty(e.id)) continue;
            _events[e.id] = e;
        }
    }

    public void SetEvents(IEnumerable<StoryEventData> events)
    {
        _events.Clear();
        Register(events);
    }

    public bool IsUnlocked(string id) => _triggered.Contains(id);

    public IEnumerable<StoryEventData> GetUnlockedEvents()
    {
        return _triggered.Select(id => _events.TryGetValue(id, out var e) ? e : null).Where(e => e != null);
    }

    public List<string> ExportTriggered() => _triggered.ToList();

    public void ImportTriggered(IEnumerable<string> ids)
    {
        _triggered.Clear();
        if (ids != null)
            foreach (var id in ids)
                _triggered.Add(id);
    }

    public void Shutdown()
    {
        _eventBus.Unsubscribe<DayChangedPublish>(OnDayChanged);
    }

    private void OnDayChanged(DayChangedPublish e)
    {
        int totalDay = CalculateTotalDay(e.year, e.season, e.day);
        foreach (var evt in _events.Values)
        {
            if (evt == null || _triggered.Contains(evt.id)) continue;
            if (evt.triggerDay <= totalDay)
            {
                _triggered.Add(evt.id);
                _eventBus.Publish(new StoryEventUnlockedPublish(evt));
            }
        }
    }

    private int CalculateTotalDay(int year, Season season, int day)
    {
        int daysPerSeason = 28;
        int seasonIndex = (int)season;
        return (year - 1) * 4 * daysPerSeason + seasonIndex * daysPerSeason + day;
    }
}
