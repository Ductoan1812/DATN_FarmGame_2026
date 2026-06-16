using UnityEngine;

public class AutoSaveOnDayChanged : MonoBehaviour
{
    [SerializeField] private bool showToast = true;

    private EventBus subscribedBus;

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        if (subscribedBus != null)
        {
            subscribedBus.Unsubscribe<DayChangedPublish>(OnDayChanged);
            subscribedBus = null;
        }
    }

    private void Subscribe()
    {
        if (subscribedBus != null) return;
        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;
        bus.Subscribe<DayChangedPublish>(OnDayChanged);
        subscribedBus = bus;
    }

    private void OnDayChanged(DayChangedPublish _)
    {
        subscribedBus?.Publish(new SaveGameRequestPublish());
        if (showToast)
            subscribedBus?.Publish(new ToastPublish("Game saved.", 1.5f));
    }
}
