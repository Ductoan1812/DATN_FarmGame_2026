using UnityEngine;

/// <summary>
/// Bridge giữa global NextDayEventPublish (EventBus) → entity NextDayEvent (TriggerEvent).
/// Subscribe EntityRoot.OnEntityReady để lấy entity ref.
/// </summary>
[DisallowMultipleComponent]
public class StageObject : MonoBehaviour
{
    private EntityRuntime entity;
    private EventBus eventBus;
    private EntityRoot _root;
    private bool subscribed;

    private void Awake()
    {
        _root = GetComponent<EntityRoot>();
    }

    private void OnEnable()
    {
        if (_root != null)
        {
            _root.OnEntityReady += SetEntityRoot;

            if (_root.IsReady)
                SetEntityRoot(_root.GetEntity());
        }

        eventBus = GameManager.Instance?.EventBus;
        if (eventBus != null && !subscribed)
        {
            eventBus.Subscribe<NextDayEventPublish>(OnGlobalNextDay);
            subscribed = true;
        }
    }

    private void OnDisable()
    {
        if (_root != null) _root.OnEntityReady -= SetEntityRoot;

        if (eventBus != null && subscribed)
        {
            eventBus.Unsubscribe<NextDayEventPublish>(OnGlobalNextDay);
            subscribed = false;
        }
    }

    public void OnGlobalNextDay(NextDayEventPublish e)
    {
        if (entity == null)
        {
            Debug.LogWarning($"[StageObject] {gameObject.name} No EntityRuntime");
            return;
        }
        entity.TriggerEvent(new NextDayEvent());
    }

    public void SetEntityRoot(EntityRuntime entityRuntime)
    {
        entity = entityRuntime;
    }
}
