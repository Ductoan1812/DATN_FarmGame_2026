using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScenePortalTrigger2D), true)]
public class ScenePortalTrigger2DEditor : Editor
{
    private SerializedProperty modeProp;
    private SerializedProperty spawnPointIdProp;
    private SerializedProperty targetSceneNameProp;
    private SerializedProperty targetSpawnPointIdProp;
    private SerializedProperty saveBeforeTransitionProp;
    private SerializedProperty hideRenderersOnPlayProp;
    private SerializedProperty cooldownSecondsProp;

    private void OnEnable()
    {
        modeProp = serializedObject.FindProperty("mode");
        spawnPointIdProp = serializedObject.FindProperty("_spawnPointId");
        targetSceneNameProp = serializedObject.FindProperty("targetSceneName");
        targetSpawnPointIdProp = serializedObject.FindProperty("targetSpawnPointId");
        saveBeforeTransitionProp = serializedObject.FindProperty("saveBeforeTransition");
        hideRenderersOnPlayProp = serializedObject.FindProperty("hideRenderersOnPlay");
        cooldownSecondsProp = serializedObject.FindProperty("cooldownSeconds");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var portal = (ScenePortalTrigger2D)target;
        bool forcedEntry = portal is SceneSpawnPoint;

        EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
        DrawModeField(forcedEntry);

        var mode = (ScenePortalPointMode)modeProp.enumValueIndex;
        EditorGUILayout.Space(4f);

        if (mode == ScenePortalPointMode.Entry)
            DrawEntryFields();
        else
            DrawExitFields();

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Trigger", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(hideRenderersOnPlayProp, new GUIContent("Hide Renderers On Play"));
        EditorGUILayout.PropertyField(cooldownSecondsProp, new GUIContent("Cooldown Seconds"));

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawModeField(bool forcedEntry)
    {
        using (new EditorGUI.DisabledScope(forcedEntry))
        {
            EditorGUILayout.PropertyField(modeProp, new GUIContent("Mode"));
        }

        if (forcedEntry)
            EditorGUILayout.HelpBox("Legacy entry alias. Component này luôn hoạt động như Entry.", MessageType.None);
    }

    private void DrawEntryFields()
    {
        EditorGUILayout.LabelField("Entry Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(spawnPointIdProp, new GUIContent("Entry Id"));
    }

    private void DrawExitFields()
    {
        EditorGUILayout.LabelField("Exit Settings", EditorStyles.boldLabel);
        DrawScenePopup();
        EditorGUILayout.PropertyField(targetSpawnPointIdProp, new GUIContent("Target Entry Id"));
        EditorGUILayout.PropertyField(saveBeforeTransitionProp, new GUIContent("Save Before Transition"));
    }

    private void DrawScenePopup()
    {
        var sceneNames = GetBuildSceneNames();
        if (sceneNames.Count == 0)
        {
            EditorGUILayout.HelpBox("Build Settings chưa có scene nào. Tạm thời vẫn cho nhập tay.", MessageType.Warning);
            EditorGUILayout.PropertyField(targetSceneNameProp, new GUIContent("Target Scene Name"));
            return;
        }

        string current = targetSceneNameProp.stringValue?.Trim() ?? string.Empty;
        int selectedIndex = sceneNames.IndexOf(current);
        if (selectedIndex < 0)
        {
            sceneNames.Insert(0, string.IsNullOrEmpty(current) ? "<Select Scene>" : $"<Missing> {current}");
            selectedIndex = 0;
        }

        int newIndex = EditorGUILayout.Popup("Target Scene", selectedIndex, sceneNames.ToArray());
        string picked = sceneNames[newIndex];
        if (!string.IsNullOrEmpty(picked) && !picked.StartsWith("<"))
            targetSceneNameProp.stringValue = picked;
    }

    private static List<string> GetBuildSceneNames()
    {
        var names = new List<string>();
        var scenes = EditorBuildSettings.scenes;
        if (scenes == null)
            return names;

        for (int i = 0; i < scenes.Length; i++)
        {
            var scene = scenes[i];
            if (scene == null || !scene.enabled || string.IsNullOrWhiteSpace(scene.path))
                continue;

            names.Add(Path.GetFileNameWithoutExtension(scene.path));
        }

        return names;
    }
}
