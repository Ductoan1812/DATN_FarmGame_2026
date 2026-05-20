using UnityEngine;

public class EntityPosition
{
    public string     idRuntime;       // GUID khớp EntityRuntime.Id
    public ObjectType idPrefab;        // prefab key để re-spawn khi load
    public Vector2    pos;             // world position (XZ plane)
    public Vector2Int[] occupiedCells; // các ô tile entity chiếm
    public EntityLayer  layer;         // layer mà entity chiếm tại cell

    // Scene marker/session metadata. Empty persistentId means this entity was not seeded from a marker.
    public string persistentId;
    public SceneEntitySavePolicy savePolicy = SceneEntitySavePolicy.Persistent;
    public string spawnGroupId;
    public int respawnMinutes;
    public int initialAmount = 1;
    public int availableAtGameMinute;
}
