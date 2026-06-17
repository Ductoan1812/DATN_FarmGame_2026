using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Visual motion cho EntityDrop:
/// - Spawn: văng nhẹ ra xung quanh, có arc.
/// - Idle: bob nhẹ và pulse scale.
/// - Pickup: recoil nhỏ rồi hút về player.
/// Gameplay pickup vẫn do PickUpObject xử lý.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class DropMotionObject : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform shadowRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteRenderer shadowRenderer;

    [Header("Spawn")]
    public float dropRadius = 0.8f;
    public float dropArcHeight = 0.6f;
    public float dropDuration = 0.35f;
    [SerializeField] private float spawnStartScale = 0.72f;
    [SerializeField] private float spawnOvershootScale = 1.08f;

    [Header("Idle")]
    [SerializeField] private float idleBobHeight = 0.06f;
    [SerializeField] private float idleBobSpeed = 2.6f;
    [SerializeField] private float idleScalePulse = 0.035f;

    [Header("Pickup")]
    [Tooltip("How far the item recoils away from the player")]
    public float recoilDist = 0.18f;
    public float recoilDuration = 0.06f;
    public float flyDuration = 0.22f;
    [SerializeField] private float pickupArcHeight = 0.1f;
    [SerializeField] private float collectShrinkScale = 0.82f;
    [SerializeField] private float collectShadowScale = 0.65f;

    private Collider2D _col;
    private Coroutine _routine;
    private Vector3 _restPosition;
    private Vector3 _baseVisualLocalPosition;
    private Vector3 _baseVisualScale = Vector3.one;
    private Vector3 _baseShadowLocalPosition;
    private Vector3 _baseShadowScale = Vector3.one;
    private Color _baseSpriteColor = Color.white;
    private Color _baseShadowColor = Color.white;
    private float _currentHeight;
    private float _currentScaleMultiplier = 1f;
    private float _currentAlpha = 1f;
    private float _currentShadowScaleMultiplier = 1f;
    private bool _isCollecting;

    public bool IsAnimatingPickup => _isCollecting;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        if (_col != null) _col.isTrigger = true;

        if (visualRoot == null)
        {
            var visual = transform.Find("Visual") ?? transform.Find("Visua");
            if (visual != null) visualRoot = visual;
        }

        if (shadowRoot == null)
            shadowRoot = transform.Find("Shadow");

        if (spriteRenderer == null && visualRoot != null)
            spriteRenderer = visualRoot.GetComponent<SpriteRenderer>();

        if (shadowRenderer == null && shadowRoot != null)
            shadowRenderer = shadowRoot.GetComponent<SpriteRenderer>();

        RefreshVisualBases();
    }

    private void OnEnable()
    {
        _isCollecting = false;
        _restPosition = transform.position;
        ResetColors();
        RefreshVisualBases();

        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(DropRoutine());
    }

    private void OnDisable()
    {
        if (_routine != null)
            StopCoroutine(_routine);

        _routine = null;
        _isCollecting = false;
    }

    private void Update()
    {
        if (_routine != null || _isCollecting)
            return;

        float bob = Mathf.Sin(Time.time * idleBobSpeed) * idleBobHeight;
        float pulse = 1f + Mathf.Sin(Time.time * idleBobSpeed * 0.8f) * idleScalePulse;
        float shadowPulse = 1f - Mathf.Abs(Mathf.Sin(Time.time * idleBobSpeed * 0.8f)) * 0.08f;
        ApplyPose(bob, pulse, 1f, shadowPulse);
    }

    public void RefreshVisualBases()
    {
        if (visualRoot != null)
        {
            _baseVisualLocalPosition = visualRoot.localPosition;
            _baseVisualScale = visualRoot.localScale;
        }

        if (shadowRoot != null)
        {
            _baseShadowLocalPosition = shadowRoot.localPosition;
            _baseShadowScale = shadowRoot.localScale;
        }

        if (spriteRenderer != null)
            _baseSpriteColor = spriteRenderer.color;

        if (shadowRenderer != null)
            _baseShadowColor = shadowRenderer.color;

        ApplyCurrentPose();
    }

    public void OnPickedUp(Transform target, Action onCompleted)
    {
        if (_isCollecting)
            return;

        if (target == null)
        {
            onCompleted?.Invoke();
            return;
        }

        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(PickupRoutine(target, onCompleted));
    }

    private IEnumerator DropRoutine()
    {
        Vector3 origin = transform.position;
        float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 dir2D = new(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector3 destination = origin + (Vector3)(dir2D * dropRadius);

        float elapsed = 0f;
        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, dropDuration));
            float eased = EaseOutCubic(t);
            float height = 4f * dropArcHeight * t * (1f - t);
            float scale = Mathf.Lerp(spawnStartScale, spawnOvershootScale, eased);
            float shadowScale = Mathf.Lerp(0.82f, 1f, eased) - (height / Mathf.Max(0.0001f, dropArcHeight)) * 0.08f;

            transform.position = Vector3.Lerp(origin, destination, eased);
            ApplyPose(height, scale, 1f, shadowScale);
            yield return null;
        }

        transform.position = destination;
        _restPosition = destination;
        ApplyPose(0f, 1f, 1f, 1f);
        _routine = null;
    }

    private IEnumerator PickupRoutine(Transform target, Action onCompleted)
    {
        _isCollecting = true;
        SetCollider(false);

        if (recoilDist > 0f && recoilDuration > 0f)
        {
            Vector3 toPlayer = (target.position - transform.position);
            if (toPlayer.sqrMagnitude > 0.0001f)
            {
                toPlayer.Normalize();
                Vector3 recoilPos = transform.position - toPlayer * recoilDist;
                yield return MoveRoot(transform.position, recoilPos, recoilDuration, 0.02f, 1f, 1f);
            }
        }

        Vector3 start = transform.position;
        float elapsed = 0f;
        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, flyDuration));
            float eased = EaseInCubic(t);
            float height = Mathf.Sin(t * Mathf.PI) * pickupArcHeight;
            float scale = Mathf.Lerp(1f, collectShrinkScale, eased);
            float alpha = Mathf.Lerp(1f, 0.15f, eased);
            float shadowScale = Mathf.Lerp(1f, collectShadowScale, eased);

            transform.position = Vector3.Lerp(start, target.position, eased);
            ApplyPose(height, scale, alpha, shadowScale);
            yield return null;
        }

        ApplyPose(0f, collectShrinkScale, 0f, collectShadowScale);
        _routine = null;
        onCompleted?.Invoke();
    }

    private IEnumerator MoveRoot(Vector3 from, Vector3 to, float duration, float visualHeight, float scale, float alpha)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
            transform.position = Vector3.Lerp(from, to, EaseOutCubic(t));
            ApplyPose(visualHeight, scale, alpha, 1f);
            yield return null;
        }

        transform.position = to;
    }

    private void ApplyPose(float height, float scaleMultiplier, float alpha, float shadowScaleMultiplier)
    {
        _currentHeight = height;
        _currentScaleMultiplier = scaleMultiplier;
        _currentAlpha = alpha;
        _currentShadowScaleMultiplier = shadowScaleMultiplier;
        ApplyCurrentPose();
    }

    private void ApplyCurrentPose()
    {
        if (visualRoot != null)
        {
            visualRoot.localPosition = _baseVisualLocalPosition + Vector3.up * _currentHeight;
            visualRoot.localScale = _baseVisualScale * _currentScaleMultiplier;
        }

        if (shadowRoot != null)
        {
            shadowRoot.localPosition = _baseShadowLocalPosition;
            shadowRoot.localScale = _baseShadowScale * _currentShadowScaleMultiplier;
        }

        if (spriteRenderer != null)
        {
            var color = _baseSpriteColor;
            color.a *= _currentAlpha;
            spriteRenderer.color = color;
        }

        if (shadowRenderer != null)
        {
            var color = _baseShadowColor;
            color.a *= Mathf.Clamp01(_currentAlpha * 0.85f);
            shadowRenderer.color = color;
        }
    }

    private void ResetColors()
    {
        _currentHeight = 0f;
        _currentScaleMultiplier = 1f;
        _currentAlpha = 1f;
        _currentShadowScaleMultiplier = 1f;
        ApplyCurrentPose();
    }

    private void SetCollider(bool enabled)
    {
        if (_col != null)
            _col.enabled = enabled;
    }

    private static float EaseOutCubic(float t)
    {
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }

    private static float EaseInCubic(float t)
    {
        return t * t * t;
    }
}
