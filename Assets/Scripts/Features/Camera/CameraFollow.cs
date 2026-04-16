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

    private void Start()
    {
        if (target == null)
        {
            target = GameObject.FindGameObjectWithTag(targetTag)?.transform;
        }

        if (target == null)
        {
            Debug.LogError("CameraFollow: Target not found.");
            return;
        }

        if (playIntroOnStart)
        {
            transform.position = target.position + introOffset;
            return;
        }

        transform.position = target.position + offset;
        introFinished = true;
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
}
