using UnityEngine;

/// <summary>
/// ScriptableObject chứa refs đến các prefab hệ thống cần thiết cho mọi scene.
/// Đặt tại: Resources/BootstrapConfig.asset
/// 
/// BootstrapLoader sẽ đọc file này và instantiate GameManager + UIRoot
/// nếu chúng chưa tồn tại trong scene hiện tại.
/// </summary>
[CreateAssetMenu(menuName = "DATN/Bootstrap Config", fileName = "BootstrapConfig")]
public class BootstrapConfig : ScriptableObject
{
    [Header("System Prefabs")]
    [Tooltip("Prefab của GameManager (thường là '_____GameManager____')")]
    public GameObject gameManagerPrefab;

    [Tooltip("Prefab của UIRoot (chứa Canvas_HUD, Canvas_Windows, v.v.)")]
    public GameObject uiRootPrefab;

    [Header("Validation")]
    [Tooltip("Nếu true: khi thiếu GameManager hoặc UIRoot sẽ throw Exception thay vì chỉ log error")]
    public bool throwOnMissingPrefab = false;

    // Resource path để BootstrapLoader dùng Resources.Load
    public const string ResourcePath = "BootstrapConfig";
}
