using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Data/Scene Spawn Tile", fileName = "SceneSpawnTile")]
public class SceneSpawnTile : TileBase
{
    public SceneMarkerKind markerKind = SceneMarkerKind.Object;
    public ObjectType objectType;
    public EntityData entityData;
    public SceneEntitySavePolicy savePolicy = SceneEntitySavePolicy.Persistent;
    public string spawnGroupId;
    public string spawnPointId;
    [Min(0)] public int respawnMinutes;
    [Min(1)] public int initialAmount = 1;
    public bool bypassPlacementValidation;

    [Header("Editor Preview")]
    public Sprite editorSprite;
    public Color editorColor = Color.white;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref UnityEngine.Tilemaps.TileData tileData)
    {
        tileData.sprite = editorSprite;
        tileData.color = editorColor;
        tileData.colliderType = Tile.ColliderType.None;
    }
}
