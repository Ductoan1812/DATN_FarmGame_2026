using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Component đặt tại tầng Environment của mỗi Scene, giữ các ref
/// trực tiếp đến tất cả Tilemap quan trọng. Thay thế hoàn toàn việc
/// dùng FindObjectsByType/FindAnyObjectByType để tìm Tilemap sau khi
/// chuyển scene — đảm bảo system luôn bind đúng tilemap ngay trong frame đầu.
///
/// Quy ước: Component này phải luôn tồn tại trên Grid hoặc một parent
/// ngay dưới root của từng Scene (FarmScene, TownScene, MineScene...).
/// Dùng [Tools > DATN > Stamp Tilemap Registry] để tự động gán.
/// </summary>
[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public class SceneTilemapRegistry : MonoBehaviour
{
    [Header("Ground Layer")]
    [SerializeField] private Tilemap tmGround;
    [SerializeField] private Tilemap tmGroundDetail;

    [Header("Gameplay Layers")]
    [SerializeField] private Tilemap tmWatered;
    [SerializeField] private Tilemap tmRuntimeMarkers;

    [Header("Visual Layers")]
    [SerializeField] private Tilemap tmCollision;
    [SerializeField] private Tilemap tmDecoration;
    [SerializeField] private Tilemap tmOverlay;

    // ── Singleton per-scene ────────────────────────────────────────────────
    public static SceneTilemapRegistry Current { get; private set; }

    // ── Public accessors ───────────────────────────────────────────────────
    public Tilemap Ground         => tmGround;
    public Tilemap GroundDetail   => tmGroundDetail;
    public Tilemap Watered        => tmWatered;
    public Tilemap RuntimeMarkers => tmRuntimeMarkers;
    public Tilemap Collision      => tmCollision;
    public Tilemap Decoration     => tmDecoration;
    public Tilemap Overlay        => tmOverlay;

    // ── Lifecycle ──────────────────────────────────────────────────────────
    private void Awake()
    {
        Current = this;
        AutoBind();
        Debug.Log($"[SceneTilemapRegistry] Scene '{gameObject.scene.name}' registered. " +
                  $"Ground={tmGround?.name}, Watered={tmWatered?.name}, " +
                  $"Markers={tmRuntimeMarkers?.name}");
    }

    private void OnDestroy()
    {
        if (Current == this)
            Current = null;
    }

    // ── AutoBind ────────────────────────────────────────────────────────────
    /// <summary>
    /// Tự động tìm và bind các Tilemap theo tên chuẩn trong cùng Grid.
    /// Chỉ bind những slot còn trống để không ghi đè gán thủ công từ Inspector.
    /// </summary>
    public void AutoBind()
    {
        // Ưu tiên tìm trong cùng Grid cha
        var grid = GetComponentInParent<Grid>() ?? GetComponentInChildren<Grid>();
        Tilemap[] tilemaps = grid != null
            ? grid.GetComponentsInChildren<Tilemap>(includeInactive: true)
            : FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var tm in tilemaps)
        {
            if (tm == null) continue;
            switch (tm.gameObject.name)
            {
                case "Tm_Ground":
                case "Tm_Ground1":
                    if (tmGround == null) tmGround = tm;
                    break;
                case "Tm_GroundDetail":
                    if (tmGroundDetail == null) tmGroundDetail = tm;
                    break;
                case "Tm_Watered":
                    if (tmWatered == null) tmWatered = tm;
                    break;
                case "Tm_RuntimeMarkers":
                    if (tmRuntimeMarkers == null) tmRuntimeMarkers = tm;
                    break;
                case "Tm_Collision":
                    if (tmCollision == null) tmCollision = tm;
                    break;
                case "Tm_Decoration":
                    if (tmDecoration == null) tmDecoration = tm;
                    break;
                case "Tm_Overlay":
                    if (tmOverlay == null) tmOverlay = tm;
                    break;
            }
        }
    }

    /// <summary>
    /// Helper: trả về Tilemap theo tên key chuẩn (giống TileRegistry key).
    /// </summary>
    public bool TryGet(string key, out Tilemap tilemap)
    {
        tilemap = key switch
        {
            "Tm_Ground"        => tmGround,
            "Tm_GroundDetail"  => tmGroundDetail,
            "Tm_Watered"       => tmWatered,
            "Tm_RuntimeMarkers"=> tmRuntimeMarkers,
            "Tm_Collision"     => tmCollision,
            "Tm_Decoration"    => tmDecoration,
            "Tm_Overlay"       => tmOverlay,
            _                  => null
        };
        return tilemap != null;
    }

#if UNITY_EDITOR
    // Hiển thị cảnh báo nếu thiếu tilemap quan trọng khi validate trong Editor
    private void OnValidate()
    {
        if (tmGround == null)
            Debug.LogWarning($"[SceneTilemapRegistry] '{gameObject.scene.name}': tmGround chưa được gán!", this);
        if (tmRuntimeMarkers == null)
            Debug.LogWarning($"[SceneTilemapRegistry] '{gameObject.scene.name}': tmRuntimeMarkers chưa được gán!", this);
    }
#endif
}
