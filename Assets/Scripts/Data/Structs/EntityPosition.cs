using UnityEngine;

public class EntityPosition
{
    public string     idRuntime;       // GUID khớp EntityRuntime.Id
    public ObjectType idPrefab;        // prefab key để re-spawn khi load
    public Vector2    pos;             // world position (XZ plane)
    public Vector2Int[] occupiedCells; // các ô tile entity chiếm
    public EntityLayer  layer;         // layer mà entity chiếm tại cell
}
