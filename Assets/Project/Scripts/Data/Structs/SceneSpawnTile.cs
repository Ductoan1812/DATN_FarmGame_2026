using UnityEngine;
using UnityEngine.Tilemaps;

public enum MarkerStageSpawnMode
{
    Default,
    FixedStage,
    RandomRange
}

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

    [Header("Initial Stage")]
    [Tooltip("Cách chọn stage khởi tạo cho entity có StageModule khi seed từ marker.")]
    public MarkerStageSpawnMode stageSpawnMode = MarkerStageSpawnMode.Default;
    [Min(0)] public int fixedStartStageIndex;
    [Min(0)] public int randomStartStageMin;
    [Min(0)] public int randomStartStageMax;

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
