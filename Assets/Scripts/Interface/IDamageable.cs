/// <summary>
/// Bất kỳ object nào có thể nhận damage: Plant, Enemy, Resource Node...
/// Tool/Player chỉ cần biết interface này, không cần biết loại cụ thể.
/// </summary>
public interface IDamageable
{
    /// <summary>HP hiện tại</summary>
    int CurrentHp { get; }

    /// <summary>HP tối đa</summary>
    int MaxHp { get; }

    bool IsAlive { get; }

    /// <summary>
    /// Nhận damage. Trả về true nếu object chết sau đòn này.
    /// </summary>
    bool TakeDamage(int damage, ToolType toolType = ToolType.None);
}
