using UnityEditor.SceneManagement;
using UnityEngine;

public static class SceneUiProbeExec
{
    public static string OpenScene(string scenePath)
    {
        if (string.IsNullOrWhiteSpace(scenePath))
            return "[SceneUiProbeExec] scenePath is empty.";

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
            return $"[SceneUiProbeExec] Failed to open scene: {scenePath}";

        return $"[SceneUiProbeExec] Opened scene: {scene.path}";
    }
}
