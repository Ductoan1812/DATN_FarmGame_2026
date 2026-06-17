using UnityEngine;

public class ExpPopupSpawner : MonoBehaviour
{
    [SerializeField] private Color expColor = new(0.35f, 0.8f, 1f);
    [SerializeField] private float fontSize = 2.8f;
    [SerializeField] private Vector3 worldOffset = new(0f, 1.35f, 0f);

    private EventBus subscribedBus;
    private FloatingTextPool pool;

    private void Awake()
    {
        pool = GetComponent<FloatingTextPool>() ?? gameObject.AddComponent<FloatingTextPool>();
    }

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
            subscribedBus.Unsubscribe<ProgressionChangedPublish>(OnProgressionChanged);
            subscribedBus = null;
        }
    }

    private void Subscribe()
    {
        if (subscribedBus != null) return;
        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;

        bus.Subscribe<ProgressionChangedPublish>(OnProgressionChanged);
        subscribedBus = bus;
    }

    private void OnProgressionChanged(ProgressionChangedPublish evt)
    {
        if (evt.amount <= 0)
            return;

        var targetGo = evt.target?.Owner?.GameObject;
        if (targetGo == null || targetGo.GetComponent<PlayerControler>() == null)
            return;

        pool ??= GetComponent<FloatingTextPool>() ?? gameObject.AddComponent<FloatingTextPool>();
        pool.Spawn(targetGo.transform.position + worldOffset, $"+{evt.amount} EXP", expColor, fontSize, false);
    }
}
