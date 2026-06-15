using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EntityRoot))]
public class SceneSleepPoint2D : MonoBehaviour
{
    private const string DefaultBedEntityDataPath = "Data/Entities/World/Utility/world_utility_bed_basic";

    [Header("Entity")]
    [SerializeField] private EntityData sleepEntityData;
    [SerializeField] private string fallbackEntityDataResourcePath = DefaultBedEntityDataPath;

    [Header("Interaction")]
    [SerializeField] private InteractablePrompt prompt;
    [SerializeField] private string promptFormat = "[{0}] Ngu";

    private EntityRoot _entityRoot;
    private string _lastPromptText;

    private void Reset()
    {
        _entityRoot = GetComponent<EntityRoot>();
        prompt = GetComponent<InteractablePrompt>();
        EnsureTriggerCollider();
    }

    private void Awake()
    {
        _entityRoot = GetComponent<EntityRoot>();
        if (prompt == null)
            prompt = GetComponent<InteractablePrompt>();

        EnsureTriggerCollider();
        EnsureEntityRuntime();
        RefreshPromptText(force: true);
    }

    private void Update()
    {
        RefreshPromptText(force: false);
    }

    private void EnsureEntityRuntime()
    {
        if (_entityRoot == null || _entityRoot.GetEntity() != null)
            return;

        var entityData = sleepEntityData != null
            ? sleepEntityData
            : Resources.Load<EntityData>(fallbackEntityDataResourcePath);

        if (entityData == null)
        {
            Debug.LogWarning($"[SceneSleepPoint2D] Missing EntityData at '{fallbackEntityDataResourcePath}' on '{name}'.", this);
            return;
        }

        sleepEntityData = entityData;
        _entityRoot.Add(new EntityRuntime(entityData));
    }

    private void RefreshPromptText(bool force)
    {
        if (prompt == null)
            return;

        string text = string.Format(
            promptFormat,
            GameplayInputSettings.FormatKey(GameplayInputSettings.GetInteractKey()));

        if (!force && string.Equals(text, _lastPromptText))
            return;

        _lastPromptText = text;
        prompt.SetPromptText(text);
    }

    private void EnsureTriggerCollider()
    {
        var col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }
}
