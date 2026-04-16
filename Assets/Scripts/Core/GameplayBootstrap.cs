using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class GameplayBootstrap : MonoBehaviour
{
    [Header("Scene Refs")]
    [SerializeField] private EventBus eventBus;
    [SerializeField] private PlayerControler playerControler;

    [Header("Auto Setup")]
    [SerializeField] private bool autoCreateEventBus = true;
    [SerializeField] private bool autoSetupPlayerToolStack = true;

    private void Awake()
    {
        EnsureEventBus();

        if (autoSetupPlayerToolStack)
        {
            EnsurePlayerToolStack();
        }
    }

    private void EnsureEventBus()
    {
        if (eventBus != null)
        {
            return;
        }

        eventBus = FindAnyObjectByType<EventBus>();
        if (eventBus != null || !autoCreateEventBus)
        {
            return;
        }

        GameObject eventBusObject = new GameObject("EventBus");
        eventBus = eventBusObject.AddComponent<EventBus>();
        Debug.Log("[GameplayBootstrap] Da tao EventBus.");
    }

    private void EnsurePlayerToolStack()
    {
        if (playerControler == null)
        {
            playerControler = FindAnyObjectByType<PlayerControler>();
        }

        if (playerControler == null)
        {
            Debug.LogWarning("[GameplayBootstrap] Khong tim thay PlayerControler de setup tool stack.");
            return;
        }

        GameObject playerObject = playerControler.gameObject;



        GameObject handlersRoot = GetOrCreateChild(playerObject.transform, "ToolHandlers");
    

        Debug.Log("[GameplayBootstrap] Player tool stack da san sang.");
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
        {
            component = target.AddComponent<T>();
        }

        return component;
    }

    private static GameObject GetOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            return child.gameObject;
        }

        GameObject childObject = new GameObject(childName);
        childObject.transform.SetParent(parent, false);
        return childObject;
    }
}