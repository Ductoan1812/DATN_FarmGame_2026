using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

[DisallowMultipleComponent]
public class StageObject : MonoBehaviour
{
    [SerializeField] private EntityRuntime entity;
    private EventBus eventBus;
    private EntityRoot _root;
    private bool subscribed;

    private void Awake()
    {
        _root = GetComponent<EntityRoot>();
        if (entity == null)
            Debug.LogWarning($"[StageObject]{gameObject.name} can not get EntityRuntime");
    }

    private void OnEnable()
    {
        if( _root != null) _root.OnEntityReady += SetEntityRoot;
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
    public void SetEntityRoot(EntityRuntime entityRuntime)
    {
        entity = entityRuntime;
    }
}
