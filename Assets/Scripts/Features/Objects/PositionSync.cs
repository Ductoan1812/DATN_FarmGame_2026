using UnityEngine;

/// <summary>
/// Sync vị trí entity vào WorldEntityRegistry khi đổi cell.
/// Chỉ gắn vào entity có thể di chuyển (enemy, NPC, pet...).
/// Cost: 1 phép so sánh Vector2Int mỗi frame.
/// </summary>
[RequireComponent(typeof(EntityRoot))]
[DisallowMultipleComponent]
public class PositionSync : MonoBehaviour
{
    private EntityRoot _root;
    private WorldEntityService _worldService;
    private Vector2Int _lastCell;
    private bool _initialized;

    private void Start()
    {
        _root = GetComponent<EntityRoot>();
        _worldService = GameManager.Instance?.WorldService;

        if (_root == null || _worldService == null)
        {
            Debug.LogWarning($"[PositionSync] Missing EntityRoot or WorldService on '{name}'");
            enabled = false;
            return;
        }

        _lastCell = CurrentCell();
        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized) return;

        var newCell = CurrentCell();
        if (newCell == _lastCell) return;

        var entity = _root.GetEntity();
        if (entity == null) return;

        // 2D: XY plane
        var pos = new Vector2(transform.position.x, transform.position.y);
        _worldService.MoveEntity(entity.Id, pos, new[] { newCell });
        _lastCell = newCell;
    }

    private Vector2Int CurrentCell()
    {
        // 2D: XY plane → cell
        return new Vector2Int(
            Mathf.FloorToInt(transform.position.x),
            Mathf.FloorToInt(transform.position.y)
        );
    }
}
