// Vùng tác động khi charge công cụ
// Ví dụ: Stage 0 = 1x1, Stage 1 = 1x3 (giữ 1 giây), Stage 2 = 3x3 (giữ 2 giây)
[System.Serializable]
public class ToolArea
{
    public int width = 1;           // Chiều ngang (vuông góc hướng nhìn)
    public int height = 1;          // Chiều sâu (song song hướng nhìn)
    public float chargeTime;        // Giữ bao lâu để lên stage này (giây)
}
