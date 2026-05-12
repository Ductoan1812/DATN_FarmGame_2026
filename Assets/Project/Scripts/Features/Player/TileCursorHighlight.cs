using UnityEngine;

/// <summary>
/// Vẽ đường kẻ hình vuông màu đỏ lên tile trước mặt Player.
/// Dùng LineRenderer để hiển thị. Có biến bool để bật/tắt.
/// Gắn vào Player GameObject.
/// </summary>
[RequireComponent(typeof(PlayerControler))]
public class TileCursorHighlight : MonoBehaviour
{
    [Header("Bật/Tắt")]
    public bool showHighlight = true;

    [Header("Hiển thị")]
    [SerializeField] private Color lineColor = Color.red;
    [SerializeField] private float lineWidth = 0.04f;
    [SerializeField] private int sortingOrder = 100;

    private PlayerControler _controller;
    private LineRenderer _line;
    private Vector3Int _lastCell;

    private void Awake()
    {
        _controller = GetComponent<PlayerControler>();

        // Tạo child object chứa LineRenderer
        var go = new GameObject("TileCursor");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;

        _line = go.AddComponent<LineRenderer>();
        _line.useWorldSpace = true;
        _line.loop = true;
        _line.positionCount = 4;
        _line.startWidth = lineWidth;
        _line.endWidth = lineWidth;
        _line.sortingOrder = sortingOrder;

        // Material unlit để không bị ảnh hưởng bởi ánh sáng
        _line.material = new Material(Shader.Find("Sprites/Default"));
        _line.startColor = lineColor;
        _line.endColor = lineColor;

        _lastCell = new Vector3Int(int.MinValue, int.MinValue, 0);
    }

    private void LateUpdate()
    {
        if (!showHighlight || _controller == null)
        {
            _line.enabled = false;
            return;
        }

        _line.enabled = true;

        var cell = GridSystem.GetCellInFrontOf(gameObject);

        // Chỉ cập nhật khi cell thay đổi
        if (cell == _lastCell) return;
        _lastCell = cell;

        // Lấy world center của cell, rồi tính 4 góc
        var center = GridSystem.GetCellCenter(cell);

        // Mặc định cell size = 1x1
        float half = 0.5f;
        var bl = new Vector3(center.x - half, center.y - half, 0f); // bottom-left
        var br = new Vector3(center.x + half, center.y - half, 0f); // bottom-right
        var tr = new Vector3(center.x + half, center.y + half, 0f); // top-right
        var tl = new Vector3(center.x - half, center.y + half, 0f); // top-left

        _line.SetPosition(0, bl);
        _line.SetPosition(1, br);
        _line.SetPosition(2, tr);
        _line.SetPosition(3, tl);
    }

    // ── Public API ────────────────────────────────────────

    /// <summary>Đổi màu đường kẻ runtime.</summary>
    public void SetColor(Color color)
    {
        lineColor = color;
        if (_line != null)
        {
            _line.startColor = color;
            _line.endColor = color;
        }
    }

    /// <summary>Bật/tắt highlight.</summary>
    public void SetEnabled(bool enabled)
    {
        showHighlight = enabled;
    }
}
