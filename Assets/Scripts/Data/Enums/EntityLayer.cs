/// <summary>
/// Layer mà entity chiếm giữ tại 1 cell.
/// Mỗi cell có thể chứa tối đa 1 entity/layer.
/// </summary>
using UnityEngine;
public enum EntityLayer
{
    [InspectorName("Nền đất")]
    Ground,      
    [InspectorName("Nội thất")]
    Furniture,   
    [InspectorName("Cây trồng")]
    Plant,       
    [InspectorName("Trang trí")]
    Decoration   
}
