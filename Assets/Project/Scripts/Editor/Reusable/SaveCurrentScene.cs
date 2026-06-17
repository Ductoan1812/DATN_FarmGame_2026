using UnityEditor;
using UnityEditor.SceneManagement;

public static class SaveCurrentScene
{
    public static void Execute()
    {
        EditorSceneManager.SaveOpenScenes();
        UnityEngine.Debug.Log("[SaveCurrentScene] Saved.");
    }
}
