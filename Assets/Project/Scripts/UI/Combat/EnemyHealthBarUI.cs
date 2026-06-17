using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EnemyHealthBarUI : MonoBehaviour
{
    [SerializeField] private Vector3 worldOffset = new(0f, 1.25f, 0f);
    [SerializeField] private Vector2 size = new(1.05f, 0.13f);
    [SerializeField] private float visibleAfterHitSeconds = 3f;
    [SerializeField] private bool showOnlyWhenDamaged;
    [SerializeField] private Color backgroundColor = new(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color fillColor = new(0.85f, 0.1f, 0.1f, 0.95f);
    [SerializeField] private string sortingLayerName = "Effect";
    [SerializeField] private int canvasSortingOrder = 121;

    private EntityRoot root;
    private EntityRuntime entity;
    private StatsRuntime stats;
    private Canvas canvas;
    private Image fill;
    private EventBus subscribedBus;
    private float visibleUntilRealtime;

    private void Awake()
    {
        root = GetComponentInParent<EntityRoot>();
        EnsureView();
    }

    private void OnEnable()
    {
        EnsureView();
        if (root == null)
            root = GetComponentInParent<EntityRoot>();
        if (root != null)
            root.OnEntityReady += OnEntityReady;

        Bind(root != null ? root.GetEntity() : null);
        Subscribe();
    }

    private void Start()
    {
        EnsureView();
        Bind(root != null ? root.GetEntity() : null);
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        if (root != null)
            root.OnEntityReady -= OnEntityReady;
        UnbindStats();

        if (subscribedBus != null)
        {
            subscribedBus.Unsubscribe<DamageAppliedPublish>(OnDamageApplied);
            subscribedBus.Unsubscribe<EntityDiedPublish>(OnEntityDied);
            subscribedBus = null;
        }
    }

    private void LateUpdate()
    {
        if (canvas == null) return;
        canvas.transform.position = transform.position + worldOffset;
        canvas.gameObject.SetActive(ShouldShow());
    }

    private void OnEntityReady(EntityRuntime runtime)
    {
        Bind(runtime);
    }

    private void Bind(EntityRuntime runtime)
    {
        if (ReferenceEquals(entity, runtime))
            return;

        UnbindStats();
        entity = runtime;
        stats = entity?.stats;
        if (stats != null)
            stats.OnChanged += OnStatChanged;
        Refresh();
    }

    private void UnbindStats()
    {
        if (stats != null)
            stats.OnChanged -= OnStatChanged;
        stats = null;
    }

    private void Subscribe()
    {
        if (subscribedBus != null) return;
        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;

        bus.Subscribe<DamageAppliedPublish>(OnDamageApplied);
        bus.Subscribe<EntityDiedPublish>(OnEntityDied);
        subscribedBus = bus;
    }

    private void OnStatChanged(StatType statType, float _)
    {
        if (statType == StatType.Hp || statType == StatType.MaxHp || statType == StatType.Level)
            Refresh();
    }

    private void OnDamageApplied(DamageAppliedPublish evt)
    {
        if (!ReferenceEquals(evt.target, entity))
            return;

        visibleUntilRealtime = Time.realtimeSinceStartup + visibleAfterHitSeconds;
        Refresh();
    }

    private void OnEntityDied(EntityDiedPublish evt)
    {
        if (!ReferenceEquals(evt.entity, entity))
            return;

        visibleUntilRealtime = 0f;
        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }

    private void EnsureView()
    {
        if (canvas != null) return;

        var canvasGo = new GameObject("EnemyHealthBarCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvasGo.transform.localPosition = worldOffset;
        canvasGo.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingLayerName = sortingLayerName;
        canvas.sortingOrder = canvasSortingOrder;

        var rect = canvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(size.x * 100f, 34f);

        var bg = RuntimeCanvasUtility.CreateImage(canvas.transform, "BarBackground", backgroundColor);
        bg.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        bg.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        bg.rectTransform.pivot = new Vector2(0.5f, 0f);
        bg.rectTransform.anchoredPosition = Vector2.zero;
        bg.rectTransform.sizeDelta = new Vector2(size.x * 100f, size.y * 100f);

        fill = RuntimeCanvasUtility.CreateImage(bg.transform, "Fill", fillColor);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = Vector2.zero;
        fill.rectTransform.offsetMax = Vector2.zero;

        canvas.gameObject.SetActive(false);
    }

    private void Refresh()
    {
        if (fill == null || stats == null)
            return;

        float maxHp = stats.Get(StatType.MaxHp);
        float hp = stats.Get(StatType.Hp);
        fill.fillAmount = maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;
    }

    private bool ShouldShow()
    {
        if (entity?.stats == null)
            return false;

        float hp = entity.stats.Get(StatType.Hp);
        if (hp <= 0f)
            return false;

        float maxHp = entity.stats.Get(StatType.MaxHp);
        if (maxHp <= 0f)
            return false;

        if (!showOnlyWhenDamaged)
            return true;

        bool recentlyHit = Time.realtimeSinceStartup < visibleUntilRealtime;
        return recentlyHit || hp < maxHp;
    }
}
