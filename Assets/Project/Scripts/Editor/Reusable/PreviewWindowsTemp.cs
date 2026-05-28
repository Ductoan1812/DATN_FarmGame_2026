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

    public static void ShowDialoguePanel()
    {
        HideCoreWindows();
        HideNpcPanels();
        SetNpcPanelActive("DialoguePanel", true);
    }

    public static void ShowShopPanel()
    {
        HideCoreWindows();
        HideNpcPanels();
        SetNpcPanelActive("ShopPanel", true);
    }

    public static void ShowQuestPanel()
    {
        HideCoreWindows();
        HideNpcPanels();
        SetNpcPanelActive("QuestPanel", true);
    }

    public static void ShowCraftingPanel()
    {
        HideCoreWindows();
        HideNpcPanels();
        SetNpcPanelActive("CraftingPanel", true);
    }

    public static void HideAll()
    {
        SetActive("QuestWindow", false);
        SetActive("SettingsWindow", false);
        HideNpcPanels();
    }

    private static void HideNpcPanels()
    {
        SetActiveAll("DialoguePanel", false);
        SetActiveAll("ShopPanel", false);
        SetActiveAll("QuestPanel", false);
        SetActiveAll("CraftingPanel", false);
    }

    private static void HideCoreWindows()
    {
        SetActive("BackpackWindow", false);
        SetActive("EquipmentWindow", false);
        SetActive("SkillsWindow", false);
        SetActive("MapWindow", false);
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

    private static void SetActiveAll(string name, bool active)
    {
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            SetActiveAllDeep(root.transform, name, active);
    }

    private static void SetNpcPanelActive(string panelName, bool active)
    {
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var npcRoot = FindDeep(root.transform, "NPCInteractionUI");
            if (npcRoot == null) continue;

            var panel = FindDeep(npcRoot.transform, panelName);
            if (panel != null)
                panel.SetActive(active);
            return;
        }
    }

    private static void SetActiveAllDeep(Transform t, string name, bool active)
    {
        if (t.name == name)
            t.gameObject.SetActive(active);

        for (int i = 0; i < t.childCount; i++)
            SetActiveAllDeep(t.GetChild(i), name, active);
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
