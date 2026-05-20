using UnityEngine;

public static class PreviewWindowsTemp
{
    public static void ShowQuest()
    {
        SetActive("SettingsWindow", false);
        SetActive("QuestWindow", true);
    }

    public static void ShowSettings()
    {
        SetActive("QuestWindow", false);
        SetActive("SettingsWindow", true);
    }

    public static void HideAll()
    {
        SetActive("QuestWindow", false);
        SetActive("SettingsWindow", false);
    }

    private static void SetActive(string name, bool active)
    {
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var go = FindDeep(root.transform, name);
            if (go != null) { go.SetActive(active); return; }
        }
    }

    private static GameObject FindDeep(Transform t, string name)
    {
        if (t.name == name) return t.gameObject;
        for (int i = 0; i < t.childCount; i++)
        {
            var r = FindDeep(t.GetChild(i), name);
            if (r != null) return r;
        }
        return null;
    }
}
