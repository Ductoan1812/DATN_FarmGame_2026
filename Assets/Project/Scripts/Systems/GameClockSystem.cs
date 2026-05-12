using UnityEngine;

/// <summary>
/// [OBSOLETE] Đã được thay thế bởi TimeManager.
/// Giữ lại để không mất reference trong scene. Hãy thay thế component này bằng TimeManager.
/// </summary>
[System.Obsolete("Dùng TimeManager thay thế. Xóa component này khỏi scene.")]
[DisallowMultipleComponent]
public class GameClockSystem : MonoBehaviour
{
    private void Awake()
    {
        Debug.LogWarning($"[GameClockSystem] OBSOLETE — hãy thay bằng TimeManager trên '{gameObject.name}'.");
    }
}
