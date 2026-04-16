/// <summary>
/// Tag dùng cho hệ thống placement.
/// Entity/Tile "provides" tag → entity khác "requires" tag để được đặt.
/// </summary>
using UnityEngine;
[System.Flags]
public enum PlacementTag
{
    [InspectorName("None")]
    None      = 0,
    [InspectorName("trồng cây")]
    Plantable = 1 << 0,  
    [InspectorName("đi qua")]
    Walkable  = 1 << 1, 
    [InspectorName("xây dựng")]
    Buildable = 1 << 2,   
    [InspectorName("tưới nước")]
    Waterable = 1 << 3,   
}
