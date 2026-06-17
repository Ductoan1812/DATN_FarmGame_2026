using UnityEngine;

[DisallowMultipleComponent]
public class WorldInteractionHintUI : MonoBehaviour
{
    private const string IconSheetPath = "UI/Icons/InteractionIcon";

    [SerializeField] private Vector3 worldOffset = new(0f, 1.9f, 0f);
    [SerializeField] private float iconScale = 1.2f;
    [SerializeField] private string sortingLayerName = "Effect";
    [SerializeField] private int sortingOrder = 200;

    private EventBus eventBus;
    private SpriteRenderer iconRenderer;
    private InteractionPreviewData currentPreview;
    private bool subscribed;
    private Sprite[] iconSprites;

    private void Awake()
    {
        LoadIcons();
        EnsureIcon();
        Hide();
    }

    private void OnEnable() => TrySubscribe();

    private void Update()
    {
        if (!subscribed) TrySubscribe();
        UpdateFollow();
    }

    private void OnDisable()
    {
        if (!subscribed || eventBus == null) return;
        eventBus.Unsubscribe<InteractionPreviewChangedPublish>(OnPreviewChanged);
        subscribed = false;
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        eventBus = GameManager.Instance?.EventBus;
        if (eventBus == null) return;
        eventBus.Subscribe<InteractionPreviewChangedPublish>(OnPreviewChanged);
        subscribed = true;
    }

    private void OnPreviewChanged(InteractionPreviewChangedPublish e)
    {
        currentPreview = e.preview;
        RenderPreview();
    }

    private void UpdateFollow()
    {
        if (iconRenderer == null) return;
        if (!TryGetTargetTransform(out var t))
        {
            Hide();
            currentPreview = default;
            return;
        }
        iconRenderer.transform.position = t.position + worldOffset;
    }

    private void RenderPreview()
    {
        EnsureIcon();
        if (iconRenderer == null) return;

        if (!TryGetTargetTransform(out _))
        {
            Hide();
            return;
        }

        int idx = ResolveIconIndex(currentPreview);
        if (idx >= 0 && idx < iconSprites.Length && iconSprites[idx] != null)
        {
            iconRenderer.sprite = iconSprites[idx];
            iconRenderer.color = currentPreview.isBlocked ? new Color(1f, 1f, 1f, 0.55f) : Color.white;
            iconRenderer.gameObject.SetActive(true);
        }
        else
        {
            Hide();
        }
    }

    // Icon index map (từ ảnh sprite sheet):
    // 0=HandHarvest, 1=WateringCan, 2=Hoe, 3=Axe, 4=Pickaxe, 5=Scythe/Liềm
    // 6=Moon/Sleep, 7=Chat"...", 8=Quest"?", 9=Chat"..."2, 10=TúiVàng/Shop
    // 11=Rương/Chest, 12=Craft, 13=Portal/Door, 14=Bowl/Animal, 15=Heart+Lock
    // 16=X/Blocked, 17=Hourglass

    // Trả về -1 = ẩn icon
    private int ResolveIconIndex(InteractionPreviewData preview)
    {
        string key = preview.actionTextKey;

        // Harvest — logic riêng
        if (key == "ui.interaction.harvest")
        {
            if (preview.isBlocked)
            {
                // Chưa sẵn sàng → ẩn
                if (!string.IsNullOrEmpty(preview.blockedReasonKey) && preview.blockedReasonKey.Contains("not_ready"))
                    return -1;
                // Cần tool → hiện icon tool đó
                if (preview.requiredTool != ToolType.None)
                    return ToolToIndex(preview.requiredTool);
                return -1;
            }
            // Harvest được → icon thu hoạch tay (0)
            return 0;
        }

        // Generic "use" hoặc rỗng → ẩn
        if (string.IsNullOrEmpty(key) || key == "ui.common.use")
            return -1;

        if (key == "ui.bed.sleep") return 6;       // Moon
        if (key == "ui.storage.open") return 11;    // Rương
        if (key == "ui.processor.open") return 12;  // Craft

        // Quest
        if (key == "ui.quest.accept" || key == "ui.quest.offer") return 8;     // "?"
        if (key == "ui.quest.view" || key == "ui.quest.in_progress") return 8; // "?"
        if (key == "ui.quest.complete" || key == "ui.quest.completed") return 8;

        // Shop, craft, portal, animal
        if (key.Contains("shop") || key.Contains("buy") || key.Contains("sell")) return 10; // Túi vàng
        if (key.Contains("craft")) return 12;   // Craft
        if (key.Contains("portal") || key.Contains("enter") || key.Contains("door")) return 13; // Portal
        if (key.Contains("feed") || key.Contains("animal")) return 14; // Bowl

        // Dialogue / NPC / bất kỳ key lạ → icon "..." (7)
        return 7;
    }

    private static int ToolToIndex(ToolType tool) => tool switch
    {
        ToolType.WateringCan => 1,
        ToolType.Hoe        => 2,
        ToolType.Axe        => 3,
        ToolType.Pickaxe    => 4,
        ToolType.Scythe     => 5,
        _                   => 16
    };

    private bool TryGetTargetTransform(out Transform targetTransform)
    {
        targetTransform = null;
        if (!currentPreview.HasTarget || currentPreview.target == null) return false;

        var owner = currentPreview.target.Owner;
        if (owner is Object ownerObject && ownerObject == null) return false;

        var go = owner?.GameObject;
        if (go == null) return false;

        targetTransform = go.transform;
        return true;
    }

    private void Hide()
    {
        if (iconRenderer != null)
            iconRenderer.gameObject.SetActive(false);
    }

    private void LoadIcons()
    {
        var loaded = Resources.LoadAll<Sprite>(IconSheetPath);
        if (loaded == null || loaded.Length == 0)
        {
            Debug.LogWarning($"[WorldInteractionHintUI] No sprites found at '{IconSheetPath}'. Check sprite sheet import settings (spriteMode must be Multiple).");
            iconSprites = System.Array.Empty<Sprite>();
            return;
        }
        System.Array.Sort(loaded, (a, b) =>
        {
            int idxA = ExtractSpriteIndex(a.name);
            int idxB = ExtractSpriteIndex(b.name);
            return idxA.CompareTo(idxB);
        });
        iconSprites = loaded;
        Debug.Log($"[WorldInteractionHintUI] Loaded {iconSprites.Length} interaction icon sprites.");
    }

    private static int ExtractSpriteIndex(string spriteName)
    {
        int underscoreIdx = spriteName.LastIndexOf('_');
        if (underscoreIdx >= 0 && int.TryParse(spriteName.Substring(underscoreIdx + 1), out int idx))
            return idx;
        return int.MaxValue;
    }

    private void EnsureIcon()
    {
        if (iconRenderer != null) return;

        var child = transform.Find("WorldHintIcon");
        if (child != null)
            iconRenderer = child.GetComponent<SpriteRenderer>();

        if (iconRenderer == null)
        {
            var go = new GameObject("WorldHintIcon");
            go.transform.SetParent(transform, false);
            iconRenderer = go.AddComponent<SpriteRenderer>();
        }

        iconRenderer.sortingLayerName = sortingLayerName;
        iconRenderer.sortingOrder = sortingOrder;
        iconRenderer.transform.localScale = Vector3.one * Mathf.Max(0.01f, iconScale);
    }
}
