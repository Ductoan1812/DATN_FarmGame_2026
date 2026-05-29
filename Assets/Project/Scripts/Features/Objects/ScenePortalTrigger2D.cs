using UnityEngine;

public enum ScenePortalPointMode
{
    Exit = 0,
    Entry = 1
}

[DisallowMultipleComponent]
public class ScenePortalTrigger2D : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private ScenePortalPointMode mode = ScenePortalPointMode.Exit;
    [SerializeField] private string _spawnPointId;

    [Header("Exit Settings")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnPointId;
    [SerializeField] private bool saveBeforeTransition = true;

    [Header("Trigger")]
    [SerializeField] private bool hideRenderersOnPlay = false;
    [SerializeField, Min(0.05f)] private float cooldownSeconds = 0.5f;

    private float nextAllowedTriggerTime;
    private bool requirePlayerExitBeforeNextTrigger;

    public ScenePortalPointMode Mode
    {
        get => mode;
        set => mode = value;
    }

    public bool IsEntryPoint => mode == ScenePortalPointMode.Entry;
    public string SpawnPointId => string.IsNullOrWhiteSpace(_spawnPointId) ? string.Empty : _spawnPointId.Trim();

    // Backward-compatible member name for existing editor/setup scripts.
    public string spawnPointId
    {
        get => _spawnPointId;
        set => _spawnPointId = value;
    }

    protected virtual ScenePortalPointMode? ForcedMode => null;

    private void Reset()
    {
        ApplyForcedMode();
        EnsureExitTriggerState();
    }

    private void OnValidate()
    {
        ApplyForcedMode();
        EnsureExitTriggerState();
    }

    private void Awake()
    {
        ApplyForcedMode();
        EnsureExitTriggerState();

        if (!hideRenderersOnPlay || mode != ScenePortalPointMode.Exit)
            return;

        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTrigger(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryTrigger(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerControler>() != null)
            requirePlayerExitBeforeNextTrigger = false;
    }

    private void TryTrigger(Collider2D other)
    {
        if (mode != ScenePortalPointMode.Exit)
            return;

        if (Time.time < nextAllowedTriggerTime)
            return;

        var player = other.GetComponentInParent<PlayerControler>();
        if (player == null)
            return;

        if (requirePlayerExitBeforeNextTrigger)
            return;

        if (string.IsNullOrWhiteSpace(targetSceneName))
            return;

        if (SceneTransitionService.ArePortalTriggersSuppressed())
        {
            requirePlayerExitBeforeNextTrigger = true;
            return;
        }

        var interactor = player.GetComponentInParent<EntityRoot>()?.GetEntity();
        nextAllowedTriggerTime = Time.time + cooldownSeconds;
        requirePlayerExitBeforeNextTrigger = true;

        bool requested = SceneTransitionService.RequestTransition(
            interactor,
            targetSceneName,
            targetSpawnPointId,
            saveBeforeTransition);

        if (!requested)
            nextAllowedTriggerTime = Time.time + 0.1f;
    }

    private void ApplyForcedMode()
    {
        if (ForcedMode.HasValue)
            mode = ForcedMode.Value;
    }

    private void EnsureExitTriggerState()
    {
        if (mode != ScenePortalPointMode.Exit)
            return;

        var trigger = GetComponent<Collider2D>();
        if (trigger != null)
            trigger.isTrigger = true;
    }
}

[AddComponentMenu("")]
public class SceneSpawnPoint : ScenePortalTrigger2D
{
    protected override ScenePortalPointMode? ForcedMode => ScenePortalPointMode.Entry;
}
