using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FixHomePortalPlacement
{
    public static void Execute()
    {
        var activeScene = SceneManager.GetActiveScene();
        var root = GameObject.Find("Door_InOut_LocalPortal");
        if (root == null || root.scene != activeScene)
        {
            Debug.LogWarning("[FixHomePortalPlacement] Door_InOut_LocalPortal not found in active scene.");
            return;
        }

        var insideTrigger = root.transform.Find("InsideTrigger");
        var insideSpawn = root.transform.Find("InsideSpawnPoint");
        if (insideTrigger == null || insideSpawn == null)
        {
            Debug.LogWarning("[FixHomePortalPlacement] Missing InsideTrigger or InsideSpawnPoint.");
            return;
        }

        Undo.RecordObjects(new Object[] { insideTrigger, insideSpawn }, "Fix Home Portal Placement");

        insideTrigger.position = new Vector3(-47.01f, 104.89f, insideTrigger.position.z);
        insideSpawn.position = new Vector3(-48.14f, 105.87f, insideSpawn.position.z);

        EditorSceneManager.MarkSceneDirty(activeScene);
        Debug.Log($"[FixHomePortalPlacement] InsideTrigger -> {insideTrigger.position}, InsideSpawnPoint -> {insideSpawn.position}");
    }
}
