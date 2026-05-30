using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InspectScenePortals
{
    public static void Execute()
    {
        InspectScene("Assets/Project/Scenes/Coreplay/FarmScene.unity");
        InspectScene("Assets/Project/Scenes/Coreplay/TownScene.unity");
        InspectScene("Assets/Project/Scenes/Coreplay/MineScene.unity");
    }

    private static void InspectScene(string path)
    {
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Debug.Log($"=== Inspecting Scene: {scene.name} ===");
        var triggers = Object.FindObjectsByType<ScenePortalTrigger2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in triggers)
        {
            Debug.Log($"GameObject: {GetPath(t.transform)}, Position: {t.transform.position}, Type: {t.GetType().Name}, Mode: {t.Mode}, SpawnPointId: {t.SpawnPointId}");
        }
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
