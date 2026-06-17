using System.Collections;
using UnityEngine;

public class HitStopManager : MonoBehaviour
{
    [SerializeField] private float hitStopScale = 0.08f;
    [SerializeField] private float normalDuration = 0.045f;
    [SerializeField] private float critDuration = 0.075f;

    private EventBus subscribedBus;
    private Coroutine routine;
    private float baseFixedDeltaTime;

    private void Awake()
    {
        baseFixedDeltaTime = Time.fixedDeltaTime;
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
            subscribedBus.Unsubscribe<DamageAppliedPublish>(OnDamageApplied);
            subscribedBus = null;
        }

        RestoreTimeScale();
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
        var attackerGo = evt.attacker?.Owner?.GameObject;
        var targetGo = evt.target?.Owner?.GameObject;
        if (attackerGo == null || targetGo == null)
            return;

        if (attackerGo.GetComponent<PlayerControler>() == null || targetGo.GetComponent<EnemyObject>() == null)
            return;

        Trigger(evt.isCrit ? critDuration : normalDuration);
    }

    public void Trigger(float duration)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = Mathf.Clamp(hitStopScale, 0.01f, 1f);
        Time.fixedDeltaTime = baseFixedDeltaTime * Time.timeScale;

        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, duration));

        RestoreTimeScale();
        routine = null;
    }

    private void RestoreTimeScale()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = baseFixedDeltaTime > 0f ? baseFixedDeltaTime : 0.02f;
    }
}
