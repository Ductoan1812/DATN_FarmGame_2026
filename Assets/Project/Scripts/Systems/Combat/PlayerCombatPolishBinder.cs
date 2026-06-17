using UnityEngine;

public class PlayerCombatPolishBinder : MonoBehaviour
{
    private EventBus subscribedBus;

    private void OnEnable()
    {
        Subscribe();
        BindCurrentPlayer();
    }

    private void Start()
    {
        Subscribe();
        BindCurrentPlayer();
    }

    private void OnDisable()
    {
        if (subscribedBus != null)
        {
            subscribedBus.Unsubscribe<PlayerReadyPublish>(OnPlayerReady);
            subscribedBus = null;
        }
    }

    private void Subscribe()
    {
        if (subscribedBus != null) return;
        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;
        bus.Subscribe<PlayerReadyPublish>(OnPlayerReady);
        subscribedBus = bus;
    }

    private void OnPlayerReady(PlayerReadyPublish _)
    {
        BindCurrentPlayer();
    }

    private static void BindCurrentPlayer()
    {
        var player = FindAnyObjectByType<PlayerControler>();
        if (player == null)
            return;

        Ensure<PlayerInvincibilityHandler>(player.gameObject);
        Ensure<WeaponSwingTrail>(player.gameObject);
    }

    private static void Ensure<T>(GameObject go) where T : Component
    {
        if (go.GetComponent<T>() == null)
            go.AddComponent<T>();
    }
}
