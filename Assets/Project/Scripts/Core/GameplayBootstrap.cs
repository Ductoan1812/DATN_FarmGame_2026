using UnityEngine;

public class GameplayBootstrap : MonoBehaviour
{
    [Header("Scene Refs")]
    [SerializeField] private EventBus eventBus;

    [Header("Auto Setup")]
    [SerializeField] private bool autoCreateEventBus = true;
    [SerializeField] private bool autoSetupPlayerToolStack = true;

    private void Awake()
    {
        EnsureEventBus();

        // Subscribe ngay sau khi có eventBus. GameReadyPublish là boot event hiện tại.
        if (eventBus != null)
            eventBus.Subscribe<GameReadyPublish>(OnGameReady);
    }

    private void OnDestroy()
    {
        if (eventBus != null)
            eventBus.Unsubscribe<GameReadyPublish>(OnGameReady);
    }

    private void OnGameReady(GameReadyPublish _)
    {
        if (autoSetupPlayerToolStack)
            EnsurePlayerToolStack();
    }

    private void EnsureEventBus()
    {
        if (eventBus != null) return;

        eventBus = FindAnyObjectByType<EventBus>();
        if (eventBus != null || !autoCreateEventBus) return;

        GameObject eventBusObject = new GameObject("EventBus");
        eventBus = eventBusObject.AddComponent<EventBus>();
        Debug.Log("[GameplayBootstrap] Da tao EventBus.");
    }

    private void EnsurePlayerToolStack()
    {
        var playerControler = FindAnyObjectByType<PlayerControler>();
        if (playerControler == null)
        {
            Debug.LogWarning("[GameplayBootstrap] Khong tim thay PlayerControler de setup tool stack.");
            return;
        }

        GameObject playerObject = playerControler.gameObject;
        GameObject handlersRoot = GetOrCreateChild(playerObject.transform, "ToolHandlers");

        Debug.Log("[GameplayBootstrap] Player tool stack da san sang.");
    }

    private static GameObject GetOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null) return child.gameObject;

        GameObject childObject = new GameObject(childName);
        childObject.transform.SetParent(parent, false);
        return childObject;
    }
}
