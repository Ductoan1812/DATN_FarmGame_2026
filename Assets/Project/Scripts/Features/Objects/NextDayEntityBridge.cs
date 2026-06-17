using UnityEngine;

/// <summary>
/// Bridge global NextDayEventPublish sang NextDayEvent trên entity runtime.
/// Dùng cho animal/resource object cần cập nhật theo ngày.
/// </summary>
[DisallowMultipleComponent]
public class NextDayEntityBridge : MonoBehaviour
{
    private EntityRoot root;
    private EntityRuntime entity;
    private EventBus eventBus;
    private bool subscribed;

    private void Awake()
    {
        root = GetComponent<EntityRoot>();
    }

    private void OnEnable()
    {
        if (root != null)
        {
            root.OnEntityReady += SetEntity;
            if (root.IsReady)
                SetEntity(root.GetEntity());
        }

        eventBus = GameManager.Instance?.EventBus;
        if (eventBus != null && !subscribed)
        {
            eventBus.Subscribe<NextDayEventPublish>(OnNextDay);
            subscribed = true;
        }
    }

    private void OnDisable()
    {
        if (root != null)
            root.OnEntityReady -= SetEntity;

        if (eventBus != null && subscribed)
        {
            eventBus.Unsubscribe<NextDayEventPublish>(OnNextDay);
            subscribed = false;
        }
    }

    private void OnNextDay(NextDayEventPublish e)
    {
        entity?.TriggerEvent(new NextDayEvent());
    }

    private void SetEntity(EntityRuntime value)
    {
        entity = value;
    }
}
