using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Data/Scene Spawn Rule Region Tile", fileName = "SceneSpawnRuleRegionTile")]
public class SceneSpawnRuleRegionTile : TileBase
{
    [Tooltip("Tên logic của vùng. Có thể để trống, scanner sẽ fallback sang tên asset + hash vùng.")]
    public string regionKey;

    public SceneSpawnRuleEntry[] entries;

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
