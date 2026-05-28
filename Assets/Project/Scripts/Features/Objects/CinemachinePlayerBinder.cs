using Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
public class CinemachinePlayerBinder : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float retrySeconds = 0.5f;
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 0f, -10f);

    private float nextRetryTime;
    private CinemachineVirtualCamera virtualCamera;
    private Transform boundTarget;

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        EnsureBody();
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

        var player = FindAnyObjectByType<PlayerControler>();
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
}
