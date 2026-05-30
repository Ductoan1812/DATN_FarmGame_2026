using UnityEngine;

/// <summary>
/// Component phải có trong MỌI Coreplay scene (FarmScene, TownScene, MineScene).
/// Chạy trước tất cả system khác (ExecutionOrder = -500).
///
/// Nhiệm vụ:
///   1. Kiểm tra GameManager — nếu chưa có, instantiate từ BootstrapConfig prefab.
///   2. Kiểm tra UIRoot   — nếu chưa có, instantiate từ BootstrapConfig prefab.
///   3. Validate   — log error rõ ràng nếu thiếu bất kỳ thứ gì.
///
/// Đặt trên GameObject tên "__Bootstrap__" ở root của mỗi scene.
/// Dùng Tools > DATN > Stamp Bootstrap Loader to All Scenes để tự động thêm.
/// </summary>
[DefaultExecutionOrder(-500)]
public class BootstrapLoader : MonoBehaviour
{
    private void Awake()
    {
        EnsureGameManager();
        EnsureUIRoot();
        Validate();
    }

    // ── GameManager ──────────────────────────────────────────────────────────

    private static void EnsureGameManager()
    {
        if (GameManager.Instance != null)
        {
            Debug.Log("[BootstrapLoader] GameManager already present — skip.");
            return;
        }

        var config = LoadConfig();
        if (config == null) return;

        if (config.gameManagerPrefab == null)
        {
            Debug.LogError("[BootstrapLoader] BootstrapConfig.gameManagerPrefab is null! " +
                           "Please assign it in Resources/BootstrapConfig.");
            if (config.throwOnMissingPrefab)
                throw new System.Exception("[BootstrapLoader] GameManager prefab missing.");
            return;
        }

        var go = Instantiate(config.gameManagerPrefab);
        go.name = config.gameManagerPrefab.name;
        DontDestroyOnLoad(go);
        Debug.Log($"[BootstrapLoader] GameManager instantiated from prefab '{go.name}'.");
    }

    // ── UIRoot ───────────────────────────────────────────────────────────────

    private static void EnsureUIRoot()
    {
        // Kiểm tra UIRoot tồn tại chưa (DontDestroyOnLoad vẫn tìm được qua tag/name)
        var existing = GameObject.Find("UIRoot");
        if (existing != null)
        {
            Debug.Log("[BootstrapLoader] UIRoot already present — skip.");
            return;
        }

        var config = LoadConfig();
        if (config == null) return;

        if (config.uiRootPrefab == null)
        {
            Debug.LogError("[BootstrapLoader] BootstrapConfig.uiRootPrefab is null! " +
                           "Please assign it in Resources/BootstrapConfig.");
            if (config.throwOnMissingPrefab)
                throw new System.Exception("[BootstrapLoader] UIRoot prefab missing.");
            return;
        }

        var go = Instantiate(config.uiRootPrefab);
        go.name = "UIRoot";
        DontDestroyOnLoad(go);
        Debug.Log($"[BootstrapLoader] UIRoot instantiated from prefab '{config.uiRootPrefab.name}'.");
    }

    // ── Validate ─────────────────────────────────────────────────────────────

    private static void Validate()
    {
        bool ok = true;

        if (GameManager.Instance == null)
        {
            Debug.LogError("[BootstrapLoader] CRITICAL: GameManager.Instance is still null after bootstrap! " +
                           "Check that BootstrapConfig.gameManagerPrefab is assigned and valid.");
            ok = false;
        }

        if (GameObject.Find("UIRoot") == null)
        {
            Debug.LogError("[BootstrapLoader] CRITICAL: UIRoot not found after bootstrap! " +
                           "Check that BootstrapConfig.uiRootPrefab is assigned and valid.");
            ok = false;
        }

        if (ok)
            Debug.Log("[BootstrapLoader] Validation passed. GameManager + UIRoot ready.");
    }

    // ── Config loader (cached) ───────────────────────────────────────────────

    private static BootstrapConfig _cachedConfig;

    private static BootstrapConfig LoadConfig()
    {
        if (_cachedConfig != null) return _cachedConfig;

        _cachedConfig = Resources.Load<BootstrapConfig>(BootstrapConfig.ResourcePath);
        if (_cachedConfig == null)
        {
            Debug.LogError("[BootstrapLoader] Cannot load 'Resources/BootstrapConfig.asset'! " +
                           "Create it via DATN > Create Bootstrap Config.");
        }

        return _cachedConfig;
    }
}
