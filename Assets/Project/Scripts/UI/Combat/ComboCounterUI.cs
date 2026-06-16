using TMPro;
using UnityEngine;

public class ComboCounterUI : MonoBehaviour
{
    [SerializeField] private float comboWindowSeconds = 2.2f;
    [SerializeField] private Color color = new(1f, 0.75f, 0.2f);

    private EventBus subscribedBus;
    private TextMeshProUGUI label;
    private int combo;
    private float expiresAtRealtime;

    private void OnEnable()
    {
        EnsureView();
        Subscribe();
    }

    private void Start()
    {
        EnsureView();
        Subscribe();
    }

    private void Update()
    {
        if (combo > 0 && Time.realtimeSinceStartup > expiresAtRealtime)
            ResetCombo();
    }

    private void OnDisable()
    {
        if (subscribedBus != null)
        {
            subscribedBus.Unsubscribe<DamageAppliedPublish>(OnDamageApplied);
            subscribedBus = null;
        }
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

        combo++;
        expiresAtRealtime = Time.realtimeSinceStartup + comboWindowSeconds;
        Refresh();
    }

    private void ResetCombo()
    {
        combo = 0;
        label.gameObject.SetActive(false);
    }

    private void Refresh()
    {
        label.text = combo <= 1 ? string.Empty : $"{combo} HIT";
        label.color = color;
        label.gameObject.SetActive(combo > 1);
    }

    private void EnsureView()
    {
        if (label != null) return;

        var canvas = RuntimeCanvasUtility.CreateOverlayCanvas("ComboCounterCanvas", transform, 95);
        label = RuntimeCanvasUtility.CreateText(canvas.transform, "ComboCounterText", 38, TextAlignmentOptions.Center);
        label.rectTransform.anchorMin = new Vector2(1f, 1f);
        label.rectTransform.anchorMax = new Vector2(1f, 1f);
        label.rectTransform.pivot = new Vector2(1f, 1f);
        label.rectTransform.anchoredPosition = new Vector2(-130f, -150f);
        label.gameObject.SetActive(false);
    }
}
