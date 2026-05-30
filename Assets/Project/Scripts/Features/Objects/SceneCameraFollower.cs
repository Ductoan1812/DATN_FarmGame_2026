using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Camera theo player đơn giản, không phụ thuộc Cinemachine.
/// - Gắn lên Camera GameObject nằm trong GameManager prefab (DontDestroyOnLoad).
/// - Tự tìm Player trong scene hiện tại và follow mượt mà bằng SmoothDamp.
/// - Không bị mất khi chuyển scene vì camera đi theo GameManager.
/// - Thay thế hoàn toàn CinemachineBrain + CinemachineVirtualCamera + CinemachinePlayerBinder.
/// </summary>
[RequireComponent(typeof(Camera))]
public class SceneCameraFollower : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float retryInterval = 0.5f;

    [Header("Camera Settings")]
    [SerializeField] private bool autoSetOrthographic = true;
    [SerializeField] private float orthographicSize = 6f;

    private Transform followTarget;
    private Vector3 velocity;
    private float nextRetryTime;
    private Camera cam;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        cam = GetComponent<Camera>();
        ApplyCameraSettings();
        TryFindPlayer();
        SubscribeEvents();
    }

    private void OnEnable()
    {
        TryFindPlayer();
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void LateUpdate()
    {
        // Retry tìm player định kỳ nếu chưa có target
        if (followTarget == null)
        {
            if (Time.time >= nextRetryTime)
            {
                nextRetryTime = Time.time + retryInterval;
                TryFindPlayer();
            }
            return;
        }

        // Check player còn tồn tại (có thể đã chết / scene transition)
        if (!followTarget.gameObject || !followTarget.gameObject.activeInHierarchy)
        {
            followTarget = null;
            return;
        }

        // SmoothDamp follow
        Vector3 targetPos = followTarget.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
    }

    // ── Player detection ──────────────────────────────────────────────────

    private void TryFindPlayer()
    {
        var player = FindBestPlayerInActiveScene();
        if (player != null)
        {
            followTarget = player.transform;
            Debug.Log($"[SceneCameraFollower] Bound to player '{player.name}' in scene '{SceneManager.GetActiveScene().name}'.");
        }
    }

    private static PlayerControler FindBestPlayerInActiveScene()
    {
        var players = FindObjectsByType<PlayerControler>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (players == null || players.Length == 0) return null;

        var activeScene = SceneManager.GetActiveScene();
        foreach (var p in players)
        {
            if (p != null && p.gameObject.scene == activeScene)
                return p;
        }

        return players[0]; // fallback
    }

    // ── Event subscriptions ───────────────────────────────────────────────

    private EventBus subscribedBus;

    private void SubscribeEvents()
    {
        if (subscribedBus != null) return;

        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;

        bus.Subscribe<PlayerReadyPublish>(OnPlayerReady);
        bus.Subscribe<GameReadyPublish>(OnGameReady);
        subscribedBus = bus;
    }

    private void UnsubscribeEvents()
    {
        if (subscribedBus == null) return;
        subscribedBus.Unsubscribe<PlayerReadyPublish>(OnPlayerReady);
        subscribedBus.Unsubscribe<GameReadyPublish>(OnGameReady);
        subscribedBus = null;
    }

    private void OnPlayerReady(PlayerReadyPublish _) => TryFindPlayer();
    private void OnGameReady(GameReadyPublish _) => TryFindPlayer();

    // ── Camera settings ───────────────────────────────────────────────────

    private void ApplyCameraSettings()
    {
        if (cam == null) return;
        if (autoSetOrthographic)
        {
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
        }
    }

    /// <summary>
    /// Buộc re-bind player — gọi sau khi scene mới load xong.
    /// </summary>
    public void ForceRebind()
    {
        followTarget = null;
        nextRetryTime = 0f;
        TryFindPlayer();
    }
}
