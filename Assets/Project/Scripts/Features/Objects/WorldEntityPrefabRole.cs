using UnityEngine;

public enum WorldEntityPrefabRoleType
{
    Unknown,
    Player,
    Npc,
    Enemy,
    Resource,
    Crop,
    Drop
}

[DisallowMultipleComponent]
public class WorldEntityPrefabRole : MonoBehaviour
{
    public WorldEntityPrefabRoleType role = WorldEntityPrefabRoleType.Unknown;
}
