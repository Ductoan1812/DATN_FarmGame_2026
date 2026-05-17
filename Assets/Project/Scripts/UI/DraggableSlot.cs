using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Gắn lên mỗi slot UI (backpack hoặc hotbar).
/// Xử lý drag & drop qua Unity EventSystem.
/// Khi drop thành công → publish SlotDragDropRequestPublish.
/// </summary>
public class DraggableSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerDownHandler, IPointerUpHandler
{
    public InventoryType InventoryType { get; private set; }
    public int SlotIndex { get; private set; }
    public bool HasItem => _iconImage != null && _iconImage.sprite != null;

    private Image _iconImage;
    private Toggle _toggle;
    private static DraggableSlot _dragging;
    private static GameObject _ghost;
    private static RectTransform _ghostRect;
    private static Image _ghostImage;
    private static Canvas _dragCanvas;

    public void Init(InventoryType type, int index, Image iconImage)
    {
        InventoryType = type;
        SlotIndex = index;
        _iconImage = iconImage;
        _toggle = GetComponent<Toggle>();
    }

    // ══════ IPointerDownHandler / IPointerUpHandler ══════
    // Giữ hooks này để sau này dễ mở rộng custom click-vs-drag behavior.

    public void OnPointerDown(PointerEventData eventData)
    {
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Nếu vừa drag xong → chặn Toggle không cho fire
        // (Toggle fire trong OnPointerClick, sau OnPointerUp)
    }

    // ══════ IBeginDragHandler ══════

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasItem)
        {
            eventData.pointerDrag = null;
            return;
        }

        _dragging = this;

        // Tắt Toggle tạm thời để drag không trigger toggle
        if (_toggle != null) _toggle.interactable = false;
        EnsureGhost();

        _ghostImage.sprite = _iconImage.sprite;
        _ghostImage.color = Color.white;
        _ghost.SetActive(true);

        UpdateGhostPosition(eventData);

        // Làm mờ icon gốc
        if (_iconImage != null)
            _iconImage.color = new Color(1f, 1f, 1f, 0.3f);
    }

    // ══════ IDragHandler ══════

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragging != this) return;
        UpdateGhostPosition(eventData);
    }

    // ══════ IEndDragHandler ══════

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragging != this) return;

        // Ẩn ghost
        if (_ghost != null)
            _ghost.SetActive(false);

        // Khôi phục icon gốc
        if (_iconImage != null && _iconImage.sprite != null)
            _iconImage.color = Color.white;

        // Bật lại Toggle
        if (_toggle != null) _toggle.interactable = true;

        _dragging = null;
    }

    // ══════ IDropHandler ══════

    public void OnDrop(PointerEventData eventData)
    {
        if (_dragging == null || _dragging == this) return;

        // Cùng inventory + cùng slot → bỏ qua
        if (_dragging.InventoryType == InventoryType && _dragging.SlotIndex == SlotIndex)
            return;

        // Publish swap request
        var bus = GameManager.Instance?.EventBus;
        bus?.Publish(new SlotDragDropRequestPublish(
            _dragging.InventoryType,
            _dragging.SlotIndex,
            InventoryType,
            SlotIndex));
    }

    // ══════ Ghost management ══════

    private void EnsureGhost()
    {
        if (_ghost != null) return;

        var sourceCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        EnsureDragCanvas(sourceCanvas);
        if (_dragCanvas == null) return;

        _ghost = new GameObject("DragGhost");
        _ghost.transform.SetParent(_dragCanvas.transform, false);
        _ghost.transform.SetAsLastSibling();

        _ghostRect = _ghost.AddComponent<RectTransform>();
        _ghostRect.sizeDelta = new Vector2(80f, 80f);
        _ghostRect.pivot = new Vector2(0.5f, 0.5f);

        _ghostImage = _ghost.AddComponent<Image>();
        _ghostImage.raycastTarget = false;

        // Thêm CanvasGroup để ghost không chặn raycast
        var cg = _ghost.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        _ghost.SetActive(false);
    }

    private void UpdateGhostPosition(PointerEventData eventData)
    {
        if (_ghostRect == null || _dragCanvas == null) return;

        if (_dragCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            _ghostRect.position = eventData.position;
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _dragCanvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint);
            _ghostRect.localPosition = localPoint;
        }
    }

    private static void EnsureDragCanvas(Canvas sourceCanvas)
    {
        if (_dragCanvas != null) return;

        var canvasGo = new GameObject("DragGhostCanvas");
        _dragCanvas = canvasGo.AddComponent<Canvas>();
        _dragCanvas.renderMode = sourceCanvas != null ? sourceCanvas.renderMode : RenderMode.ScreenSpaceOverlay;
        _dragCanvas.worldCamera = sourceCanvas != null ? sourceCanvas.worldCamera : null;
        _dragCanvas.planeDistance = sourceCanvas != null ? sourceCanvas.planeDistance : 100f;
        _dragCanvas.overrideSorting = true;
        _dragCanvas.sortingLayerID = sourceCanvas != null ? sourceCanvas.sortingLayerID : 0;
        _dragCanvas.sortingOrder = short.MaxValue;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        if (sourceCanvas != null && sourceCanvas.TryGetComponent<CanvasScaler>(out var sourceScaler))
        {
            scaler.uiScaleMode = sourceScaler.uiScaleMode;
            scaler.referenceResolution = sourceScaler.referenceResolution;
            scaler.screenMatchMode = sourceScaler.screenMatchMode;
            scaler.matchWidthOrHeight = sourceScaler.matchWidthOrHeight;
            scaler.referencePixelsPerUnit = sourceScaler.referencePixelsPerUnit;
        }

        canvasGo.AddComponent<GraphicRaycaster>().enabled = false;
        Object.DontDestroyOnLoad(canvasGo);
    }
}
