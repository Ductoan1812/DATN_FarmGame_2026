using UnityEngine;

/// <summary>
/// Component làm hiệu ứng sprite (hoặc transform) lên xuống nhịp nhàng (bobbing/floating effect) dùng hàm Sin.
/// Thường dùng cho các item rơi trên đất, mũi tên chỉ hướng, hoặc các sprite trang trí cần gây chú ý.
/// </summary>
public class SpriteBobbing : MonoBehaviour
{
    [Header("Bobbing Settings")]
    [Tooltip("Biên độ dao động (độ cao lên xuống).")]
    [SerializeField] private float amplitude = 0.15f;

    [Tooltip("Tần số dao động (tốc độ lên xuống).")]
    [SerializeField] private float frequency = 2.0f;

    [Tooltip("Hướng dao động. Mặc định là lên xuống (Y-Axis).")]
    [SerializeField] private Vector3 bobbingDirection = Vector3.up;

    [Tooltip("Sử dụng Local Position thay vì World Position để tránh lỗi khi cha di chuyển.")]
    [SerializeField] private bool useLocalPosition = true;

    [Tooltip("Tạo độ lệch pha ngẫu nhiên để các object không di chuyển giống hệt nhau cùng lúc.")]
    [SerializeField] private bool randomOffset = true;

    private Vector3 startPosition;
    private float timeOffset;

    private void Start()
    {
        // Ghi lại vị trí bắt đầu
        startPosition = useLocalPosition ? transform.localPosition : transform.position;

        // Nếu random offset được bật, tạo một pha ngẫu nhiên từ 0 đến 2*PI
        if (randomOffset)
        {
            timeOffset = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    private void Update()
    {
        // Tính toán khoảng dịch chuyển dựa trên hàm Sin
        float sinValue = Mathf.Sin(Time.time * frequency + timeOffset);
        Vector3 displacement = bobbingDirection.normalized * sinValue * amplitude;

        // Cập nhật vị trí
        if (useLocalPosition)
        {
            transform.localPosition = startPosition + displacement;
        }
        else
        {
            transform.position = startPosition + displacement;
        }
    }

    /// <summary>
    /// Cho phép thiết lập lại vị trí ban đầu nếu Transform bị di chuyển cưỡng bức bởi script khác.
    /// </summary>
    public void ResetStartPosition()
    {
        startPosition = useLocalPosition ? transform.localPosition : transform.position;
    }
}
