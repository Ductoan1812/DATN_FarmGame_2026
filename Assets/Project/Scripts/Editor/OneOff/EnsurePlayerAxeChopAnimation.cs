using System.IO;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.Reflection;
using Object = UnityEngine.Object;

public static class EnsurePlayerAxeChopAnimation
{
    private const string ControllerPath = "Assets/Project/Animations/Player/Controller.controller";
    private const string SourceClipPath = "Assets/Project/Animations/Player/Upper/HoeU.anim";
    private const string OutputClipPath = "Assets/Project/Animations/Player/Upper/AxeChopU.anim";
    private const string PlayerPrefabPath = "Assets/Project/Prefabs/Characters/Player.prefab";
    private const string HumanPrefabPath = "Assets/Plugins/HeroEditor4D/FantasyHeroes/Prefabs/Human.prefab";
    private const string AxeAssetPath = "Assets/Project/ScriptableObjects/Items/Tools/Axe_01.asset";
    private const string PreviewFolder = "Temp/AxeChopPreview";
    private const string TriggerName = "Axe";

    [MenuItem("Tools/DATN/One-off Setup/Animation/Ensure Player Axe Chop Animation")]
    public static void Run()
    {
        var clip = CreateOrUpdateClip();
        PatchController(clip);
        PatchAxeToolData();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[EnsurePlayerAxeChopAnimation] Axe chop clip, controller trigger, and Strike event are ready.");
    }

    [MenuItem("Tools/DATN/One-off Setup/Animation/Preview Axe Chop Down Windup")]
    public static void PreviewDownWindup() => PreviewFrame(Vector2.down, 0.12f);

    [MenuItem("Tools/DATN/One-off Setup/Animation/Preview Axe Chop Down Strike")]
    public static void PreviewDownStrike() => PreviewFrame(Vector2.down, 0.23f);

    [MenuItem("Tools/DATN/One-off Setup/Animation/Preview Axe Chop Up Windup")]
    public static void PreviewUpWindup() => PreviewFrame(Vector2.up, 0.12f);

    [MenuItem("Tools/DATN/One-off Setup/Animation/Preview Axe Chop Up Strike")]
    public static void PreviewUpStrike() => PreviewFrame(Vector2.up, 0.23f);

    [MenuItem("Tools/DATN/One-off Setup/Animation/Open Axe Chop Editing Rig")]
    public static void OpenEditingRig()
    {
        OpenEditingRigWithClip(OutputClipPath);
    }

    [MenuItem("Tools/DATN/One-off Setup/Animation/Open Hoe Editing Rig")]
    public static void OpenHoeEditingRig()
    {
        OpenEditingRigWithClip(SourceClipPath);
    }

    private static void OpenEditingRigWithClip(string clipPath)
    {
        Run();

        if (AnimationMode.InAnimationMode())
            AnimationMode.StopAnimationMode();

        var stage = PrefabStageUtility.OpenPrefab(HumanPrefabPath);
        var preview = stage != null ? stage.prefabContentsRoot : null;
        if (preview == null)
            preview = GetOrCreatePreviewInstance("AxeChopEditing_Player");

        var character = preview.GetComponent<Character4D>() ?? preview.GetComponentInChildren<Character4D>();

        if (character != null)
        {
            character.SetDirection(Vector2.down);
            Selection.activeGameObject = character.gameObject;
        }
        else
        {
            Selection.activeGameObject = preview;
        }

        FocusSceneView(preview.transform.position);
        OpenAnimationWindowWithClip(AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath));

        Debug.Log("[EnsurePlayerAxeChopAnimation] Opened editing rig on Human for clip: " + clipPath);
    }

    [MenuItem("Tools/DATN/One-off Setup/Animation/Report Animation Window State")]
    public static string ReportAnimationWindowState()
    {
        var animationWindowType = Type.GetType("UnityEditor.AnimationWindow, UnityEditor");
        if (animationWindowType == null)
            return "AnimationWindow type not found.";

        var window = EditorWindow.GetWindow(animationWindowType);
        if (window == null)
            return "AnimationWindow not available.";

        var report = new System.Text.StringBuilder();
        report.AppendLine("Window: " + window.titleContent.text);
        report.AppendLine("Selection: " + (Selection.activeGameObject != null ? Selection.activeGameObject.name : "<null>"));
        report.AppendLine("EditorAnimationMode: " + AnimationMode.InAnimationMode());

        var stateProperty = animationWindowType.GetProperty("state", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var state = stateProperty?.GetValue(window);
        if (state == null)
        {
            report.AppendLine("State: <null>");
            var empty = report.ToString();
            Debug.Log("[EnsurePlayerAxeChopAnimation] " + empty);
            return empty;
        }

        foreach (var name in new[]
        {
            "activeRootGameObject",
            "activeGameObject",
            "activeAnimationPlayer",
            "activeAnimationClip",
            "animationClip",
            "previewing",
            "recording",
            "playing",
            "canPreview",
            "canRecord",
            "disabled",
            "refresh",
            "linkedWithSequencer"
        })
        {
            AppendMemberValue(report, state, name);
        }

        report.AppendLine("Interesting members:");
        foreach (var property in state.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (property.Name.IndexOf("play", StringComparison.OrdinalIgnoreCase) >= 0 ||
                property.Name.IndexOf("preview", StringComparison.OrdinalIgnoreCase) >= 0 ||
                property.Name.IndexOf("record", StringComparison.OrdinalIgnoreCase) >= 0 ||
                property.Name.IndexOf("clip", StringComparison.OrdinalIgnoreCase) >= 0 ||
                property.Name.IndexOf("gameObject", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                report.AppendLine("  prop: " + property.Name);
            }
        }

        foreach (var method in state.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (method.Name.IndexOf("play", StringComparison.OrdinalIgnoreCase) >= 0 ||
                method.Name.IndexOf("preview", StringComparison.OrdinalIgnoreCase) >= 0 ||
                method.Name.IndexOf("record", StringComparison.OrdinalIgnoreCase) >= 0 ||
                method.Name.IndexOf("clip", StringComparison.OrdinalIgnoreCase) >= 0 ||
                method.Name.IndexOf("selection", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                report.AppendLine("  method: " + method.Name);
            }
        }

        var text = report.ToString();
        Debug.Log("[EnsurePlayerAxeChopAnimation] AnimationWindow state:\n" + text);
        return text;
    }

    [MenuItem("Tools/DATN/One-off Setup/Animation/Force Animation Preview")]
    public static string ForceAnimationPreview()
    {
        var animationWindowType = Type.GetType("UnityEditor.AnimationWindow, UnityEditor");
        if (animationWindowType == null)
            return "AnimationWindow type not found.";

        var window = EditorWindow.GetWindow(animationWindowType);
        if (window == null)
            return "AnimationWindow not available.";

        var stateProperty = animationWindowType.GetProperty("state", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var state = stateProperty?.GetValue(window);
        if (state == null)
            return "AnimationWindow state is null.";

        InvokeIfExists(state, "StartPreview");
        InvokeIfExists(state, "StartPlayback");

        var report = ReportAnimationWindowState();
        Debug.Log("[EnsurePlayerAxeChopAnimation] ForceAnimationPreview executed.");
        return report;
    }

    [MenuItem("Tools/DATN/One-off Setup/Animation/Render Axe Chop Preview Sheet")]
    public static string RenderPreviewSheet()
    {
        Run();
        Directory.CreateDirectory(PreviewFolder);

        var sheetPath = Path.Combine(PreviewFolder, "axe_chop_down_up_sheet.png").Replace("\\", "/");
        var frames = new[]
        {
            RenderFrame(Vector2.down, 0.12f, "down_windup"),
            RenderFrame(Vector2.down, 0.23f, "down_strike"),
            RenderFrame(Vector2.up, 0.12f, "up_windup"),
            RenderFrame(Vector2.up, 0.23f, "up_strike"),
        };

        const int frameWidth = 320;
        const int frameHeight = 320;
        var sheet = new Texture2D(frameWidth * 2, frameHeight * 2, TextureFormat.RGBA32, false);
        var bg = new Color32(35, 56, 76, 255);
        var pixels = sheet.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = bg;
        sheet.SetPixels32(pixels);

        Blit(frames[0], sheet, 0, frameHeight);
        Blit(frames[1], sheet, frameWidth, frameHeight);
        Blit(frames[2], sheet, 0, 0);
        Blit(frames[3], sheet, frameWidth, 0);
        sheet.Apply();

        File.WriteAllBytes(sheetPath, sheet.EncodeToPNG());
        foreach (var frame in frames)
            Object.DestroyImmediate(frame);
        Object.DestroyImmediate(sheet);

        Debug.Log("[EnsurePlayerAxeChopAnimation] Preview sheet: " + Path.GetFullPath(sheetPath));
        return Path.GetFullPath(sheetPath);
    }

    public static string ReportPreview()
    {
        var preview = GameObject.Find("AxeChopPreview_Player");
        if (preview == null)
        {
            Debug.Log("[EnsurePlayerAxeChopAnimation] Preview report: missing");
            return "missing";
        }

        var renderers = preview.GetComponentsInChildren<SpriteRenderer>(true);
        int enabled = 0;
        Bounds bounds = new Bounds(preview.transform.position, Vector3.zero);
        bool hasBounds = false;
        foreach (var renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            enabled++;
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        string report = $"preview.active={preview.activeInHierarchy}; pos={preview.transform.position}; renderers={renderers.Length}; enabled={enabled}; bounds={bounds.center}/{bounds.size}";
        Debug.Log("[EnsurePlayerAxeChopAnimation] Preview report: " + report);
        return report;
    }

    public static string ReportActiveWeaponRenderers()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null)
            return "missing prefab";

        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
            return "instantiate failed";

        instance.name = "AxeChopReport_Player";
        instance.SetActive(true);

        var character = instance.GetComponentInChildren<Character4D>();
        var report = new System.Text.StringBuilder();
        foreach (var direction in new[] { Vector2.down, Vector2.up, Vector2.left, Vector2.right })
        {
            if (character != null)
                character.SetDirection(direction);

            report.Append($"direction={direction}: ");
            foreach (var renderer in instance.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (!renderer.name.Contains("Weapon")) continue;
                report.Append($"{GetPath(instance.transform, renderer.transform)} pos={renderer.transform.localPosition} rot={renderer.transform.localEulerAngles}; ");
            }
            report.AppendLine();
        }

        Object.DestroyImmediate(instance);
        string text = report.ToString();
        Debug.Log("[EnsurePlayerAxeChopAnimation] Active weapon renderers:\n" + text);
        return text;
    }

    public static string ReportFrontBackPoseDefaults()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null)
            return "missing prefab";

        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
            return "instantiate failed";

        instance.name = "AxeChopDefaults_Player";
        instance.SetActive(true);

        var character = instance.GetComponentInChildren<Character4D>();
        var report = new System.Text.StringBuilder();
        ReportDirectionPose(instance, character, Vector2.down, "Front", report);
        ReportDirectionPose(instance, character, Vector2.up, "Back", report);

        Object.DestroyImmediate(instance);
        string text = report.ToString();
        Debug.Log("[EnsurePlayerAxeChopAnimation] Front/Back defaults:\n" + text);
        return text;
    }

    private static void ReportDirectionPose(GameObject instance, Character4D character, Vector2 direction, string sideName, System.Text.StringBuilder report)
    {
        if (character != null)
            character.SetDirection(direction);

        foreach (string path in new[]
        {
            $"{sideName}/UpperBody",
            $"{sideName}/UpperBody/ArmRAnchor/ArmR",
            $"{sideName}/UpperBody/ArmRAnchor/ArmR/HandR",
            $"{sideName}/UpperBody/ArmRAnchor/ArmR/HandR/PrimaryWeapon",
        })
        {
            var transform = character != null ? character.transform.Find(path) : instance.transform.Find("Human/" + path);
            if (transform == null)
            {
                report.AppendLine($"{path}: missing");
                continue;
            }

            report.AppendLine($"{path}: pos={transform.localPosition}; rot={transform.localEulerAngles}; scale={transform.localScale}");
        }
    }

    private static AnimationClip CreateOrUpdateClip()
    {
        var source = AssetDatabase.LoadAssetAtPath<AnimationClip>(SourceClipPath);
        if (source == null)
            throw new FileNotFoundException($"Missing source clip at {SourceClipPath}");

        AnimationClip clip;
        if (!File.Exists(OutputClipPath))
        {
            if (!AssetDatabase.CopyAsset(SourceClipPath, OutputClipPath))
                throw new IOException($"Could not copy {SourceClipPath} to {OutputClipPath}");
            clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(OutputClipPath);
        }
        else
        {
            clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(OutputClipPath);
        }

        if (clip == null)
            throw new FileNotFoundException($"Missing output clip at {OutputClipPath}");

        clip.name = "AxeChopU";
        clip.frameRate = 60f;

        ApplyDownCurves(clip);
        ApplyUpCurves(clip);
        ApplyStrikeEvents(clip);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void ApplyDownCurves(AnimationClip clip)
    {
        const string body = "Front/UpperBody";
        const string arm = "Front/UpperBody/ArmRAnchor/ArmR";
        const string hand = "Front/UpperBody/ArmRAnchor/ArmR/HandR";
        const string weapon = "Front/UpperBody/ArmRAnchor/ArmR/HandR/PrimaryWeapon";

        SetPos(clip, body, "x", Ease(0f, 0f, 0.1f, -0.012f, 0.18f, -0.024f, 0.23f, 0.02f, 0.32f, 0.006f, 0.44f, 0f));
        SetPos(clip, body, "y", Ease(0f, 0.75f, 0.1f, 0.765f, 0.18f, 0.78f, 0.23f, 0.695f, 0.32f, 0.735f, 0.44f, 0.75f));
        SetRotZ(clip, body, Ease(0f, 0f, 0.1f, 1f, 0.18f, 2.5f, 0.23f, -4.5f, 0.32f, -1f, 0.44f, 0f));

        SetPos(clip, arm, "x", Ease(0f, -0.45f, 0.44f, -0.45f));
        SetPos(clip, arm, "y", Ease(0f, 0.55f, 0.44f, 0.55f));
        SetPos(clip, hand, "x", Ease(0f, -0.01f, 0.44f, -0.01f));
        SetPos(clip, hand, "y", Ease(0f, -0.65f, 0.44f, -0.65f));
        SetRotZ(clip, arm, Ease(0f, 340f, 0.1f, 318f, 0.18f, 292f, 0.23f, 374f, 0.32f, 354f, 0.44f, 340f));
        SetRotZ(clip, hand, Ease(0f, 20f, 0.1f, 50f, 0.18f, 72f, 0.23f, 342f, 0.32f, 5f, 0.44f, 20f));

        SetPos(clip, weapon, "x", Ease(0f, 0f, 0.1f, 0.055f, 0.18f, 0.1f, 0.23f, 0.02f, 0.32f, 0.01f, 0.44f, 0f));
        SetPos(clip, weapon, "y", Ease(0f, 0f, 0.1f, 0.08f, 0.18f, 0.15f, 0.23f, -0.42f, 0.32f, -0.15f, 0.44f, 0f));
        SetRotZ(clip, weapon, Ease(0f, 227f, 0.1f, 250f, 0.18f, 272f, 0.23f, 187f, 0.32f, 210f, 0.44f, 227f));
        SetScale(clip, weapon, "x", Ease(0f, 1f, 0.2f, 1.02f, 0.23f, 1.18f, 0.28f, 1f, 0.42f, 1f));
        SetScale(clip, weapon, "y", Ease(0f, 1f, 0.2f, 0.98f, 0.23f, 0.92f, 0.28f, 1f, 0.42f, 1f));
    }

    private static void ApplyUpCurves(AnimationClip clip)
    {
        const string body = "Back/UpperBody";
        const string arm = "Back/UpperBody/ArmRAnchor/ArmR";
        const string hand = "Back/UpperBody/ArmRAnchor/ArmR/HandR";
        const string weapon = "Back/UpperBody/ArmRAnchor/ArmR/HandR/PrimaryWeapon";

        SetPos(clip, body, "x", Ease(0f, 0f, 0.1f, -0.01f, 0.18f, -0.02f, 0.23f, 0.018f, 0.32f, 0.004f, 0.44f, 0f));
        SetPos(clip, body, "y", Ease(0f, 0.75f, 0.1f, 0.735f, 0.18f, 0.72f, 0.23f, 0.815f, 0.32f, 0.765f, 0.44f, 0.75f));
        SetRotZ(clip, body, Ease(0f, 0f, 0.1f, -1f, 0.18f, -2.5f, 0.23f, 4.5f, 0.32f, 1f, 0.44f, 0f));

        SetPos(clip, arm, "x", Ease(0f, 0.45f, 0.44f, 0.45f));
        SetPos(clip, arm, "y", Ease(0f, 0.55f, 0.44f, 0.55f));
        SetPos(clip, hand, "x", Ease(0f, 0f, 0.44f, 0f));
        SetPos(clip, hand, "y", Ease(0f, -0.65f, 0.44f, -0.65f));
        SetRotZ(clip, arm, Ease(0f, 20f, 0.1f, 42f, 0.18f, 66f, 0.23f, -42f, 0.32f, -15f, 0.44f, 20f));
        SetRotZ(clip, hand, Ease(0f, 330f, 0.1f, 305f, 0.18f, 284f, 0.23f, 372f, 0.32f, 345f, 0.44f, 330f));

        SetPos(clip, weapon, "x", Ease(0f, 0f, 0.1f, 0.05f, 0.18f, 0.09f, 0.23f, 0.015f, 0.32f, 0.01f, 0.44f, 0f));
        SetPos(clip, weapon, "y", Ease(0f, 0f, 0.1f, -0.08f, 0.18f, -0.15f, 0.23f, 0.42f, 0.32f, 0.14f, 0.44f, 0f));
        SetRotZ(clip, weapon, Ease(0f, 70f, 0.1f, 48f, 0.18f, 28f, 0.23f, 128f, 0.32f, 94f, 0.44f, 70f));
        SetScale(clip, weapon, "x", Ease(0f, 1f, 0.2f, 1.02f, 0.23f, 1.16f, 0.28f, 1f, 0.42f, 1f));
        SetScale(clip, weapon, "y", Ease(0f, 1f, 0.2f, 0.98f, 0.23f, 0.92f, 0.28f, 1f, 0.42f, 1f));
    }

    private static void ApplyStrikeEvents(AnimationClip clip)
    {
        AnimationUtility.SetAnimationEvents(clip, new[]
        {
            new AnimationEvent { time = 0.23f, functionName = "CustomEvent", stringParameter = "Strike" },
            new AnimationEvent { time = 0.44f, functionName = "CustomEvent", stringParameter = "End" },
        });
    }

    private static void PatchController(AnimationClip clip)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            throw new FileNotFoundException($"Missing controller at {ControllerPath}");

        if (!HasParameter(controller, TriggerName))
            controller.AddParameter(TriggerName, AnimatorControllerParameterType.Trigger);

        var upperLayer = FindLayer(controller, "Upper") ?? controller.layers[0];
        var stateMachine = upperLayer.stateMachine;
        var state = FindState(stateMachine, TriggerName);
        var legacyState = FindStateIgnoreCase(stateMachine, TriggerName);

        if (state == null && legacyState != null)
        {
            legacyState.name = TriggerName;
            state = legacyState;
        }

        state ??= stateMachine.AddState(TriggerName);
        state.motion = clip;
        state.writeDefaultValues = true;
        state.speed = 1f;

        RemoveInvalidAnyStateTransitions(stateMachine, state, TriggerName);
        RemoveDuplicateStatesFromOtherLayers(controller, upperLayer.name, TriggerName);

        bool hasTransition = false;
        foreach (var transition in stateMachine.anyStateTransitions)
        {
            if (transition.destinationState != state) continue;
            foreach (var condition in transition.conditions)
            {
                if (condition.parameter == TriggerName)
                {
                    hasTransition = true;
                    break;
                }
            }
        }

        if (!hasTransition)
        {
            var transition = stateMachine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.03f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, TriggerName);
        }

        EditorUtility.SetDirty(controller);
    }

    private static void PatchAxeToolData()
    {
        var axe = AssetDatabase.LoadAssetAtPath<EntityData>(AxeAssetPath);
        if (axe?.modules == null) return;

        foreach (var module in axe.modules)
        {
            if (module is not ToolModule tool) continue;
            tool.toolType = ToolType.Axe;
            tool.animTrigger = TriggerName;
            EditorUtility.SetDirty(axe);
            return;
        }
    }

    private static void PreviewFrame(Vector2 direction, float time)
    {
        Run();

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(OutputClipPath);
        if (clip == null)
            throw new FileNotFoundException("Missing player prefab or AxeChopU clip.");

        var instance = GetOrCreatePreviewInstance("AxeChopPreview_Player");

        var character = instance.GetComponentInChildren<Character4D>();
        if (character != null)
            character.SetDirection(direction);

        AnimationMode.StartAnimationMode();
        AnimationMode.SampleAnimationClip(character != null ? character.gameObject : instance, clip, time);
        Selection.activeGameObject = character != null ? character.gameObject : instance;
        FocusSceneView(instance.transform.position);
        OpenAnimationWindowWithClip(clip);

        Debug.Log($"[EnsurePlayerAxeChopAnimation] Preview direction={direction} time={time:0.00}s");
    }

    private static GameObject GetOrCreatePreviewInstance(string previewName)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null)
            throw new FileNotFoundException($"Missing player prefab at {PlayerPrefabPath}");

        var existing = GameObject.Find(previewName);
        if (existing != null)
        {
            existing.hideFlags = HideFlags.None;
            if (PrefabUtility.IsPartOfPrefabInstance(existing))
                PrefabUtility.UnpackPrefabInstance(existing, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            return existing;
        }

        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
            throw new IOException("Could not instantiate player prefab.");

        if (PrefabUtility.IsPartOfPrefabInstance(instance))
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        instance.name = previewName;
        instance.transform.position = Vector3.zero;
        instance.SetActive(true);
        instance.hideFlags = HideFlags.None;
        return instance;
    }

    private static void FocusSceneView(Vector3 position)
    {
        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
            return;

        sceneView.pivot = position + new Vector3(0f, 0.45f, 0f);
        sceneView.size = 2.6f;
        if (!sceneView.in2DMode)
            sceneView.rotation = Quaternion.identity;
        sceneView.Repaint();
    }

    private static void OpenAnimationWindowWithClip(AnimationClip clip)
    {
        var animationWindowType = Type.GetType("UnityEditor.AnimationWindow, UnityEditor");
        if (animationWindowType == null)
            return;

        var window = EditorWindow.GetWindow(animationWindowType);
        window.Show();
        window.Focus();

        if (clip == null)
            return;

        var stateProperty = animationWindowType.GetProperty("state", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var state = stateProperty?.GetValue(window);
        if (state == null)
            return;

        var selectionProperty = state.GetType().GetProperty("selection", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var selectionItem = CreateSelectionItem(Selection.activeGameObject);
        if (selectionProperty != null && selectionProperty.CanWrite && selectionItem != null)
            selectionProperty.SetValue(state, selectionItem);

        InvokeIfExists(state, "OnSelectionChanged");
        InvokeIfExists(state, "OnSelectionUpdated");
        InvokeIfExists(state, "SyncSceneSelection");
        InvokeIfExists(state, "UpdateSelectionFilter");

        var activeAnimationClipProperty = state.GetType().GetProperty("activeAnimationClip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (activeAnimationClipProperty != null && activeAnimationClipProperty.CanWrite)
            activeAnimationClipProperty.SetValue(state, clip);

        var animationClipProperty = state.GetType().GetProperty("animationClip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (animationClipProperty != null && animationClipProperty.CanWrite)
            animationClipProperty.SetValue(state, clip);

        InvokeIfExists(state, "OnSelectionChanged");
        InvokeIfExists(state, "OnSelectionUpdated");
        InvokeIfExists(state, "SyncSceneSelection");
        InvokeIfExists(state, "UpdateSelectionFilter");
    }

    private static void AppendMemberValue(System.Text.StringBuilder report, object target, string name)
    {
        var type = target.GetType();
        var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null)
        {
            object value = null;
            try { value = property.GetValue(target); }
            catch (Exception ex) { value = "<error: " + ex.GetType().Name + ">"; }

            report.AppendLine(name + ": " + FormatValue(value));
            return;
        }

        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
        {
            object value = null;
            try { value = field.GetValue(target); }
            catch (Exception ex) { value = "<error: " + ex.GetType().Name + ">"; }

            report.AppendLine(name + ": " + FormatValue(value));
        }
    }

    private static string FormatValue(object value)
    {
        if (value == null)
            return "<null>";

        if (value is UnityEngine.Object unityObject)
            return unityObject.name + " (" + unityObject.GetType().Name + ")";

        return value.ToString();
    }

    private static void InvokeIfExists(object target, string methodName)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null || method.GetParameters().Length != 0)
            return;

        method.Invoke(target, null);
    }

    private static object CreateSelectionItem(GameObject gameObject)
    {
        if (gameObject == null)
            return null;

        var selectionItemType = Type.GetType("UnityEditorInternal.GameObjectSelectionItem, UnityEditor");
        if (selectionItemType == null)
            return null;

        var createMethod = selectionItemType.GetMethod("Create", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(GameObject) }, null);
        if (createMethod == null)
            return null;

        return createMethod.Invoke(null, new object[] { gameObject });
    }

    private static Texture2D RenderFrame(Vector2 direction, float time, string frameName)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(OutputClipPath);
        if (prefab == null || clip == null)
            throw new FileNotFoundException("Missing player prefab or AxeChopU clip.");

        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
            throw new IOException("Could not instantiate player prefab.");

        instance.name = "AxeChopRender_" + frameName;
        instance.transform.position = new Vector3(500f, 500f, 0f);
        instance.SetActive(true);

        var character = instance.GetComponentInChildren<Character4D>();
        if (character != null)
            character.SetDirection(direction);

        clip.SampleAnimation(character != null ? character.gameObject : instance, time);

        var cameraObject = new GameObject("AxeChopPreviewCamera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.14f, 0.22f, 0.3f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 2.2f;
        camera.transform.position = new Vector3(500f, 500.65f, -10f);

        const int size = 320;
        var renderTexture = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        camera.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;
        camera.Render();

        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        texture.Apply();

        camera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        Object.DestroyImmediate(renderTexture);
        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(instance);
        return texture;
    }

    private static void Blit(Texture2D source, Texture2D target, int targetX, int targetY)
    {
        target.SetPixels(targetX, targetY, source.width, source.height, source.GetPixels());
    }

    private static string GetPath(Transform root, Transform target)
    {
        var stack = new System.Collections.Generic.Stack<string>();
        var current = target;
        while (current != null && current != root)
        {
            stack.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", stack);
    }

    private static bool HasParameter(AnimatorController controller, string name)
    {
        foreach (var parameter in controller.parameters)
        {
            if (parameter.name == name)
                return true;
        }
        return false;
    }

    private static AnimatorState FindState(AnimatorStateMachine stateMachine, string name)
    {
        foreach (var child in stateMachine.states)
        {
            if (child.state != null && child.state.name == name)
                return child.state;
        }
        return null;
    }

    private static AnimatorState FindStateIgnoreCase(AnimatorStateMachine stateMachine, string name)
    {
        foreach (var child in stateMachine.states)
        {
            if (child.state == null)
                continue;

            if (string.Equals(child.state.name, name, StringComparison.OrdinalIgnoreCase))
                return child.state;
        }

        return null;
    }

    private static AnimatorControllerLayer FindLayer(AnimatorController controller, string layerName)
    {
        foreach (var layer in controller.layers)
        {
            if (string.Equals(layer.name, layerName, StringComparison.Ordinal))
                return layer;
        }

        return null;
    }

    private static void RemoveInvalidAnyStateTransitions(AnimatorStateMachine stateMachine, AnimatorState destination, string triggerName)
    {
        var transitions = stateMachine.anyStateTransitions;
        for (int index = transitions.Length - 1; index >= 0; index--)
        {
            var transition = transitions[index];
            if (transition == null || transition.destinationState != destination)
                continue;

            bool hasValidCondition = false;
            foreach (var condition in transition.conditions)
            {
                if (condition.parameter == triggerName)
                {
                    hasValidCondition = true;
                    break;
                }
            }

            if (!hasValidCondition)
                stateMachine.RemoveAnyStateTransition(transition);
        }
    }

    private static void RemoveAnyStateTransitionsTo(AnimatorStateMachine stateMachine, AnimatorState destination)
    {
        if (destination == null)
            return;

        var transitions = stateMachine.anyStateTransitions;
        for (int index = transitions.Length - 1; index >= 0; index--)
        {
            var transition = transitions[index];
            if (transition != null && transition.destinationState == destination)
                stateMachine.RemoveAnyStateTransition(transition);
        }
    }

    private static void RemoveDuplicateStatesFromOtherLayers(AnimatorController controller, string keepLayerName, string stateName)
    {
        foreach (var layer in controller.layers)
        {
            if (string.Equals(layer.name, keepLayerName, StringComparison.Ordinal))
                continue;

            var duplicate = FindStateIgnoreCase(layer.stateMachine, stateName);
            if (duplicate == null)
                continue;

            RemoveAnyStateTransitionsTo(layer.stateMachine, duplicate);
            layer.stateMachine.RemoveState(duplicate);
        }
    }

    private static void SetRotZ(AnimationClip clip, string path, AnimationCurve curve)
    {
        SetCurve(clip, path, typeof(Transform), "localEulerAnglesRaw.z", curve);
    }

    private static void SetPos(AnimationClip clip, string path, string axis, AnimationCurve curve)
    {
        SetCurve(clip, path, typeof(Transform), $"m_LocalPosition.{axis}", curve);
    }

    private static void SetScale(AnimationClip clip, string path, string axis, AnimationCurve curve)
    {
        SetCurve(clip, path, typeof(Transform), $"m_LocalScale.{axis}", curve);
    }

    private static void SetCurve(AnimationClip clip, string path, System.Type type, string propertyName, AnimationCurve curve)
    {
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, propertyName), curve);
    }

    private static AnimationCurve Ease(params float[] keyPairs)
    {
        var keyframes = new Keyframe[keyPairs.Length / 2];
        for (int index = 0; index < keyframes.Length; index++)
        {
            keyframes[index] = new Keyframe(keyPairs[index * 2], keyPairs[index * 2 + 1]);
            keyframes[index].weightedMode = WeightedMode.None;
        }

        var curve = new AnimationCurve(keyframes);
        for (int index = 0; index < curve.length; index++)
            curve.SmoothTangents(index, 0f);
        return curve;
    }
}
