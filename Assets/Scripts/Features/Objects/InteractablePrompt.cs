using UnityEngine;
using TMPro;

/// <summary>
/// Gắn lên bất kỳ GameObject nào có thể tương tác.
/// Khi Player vào vùng trigger → hiện dòng chữ phía trên đầu (vd: "[E] Thu hoạch").
/// Khi Player ra khỏi → ẩn.
///
/// Yêu cầu:
///   - GameObject cần có Collider2D (isTrigger = true).
///   - Player cần có tag "Player" và Rigidbody2D.
/// </summary>
public class InteractablePrompt : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    [SerializeField] private string promptText = "[RMB] Tương tác";
    [SerializeField] private Vector2 offset = new(0f, 1f);
    [SerializeField] private float fontSize = 3f;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color bgColor = new(0f, 0f, 0f, 0.6f);

    private GameObject _promptGO;
    private TextMeshPro _tmp;
    private bool _isShowing;

    private void Start()
    {
        CreatePrompt();
        _promptGO.SetActive(false);
    }

    // ── Trigger ───────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Show();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Hide();
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
        if (_promptGO == null) return;
        _isShowing = true;
        _promptGO.SetActive(true);
    }

    public void Hide()
    {
        if (_promptGO == null) return;
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
        _tmp.text = promptText;
        _tmp.fontSize = fontSize;
        _tmp.color = textColor;
        _tmp.alignment = TextAlignmentOptions.Center;
        _tmp.sortingOrder = 100;

        // Auto-size vừa text
        _tmp.enableAutoSizing = false;
        _tmp.rectTransform.sizeDelta = new Vector2(4f, 1f);
    }

    private void OnDestroy()
    {
        if (_promptGO != null) Destroy(_promptGO);
    }
}
