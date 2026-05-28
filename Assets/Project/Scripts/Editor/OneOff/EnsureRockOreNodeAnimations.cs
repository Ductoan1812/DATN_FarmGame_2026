using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class EnsureRockOreNodeAnimations
{
    private sealed class NodeAnimationConfig
    {
        public string DisplayName;
        public string AnimationFolder;
        public string IdleClipPath;
        public string HitClipPath;
        public string ControllerPath;
        public string PrefabPath;
        public string PromptText;
        public Color BaseColor;
        public Color FlashColor;
        public Vector3 HitFxAnchorLocalPosition;
        public Vector3 DropAnchorLocalPosition;
        public float[] PositionKeys;
        public float[] ScaleXKeys;
        public float[] ScaleYKeys;
    }

    private static readonly NodeAnimationConfig RockConfig = new()
    {
        DisplayName = "RockNode",
        AnimationFolder = "Assets/Project/Animations/WorldEntities/RockNode",
        IdleClipPath = "Assets/Project/Animations/WorldEntities/RockNode/RockNode_Idle.anim",
        HitClipPath = "Assets/Project/Animations/WorldEntities/RockNode/RockNode_Hit.anim",
        ControllerPath = "Assets/Project/Animations/WorldEntities/RockNode/RockNode.controller",
        PrefabPath = "Assets/Project/Prefabs/WorldEntities/RockNode_Base.prefab",
        PromptText = "[RMB] Đập đá",
        BaseColor = new Color(0.55f, 0.57f, 0.62f, 1f),
        FlashColor = new Color(0.95f, 0.9f, 0.8f, 1f),
        HitFxAnchorLocalPosition = new Vector3(0f, 0.45f, 0f),
        DropAnchorLocalPosition = new Vector3(0f, 0.15f, 0f),
        PositionKeys = new[] { 0f, 0f, 0.03f, -0.04f, 0.06f, 0.06f, 0.1f, -0.025f, 0.16f, 0f },
        ScaleXKeys = new[] { 0f, 1f, 0.03f, 1.06f, 0.06f, 0.965f, 0.1f, 1.02f, 0.16f, 1f },
        ScaleYKeys = new[] { 0f, 1f, 0.03f, 0.92f, 0.06f, 1.05f, 0.1f, 0.985f, 0.16f, 1f },
    };

    private static readonly NodeAnimationConfig OreConfig = new()
    {
        DisplayName = "OreNode",
        AnimationFolder = "Assets/Project/Animations/WorldEntities/OreNode",
        IdleClipPath = "Assets/Project/Animations/WorldEntities/OreNode/OreNode_Idle.anim",
        HitClipPath = "Assets/Project/Animations/WorldEntities/OreNode/OreNode_Hit.anim",
        ControllerPath = "Assets/Project/Animations/WorldEntities/OreNode/OreNode.controller",
        PrefabPath = "Assets/Project/Prefabs/WorldEntities/OreNode_Base.prefab",
        PromptText = "[RMB] Khai thác quặng",
        BaseColor = new Color(0.7f, 0.75f, 0.85f, 1f),
        FlashColor = new Color(0.9f, 1f, 1f, 1f),
        HitFxAnchorLocalPosition = new Vector3(0f, 0.52f, 0f),
        DropAnchorLocalPosition = new Vector3(0f, 0.18f, 0f),
        PositionKeys = new[] { 0f, 0f, 0.025f, 0.03f, 0.05f, -0.035f, 0.09f, 0.055f, 0.16f, 0f },
        ScaleXKeys = new[] { 0f, 1f, 0.025f, 1.035f, 0.05f, 0.975f, 0.09f, 1.018f, 0.16f, 1f },
        ScaleYKeys = new[] { 0f, 1f, 0.025f, 0.955f, 0.05f, 1.045f, 0.09f, 0.99f, 0.16f, 1f },
    };

    private static readonly string[] ProceduralHitDisabledEntityPaths =
    {
        "Assets/Project/ScriptableObjects/WorldObjects/Resources/RockNode_01.asset",
        "Assets/Project/ScriptableObjects/WorldObjects/Resources/OreNode_01.asset",
        "Assets/Project/ScriptableObjects/WorldObjects/Resources/OreNode_T1_Copper.asset",
        "Assets/Project/ScriptableObjects/WorldObjects/Resources/OreNode_T2_Iron.asset",
        "Assets/Project/ScriptableObjects/WorldObjects/Resources/OreNode_T3_Silver.asset",
        "Assets/Project/ScriptableObjects/WorldObjects/Resources/OreNode_T4_Gold.asset",
        "Assets/Project/ScriptableObjects/WorldObjects/Resources/OreNode_T5_Mythril.asset",
    };

    [MenuItem("Tools/DATN/One-off Setup/Animation/Ensure Rock Ore Node Animations")]
    public static void RunFromMenu()
    {
        Run();
    }

    public static void Run()
    {
        EnsureNode(RockConfig);
        EnsureNode(OreConfig);
        DisableProceduralHitMotion();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[EnsureRockOreNodeAnimations] RockNode and OreNode animation assets and prefabs are ready.");
    }

    public static string RunPrefabSmokeTest()
    {
        var results = new List<string>();
        foreach (var config in new[] { RockConfig, OreConfig })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(config.PrefabPath);
            if (prefab == null)
            {
                results.Add($"{config.DisplayName}: missing prefab");
                continue;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                results.Add($"{config.DisplayName}: instantiate failed");
                continue;
            }

            instance.name = $"Playtest_{config.DisplayName}";
            instance.SetActive(true);
            instance.transform.position = config == RockConfig ? new Vector3(-2f, -1f, 0f) : new Vector3(0.25f, -1f, 0f);

            var renderer = instance.GetComponentInChildren<SpriteRenderer>(true);
            var animator = instance.GetComponentInChildren<Animator>(true);
            var reaction = instance.GetComponent<ResourceHitReactionObject>();
            reaction?.PlayHit();

            string controllerName = animator != null && animator.runtimeAnimatorController != null
                ? animator.runtimeAnimatorController.name
                : "null";

            results.Add(
                $"{config.DisplayName}: active={instance.activeSelf}; visual={renderer?.gameObject.name ?? "null"}; animator={controllerName}; prompt={(instance.GetComponent<InteractablePrompt>() != null)}; reaction={(reaction != null)}");
        }

        var report = string.Join(" | ", results);
        Debug.Log("[EnsureRockOreNodeAnimations] SmokeTest => " + report);
        return report;
    }

    private static void EnsureNode(NodeAnimationConfig config)
    {
        EnsureFolder(config.AnimationFolder);

        var idleClip = CreateOrUpdateIdleClip(config);
        var hitClip = CreateOrUpdateHitClip(config);
        var controller = CreateOrUpdateController(config, idleClip, hitClip);
        PatchPrefab(config, controller);
    }

    private static AnimationClip CreateOrUpdateIdleClip(NodeAnimationConfig config)
    {
        var clip = LoadOrCreateClip(config.IdleClipPath, $"{config.DisplayName}_Idle");
        clip.ClearCurves();
        clip.frameRate = 60f;

        SetCurve(clip, typeof(Transform), "m_LocalPosition.x", Constant(0f, 0f));
        SetCurve(clip, typeof(Transform), "m_LocalPosition.y", Constant(0f, 0f));
        SetCurve(clip, typeof(Transform), "m_LocalScale.x", Constant(0f, 1f));
        SetCurve(clip, typeof(Transform), "m_LocalScale.y", Constant(0f, 1f));
        SetCurve(clip, typeof(Transform), "m_LocalScale.z", Constant(0f, 1f));
        SetCurve(clip, typeof(SpriteRenderer), "m_Color.r", Constant(0f, config.BaseColor.r));
        SetCurve(clip, typeof(SpriteRenderer), "m_Color.g", Constant(0f, config.BaseColor.g));
        SetCurve(clip, typeof(SpriteRenderer), "m_Color.b", Constant(0f, config.BaseColor.b));
        SetCurve(clip, typeof(SpriteRenderer), "m_Color.a", Constant(0f, config.BaseColor.a));
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimationClip CreateOrUpdateHitClip(NodeAnimationConfig config)
    {
        var clip = LoadOrCreateClip(config.HitClipPath, $"{config.DisplayName}_Hit");
        clip.ClearCurves();
        clip.frameRate = 60f;

        SetCurve(clip, typeof(Transform), "m_LocalPosition.x", Ease(config.PositionKeys));
        SetCurve(clip, typeof(Transform), "m_LocalScale.x", Ease(config.ScaleXKeys));
        SetCurve(clip, typeof(Transform), "m_LocalScale.y", Ease(config.ScaleYKeys));
        SetCurve(clip, typeof(SpriteRenderer), "m_Color.r", Ease(
            0f, config.BaseColor.r,
            0.03f, config.FlashColor.r,
            0.08f, (config.BaseColor.r + config.FlashColor.r) * 0.5f,
            0.16f, config.BaseColor.r));
        SetCurve(clip, typeof(SpriteRenderer), "m_Color.g", Ease(
            0f, config.BaseColor.g,
            0.03f, config.FlashColor.g,
            0.08f, (config.BaseColor.g + config.FlashColor.g) * 0.5f,
            0.16f, config.BaseColor.g));
        SetCurve(clip, typeof(SpriteRenderer), "m_Color.b", Ease(
            0f, config.BaseColor.b,
            0.03f, config.FlashColor.b,
            0.08f, (config.BaseColor.b + config.FlashColor.b) * 0.5f,
            0.16f, config.BaseColor.b));
        SetCurve(clip, typeof(SpriteRenderer), "m_Color.a", Constant(0f, config.BaseColor.a));
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CreateOrUpdateController(NodeAnimationConfig config, AnimationClip idleClip, AnimationClip hitClip)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(config.ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(config.ControllerPath);

        while (controller.parameters.Length > 0)
            controller.RemoveParameter(controller.parameters[0]);

        if (controller.layers.Length == 0)
            controller.AddLayer("Base Layer");

        var stateMachine = controller.layers[0].stateMachine;
        while (stateMachine.states.Length > 0)
            stateMachine.RemoveState(stateMachine.states[0].state);
        while (stateMachine.anyStateTransitions.Length > 0)
            stateMachine.RemoveAnyStateTransition(stateMachine.anyStateTransitions[0]);

        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);

        var idleState = stateMachine.AddState("Idle");
        idleState.motion = idleClip;
        idleState.writeDefaultValues = true;
        stateMachine.defaultState = idleState;

        var hitState = stateMachine.AddState("Hit");
        hitState.motion = hitClip;
        hitState.speed = 1f;
        hitState.writeDefaultValues = true;

        var anyToHit = stateMachine.AddAnyStateTransition(hitState);
        anyToHit.hasExitTime = false;
        anyToHit.hasFixedDuration = true;
        anyToHit.duration = 0.02f;
        anyToHit.canTransitionToSelf = false;
        anyToHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");

        var hitToIdle = hitState.AddTransition(idleState);
        hitToIdle.hasExitTime = true;
        hitToIdle.exitTime = 0.95f;
        hitToIdle.hasFixedDuration = true;
        hitToIdle.duration = 0.04f;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void PatchPrefab(NodeAnimationConfig config, AnimatorController controller)
    {
        var root = PrefabUtility.LoadPrefabContents(config.PrefabPath);
        if (root == null)
            throw new FileNotFoundException($"Missing prefab at {config.PrefabPath}");

        try
        {
            var visualRoot = EnsureChild(root.transform, "VisualRoot", Vector3.zero);
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;
            visualRoot.gameObject.layer = root.layer;

            var originalRenderer = root.GetComponent<SpriteRenderer>();
            var visualRenderer = visualRoot.GetComponent<SpriteRenderer>();
            if (visualRenderer == null)
                visualRenderer = visualRoot.gameObject.AddComponent<SpriteRenderer>();

            if (originalRenderer != null)
            {
                EditorUtility.CopySerialized(originalRenderer, visualRenderer);
                UnityEngine.Object.DestroyImmediate(originalRenderer, true);
            }

            var animator = visualRoot.GetComponent<Animator>();
            if (animator == null)
                animator = visualRoot.gameObject.AddComponent<Animator>();

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var reaction = root.GetComponent<ResourceHitReactionObject>();
            if (reaction == null)
                reaction = root.AddComponent<ResourceHitReactionObject>();

            var prompt = root.GetComponent<InteractablePrompt>();
            prompt?.SetPromptText(config.PromptText);

            EnsureChild(root.transform, "HitFxAnchor", config.HitFxAnchorLocalPosition).gameObject.layer = root.layer;
            EnsureChild(root.transform, "DropAnchor", config.DropAnchorLocalPosition).gameObject.layer = root.layer;

            var serialized = new SerializedObject(reaction);
            serialized.FindProperty("targetRenderer").objectReferenceValue = visualRenderer;
            serialized.FindProperty("targetAnimator").objectReferenceValue = animator;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, config.PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void DisableProceduralHitMotion()
    {
        foreach (var assetPath in ProceduralHitDisabledEntityPaths)
        {
            var entity = AssetDatabase.LoadAssetAtPath<EntityData>(assetPath);
            if (entity == null || entity.modules == null)
                continue;

            bool changed = false;
            foreach (var module in entity.modules)
            {
                if (module is not ResourceHitReactionModule reactionModule)
                    continue;

                if (reactionModule.useProceduralMotion)
                {
                    reactionModule.useProceduralMotion = false;
                    changed = true;
                }

                if (!string.Equals(reactionModule.animatorHitTrigger, "Hit", StringComparison.Ordinal))
                {
                    reactionModule.animatorHitTrigger = "Hit";
                    changed = true;
                }
            }

            if (changed)
                EditorUtility.SetDirty(entity);
        }
    }

    private static Transform EnsureChild(Transform parent, string childName, Vector3 localPosition)
    {
        var child = parent.Find(childName);
        if (child == null)
        {
            var childObject = new GameObject(childName);
            child = childObject.transform;
            child.SetParent(parent, false);
        }

        child.localPosition = localPosition;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;
        return child;
    }

    private static AnimationClip LoadOrCreateClip(string path, string clipName)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip != null)
            return clip;

        clip = new AnimationClip { name = clipName };
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void SetCurve(AnimationClip clip, Type type, string propertyName, AnimationCurve curve)
    {
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, type, propertyName), curve);
    }

    private static AnimationCurve Constant(float time, float value)
    {
        return AnimationCurve.Constant(time, time + 1f / 60f, value);
    }

    private static AnimationCurve Ease(params float[] keyPairs)
    {
        var keyframes = new Keyframe[keyPairs.Length / 2];
        for (int index = 0; index < keyframes.Length; index++)
            keyframes[index] = new Keyframe(keyPairs[index * 2], keyPairs[index * 2 + 1]);

        return new AnimationCurve(keyframes);
    }

    private static void EnsureFolder(string assetFolder)
    {
        var normalized = assetFolder.Replace("\\", "/");
        var parts = normalized.Split('/');
        string current = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string next = current + "/" + parts[index];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[index]);
            current = next;
        }
    }
}
