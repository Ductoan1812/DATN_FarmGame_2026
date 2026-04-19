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
    private bool subscribed;

    private void Awake()
    {
        var root = GetComponent<EntityRoot>();
    }

    private void OnDisable()
    {
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
}
