using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavAgent2D))]
public class NpcScheduleController : MonoBehaviour
{
    [SerializeField] private DailyScheduleData schedule;
    [SerializeField] private bool autoResolveScheduleFromEntityData = true;
    [SerializeField] private string resourcesScheduleFolder = "Data/Schedules";
    [SerializeField, Min(0f)] private float standStopDistance = 0.05f;

    private NavAgent2D agent;
    private EntityRoot entityRoot;
    private EventBus eventBus;
    private ScheduleEntry activeEntry;
    private Vector2 activeAnchorPosition;
    private float nextWanderTime;
    private bool subscribed;

    private void Awake()
    {
        agent = GetComponent<NavAgent2D>();
        entityRoot = GetComponent<EntityRoot>();
    }

    private void OnEnable()
    {
        if (entityRoot != null)
            entityRoot.OnEntityReady += OnEntityReady;

        TrySubscribe();
    }

    private void Start()
    {
        TryResolveScheduleFromEntityData();
        ApplyCurrentSchedule();
    }

    private void Update()
    {
        if (!subscribed)
            TrySubscribe();

        if (activeEntry == null || activeEntry.action != ScheduleAction.WanderRadius || agent == null || !agent.IsAtDestination)
            return;

        if (Time.time < nextWanderTime)
            return;

        Vector2 target = PickWanderTarget(activeAnchorPosition, activeEntry.wanderRadius);
        agent.SetDestination(target, standStopDistance);
        nextWanderTime = Time.time + Mathf.Max(0.25f, activeEntry.waitSeconds);
    }

    private void OnDisable()
    {
        if (entityRoot != null)
            entityRoot.OnEntityReady -= OnEntityReady;

        if (subscribed && eventBus != null)
            eventBus.Unsubscribe<GameTimeChangedPublish>(OnGameTimeChanged);

        subscribed = false;
    }

    private void OnEntityReady(EntityRuntime _)
    {
        TryResolveScheduleFromEntityData();
        ApplyCurrentSchedule();
    }

    private void TrySubscribe()
    {
        if (subscribed)
            return;

        eventBus = GameManager.Instance?.EventBus;
        if (eventBus == null)
            return;

        eventBus.Subscribe<GameTimeChangedPublish>(OnGameTimeChanged);
        subscribed = true;
    }

    private void OnGameTimeChanged(GameTimeChangedPublish _)
    {
        ApplyCurrentSchedule();
    }

    private void ApplyCurrentSchedule()
    {
        TryResolveScheduleFromEntityData();
        if (schedule == null || agent == null)
            return;

        var time = GameManager.Instance?.TimeManager;
        Season season = time != null ? time.Season : Season.Spring;
        int minuteOfDay = time != null ? time.Hour * 60 + time.Minute : 6 * 60;

        if (!schedule.TryGetEntry(season, minuteOfDay, out var entry) || entry == null)
            return;

        if (activeEntry == entry && agent.HasDestination)
            return;

        if (!SceneAnchor.TryResolve(entry.targetAnchorId, out activeAnchorPosition))
        {
            Debug.LogWarning($"[NpcScheduleController] Anchor '{entry.targetAnchorId}' not found for '{name}'.");
            return;
        }

        activeEntry = entry;
        nextWanderTime = Time.time + Mathf.Max(0.25f, entry.waitSeconds);

        if (entry.action == ScheduleAction.Stand)
        {
            agent.SetDestination(activeAnchorPosition, standStopDistance);
            return;
        }

        if (entry.action == ScheduleAction.WanderRadius)
        {
            agent.SetDestination(PickWanderTarget(activeAnchorPosition, entry.wanderRadius), standStopDistance);
            return;
        }

        agent.SetDestination(activeAnchorPosition, standStopDistance);
    }

    private void TryResolveScheduleFromEntityData()
    {
        if (!autoResolveScheduleFromEntityData || schedule != null)
            return;

        var entity = entityRoot != null ? entityRoot.GetEntity() : null;
        string entityId = entity?.entityData?.id;
        if (string.IsNullOrWhiteSpace(entityId))
            return;

        string path = $"{resourcesScheduleFolder.TrimEnd('/')}/{entityId.Trim()}";
        schedule = Resources.Load<DailyScheduleData>(path);
    }

    private Vector2 PickWanderTarget(Vector2 center, float radius)
    {
        radius = Mathf.Max(0f, radius);
        if (radius <= 0.01f)
            return center;

        for (int i = 0; i < 8; i++)
        {
            Vector2 candidate = center + Random.insideUnitCircle * radius;
            if (agent == null || agent.IsWalkable(candidate))
                return candidate;
        }

        return center;
    }
}
