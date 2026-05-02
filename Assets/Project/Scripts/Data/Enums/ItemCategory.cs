
using UnityEngine;
public enum ItemCategory
{
    [InspectorName("Không phân loại")]
    None,
    [InspectorName("Công cụ")]
    Tool,          
    [InspectorName("Hạt giống")]
    Seed,      
    [InspectorName("Nông sản")]
    Crop,         
    [InspectorName("Đồ ăn")]
    Food,        
    [InspectorName("Vật liệu")]
    Material,      
    [InspectorName("Vũ khí")]
    Weapon,        
    [InspectorName("Trang bị")]
    Armor,          
    [InspectorName("Phụ kiện")]
    Accessory,     
    [InspectorName("Có thể đặt")]
    Placeable,     
    [InspectorName("Sản phẩm động vật")]
    AnimalProduct, 
    [InspectorName("Tiêu hao")]

    Consumable,    
    [InspectorName("Nhiệm vụ")]
    Quest,         
    [InspectorName("Tiền")]
    Currency,     
    [InspectorName("Khác")]
    Misc           
}
