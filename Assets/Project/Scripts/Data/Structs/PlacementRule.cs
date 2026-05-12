using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Quy tắc đặt entity — gắn vào EntityData.
/// Tách riêng struct để EntityData gọn, dễ serialize trong Inspector.
/// </summary>
[System.Serializable]
public struct PlacementRule
{
    [Tooltip("Entity chiếm layer nào")]
    [InspectorName("Layer chiếm")]
    public EntityLayer occupyLayer;

    [Tooltip("Cần tag nào tại cell mới được đặt (OR logic — cần ít nhất 1 tag match)")]
    [InspectorName("Tag cần")]
    public PlacementTag requireTags;

    [Tooltip("Entity này cung cấp tag gì cho các entity phía trên")]
    [InspectorName("Tag cung cấp")]
    public PlacementTag provideTags;

    [Tooltip("Chặn layer nào — không cho entity ở layer đó được đặt cùng cell")]
    [InspectorName("Layer chặn")]
    public EntityLayer[] blockLayers;
}
