using UnityEngine;
using UnityEditor;

public class CheckPlayerStats
{
    public static void Execute()
    {
        var player = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Project/ScriptableObjects/Characters/Player/Player.asset");
        if (player == null) { Debug.LogError("Player asset not found!"); return; }

        var stats = player.baseStats;
        if (stats == null || stats.baseStats == null || stats.baseStats.Count == 0)
        {
            Debug.LogWarning("Player baseStats is NULL or EMPTY! Đây là lý do stamina = 0.");
            return;
        }

        Debug.Log($"Player baseStats có {stats.baseStats.Count} entries:");
        foreach (var entry in stats.baseStats)
        {
            Debug.Log($"  {entry.statType} = {entry.value}");
        }
    }
}
