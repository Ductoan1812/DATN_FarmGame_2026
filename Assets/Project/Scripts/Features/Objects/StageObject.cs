using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

[DisallowMultipleComponent]
public class StageObject : MonoBehaviour
{
    [SerializeField] private EntityRuntime entity;
    private EventBus eventBus;
    private bool subscribed;

    private void Start()
    {
        entity = GetComponent<EntityRoot>()?.GetEntity();
        if (entity == null)
            Debug.LogWarning($"[StageObject]{gameObject.name} can not get EntityRuntime");
        subscribed = false;
    }

    private void OnEnable()
    {
        entity = GetComponent<EntityRoot>()?.GetEntity();
        eventBus = GameManager.Instance?.EventBus;
        if (eventBus != null && !subscribed)
        {
            eventBus.Subscribe<NextDayEventPublish>(OnGlobalNextDay);
            subscribed = true;
        }
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
            Debug.LogWarning($"[StageObject]{gameObject.name} No EntityRuntime");
            return;
        }
        entity.TriggerEvent<NextDayEvent>(new NextDayEvent());
    }
}
