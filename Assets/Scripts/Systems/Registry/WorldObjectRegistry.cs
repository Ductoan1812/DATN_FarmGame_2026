using System.Collections.Generic;
using UnityEngine;
public class WorldObjectRegistry
{
    private Dictionary<ObjectType, WorldObjectDefinition> map = new();

    public void RegisterAll(WorldObjectDefinition[] defs)
    {
        foreach (var d in defs) map[d.idObject] = d;
    }

    public GameObject GetPrefab(ObjectType typeId)
        => map.TryGetValue(typeId, out var def) ? def.prefab : null;

    public IEnumerable<ObjectType> GetAllIds() => map.Keys;

    public bool Has(ObjectType typeId) => map.ContainsKey(typeId);

    public List<ObjectType> Search(ObjectType query)
    {
        var result = new List<ObjectType>();
        if (map.ContainsKey(query))
            result.Add(query);
        return result;
    }
}