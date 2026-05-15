using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI-only bridge: mirrors the player's current front-facing Character4D visual into a HUD avatar.
/// This does not read inventory/equipment data directly, so it stays independent from gameplay/backend logic.
/// </summary>
public class PlayerAvatarFollowerUI : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private Character4D sourceCharacter;
    [SerializeField] private bool autoFindPlayer = true;

    [Header("Target")]
    [SerializeField] private AvatarSetup targetAvatar;

    [Header("UI Image Target")]
    [SerializeField] private RectTransform imageRoot;
    [SerializeField] private Image headImage;
    [SerializeField] private Image hairImage;
    [SerializeField] private Image helmetImage;
    [SerializeField] private Image eyesImage;
    [SerializeField] private Image eyebrowsImage;
    [SerializeField] private Image mouthImage;
    [SerializeField] private Image beardImage;
    [SerializeField] private Image leftEarImage;
    [SerializeField] private Image rightEarImage;
    [SerializeField] private bool hideSpriteRendererAvatar = true;
    [SerializeField, Min(1f)] private float imagePixelsPerUnit = 32f;
    [SerializeField, Min(0.01f)] private float avatarScale = 0.35f;
    [SerializeField] private bool mirrorFromSourceTransforms = true;
    [SerializeField] private Vector2 imageOffset = Vector2.zero;

    [Header("Refresh")]
    [SerializeField, Min(0.05f)] private float refreshInterval = 0.15f;

    private EventBus subscribedBus;
    private float nextRefreshTime;

    private void Awake()
    {
        AutoFindTarget();
    }

    private void OnEnable()
    {
        TrySubscribe();
        TryBindSource();
        RefreshAvatar();
    }

    private void Start()
    {
        TrySubscribe();
        TryBindSource();
        RefreshAvatar();
    }

    private void Update()
    {
        if (subscribedBus == null)
            TrySubscribe();

        if (sourceCharacter == null && autoFindPlayer)
            TryBindSource();

        if (Time.unscaledTime < nextRefreshTime) return;
        nextRefreshTime = Time.unscaledTime + refreshInterval;

        RefreshAvatar();
    }

    private void OnDisable()
    {
        if (subscribedBus == null) return;

        subscribedBus.Unsubscribe<PlayerReadyPublish>(OnPlayerReady);
        subscribedBus.Unsubscribe<EquipmentSlotChangedPublish>(OnEquipmentSlotChanged);
        subscribedBus = null;
    }

    public void SetSource(Character4D character)
    {
        sourceCharacter = character;
        RefreshAvatar();
    }

    public void SetTarget(AvatarSetup avatar)
    {
        targetAvatar = avatar;
        RefreshAvatar();
    }

    public void RefreshAvatar()
    {
        if (targetAvatar == null)
            AutoFindTarget();

        AutoFindImageTargets();

        if (sourceCharacter == null)
            TryBindSource();

        var source = sourceCharacter != null ? sourceCharacter.Front : null;
        if (source == null) return;

        if (HasImageTarget())
        {
            CopyRendererToImage(source, source.HeadRenderer, headImage);
            CopyRendererToImage(source, source.HairRenderer, hairImage);
            CopyRendererToImage(source, source.HelmetRenderer, helmetImage);
            CopyRendererToImage(source, source.EyesRenderer, eyesImage);
            CopyRendererToImage(source, source.EyebrowsRenderer, eyebrowsImage);
            CopyRendererToImage(source, source.MouthRenderer, mouthImage);
            CopyRendererToImage(source, source.BeardRenderer, beardImage);
            CopyEarsToImages(source);

            if (hideSpriteRendererAvatar)
                SetAvatarRenderersVisible(false);

            return;
        }

        if (targetAvatar == null) return;

        CopyRenderer(source.HeadRenderer, targetAvatar.Head);
        CopyRenderer(source.HairRenderer, targetAvatar.Hair);
        CopyRenderer(source.HelmetRenderer, targetAvatar.Helmet);
        CopyRenderer(source.EyesRenderer, targetAvatar.Eyes);
        CopyRenderer(source.EyebrowsRenderer, targetAvatar.Eyebrows);
        CopyRenderer(source.MouthRenderer, targetAvatar.Mouth);
        CopyRenderer(source.BeardRenderer, targetAvatar.Beard);
        CopyEars(source, targetAvatar);
    }

    private void TrySubscribe()
    {
        if (subscribedBus != null) return;

        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;

        bus.Subscribe<PlayerReadyPublish>(OnPlayerReady);
        bus.Subscribe<EquipmentSlotChangedPublish>(OnEquipmentSlotChanged);
        subscribedBus = bus;
    }

    private void OnPlayerReady(PlayerReadyPublish _)
    {
        TryBindSource(true);
        RefreshAvatar();
    }

    private void OnEquipmentSlotChanged(EquipmentSlotChangedPublish _)
    {
        RefreshAvatar();
    }

    private void TryBindSource(bool force = false)
    {
        if (!autoFindPlayer) return;
        if (sourceCharacter != null && !force) return;

        var bridge = FindAnyObjectByType<PlayerBridge>();
        if (bridge != null)
            sourceCharacter = bridge.GetComponentInChildren<Character4D>(true);

        if (sourceCharacter == null)
            sourceCharacter = FindAnyObjectByType<Character4D>();
    }

    private void AutoFindTarget()
    {
        if (targetAvatar != null) return;
        targetAvatar = GetComponentInChildren<AvatarSetup>(true);
    }

    private void AutoFindImageTargets()
    {
        if (imageRoot == null)
            imageRoot = FindDeepChild(transform, "AvatarImageRoot") as RectTransform;

        var root = imageRoot != null ? imageRoot : transform;

        headImage ??= FindImage(root, "Head");
        hairImage ??= FindImage(root, "Hair");
        helmetImage ??= FindImage(root, "Helmet");
        eyesImage ??= FindImage(root, "Eyes");
        eyebrowsImage ??= FindImage(root, "Eyebrows") ?? FindImage(root, "Eyesbrows");
        mouthImage ??= FindImage(root, "Mouth");
        beardImage ??= FindImage(root, "Beard");
        leftEarImage ??= FindImage(root, "EarL") ?? FindImage(root, "LeftEar");
        rightEarImage ??= FindImage(root, "EarR") ?? FindImage(root, "RightEar");
    }

    [ContextMenu("Build UI Image Avatar From Sprite Avatar")]
    private void BuildUiImageAvatarFromSpriteAvatar()
    {
        AutoFindTarget();

        if (imageRoot == null)
        {
            var root = new GameObject("AvatarImageRoot", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            imageRoot = root.GetComponent<RectTransform>();
            imageRoot.anchorMin = Vector2.zero;
            imageRoot.anchorMax = Vector2.one;
            imageRoot.offsetMin = Vector2.zero;
            imageRoot.offsetMax = Vector2.zero;
        }

        headImage = EnsureImage("Head", targetAvatar?.Head, 0);
        leftEarImage = EnsureImage("EarL", targetAvatar != null && targetAvatar.Ears.Count > 0 ? targetAvatar.Ears[0] : null, 1);
        rightEarImage = EnsureImage("EarR", targetAvatar != null && targetAvatar.Ears.Count > 1 ? targetAvatar.Ears[1] : null, 2);
        beardImage = EnsureImage("Beard", targetAvatar?.Beard, 3);
        hairImage = EnsureImage("Hair", targetAvatar?.Hair, 4);
        eyesImage = EnsureImage("Eyes", targetAvatar?.Eyes, 5);
        eyebrowsImage = EnsureImage("Eyebrows", targetAvatar?.Eyebrows, 6);
        mouthImage = EnsureImage("Mouth", targetAvatar?.Mouth, 7);
        helmetImage = EnsureImage("Helmet", targetAvatar?.Helmet, 8);

        RefreshAvatar();
    }

    private Image EnsureImage(string objectName, SpriteRenderer template, int siblingIndex)
    {
        var child = FindDeepChild(imageRoot, objectName);
        if (child == null)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(imageRoot, false);
            child = go.transform;
        }

        child.SetSiblingIndex(siblingIndex);

        var rect = (RectTransform)child;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        if (template != null)
        {
            rect.anchoredPosition = new Vector2(
                template.transform.localPosition.x * imagePixelsPerUnit * avatarScale,
                template.transform.localPosition.y * imagePixelsPerUnit * avatarScale);

            if (template.sprite != null)
            {
                rect.sizeDelta = template.sprite.rect.size / imagePixelsPerUnit * avatarScale;
            }
            else
            {
                rect.sizeDelta = Vector2.one * 48f * avatarScale;
            }
        }
        else
        {
            rect.sizeDelta = Vector2.one * 48f * avatarScale;
        }

        var image = child.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    private static void CopyEars(Character source, AvatarSetup target)
    {
        if (source?.EarsRenderers == null || target?.Ears == null) return;

        int count = Mathf.Min(source.EarsRenderers.Count, target.Ears.Count);
        for (int i = 0; i < count; i++)
            CopyRenderer(source.EarsRenderers[i], target.Ears[i]);
    }

    private void CopyEarsToImages(Character source)
    {
        if (source?.EarsRenderers == null) return;

        if (source.EarsRenderers.Count > 0)
            CopyRendererToImage(source, source.EarsRenderers[0], leftEarImage);

        if (source.EarsRenderers.Count > 1)
            CopyRendererToImage(source, source.EarsRenderers[1], rightEarImage);
    }

    private static void CopyRenderer(SpriteRenderer source, SpriteRenderer target)
    {
        if (target == null) return;

        if (source == null || source.sprite == null)
        {
            target.sprite = null;
            target.enabled = false;
            return;
        }

        target.sprite = source.sprite;
        target.color = source.color;
        target.sharedMaterial = source.sharedMaterial;
        target.enabled = source.enabled;
        target.flipX = source.flipX;
        target.flipY = source.flipY;
    }

    private void CopyRendererToImage(Character sourceRoot, SpriteRenderer source, Image target)
    {
        if (target == null) return;

        if (source == null || source.sprite == null || !source.enabled)
        {
            target.sprite = null;
            target.enabled = false;
            return;
        }

        target.sprite = source.sprite;
        target.color = source.color;
        target.enabled = true;

        if (!mirrorFromSourceTransforms) return;

        var rect = target.rectTransform;
        var sprite = source.sprite;
        var spriteSize = sprite.rect.size;
        var safeWidth = Mathf.Max(1f, spriteSize.x);
        var safeHeight = Mathf.Max(1f, spriteSize.y);

        rect.pivot = new Vector2(sprite.pivot.x / safeWidth, sprite.pivot.y / safeHeight);
        rect.sizeDelta = spriteSize / imagePixelsPerUnit * avatarScale;

        Vector3 localPosition = sourceRoot != null
            ? sourceRoot.transform.InverseTransformPoint(source.transform.position)
            : source.transform.localPosition;

        rect.anchoredPosition = new Vector2(
            localPosition.x * imagePixelsPerUnit * avatarScale + imageOffset.x,
            localPosition.y * imagePixelsPerUnit * avatarScale + imageOffset.y);

        rect.localRotation = Quaternion.identity;
        rect.localScale = new Vector3(source.flipX ? -1f : 1f, source.flipY ? -1f : 1f, 1f);
    }

    private bool HasImageTarget()
    {
        return headImage != null ||
               hairImage != null ||
               helmetImage != null ||
               eyesImage != null ||
               eyebrowsImage != null ||
               mouthImage != null ||
               beardImage != null ||
               leftEarImage != null ||
               rightEarImage != null;
    }

    private void SetAvatarRenderersVisible(bool visible)
    {
        if (targetAvatar == null) return;

        foreach (var renderer in targetAvatar.GetComponentsInChildren<SpriteRenderer>(true))
            renderer.enabled = visible && renderer.sprite != null;
    }

    private static Image FindImage(Transform root, string name)
    {
        var child = FindDeepChild(root, name);
        return child != null ? child.GetComponent<Image>() : null;
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
