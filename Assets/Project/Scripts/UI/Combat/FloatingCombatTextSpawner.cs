using UnityEngine;

public class FloatingCombatTextSpawner : MonoBehaviour
{
    [SerializeField] private Color normalDamageColor = new(1f, 0.92f, 0.45f);
    [SerializeField] private Color critDamageColor = new(1f, 0.35f, 0.15f);
    [SerializeField] private float normalFontSize = 3.2f;
    [SerializeField] private float critFontSize = 4.2f;
    [SerializeField] private Vector3 worldOffset = new(0f, 0.85f, 0f);

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
        if (subscribedBus == null) return;
        subscribedBus.Unsubscribe<DamageAppliedPublish>(OnDamageApplied);
        subscribedBus = null;
    }

    private void Subscribe()
    {
        if (subscribedBus != null) return;
        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;

        bus.Subscribe<DamageAppliedPublish>(OnDamageApplied);
        subscribedBus = bus;
    }

    private void OnDamageApplied(DamageAppliedPublish evt)
    {
        if (evt.finalDamage <= 0f)
            return;

        pool ??= GetComponent<FloatingTextPool>() ?? gameObject.AddComponent<FloatingTextPool>();
        string text = Mathf.CeilToInt(evt.finalDamage).ToString();
        pool.Spawn(
            evt.worldPosition + worldOffset,
            text,
            evt.isCrit ? critDamageColor : normalDamageColor,
            evt.isCrit ? critFontSize : normalFontSize,
            evt.isCrit);
    }
}
