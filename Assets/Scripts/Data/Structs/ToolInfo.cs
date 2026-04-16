using UnityEngine;

// Module: Khả năng công cụ
// Gắn vào ItemData khi item là cuốc, rìu, bình tưới, liềm...
// Cách check: item.IsTool (kiểm tra toolType != None)
[System.Serializable]
public class ToolInfo
{
    public ToolType toolType = ToolType.None;   // Loại tool (None = không phải tool)
    public float useSpeed = 1f;                 // Tốc độ sử dụng
    public float range = 1f;                    // Tầm tác động
    public ToolArea[] areaOfEffect;             // Vùng tác động theo stage charge
    public AudioClip useSound;                  // Âm thanh khi dùng
}
