using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Reusable screen-space leader-line and label overlay component for Unity video production.
/// Projects a world-space anchor Transform into screen space every frame and renders a thin connecting
/// leader line and TextMeshProUGUI caption tag. Handles off-screen / behind-camera culling and multi-camera depth prioritization.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class LeaderLineOverlay : MonoBehaviour
{
    [Header("Tracking Target")]
    [Tooltip("World-space Transform to track in screen space.")]
    public Transform anchor;

    [Tooltip("Local-space offset from the anchor Transform position.")]
    public Vector3 anchorOffset = Vector3.zero;

    [Header("Label Content")]
    [TextArea(1, 4)]
    [Tooltip("Text string to display in the overlay tag. Supports TextMeshPro rich text formatting.")]
    public string text = "LABEL";

    [Header("Screen Space Layout")]
    [Tooltip("Screen-space pixel offset from the anchor projection to the label tag.")]
    public Vector2 screenOffset = new Vector2(100f, 60f);

    [Tooltip("Margin in pixels beyond screen boundaries before the overlay is culled.")]
    public float screenMargin = 0f;

    [Header("State")]
    [Tooltip("Master visibility toggle. When false, the overlay is hidden.")]
    public bool isOverlayActive = true;

    [Header("Camera Configuration")]
    [Tooltip("Explicit camera to use for projection. If null, automatically resolves the active enabled camera with highest depth or Camera.main.")]
    public Camera targetCamera;

    [Header("Visual Styling")]
    [Tooltip("Thickness of the leader line in pixels.")]
    public float lineWidth = 1.5f;

    [Tooltip("Color of the leader line and anchor dot.")]
    public Color lineColor = new Color(0.0f, 0.85f, 1.0f, 0.90f);

    [Tooltip("Color of the label text.")]
    public Color textColor = Color.white;

    [Tooltip("Background plate color.")]
    public Color backgroundColor = new Color(0.06f, 0.08f, 0.12f, 0.85f);

    [Tooltip("Whether to render a background plate behind the text.")]
    public bool showBackground = true;

    [Tooltip("Whether to render a small dot at the anchor point.")]
    public bool showAnchorDot = true;

    [Tooltip("Diameter of the anchor dot in pixels.")]
    public float dotSize = 4f;

    [Tooltip("Font size for TextMeshProUGUI.")]
    public float fontSize = 14f;

    [Tooltip("Optional custom TMP Font Asset. If null, falls back to LiberationSans SDF.")]
    public TMP_FontAsset fontAsset;

    [Header("UI Element References (Auto-generated if null)")]
    [SerializeField] private RectTransform dotRect;
    [SerializeField] private Image dotImage;
    [SerializeField] private RectTransform lineRect;
    [SerializeField] private Image lineImage;
    [SerializeField] private RectTransform labelRootRect;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private RectTransform labelTextRect;
    [SerializeField] private TextMeshProUGUI labelTextComponent;
    [SerializeField] private CanvasGroup canvasGroup;

    private Canvas cachedCanvas;
    private bool _isVisible = false;

    #region Public Properties

    /// <summary>
    /// Gets or sets whether the overlay is active.
    /// </summary>
    public bool IsActive
    {
        get => isOverlayActive;
        set => SetActive(value);
    }

    /// <summary>
    /// Gets or sets the label text string.
    /// </summary>
    public string Text
    {
        get => text;
        set => SetText(value);
    }

    /// <summary>
    /// Gets or sets the target world-space anchor Transform.
    /// </summary>
    public Transform Anchor
    {
        get => anchor;
        set => SetAnchor(value);
    }

    /// <summary>
    /// Gets or sets the screen-space pixel offset vector.
    /// </summary>
    public Vector2 ScreenOffset
    {
        get => screenOffset;
        set => SetScreenOffset(value);
    }

    /// <summary>
    /// Returns true if the overlay is currently visible on screen (active, in front of camera, inside screen bounds).
    /// </summary>
    public bool IsVisible => _isVisible;

    public TextMeshProUGUI TextComponent => labelTextComponent;
    public RectTransform LabelRect => labelRootRect;
    public RectTransform LineRect => lineRect;
    public RectTransform DotRect => dotRect;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        EnsureUIHierarchy();
        ApplyStyling();
    }

    private void Start()
    {
        EnsureUIHierarchy();
        ApplyStyling();
        UpdateOverlay();
    }

    private void OnEnable()
    {
        UpdateOverlay();
    }

    private void LateUpdate()
    {
        UpdateOverlay();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            EnsureUIHierarchy();
            ApplyStyling();
            UpdateOverlay();
        }
    }
#endif

    #endregion

    #region Public API Methods

    /// <summary>
    /// Activates or deactivates the overlay.
    /// </summary>
    public void SetActive(bool active)
    {
        isOverlayActive = active;
        if (!isOverlayActive)
        {
            SetVisible(false);
        }
        else
        {
            UpdateOverlay();
        }
    }

    public void Show() => SetActive(true);
    public void Hide() => SetActive(false);
    public void Toggle() => SetActive(!isOverlayActive);

    /// <summary>
    /// Sets the label text and triggers layout recalculation.
    /// </summary>
    public void SetText(string newText)
    {
        text = newText;
        if (labelTextComponent != null)
        {
            labelTextComponent.text = newText;
            if (labelRootRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(labelRootRect);
            }
        }
    }

    /// <summary>
    /// Sets the world-space target Transform and optional local offset.
    /// </summary>
    public void SetAnchor(Transform targetAnchor, Vector3 offset = default)
    {
        anchor = targetAnchor;
        anchorOffset = offset;
        UpdateOverlay();
    }

    /// <summary>
    /// Sets the screen-space offset vector from anchor to label.
    /// </summary>
    public void SetScreenOffset(Vector2 offset)
    {
        screenOffset = offset;
        UpdateOverlay();
    }

    /// <summary>
    /// Updates line, text, and background colors.
    /// </summary>
    public void SetColors(Color lineAndDotColor, Color textCol, Color? bgCol = null)
    {
        lineColor = lineAndDotColor;
        textColor = textCol;
        if (bgCol.HasValue)
        {
            backgroundColor = bgCol.Value;
        }
        ApplyStyling();
    }

    /// <summary>
    /// Re-evaluates hierarchy, styling, and performs an immediate projection update.
    /// </summary>
    public void RefreshLayout()
    {
        EnsureUIHierarchy();
        ApplyStyling();
        UpdateOverlay();
    }

    /// <summary>
    /// Resolves the camera to use for screen projection.
    /// Returns targetCamera if explicitly set and active; otherwise selects the enabled camera with the highest depth, or Camera.main.
    /// </summary>
    public Camera GetTargetCamera()
    {
        if (targetCamera != null && targetCamera.isActiveAndEnabled)
        {
            return targetCamera;
        }

        Camera bestCam = null;
        float highestDepth = float.MinValue;

        Camera[] cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam != null && cam.isActiveAndEnabled)
            {
                if (cam.depth > highestDepth)
                {
                    highestDepth = cam.depth;
                    bestCam = cam;
                }
            }
        }

        if (bestCam != null)
        {
            return bestCam;
        }

        if (Camera.main != null && Camera.main.isActiveAndEnabled)
        {
            return Camera.main;
        }

        return null;
    }

    #endregion

    #region Layout & Projection Engine

    /// <summary>
    /// Calculates screen projection of the anchor and positions the dot, line, and label.
    /// Gracefully hides the overlay if behind camera or off-screen.
    /// </summary>
    public void UpdateOverlay()
    {
        if (!isOverlayActive || anchor == null)
        {
            SetVisible(false);
            return;
        }

        Camera cam = GetTargetCamera();
        if (cam == null)
        {
            SetVisible(false);
            return;
        }

        Canvas rootCanvas = GetRootCanvas();
        if (rootCanvas == null)
        {
            SetVisible(false);
            return;
        }

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect == null)
        {
            SetVisible(false);
            return;
        }

        // Calculate world anchor position with local offset
        Vector3 worldAnchorPos = anchor.position + anchor.TransformVector(anchorOffset);
        Vector3 screenPoint = cam.WorldToScreenPoint(worldAnchorPos);

        // 1. Cull if behind camera plane
        if (screenPoint.z <= 0f)
        {
            SetVisible(false);
            return;
        }

        // 2. Cull if outside screen boundaries
        if (screenPoint.x < -screenMargin || screenPoint.x > Screen.width + screenMargin ||
            screenPoint.y < -screenMargin || screenPoint.y > Screen.height + screenMargin)
        {
            SetVisible(false);
            return;
        }

        Camera canvasCamera = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : (rootCanvas.worldCamera != null ? rootCanvas.worldCamera : cam);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, canvasCamera, out Vector2 localAnchorPos))
        {
            SetVisible(false);
            return;
        }

        // Anchor is valid and in front of camera
        SetVisible(true);

        // 1. Position Anchor Dot
        if (dotRect != null)
        {
            dotRect.anchoredPosition = localAnchorPos;
            dotRect.sizeDelta = new Vector2(dotSize, dotSize);
            if (dotImage != null)
            {
                dotImage.enabled = showAnchorDot;
                dotImage.color = lineColor;
            }
        }

        // 2. Position Label Container & Adjust Pivot based on offset direction
        Vector2 labelPos = localAnchorPos + screenOffset;
        Vector2 labelPivot = GetLabelPivot(screenOffset);

        if (labelRootRect != null)
        {
            labelRootRect.pivot = labelPivot;
            labelRootRect.anchoredPosition = labelPos;

            if (backgroundImage != null)
            {
                backgroundImage.enabled = showBackground;
                backgroundImage.color = backgroundColor;
            }
        }

        if (labelTextComponent != null)
        {
            if (labelTextComponent.text != text)
            {
                labelTextComponent.text = text;
            }
            labelTextComponent.fontSize = fontSize;
            labelTextComponent.color = textColor;
            if (fontAsset != null && labelTextComponent.font != fontAsset)
            {
                labelTextComponent.font = fontAsset;
            }
        }

        // 3. Position and stretch Leader Line from anchor to label
        if (lineRect != null && lineImage != null)
        {
            float distance = screenOffset.magnitude;
            if (distance > 0.5f)
            {
                lineImage.enabled = true;
                lineImage.color = lineColor;
                lineRect.pivot = new Vector2(0f, 0.5f);
                lineRect.anchoredPosition = localAnchorPos;
                lineRect.sizeDelta = new Vector2(distance, lineWidth);

                float angle = Mathf.Atan2(screenOffset.y, screenOffset.x) * Mathf.Rad2Deg;
                lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            else
            {
                lineImage.enabled = false;
            }
        }
    }

    private static Vector2 GetLabelPivot(Vector2 offset)
    {
        if (Mathf.Abs(offset.x) > 0.001f)
        {
            return new Vector2(offset.x >= 0f ? 0f : 1f, 0.5f);
        }
        else
        {
            return new Vector2(0.5f, offset.y >= 0f ? 0f : 1f);
        }
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
        }
        _isVisible = visible;
    }

    private Canvas GetRootCanvas()
    {
        if (cachedCanvas != null) return cachedCanvas;
        cachedCanvas = GetComponentInParent<Canvas>();
        if (cachedCanvas != null)
        {
            cachedCanvas = cachedCanvas.rootCanvas;
        }
        return cachedCanvas;
    }

    #endregion

    #region Hierarchy & Styling Initialization

    /// <summary>
    /// Builds or binds child UI elements (AnchorDot, LeaderLine, LabelRoot, TextMeshProUGUI) automatically.
    /// </summary>
    public void EnsureUIHierarchy()
    {
        RectTransform rootRect = GetComponent<RectTransform>();
        if (rootRect == null) rootRect = gameObject.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // 1. Anchor Dot
        if (dotRect == null)
        {
            Transform dotTransform = transform.Find("AnchorDot");
            GameObject dotGo = dotTransform != null ? dotTransform.gameObject : new GameObject("AnchorDot", typeof(RectTransform), typeof(Image));
            if (dotTransform == null) dotGo.transform.SetParent(transform, false);
            dotRect = dotGo.GetComponent<RectTransform>();
            dotImage = dotGo.GetComponent<Image>();
        }
        if (dotImage != null) dotImage.raycastTarget = false;
        dotRect.pivot = new Vector2(0.5f, 0.5f);
        dotRect.sizeDelta = new Vector2(dotSize, dotSize);

        // 2. Leader Line
        if (lineRect == null)
        {
            Transform lineTransform = transform.Find("LeaderLine");
            GameObject lineGo = lineTransform != null ? lineTransform.gameObject : new GameObject("LeaderLine", typeof(RectTransform), typeof(Image));
            if (lineTransform == null) lineGo.transform.SetParent(transform, false);
            lineRect = lineGo.GetComponent<RectTransform>();
            lineImage = lineGo.GetComponent<Image>();
        }
        if (lineImage != null) lineImage.raycastTarget = false;
        lineRect.pivot = new Vector2(0f, 0.5f);

        // 3. Label Root
        if (labelRootRect == null)
        {
            Transform labelTransform = transform.Find("LabelRoot");
            GameObject labelGo = labelTransform != null ? labelTransform.gameObject : new GameObject("LabelRoot", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            if (labelTransform == null) labelGo.transform.SetParent(transform, false);
            labelRootRect = labelGo.GetComponent<RectTransform>();
            backgroundImage = labelGo.GetComponent<Image>();
        }
        if (backgroundImage != null) backgroundImage.raycastTarget = false;

        HorizontalLayoutGroup layout = labelRootRect.GetComponent<HorizontalLayoutGroup>();
        if (layout == null) layout = labelRootRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 4, 4);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = labelRootRect.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = labelRootRect.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 4. Label Text
        if (labelTextComponent == null)
        {
            Transform textTransform = labelRootRect.Find("LabelText");
            GameObject textGo = textTransform != null ? textTransform.gameObject : new GameObject("LabelText", typeof(RectTransform), typeof(TextMeshProUGUI));
            if (textTransform == null) textGo.transform.SetParent(labelRootRect, false);
            labelTextRect = textGo.GetComponent<RectTransform>();
            labelTextComponent = textGo.GetComponent<TextMeshProUGUI>();
        }
        if (labelTextComponent != null)
        {
            labelTextComponent.raycastTarget = false;
            labelTextComponent.textWrappingMode = TextWrappingModes.NoWrap;
            labelTextComponent.overflowMode = TextOverflowModes.Overflow;
            labelTextComponent.text = text;
            labelTextComponent.fontSize = fontSize;
            labelTextComponent.color = textColor;

            if (fontAsset != null)
            {
                labelTextComponent.font = fontAsset;
            }
            else if (labelTextComponent.font == null)
            {
                TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (defaultFont != null)
                {
                    labelTextComponent.font = defaultFont;
                }
            }
        }
    }

    /// <summary>
    /// Applies visual colors and font settings to all child UI components.
    /// </summary>
    public void ApplyStyling()
    {
        if (dotImage != null)
        {
            dotImage.enabled = showAnchorDot;
            dotImage.color = lineColor;
        }
        if (dotRect != null)
        {
            dotRect.sizeDelta = new Vector2(dotSize, dotSize);
        }

        if (lineImage != null)
        {
            lineImage.color = lineColor;
        }

        if (backgroundImage != null)
        {
            backgroundImage.enabled = showBackground;
            backgroundImage.color = backgroundColor;
        }

        if (labelTextComponent != null)
        {
            labelTextComponent.text = text;
            labelTextComponent.fontSize = fontSize;
            labelTextComponent.color = textColor;
            if (fontAsset != null)
            {
                labelTextComponent.font = fontAsset;
            }
        }
    }

    #endregion

    #region Static Helpers & Factory

    /// <summary>
    /// Finds or creates a Screen Space - Overlay UI Canvas configured for leader line overlays.
    /// </summary>
    public static Canvas GetOrCreateDefaultCanvas()
    {
        const string canvasName = "LeaderLineCanvas";
        GameObject canvasObj = GameObject.Find(canvasName);
        if (canvasObj != null)
        {
            Canvas c = canvasObj.GetComponent<Canvas>();
            if (c != null) return c;
        }

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i].isRootCanvas && canvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return canvases[i];
            }
        }

        canvasObj = new GameObject(canvasName);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        return canvas;
    }

    /// <summary>
    /// Creates and configures a LeaderLineOverlay instance tracking the given anchor.
    /// </summary>
    /// <param name="anchor">World-space Transform to track.</param>
    /// <param name="text">Label caption text.</param>
    /// <param name="screenOffset">Screen-space pixel offset (optional, defaults to (100, 60)).</param>
    /// <param name="parentCanvas">Parent Canvas or Transform (optional, defaults to LeaderLineCanvas).</param>
    /// <returns>The created LeaderLineOverlay component.</returns>
    public static LeaderLineOverlay Create(Transform anchor, string text, Vector2? screenOffset = null, Transform parentCanvas = null)
    {
        Canvas canvas = null;
        if (parentCanvas != null)
        {
            canvas = parentCanvas.GetComponentInParent<Canvas>();
        }
        if (canvas == null)
        {
            canvas = GetOrCreateDefaultCanvas();
        }

        string objName = anchor != null ? $"LeaderLineOverlay_{anchor.name}" : "LeaderLineOverlay";
        GameObject go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);

        LeaderLineOverlay overlay = go.AddComponent<LeaderLineOverlay>();
        overlay.anchor = anchor;
        overlay.text = text;
        if (screenOffset.HasValue)
        {
            overlay.screenOffset = screenOffset.Value;
        }

        overlay.EnsureUIHierarchy();
        overlay.ApplyStyling();
        overlay.UpdateOverlay();

        return overlay;
    }

    #endregion
}
