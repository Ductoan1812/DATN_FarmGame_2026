using UnityEngine;
[CreateAssetMenu(menuName = "Data/WorldObject", fileName = "NewWorldObject")]
public class WorldObjectDefinition : ScriptableObject
{
    public ObjectType idObject;
    public GameObject prefab;
}