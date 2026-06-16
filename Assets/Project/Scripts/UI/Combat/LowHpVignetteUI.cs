using UnityEngine;
using UnityEngine.UI;

public class LowHpVignetteUI : MonoBehaviour
{
    [SerializeField, Range(0.05f, 1f)] private float warningThreshold = 0.35f;
    [SerializeField, Range(0f, 0.8f)] private float maxAlpha = 0.32f;
    [SerializeField, Min(0.1f)] private float fadeSpeed = 8f;
    [SerializeField] private Color vignetteColor = new(0.75f, 0f, 0f, 0f);

    private EventBus subscribedBus;
    private EntityRuntime playerEntity;
    private Image overlay;
    private float targetAlpha;
    private float currentAlpha;

    private void OnEnable()
    {
        EnsureView();
        Subscribe();
        BindCurrentPlayer();
    }

    private void Start()
    {
        EnsureView();
        Subscribe();
        BindCurrentPlayer();
    }

    private void OnDisable()
    {
        if (subscribedBus != null)
        {
            subscribedBus.Unsubscribe<PlayerReadyPublish>(OnPlayerReady);
            subscribedBus.Unsubscribe<StatsChangedPublish>(OnStatsChanged);
            subscribedBus = null;
        }
    }

    private void Update()
    {
        if (overlay == null)
            return;

        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, 1f - Mathf.Exp(-fadeSpeed * Time.unscaledDeltaTime));
        var color = vignetteColor;
        color.a = currentAlpha;
        overlay.color = color;
        overlay.enabled = currentAlpha > 0.01f;
    }

    private void Subscribe()
    {
        if (subscribedBus != null) return;
        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;

        bus.Subscribe<PlayerReadyPublish>(OnPlayerReady);
        bus.Subscribe<StatsChangedPublish>(OnStatsChanged);
        subscribedBus = bus;
    }

    private void OnPlayerReady(PlayerReadyPublish _)
    {
        BindCurrentPlayer();
    }

    private void OnStatsChanged(StatsChangedPublish evt)
    {
        if (playerEntity == null || evt.entityId != playerEntity.id)
            return;

        if (evt.statType == StatType.Hp || evt.statType == StatType.MaxHp)
            Refresh();
    }

    private void BindCurrentPlayer()
    {
        var player = FindAnyObjectByType<PlayerControler>();
        var root = player != null ? player.GetComponent<EntityRoot>() : null;
        playerEntity = root != null ? root.GetEntity() : null;
        Refresh();
    }

    private void EnsureView()
    {
        if (overlay != null) return;

        var canvas = RuntimeCanvasUtility.CreateOverlayCanvas("LowHpVignetteCanvas", transform, 80);
        overlay = RuntimeCanvasUtility.CreateImage(canvas.transform, "LowHpVignette", Color.clear);
        overlay.rectTransform.anchorMin = Vector2.zero;
        overlay.rectTransform.anchorMax = Vector2.one;
        overlay.rectTransform.offsetMin = Vector2.zero;
        overlay.rectTransform.offsetMax = Vector2.zero;
    }

    private void Refresh()
    {
        if (overlay == null || playerEntity?.stats == null)
        {
            targetAlpha = 0f;
            return;
        }

        float hp = playerEntity.stats.Get(StatType.Hp);
        float maxHp = playerEntity.stats.Get(StatType.MaxHp);
        float ratio = maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 1f;
        float danger = Mathf.InverseLerp(warningThreshold, 0f, ratio);
        targetAlpha = Mathf.Lerp(0f, maxAlpha, danger);
    }
}
