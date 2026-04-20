using UnityEngine;
using System.Collections;

/// <summary>
/// Drop effect  : object launches in a random direction with a parabolic arc.
/// Pickup effect: slight recoil, fly to player, then return to rest position
///                and wait for cooldown before allowing another pickup.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class DropMotionObject : MonoBehaviour
{
    [Header("Drop")]
    public float dropRadius = 0.8f;
    public float dropArcHeight = 1.2f;
    public float dropDuration = 0.45f;

    [Header("Pickup")]
    [Tooltip("Seconds after spawn before pickup is allowed")]
    public float pickDelay = 0.6f;

    [Tooltip("Cooldown (seconds) after pickup animation before next pickup allowed")]
    public float pickupCooldown = 2f;

    [Tooltip("How far the item recoils away from the player")]
    public float recoilDist = 0.3f;
    public float recoilDuration = 0.08f;
    public float flyDuration = 0.3f;

    [Tooltip("How fast the item returns to rest position after a failed pickup")]
    public float returnDuration = 0.4f;

    public string targetTag = "Player";

    // ── private ──────────────────────────────────────────────────────────────
    private Collider2D _col;
    private Coroutine _routine;
    private float _spawnTime;
    private Vector3 _restPosition;

    // ── Unity ─────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        if (_col != null) _col.isTrigger = true;
    }

    private void OnEnable()
    {
        _spawnTime = Time.time;
        _restPosition = transform.position;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(DropRoutine());
    }

    // ── Public API ────────────────────────────────────────────────────────────
    //   Trigger detection đã được chuyển sang PickUpObject để tránh xung đột
    //   2 component cùng xử lý collider. File này chỉ còn visual/animation.
    public void OnPickedUp(Transform target)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PickupRoutine(target));
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    private IEnumerator DropRoutine()
    {
        Vector3 origin = transform.position;

        // Random direction on XY plane
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 dir2D = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector3 destination = origin + (Vector3)(dir2D * dropRadius);

        float elapsed = 0f;
        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dropDuration);
            Vector3 flat = Vector3.Lerp(origin, destination, t);
            // Arc on Y axis (visual bounce effect)
            float arcY = 4f * dropArcHeight * t * (1f - t);
            transform.position = new Vector3(flat.x, flat.y + arcY, 0f);
            yield return null;
        }

        transform.position = destination;
        _restPosition = destination;
        _routine = null;
    }

    private IEnumerator PickupRoutine(Transform target)
    {
        // Tắt collider trong suốt animation để tránh re-trigger
        SetCollider(false);

        // ── Phase 1: recoil ──────────────────────────────────────────────────
        if (recoilDist > 0f && recoilDuration > 0f)
        {
            Vector3 toPlayer = (target.position - transform.position).normalized;
            Vector3 recoilPos = transform.position - toPlayer * recoilDist;
            Vector3 from = transform.position;
            float t = 0f;

            while (t < recoilDuration)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(from, recoilPos, Mathf.Clamp01(t / recoilDuration));
                yield return null;
            }
            transform.position = recoilPos;
        }

        // ── Phase 2: fly to player ───────────────────────────────────────────
        Vector3 flyStart = transform.position;
        float e = 0f;

        while (e < flyDuration)
        {
            e += Time.deltaTime;
            float p = Mathf.Clamp01(e / flyDuration);
            transform.position = Vector3.Lerp(flyStart, target.position, p * p);
            yield return null;
        }

        // ── Phase 3: nếu object vẫn còn active → trở về rest + cooldown ─────
        yield return null;

        if (!gameObject.activeInHierarchy) yield break;

        // Bay về vị trí nghỉ
        Vector3 returnStart = transform.position;
        float r = 0f;
        while (r < returnDuration)
        {
            r += Time.deltaTime;
            float p = Mathf.Clamp01(r / returnDuration);
            transform.position = Vector3.Lerp(returnStart, _restPosition, p * p);
            yield return null;
        }
        transform.position = _restPosition;

        // Cooldown trước khi bật collider lại
        yield return new WaitForSeconds(pickupCooldown);

        SetCollider(true);
        _routine = null;
    }

    private void SetCollider(bool enabled)
    {
        if (_col != null) _col.enabled = enabled;
    }
}
