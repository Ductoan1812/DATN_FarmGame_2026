using UnityEngine;
using TMPro;

/// <summary>
/// Gắn lên bất kỳ GameObject nào có thể tương tác.
/// Khi Player đứng trong interactionCollider → hiện prompt "[E] Tương tác".
///
/// Yêu cầu:
///   - Gán interactionCollider là vùng tương tác mong muốn.
///   - interactionCollider nên là trigger collider.
///   - Player cần có tag "Player" và Rigidbody2D.
/// </summary>
public class InteractablePrompt : MonoBehaviour, IInteractable
{
    private const string DefaultFontResourcePath = "Fonts & Materials/Roboto-Bold SDF";

    [Header("Interaction")]
    [SerializeField] private Collider2D interactionCollider;
    [SerializeField] private bool autoAssignCollider = true;
    [SerializeField] private bool warnIfColliderIsNotTrigger = true;
    [SerializeField] private string playerTag = "Player";

    [Header("Prompt")]
    [SerializeField] private string promptText = "[E] Tương tác";
    [SerializeField] private Vector2 offset = new(0f, 1f);
    [SerializeField] private float fontSize = 3f;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color bgColor = new(0f, 0f, 0f, 0.6f);
    [SerializeField] private TMP_FontAsset fontAsset;

    private readonly Collider2D[] overlapBuffer = new Collider2D[8];
    private GameObject _promptGO;
    private TextMeshPro _tmp;
    private bool _isShowing;
    private bool _playerInside;

    public Collider2D InteractionCollider => interactionCollider;

    private void Reset()
    {
        AutoAssignCollider();
    }

    private void Awake()
    {
        if (interactionCollider == null && autoAssignCollider)
            AutoAssignCollider();

        if (interactionCollider != null && warnIfColliderIsNotTrigger && !interactionCollider.isTrigger)
        {
            Debug.LogWarning(
                $"[InteractablePrompt] '{name}' interactionCollider is not trigger. Prompt can still work, but a trigger collider is recommended.",
                this);
        }
    }

    private void Start()
    {
        CreatePrompt();
        _promptGO.SetActive(false);
    }

    private void Update()
    {
        RefreshPromptState();
    }

    public bool AcceptsScanCollider(Collider2D candidate)
    {
        if (candidate == null) return false;
        if (interactionCollider == null) return true;
        return candidate == interactionCollider;
    }

    // ── IInteractable ─────────────────────────────────────────────────────────

    public void Interact(EntityRuntime interactor)
    {
        // Sẽ được gọi bởi ActionRuntime khi SecondaryAction
        // Logic cụ thể do entity's module xử lý (HarvestRuntime, DialogRuntime...)
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void SetPromptText(string text)
    {
        promptText = text;
        if (_tmp != null) _tmp.text = text;
    }

    public void Show()
    {
        if (_promptGO == null || _isShowing) return;
        _isShowing = true;
        _promptGO.SetActive(true);
    }

    public void Hide()
    {
        if (_promptGO == null || !_isShowing) return;
        _isShowing = false;
        _promptGO.SetActive(false);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void CreatePrompt()
    {
        _promptGO = new GameObject("InteractPrompt");
        _promptGO.transform.SetParent(transform);
        _promptGO.transform.localPosition = offset;
        _promptGO.transform.localScale = Vector3.one;

        _tmp = _promptGO.AddComponent<TextMeshPro>();
        if (fontAsset == null)
            fontAsset = Resources.Load<TMP_FontAsset>(DefaultFontResourcePath);
        if (fontAsset != null)
            _tmp.font = fontAsset;
        _tmp.text = promptText;
        _tmp.fontSize = fontSize;
        _tmp.color = textColor;
        _tmp.alignment = TextAlignmentOptions.Center;
        _tmp.sortingOrder = 100;

        // Auto-size vừa text
        _tmp.enableAutoSizing = false;
        _tmp.rectTransform.sizeDelta = new Vector2(4f, 1f);
    }

    private void RefreshPromptState()
    {
        if (_promptGO == null || interactionCollider == null)
        {
            if (_playerInside)
            {
                _playerInside = false;
                Hide();
            }
            return;
        }

        bool hasPlayer = false;
        var filter = new ContactFilter2D();
        filter.NoFilter();

        int count = interactionCollider.OverlapCollider(filter, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            var hit = overlapBuffer[i];
            if (hit == null) continue;
            if (!IsPlayerCollider(hit)) continue;

            hasPlayer = true;
            break;
        }

        if (hasPlayer == _playerInside)
            return;

        _playerInside = hasPlayer;
        if (_playerInside) Show();
        else Hide();
    }

    private void AutoAssignCollider()
    {
        interactionCollider = GetComponent<Collider2D>();
        if (interactionCollider != null) return;

        interactionCollider = GetComponentInChildren<Collider2D>();
    }

    private bool IsPlayerCollider(Collider2D hit)
    {
        var current = hit.transform;
        while (current != null)
        {
            if (current.CompareTag(playerTag))
                return true;

            current = current.parent;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (interactionCollider == null) return;

        Gizmos.color = new Color(0.2f, 0.9f, 0.45f, 0.35f);
        var bounds = interactionCollider.bounds;
        Gizmos.DrawCube(bounds.center, bounds.size);

        Gizmos.color = new Color(0.2f, 0.9f, 0.45f, 1f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }

    private void OnDestroy()
    {
        if (_promptGO != null) Destroy(_promptGO);
    }
}
