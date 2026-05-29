using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class CinemachinePlayerBinder : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float retrySeconds = 0.5f;
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 0f, -10f);

    private float nextRetryTime;
    private CinemachineVirtualCamera virtualCamera;
    private Transform boundTarget;
    private EventBus eventBus;

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        EnsureBody();
        TryBind();
    }

    private void OnEnable()
    {
        SubscribePlayerReady();
        TryBind();
    }

    private void OnDisable()
    {
        if (eventBus != null)
        {
            eventBus.Unsubscribe<PlayerReadyPublish>(OnPlayerReady);
            eventBus = null;
        }
    }

    private void Update()
    {
        if (Time.time < nextRetryTime)
            return;

        nextRetryTime = Time.time + retrySeconds;
        TryBind();
    }

    private void TryBind()
    {
        if (virtualCamera == null)
            virtualCamera = GetComponent<CinemachineVirtualCamera>();

        if (virtualCamera == null)
            return;

        var player = FindBestPlayerTarget();
        if (player == null || player.transform == boundTarget)
            return;

        EnsureBody();
        virtualCamera.Follow = player.transform;
        boundTarget = player.transform;
    }

    private void EnsureBody()
    {
        if (virtualCamera == null)
            return;

        var transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        if (transposer == null)
            transposer = virtualCamera.AddCinemachineComponent<CinemachineTransposer>();

        transposer.m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace;
        transposer.m_FollowOffset = followOffset;
    }

    private void SubscribePlayerReady()
    {
        if (eventBus != null)
            return;

        eventBus = GameManager.Instance?.EventBus;
        if (eventBus != null)
            eventBus.Subscribe<PlayerReadyPublish>(OnPlayerReady);
    }

    private void OnPlayerReady(PlayerReadyPublish _)
    {
        TryBind();
    }

    private static PlayerControler FindBestPlayerTarget()
    {
        var players = FindObjectsByType<PlayerControler>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (players == null || players.Length == 0)
            return null;

        var activeScene = SceneManager.GetActiveScene();
        for (int i = 0; i < players.Length; i++)
        {
            var player = players[i];
            if (player != null && player.gameObject.scene == activeScene)
                return player;
        }

        return players[0];
    }
}
