using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoHUDUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private CanvasGroup rootCanvasGroup;

    [Header("HP")]
    [SerializeField] private GameObject hpInfo;
    [SerializeField] private Image hpFill;
    [SerializeField] private TMP_Text hpText;

    [Header("MP")]
    [SerializeField] private GameObject mpInfo;
    [SerializeField] private Image mpFill;
    [SerializeField] private TMP_Text mpText;

    [Header("EXP")]
    [SerializeField] private GameObject expInfo;
    [SerializeField] private Image expFill;
    [SerializeField] private TMP_Text expText;

    [Header("Level")]
    [SerializeField] private GameObject levelInfo;
    [SerializeField] private TMP_Text levelText;

    private EntityRuntime playerEntity;
    private EventBus subscribedBus;

    private void Awake()
    {
        AutoFindRefs();
        HideAll();
    }

    private void OnEnable()
    {
        TrySubscribe();
        TryBindPlayer();
        RefreshAll();
    }

    private void Start()
    {
        TrySubscribe();
        TryBindPlayer();
        RefreshAll();
    }

    private void Update()
    {
        if (subscribedBus == null)
            TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribedBus == null) return;

        subscribedBus.Unsubscribe<PlayerReadyPublish>(OnPlayerReady);
        subscribedBus.Unsubscribe<GameReadyPublish>(OnGameReady);
        subscribedBus.Unsubscribe<StatsChangedPublish>(OnStatsChanged);
        subscribedBus = null;
    }

    private void OnPlayerReady(PlayerReadyPublish _)
    {
        TryBindPlayer();
        RefreshAll();
    }

    private void OnGameReady(GameReadyPublish _)
    {
        TryBindPlayer();
        RefreshAll();
    }

    private void OnStatsChanged(StatsChangedPublish e)
    {
        if (playerEntity == null || e.entityId != playerEntity.id) return;

        switch (e.statType)
        {
            case StatType.Hp:
            case StatType.MaxHp:
                RefreshBar(hpInfo, hpFill, hpText, StatType.Hp, StatType.MaxHp);
                break;
            case StatType.Mp:
            case StatType.MaxMp:
                RefreshBar(mpInfo, mpFill, mpText, StatType.Mp, StatType.MaxMp);
                break;
            case StatType.Exp:
            case StatType.MaxExp:
                RefreshBar(expInfo, expFill, expText, StatType.Exp, StatType.MaxExp);
                break;
            case StatType.Level:
                RefreshLevel();
                break;
        }

        RefreshRootVisibility();
    }

    private void TrySubscribe()
    {
        if (subscribedBus != null) return;

        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;

        bus.Subscribe<PlayerReadyPublish>(OnPlayerReady);
        bus.Subscribe<GameReadyPublish>(OnGameReady);
        bus.Subscribe<StatsChangedPublish>(OnStatsChanged);
        subscribedBus = bus;
    }

    private void TryBindPlayer()
    {
        if (playerEntity != null) return;

        var bridge = FindAnyObjectByType<PlayerBridge>();
        var rootComponent = bridge != null
            ? bridge.GetComponent<EntityRoot>()
            : FindAnyObjectByType<PlayerInventory>()?.GetComponent<EntityRoot>();

        playerEntity = rootComponent != null ? rootComponent.GetEntity() : null;
    }

    private void RefreshAll()
    {
        if (playerEntity == null)
        {
            HideAll();
            return;
        }

        RefreshBar(hpInfo, hpFill, hpText, StatType.Hp, StatType.MaxHp);
        RefreshBar(mpInfo, mpFill, mpText, StatType.Mp, StatType.MaxMp);
        RefreshBar(expInfo, expFill, expText, StatType.Exp, StatType.MaxExp);
        RefreshLevel();
        RefreshRootVisibility();
    }

    private void RefreshBar(GameObject info, Image fill, TMP_Text text, StatType currentType, StatType maxType)
    {
        if (info == null || playerEntity?.stats == null) return;

        bool hasStats = playerEntity.stats.Has(currentType) && playerEntity.stats.Has(maxType);
        if (!hasStats)
        {
            info.SetActive(false);
            return;
        }

        float current = playerEntity.stats.Get(currentType);
        float max = playerEntity.stats.Get(maxType);
        bool valid = max > 0f;
        info.SetActive(valid);
        if (!valid) return;

        if (fill != null)
            fill.fillAmount = Mathf.Clamp01(current / max);

        if (text != null)
            text.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }

    private void RefreshLevel()
    {
        if (levelInfo == null && levelText == null) return;

        bool hasLevel = playerEntity?.stats != null && playerEntity.stats.Has(StatType.Level);
        if (levelInfo != null)
            levelInfo.SetActive(hasLevel);

        if (!hasLevel || levelText == null) return;

        int level = Mathf.FloorToInt(playerEntity.stats.Get(StatType.Level));
        levelText.text = $"LV.{level}";
    }

    private void RefreshRootVisibility()
    {
        if (root == null) return;

        bool anyVisible =
            IsVisible(hpInfo) ||
            IsVisible(mpInfo) ||
            IsVisible(expInfo) ||
            IsVisible(levelInfo);

        SetRootVisible(anyVisible);
    }

    private void HideAll()
    {
        if (hpInfo != null) hpInfo.SetActive(false);
        if (mpInfo != null) mpInfo.SetActive(false);
        if (expInfo != null) expInfo.SetActive(false);
        if (levelInfo != null) levelInfo.SetActive(false);
        SetRootVisible(false);
    }

    private void AutoFindRefs()
    {
        if (root == null)
            root = gameObject;

        if (rootCanvasGroup == null && root != null)
        {
            rootCanvasGroup = root.GetComponent<CanvasGroup>();
            if (rootCanvasGroup == null)
                rootCanvasGroup = root.AddComponent<CanvasGroup>();
        }

        hpInfo ??= FindDeepChild(transform, "HP_info")?.gameObject;
        hpFill ??= FindDeepChild(hpInfo != null ? hpInfo.transform : transform, "Hp_fill")?.GetComponent<Image>();
        hpText ??= FindFirstText(hpInfo);

        mpInfo ??= FindDeepChild(transform, "Enegy_info")?.gameObject
                ?? FindDeepChild(transform, "Energy_info")?.gameObject
                ?? FindDeepChild(transform, "MP_info")?.gameObject;
        mpFill ??= FindDeepChild(mpInfo != null ? mpInfo.transform : transform, "Enegy_fill")?.GetComponent<Image>()
                ?? FindDeepChild(mpInfo != null ? mpInfo.transform : transform, "Energy_fill")?.GetComponent<Image>()
                ?? FindDeepChild(mpInfo != null ? mpInfo.transform : transform, "Mp_fill")?.GetComponent<Image>();
        mpText ??= FindFirstText(mpInfo);

        expInfo ??= FindDeepChild(transform, "Xp_info")?.gameObject
                 ?? FindDeepChild(transform, "Exp_info")?.gameObject;
        expFill ??= FindDeepChild(expInfo != null ? expInfo.transform : transform, "Xp_fill")?.GetComponent<Image>()
                 ?? FindDeepChild(expInfo != null ? expInfo.transform : transform, "Exp_fill")?.GetComponent<Image>();
        expText ??= FindFirstText(expInfo);

        levelText ??= FindDeepChild(transform, "LevelLabel")?.GetComponent<TMP_Text>()
                   ?? FindDeepChild(transform, "LevelText")?.GetComponent<TMP_Text>()
                   ?? FindDeepChild(transform, "LvText")?.GetComponent<TMP_Text>()
                   ?? FindDeepChild(transform, "Lv_Label")?.GetComponent<TMP_Text>();
        levelInfo ??= levelText != null ? levelText.gameObject : null;
    }

    private static bool IsVisible(GameObject target)
    {
        return target != null && target.activeSelf;
    }

    private void SetRootVisible(bool visible)
    {
        if (root == null) return;

        if (root == gameObject)
        {
            if (rootCanvasGroup == null)
                rootCanvasGroup = root.GetComponent<CanvasGroup>() ?? root.AddComponent<CanvasGroup>();

            rootCanvasGroup.alpha = visible ? 1f : 0f;
            rootCanvasGroup.interactable = visible;
            rootCanvasGroup.blocksRaycasts = visible;
            return;
        }

        root.SetActive(visible);
    }

    private static TMP_Text FindFirstText(GameObject rootObject)
    {
        return rootObject != null ? rootObject.GetComponentInChildren<TMP_Text>(true) : null;
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;
        if (root.name == name) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeepChild(root.GetChild(i), name);
            if (found != null) return found;
        }

        return null;
    }
}
