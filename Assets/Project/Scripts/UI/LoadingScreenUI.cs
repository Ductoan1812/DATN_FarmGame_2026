using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenUI : MonoBehaviour
{
    private const int OverlaySortingOrder = 5000;

    [SerializeField] private LoadingScreenView loadingScreenPrefab;

    private EventBus subscribedBus;
    private LoadingScreenView loadingScreenView;
    private Transform uiRoot;

    private void Awake()
    {
        GetOrCreateLoadingScreenView()?.HideImmediate();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Update()
    {
        if (subscribedBus == null)
            TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribedBus == null)
            return;

        subscribedBus.Unsubscribe<LoadingScreenShowPublish>(OnShow);
        subscribedBus.Unsubscribe<LoadingScreenProgressPublish>(OnProgress);
        subscribedBus.Unsubscribe<LoadingScreenHidePublish>(OnHide);
        subscribedBus = null;
    }

    private void TrySubscribe()
    {
        if (subscribedBus != null)
            return;

        var bus = GameManager.Instance?.EventBus;
        if (bus == null)
            return;

        bus.Subscribe<LoadingScreenShowPublish>(OnShow);
        bus.Subscribe<LoadingScreenProgressPublish>(OnProgress);
        bus.Subscribe<LoadingScreenHidePublish>(OnHide);
        subscribedBus = bus;
    }

    private void OnShow(LoadingScreenShowPublish e)
    {
        var view = GetOrCreateLoadingScreenView();
        if (view == null)
            return;

        view.Show(e.targetSceneName);
        view.SetProgress(0f);
    }

    private void OnProgress(LoadingScreenProgressPublish e)
    {
        GetOrCreateLoadingScreenView()?.SetProgress(e.progress);
    }

    private void OnHide(LoadingScreenHidePublish e)
    {
        GetOrCreateLoadingScreenView()?.Hide();
    }

    private LoadingScreenView GetOrCreateLoadingScreenView()
    {
        if (loadingScreenView != null)
            return loadingScreenView;

        var overlay = GetOrCreateCanvasOverlayRoot();
        loadingScreenView = overlay.GetComponentInChildren<LoadingScreenView>(includeInactive: true);
        if (loadingScreenView == null)
            loadingScreenView = LoadingScreenView.InstantiateForParent(overlay, loadingScreenPrefab);

        return loadingScreenView;
    }

    private Transform GetOrCreateCanvasOverlayRoot()
    {
        var rootObject = GameObject.Find("UIRoot");
        if (rootObject == null)
        {
            rootObject = new GameObject("UIRoot");
            uiRoot = rootObject.transform;
        }
        else
        {
            uiRoot = rootObject.transform;
        }

        var overlay = uiRoot.Find("CanvasOverlay");
        if (overlay == null)
        {
            var overlayObject = new GameObject("CanvasOverlay", typeof(RectTransform));
            overlayObject.transform.SetParent(uiRoot, false);
            overlay = overlayObject.transform;
        }

        var overlayRect = overlay as RectTransform;
        if (overlayRect != null)
            Stretch(overlayRect);

        var canvas = overlay.GetComponent<Canvas>();
        if (canvas == null)
            canvas = overlay.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = OverlaySortingOrder - 10;

        var scaler = overlay.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = overlay.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (overlay.GetComponent<GraphicRaycaster>() == null)
            overlay.gameObject.AddComponent<GraphicRaycaster>();

        return overlay;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
