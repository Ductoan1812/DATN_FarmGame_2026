using UnityEngine;

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

    private float introTimer;
    private bool introFinished;
    private EventBus eventBus;

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
        transform.position = smoothedPosition;
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
        target = GameObject.FindGameObjectWithTag(targetTag)?.transform;

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
}
