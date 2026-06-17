using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ResourceHitReactionObject : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Animator targetAnimator;

    private ResourceHitReactionModule config;
    private Coroutine activeRoutine;
    private Color baseColor = Color.white;
    private Vector3 baseScale = Vector3.one;
    private Quaternion baseRotation = Quaternion.identity;

    private void Awake()
    {
        CacheTargets();
        CaptureBaseState();
    }

    private void OnDisable()
    {
        RestoreBaseState();
    }

    public void Configure(ResourceHitReactionModule module)
    {
        config = module;
        CacheTargets();
        CaptureBaseState();
    }

    public void PlayHit()
    {
        CacheTargets();
        CaptureBaseState();

        if (targetAnimator != null && !string.IsNullOrWhiteSpace(config?.animatorHitTrigger))
            targetAnimator.SetTrigger(config.animatorHitTrigger);

        if (config != null && !config.useProceduralMotion)
        {
            RestoreBaseState();
            return;
        }

        if (!isActiveAndEnabled)
            return;

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(HitRoutine());
    }

    private IEnumerator HitRoutine()
    {
        float duration = Mathf.Max(0.01f, config != null ? config.flashDuration : 0.12f);
        float scalePunch = Mathf.Max(0f, config != null ? config.scalePunch : 0.12f);
        float rotationPunch = Mathf.Max(0f, config != null ? config.rotationPunch : 4f);
        Color flashColor = config != null ? config.flashColor : new Color(1f, 0.9f, 0.65f, 1f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float punch = Mathf.Sin(normalized * Mathf.PI);
            float wobble = Mathf.Sin(normalized * Mathf.PI * 3f) * (1f - normalized);

            transform.localScale = baseScale * (1f + scalePunch * punch);
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, rotationPunch * wobble);

            if (targetRenderer != null)
                targetRenderer.color = Color.Lerp(flashColor, baseColor, normalized);

            yield return null;
        }

        RestoreBaseState();
        activeRoutine = null;
    }

    private void CacheTargets()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SpriteRenderer>();

        if (targetAnimator == null)
            targetAnimator = GetComponentInChildren<Animator>();
    }

    private void CaptureBaseState()
    {
        baseScale = transform.localScale;
        baseRotation = transform.localRotation;
        if (targetRenderer != null)
            baseColor = targetRenderer.color;
    }

    private void RestoreBaseState()
    {
        transform.localScale = baseScale;
        transform.localRotation = baseRotation;
        if (targetRenderer != null)
            targetRenderer.color = baseColor;
    }
}
