using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Centralized grid utility system
/// Manages Tilemap conversion and grid helpers
/// </summary>
public class GridSystem : MonoBehaviour
{
    public static GridSystem Instance { get; private set; }

    [SerializeField] private Tilemap tilemap;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Dùng AutoRebind thay vì FindAnyObjectByType<Tilemap>() để đảm bảo đúng Ground tilemap
        // (FindAnyObjectByType có thể pick Tm_background thay vì Tm_Ground1)
        if (tilemap == null)
            AutoRebind();

        if (tilemap == null)
        {
            Debug.LogWarning("[GridSystem] No Ground Tilemap found in scene!");
        }
    }

    /// <summary>
    /// Convert world position to cell position
    /// </summary>
    public static Vector3Int WorldToCell(Vector3 worldPos)
    {
        if (Instance == null || Instance.tilemap == null)
        {
            Debug.LogError("[GridSystem] GridSystem not initialized or tilemap not assigned!");
            return Vector3Int.zero;
        }

        return Instance.tilemap.WorldToCell(worldPos);
    }

    /// <summary>
    /// Convert cell position to world center
    /// </summary>
    public static Vector3 GetCellCenter(Vector3Int cell)
    {
        if (Instance == null || Instance.tilemap == null)
        {
            Debug.LogError("[GridSystem] GridSystem not initialized or tilemap not assigned!");
            return Vector3.zero;
        }

        return Instance.tilemap.GetCellCenterWorld(cell);
    }

    /// <summary>
    /// Snap direction to cardinal (4-directional) on XY plane
    /// </summary>
    public static Vector3 ToCardinal(Vector3 dir)
    {
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
        {
            return new Vector3(Mathf.Sign(dir.x), 0f, 0f);
        }

        return new Vector3(0f, Mathf.Sign(dir.y), 0f);
    }

    /// <summary>
    /// Check if cell is valid in tilemap
    /// </summary>
    public static bool IsValidCell(Vector3Int cell)
    {
        if (Instance == null || Instance.tilemap == null)
        {
            return false;
        }

        BoundsInt bounds = Instance.tilemap.cellBounds;
        return bounds.Contains(cell);
    }

    /// <summary>
    /// Lấy cell phía trước của một vị trí theo hướng facing (snap về 4 hướng cardinal).
    /// </summary>
    public static Vector3Int GetCellInFront(Vector3 position, Vector3 facing)
    {
        var cardinal = ToCardinal(facing.sqrMagnitude > 0.001f ? facing : Vector3.up);
        return WorldToCell(position + cardinal);
    }

    /// <summary>
    /// Shortcut: lấy cell phía trước của một GameObject (tự lấy facing từ PlayerControler).
    /// </summary>
    public static Vector3Int GetCellInFrontOf(UnityEngine.GameObject target)
    {
        var ctrl = target.GetComponent<PlayerControler>();
        var facing = ctrl != null ? ctrl.LastMoveDirection : Vector3.up;
        return GetCellInFront(target.transform.position, facing);
    }

    /// <summary>
    /// Get tilemap reference (exposed for systems that need direct access)
    /// </summary>
    public static Tilemap GetTilemap()
    {
        if (Instance == null || Instance.tilemap == null)
        {
            Debug.LogError("[GridSystem] GridSystem not initialized!");
            return null;
        }

        return Instance.tilemap;
    }

    /// <summary>
    /// Auto rebind tilemap after scene transitions.
    /// Ưu tiên: SceneTilemapRegistry → GameManager.TmGround → FindAnyObjectByType
    /// </summary>
    public void AutoRebind()
    {
        // 1. SceneTilemapRegistry — bind chính xác, không scan scene
        var reg = SceneTilemapRegistry.Current;
        if (reg != null && reg.Ground != null)
        {
            tilemap = reg.Ground;
            Debug.Log($"[GridSystem] AutoRebind from SceneTilemapRegistry: '{tilemap.name}'.");
            return;
        }

        // 2. GameManager serialized ref
        if (GameManager.Instance != null && GameManager.Instance.TmGround != null)
        {
            tilemap = GameManager.Instance.TmGround;
            Debug.Log($"[GridSystem] AutoRebind from GameManager.TmGround: '{tilemap.name}'.");
            return;
        }

        // 3. Fallback: FindAnyObjectByType (chỉ khi scene chưa có registry)
        tilemap = FindAnyObjectByType<Tilemap>();
        if (tilemap != null)
            Debug.Log($"[GridSystem] AutoRebind fallback FindAnyObjectByType: '{tilemap.name}'.");
        else
            Debug.LogWarning("[GridSystem] AutoRebind failed: No Tilemap found in active scene!");
    }
}

