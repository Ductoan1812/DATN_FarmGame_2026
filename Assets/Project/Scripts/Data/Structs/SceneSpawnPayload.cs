using UnityEngine;

[System.Serializable]
public class SceneSpawnPayload
{
    public string sceneName;
    public SceneMarkerKind markerKind = SceneMarkerKind.Object;
    public ObjectType objectType;
    public Vector3Int cell;
    public string spawnGroupId;
    public string persistentId;
    public SceneEntitySavePolicy savePolicy = SceneEntitySavePolicy.Persistent;
    public int respawnMinutes;
    public int initialAmount = 1;
    public int availableAtGameMinute;

    public static string BuildPersistentId(
        string sceneName,
        SceneMarkerKind markerKind,
        ObjectType objectType,
        Vector3Int cell,
        string spawnGroupId)
    {
        string safeScene = string.IsNullOrWhiteSpace(sceneName) ? "Scene" : sceneName.Trim();
        string safeGroup = string.IsNullOrWhiteSpace(spawnGroupId) ? "default" : spawnGroupId.Trim();
        return $"{safeScene}:{markerKind}:{objectType}:{cell.x}_{cell.y}_{cell.z}:{safeGroup}";
    }
}
