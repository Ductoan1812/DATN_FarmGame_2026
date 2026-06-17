using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float smoothSpeed = 8f;

    [Header("Intro")]
    [SerializeField] private bool playIntroOnStart = true;
    [SerializeField] private float introDuration = 2f;
    [SerializeField] private Vector3 introOffset = new Vector3(0f, 0f, -10f);

    [Header("Hit Shake")]
    [SerializeField] private float shakeIntensity = 0.08f;

    private float introTimer;
    private bool introFinished;
    private EventBus eventBus;
    private float shakeUntilRealtime;
    private float shakeStartedRealtime;
    private float shakeDuration;
    private float activeShakeIntensity;

    private void OnEnable()
    {
        SubscribePlayerReady();
    }

    private void Start()
    {
        SubscribePlayerReady();

        if (target != null)
        {
            InitializeFollow();
        }
        else
        {
            TryBindTargetFromTag(false);
        }
    }

    private void OnDisable()
    {
        if (eventBus != null)
        {
            eventBus.Unsubscribe<PlayerReadyPublish>(OnPlayerReady);
            eventBus = null;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        if (!introFinished && playIntroOnStart)
        {
            PlayIntro();
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition + CalculateShakeOffset();
    }

    private void PlayIntro()
    {
        float safeDuration = Mathf.Max(0.01f, introDuration);
        introTimer += Time.deltaTime;
        float t = Mathf.Clamp01(introTimer / safeDuration);

        Vector3 nearPosition = target.position + introOffset;
        Vector3 farPosition = target.position + offset;
        transform.position = Vector3.Lerp(nearPosition, farPosition, t);

        if (t >= 1f)
        {
            introFinished = true;
        }
    }

    private void SubscribePlayerReady()
    {
        if (eventBus != null)
        {
            return;
        }

        eventBus = GameManager.Instance?.EventBus;
        if (eventBus != null)
        {
            eventBus.Subscribe<PlayerReadyPublish>(OnPlayerReady);
        }
    }

    private void OnPlayerReady(PlayerReadyPublish _)
    {
        TryBindTargetFromTag(true);
    }

    private void TryBindTargetFromTag(bool logWarning)
    {
        var player = FindBestPlayerTarget();
        if (player != null)
        {
            target = player.transform;
        }
        else
        {
            target = GameObject.FindGameObjectWithTag(targetTag)?.transform;
        }

        if (target == null)
        {
            if (logWarning)
            {
                Debug.LogWarning($"CameraFollow: Target with tag '{targetTag}' not found after PlayerReadyPublish.");
            }

            return;
        }

        InitializeFollow();
    }

    private void InitializeFollow()
    {
        introTimer = 0f;
        introFinished = false;

        if (playIntroOnStart)
        {
            transform.position = target.position + introOffset;
            return;
        }

        transform.position = target.position + offset;
        introFinished = true;
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

    public void Shake(float intensity, float duration)
    {
        activeShakeIntensity = Mathf.Max(activeShakeIntensity, intensity > 0f ? intensity : shakeIntensity);
        shakeDuration = Mathf.Max(0.01f, duration);
        shakeStartedRealtime = Time.realtimeSinceStartup;
        shakeUntilRealtime = shakeStartedRealtime + shakeDuration;
    }

    private Vector3 CalculateShakeOffset()
    {
        if (Time.realtimeSinceStartup >= shakeUntilRealtime || activeShakeIntensity <= 0f)
            return Vector3.zero;

        float elapsed = Time.realtimeSinceStartup - shakeStartedRealtime;
        float t = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.01f, shakeDuration));
        Vector2 offset2D = Random.insideUnitCircle * (activeShakeIntensity * t);
        return new Vector3(offset2D.x, offset2D.y, 0f);
    }
}
