using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Khi đồng hồ chạm 2:00 sáng, player bị ngất:
///   - Fade đen (SleepTransitionPublish)
///   - Chuyển về FarmScene nếu đang ở scene khác
///   - Teleport đến vị trí giường ngủ
///   - Hồi 20% thể lực tối đa
///   - Nếu HP dưới 20% → set HP = 20% MaxHp
///   - Chuyển sang ngày mới
/// </summary>
public class PassOutSystem : MonoBehaviour
{
    private const int PassOutHour = 2;
    private const string FarmSceneName = "FarmScene";

    [SerializeField] private Vector2 bedPosition = new(-40f, 115f);
    [SerializeField, Range(0f, 1f)] private float staminaRecoverRatio = 0.2f;
    [SerializeField, Range(0f, 1f)] private float hpThresholdRatio = 0.2f;

    private EventBus _eventBus;
    private bool _passedOutToday;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (_eventBus != null)
        {
            _eventBus.Unsubscribe<GameHourChangedPublish>(OnHourChanged);
            _eventBus.Unsubscribe<DayChangedPublish>(OnDayChanged);
            _eventBus = null;
        }
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void Update()
    {
        if (_eventBus == null)
            TrySubscribe();
    }

    private void TrySubscribe()
    {
        var bus = GameManager.Instance?.EventBus;
        if (bus == null || bus == _eventBus) return;

        if (_eventBus != null)
        {
            _eventBus.Unsubscribe<GameHourChangedPublish>(OnHourChanged);
            _eventBus.Unsubscribe<DayChangedPublish>(OnDayChanged);
        }

        _eventBus = bus;
        _eventBus.Subscribe<GameHourChangedPublish>(OnHourChanged);
        _eventBus.Subscribe<DayChangedPublish>(OnDayChanged);
    }

    private void OnDayChanged(DayChangedPublish _)
    {
        _passedOutToday = false;
    }

    private void OnHourChanged(GameHourChangedPublish e)
    {
        if (_passedOutToday) return;
        if (e.hour != PassOutHour) return;

        _passedOutToday = true;
        StartCoroutine(PassOutRoutine());
    }

    private IEnumerator PassOutRoutine()
    {
        var gm = GameManager.Instance;
        if (gm == null) yield break;

        var timeManager = gm.TimeManager;
        var eventBus = gm.EventBus;

        // Pause thời gian trong lúc xử lý
        timeManager?.Pause();

        // Tìm player
        var player = FindAnyObjectByType<PlayerControler>();
        EntityRuntime playerEntity = null;
        if (player != null)
        {
            player.InputEnabled = false;
            var root = player.GetComponent<EntityRoot>();
            playerEntity = root != null ? root.GetEntity() : null;
        }

        // Hiển thị fade đen
        eventBus?.Publish(new SleepTransitionPublish());

        // Chờ fade in xong
        yield return new WaitForSecondsRealtime(0.5f);

        // Chuyển scene nếu cần
        string currentScene = SceneManager.GetActiveScene().name;
        if (!string.Equals(currentScene, FarmSceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            // Chuyển về FarmScene
            SceneTransitionService.RequestTransition(
                playerEntity,
                FarmSceneName,
                string.Empty,
                saveBeforeTransition: true
            );

            // Chờ scene load xong
            yield return new WaitUntil(() =>
                string.Equals(SceneManager.GetActiveScene().name, FarmSceneName,
                    System.StringComparison.OrdinalIgnoreCase));

            yield return new WaitForSecondsRealtime(0.3f);

            // Tìm lại player sau khi chuyển scene
            player = FindAnyObjectByType<PlayerControler>();
            if (player != null)
            {
                player.InputEnabled = false;
                var root = player.GetComponent<EntityRoot>();
                playerEntity = root != null ? root.GetEntity() : null;
            }

            // Lấy lại reference
            gm = GameManager.Instance;
            timeManager = gm?.TimeManager;
            eventBus = gm?.EventBus;
        }

        // Teleport player đến vị trí giường
        if (player != null)
            player.transform.position = new Vector3(bedPosition.x, bedPosition.y, 0f);

        // Áp dụng penalty stats
        if (playerEntity != null)
        {
            // Hồi 20% thể lực
            float maxStamina = playerEntity.stats.Get(StatType.MaxStamina);
            if (maxStamina > 0f)
                playerEntity.stats.Set(StatType.Stamina, maxStamina * staminaRecoverRatio);

            // Nếu HP dưới 20% → set HP = 20% MaxHp
            float maxHp = playerEntity.stats.Get(StatType.MaxHp);
            float currentHp = playerEntity.stats.Get(StatType.Hp);
            if (maxHp > 0f && currentHp < maxHp * hpThresholdRatio)
                playerEntity.stats.Set(StatType.Hp, maxHp * hpThresholdRatio);
        }

        // Chuyển sang ngày mới
        timeManager?.SkipToNextDay();

        // Chờ thêm chút cho fade out hoàn tất
        yield return new WaitForSecondsRealtime(0.8f);

        // Mở lại input
        if (player != null)
            player.InputEnabled = true;

        // Resume thời gian
        timeManager?.Play();

        // Toast thông báo
        eventBus?.Publish(new ToastPublish("Bạn đã ngất vì quá khuya...", 3f));

        Debug.Log("[PassOutSystem] Player passed out at 2:00 AM. Teleported to bed, new day started.");
    }
}
