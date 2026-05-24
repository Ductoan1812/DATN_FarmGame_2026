using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bind HP/Stamina → UI fill + text.
/// Subscribe EventBus: StatsChangedPublish.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private Image hpFill;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("Stamina")]
    [SerializeField] private Image staminaFill;
    [SerializeField] private TextMeshProUGUI staminaText;

    private EntityRuntime _playerEntity;
    private bool _ready;

    private void OnEnable()
    {
        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;
        bus.Subscribe<GameReadyPublish>(OnGameReady);
        bus.Subscribe<StatsChangedPublish>(OnStatsChanged);
    }

    private void OnDisable()
    {
        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;
        bus.Unsubscribe<GameReadyPublish>(OnGameReady);
        bus.Unsubscribe<StatsChangedPublish>(OnStatsChanged);
    }

    private void OnGameReady(GameReadyPublish _)
    {
        var playerRoot = FindAnyObjectByType<PlayerInventory>()?.GetComponent<EntityRoot>();
        if (playerRoot == null) return;

        _playerEntity = playerRoot.GetEntity();
        _ready = true;
        RefreshAll();
    }

    private void OnStatsChanged(StatsChangedPublish e)
    {
        if (!_ready || e.entityId != _playerEntity?.id) return;

        if (e.statType == StatType.Hp || e.statType == StatType.MaxHp)
            RefreshHP();
        else if (e.statType == StatType.Mp || e.statType == StatType.MaxMp)
            RefreshStamina();
    }

    private void RefreshAll()
    {
        RefreshHP();
        RefreshStamina();
    }

    private void RefreshHP()
    {
        if (_playerEntity == null) return;
        float hp    = _playerEntity.stats.Get(StatType.Hp);
        float maxHp = _playerEntity.stats.Get(StatType.MaxHp);

        if (hpFill != null)
            hpFill.fillAmount = maxHp > 0 ? hp / maxHp : 0f;
        if (hpText != null)
            hpText.text = $"{Mathf.CeilToInt(hp)}/{Mathf.CeilToInt(maxHp)}";
    }

    private void RefreshStamina()
    {
        if (_playerEntity == null) return;
        float stamina    = _playerEntity.stats.Get(StatType.Mp);
        float maxStamina = _playerEntity.stats.Get(StatType.MaxMp);

        if (staminaFill != null)
            staminaFill.fillAmount = maxStamina > 0 ? stamina / maxStamina : 0f;
        if (staminaText != null)
            staminaText.text = $"{Mathf.CeilToInt(stamina)}/{Mathf.CeilToInt(maxStamina)}";
    }
}
