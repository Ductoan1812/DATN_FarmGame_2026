/// <summary>
/// Tool Scythe: quét damage tất cả entity trong tầm.
/// </summary>
public class ScytheRuntime : DamageToolRuntime
{
    public ScytheRuntime(ToolModule data) : base(data, ToolType.Scythe, hitAllTargets: true) { }
}

/// <summary>
/// Tool Axe: gây damage đơn mục tiêu bằng ToolType.Axe.
/// Thường dùng cho cây/gỗ hoặc enemy gần nhất phía trước.
/// </summary>
public class AxeRuntime : DamageToolRuntime
{
    public AxeRuntime(ToolModule data) : base(data, ToolType.Axe, hitAllTargets: false) { }
}

/// <summary>
/// Tool Pickaxe: gây damage đơn mục tiêu bằng ToolType.Pickaxe.
/// Dùng cho quặng/đá hoặc target gần nhất phía trước.
/// </summary>
public class PickaxeRuntime : DamageToolRuntime
{
    public PickaxeRuntime(ToolModule data) : base(data, ToolType.Pickaxe, hitAllTargets: false) { }
}
