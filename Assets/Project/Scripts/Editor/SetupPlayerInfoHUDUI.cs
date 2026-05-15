using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupPlayerInfoHUDUI
{
    [MenuItem("Tools/DATN/UI/Setup Player Info HUD")]
    public static void Execute()
    {
        var infoPlayer = FindSceneObject("InfoPlayer");
        if (infoPlayer == null)
        {
            Debug.LogError("[SetupPlayerInfoHUDUI] Không tìm thấy InfoPlayer trong scene.");
            return;
        }

        var ui = infoPlayer.GetComponent<PlayerInfoHUDUI>();
        if (ui == null)
            ui = Undo.AddComponent<PlayerInfoHUDUI>(infoPlayer);

        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(infoPlayer.scene);
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[SetupPlayerInfoHUDUI] PlayerInfoHUDUI đã được gắn vào InfoPlayer.");
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name == objectName && go.scene.IsValid())
                return go;
        }

        return null;
    }
}
