using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Starter Loadout", fileName = "StarterLoadout")]
public class StarterLoadoutData : ScriptableObject
{
    public string startSpawnPointId = SceneSpawnResolver.DefaultPlayerSpawnPointId;
    public int initialMoney = 500;
    public int selectedHotbarIndex = 0;
    public StarterLoadoutEntry[] entries = Array.Empty<StarterLoadoutEntry>();
}

[Serializable]
public class StarterLoadoutEntry
{
    public InventoryType inventoryType = InventoryType.Hotbar;
    [Min(0)] public int slotIndex;
    public EntityData itemData;
    [Min(1)] public int amount = 1;
}
