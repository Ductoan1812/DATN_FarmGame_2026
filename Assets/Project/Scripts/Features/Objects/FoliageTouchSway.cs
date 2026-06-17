using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class FoliageTouchSway : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private Collider2D swayCollider;
    [SerializeField] private bool preferInteractionCollider = true;
    [SerializeField] private string playerTag = "Player";

    [Header("Visual")]
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private float swayAngle = 7f;
    [SerializeField] private float swayInDuration = 0.08f;
    [SerializeField] private float swayOutDuration = 0.16f;

    private readonly Collider2D[] overlapBuffer = new Collider2D[8];
    private Transform visualTransform;
    private Quaternion baseRotation;
    private bool playerInside;
    private Coroutine swayCoroutine;

    private void Awake()
    {
        ResolveRefs();
    }

    private void OnEnable()
    {
        ResolveRefs();
        if (visualTransform != null)
            visualTransform.localRotation = baseRotation;
        playerInside = false;
    }

    private void Update()
    {
        if (swayCollider == null || visualTransform == null)
            return;

        bool hasPlayer = HasPlayerOverlap();
        if (hasPlayer && !playerInside)
            TriggerSway();

        playerInside = hasPlayer;
    }

    private void Reset()
    {
        ResolveRefs();
    }

    private void ResolveRefs()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SpriteRenderer>();

        visualTransform = targetRenderer != null ? targetRenderer.transform : transform;
        baseRotation = visualTransform.localRotation;

        if (swayCollider == null)
            swayCollider = ResolveSwayCollider();
    }

    private Collider2D ResolveSwayCollider()
    {
        if (preferInteractionCollider)
        {
            var prompt = GetComponent<InteractablePrompt>();
            if (prompt != null && prompt.InteractionCollider != null)
                return prompt.InteractionCollider;
        }

        var colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && colliders[i].isTrigger)
                return colliders[i];
        }

        return GetComponent<Collider2D>();
    }

    private bool HasPlayerOverlap()
    {
        var filter = new ContactFilter2D();
        filter.NoFilter();

        int count = swayCollider.OverlapCollider(filter, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            if (IsPlayerCollider(overlapBuffer[i]))
                return true;
        }

        return false;
    }

    private bool IsPlayerCollider(Collider2D hit)
    {
        if (hit == null)
            return false;

        var current = hit.transform;
        while (current != null)
        {
            if (current.CompareTag(playerTag))
                return true;

            current = current.parent;
        }

        return false;
    }

    private void TriggerSway()
    {
        if (swayCoroutine != null)
            StopCoroutine(swayCoroutine);

        swayCoroutine = StartCoroutine(SwayCoroutine());
    }

    private IEnumerator SwayCoroutine()
    {
        Quaternion from = baseRotation;
        Quaternion to = baseRotation * Quaternion.Euler(0f, 0f, swayAngle);

        yield return RotateOverTime(from, to, swayInDuration);
        yield return RotateOverTime(to, baseRotation, swayOutDuration);

        visualTransform.localRotation = baseRotation;
        swayCoroutine = null;
    }

    private IEnumerator RotateOverTime(Quaternion from, Quaternion to, float duration)
    {
        if (duration <= 0f)
        {
            visualTransform.localRotation = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            visualTransform.localRotation = Quaternion.SlerpUnclamped(from, to, t);
            yield return null;
        }

        visualTransform.localRotation = to;
    }
}
