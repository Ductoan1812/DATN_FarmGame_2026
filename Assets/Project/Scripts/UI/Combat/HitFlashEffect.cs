using System.Collections;
using UnityEngine;

public class HitFlashEffect : MonoBehaviour
{
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.08f;

    private SpriteRenderer[] renderers;
    private Color[] originalColors;
    private EntityRoot root;
    private EntityRuntime entity;
    private EventBus subscribedBus;
    private Coroutine flashRoutine;

    private void Awake()
    {
        root = GetComponent<EntityRoot>();
        RefreshRenderers();
    }

    private void OnEnable()
    {
        if (root == null)
            root = GetComponent<EntityRoot>();
        if (root != null)
        {
            root.OnEntityReady += OnEntityReady;
            entity = root.GetEntity();
        }

        Subscribe();
    }

    private void Start()
    {
        entity ??= root != null ? root.GetEntity() : null;
        Subscribe();
    }

    private void OnDisable()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        if (root != null)
            root.OnEntityReady -= OnEntityReady;

        if (subscribedBus != null)
        {
            subscribedBus.Unsubscribe<DamageAppliedPublish>(OnDamageApplied);
            subscribedBus = null;
        }

        TryRestoreColors();
    }

    private void OnEntityReady(EntityRuntime runtime)
    {
        entity = runtime;
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
        if (!ReferenceEquals(evt.target, entity))
            return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
            TryRestoreColors();
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        RefreshRenderers();
        SetColor(flashColor);
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, flashDuration));
        TryRestoreColors();
        flashRoutine = null;
    }

    private void RefreshRenderers()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
    }

    private void SetColor(Color color)
    {
        if (renderers == null) return;
        foreach (var sr in renderers)
        {
            if (sr == null) continue;
            sr.color = color;
        }
    }

    private void TryRestoreColors()
    {
        if (renderers == null || originalColors == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            var sr = renderers[i];
            if (sr == null) continue;
            sr.color = i < originalColors.Length ? originalColors[i] : Color.white;
        }
    }
}
