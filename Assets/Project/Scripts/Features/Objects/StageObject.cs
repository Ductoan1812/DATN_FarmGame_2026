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
<<<<<<< HEAD:Assets/Scripts/Features/Objects/StageObject.cs
        var root = GetComponent<EntityRoot>();
        if (root != null)
            root.OnEntityReady += OnEntityReady;
=======
        _root = GetComponent<EntityRoot>();
>>>>>>> BranchFixCrash:Assets/Project/Scripts/Features/Objects/StageObject.cs
    }

    private void OnEntityReady(EntityRuntime e)
    {
<<<<<<< HEAD:Assets/Scripts/Features/Objects/StageObject.cs
        entity = e;

        // Subscribe NextDay nếu chưa
        if (!subscribed)
=======
        if (_root != null) _root.OnEntityReady += SetEntityRoot;

        eventBus = GameManager.Instance?.EventBus;
        if (eventBus != null && !subscribed)
>>>>>>> BranchFixCrash:Assets/Project/Scripts/Features/Objects/StageObject.cs
        {
            eventBus = GameManager.Instance?.EventBus;
            if (eventBus != null)
            {
                eventBus.Subscribe<NextDayEventPublish>(OnGlobalNextDay);
                subscribed = true;
            }
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
<<<<<<< HEAD:Assets/Scripts/Features/Objects/StageObject.cs
=======
    }

    public void SetEntityRoot(EntityRuntime entityRuntime)
    {
        entity = entityRuntime;
>>>>>>> BranchFixCrash:Assets/Project/Scripts/Features/Objects/StageObject.cs
    }
}
