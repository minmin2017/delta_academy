using UnityEngine;

/// <summary>
/// TelemetryOverlayController - Manages live screen-space telemetry leader-line overlays
/// for the KMITL Delta Academy changeover sequence demonstration.
/// Creates and updates overlays tracking:
/// 1. Guide Rail Assembly width (CurrentRailGapMm) during S7_AdjustRailWidth.
/// 2. Nozzle Assembly height (CurrentNozzleHeightMm) during S6_RetractNozzleToHome through S9_FirstArticleCheck.
/// </summary>
public class TelemetryOverlayController : MonoBehaviour
{
    [Header("Changeover Sequencer Reference")]
    [Tooltip("Reference to the ChangeoverSequencer driving the state machine.")]
    public ChangeoverSequencer sequencer;

    [Header("Tracking Anchors")]
    [Tooltip("World-space Transform anchor for guide rail width telemetry (GuideRailAssembly_X).")]
    public Transform guideRailAssembly;

    [Tooltip("World-space Transform anchor for nozzle height telemetry (NozzleAssembly_Z).")]
    public Transform nozzleAssembly;

    [Header("Overlay Instances (Created at Runtime)")]
    [Tooltip("LeaderLineOverlay instance tracking the guide rails.")]
    public LeaderLineOverlay railOverlay;

    [Tooltip("LeaderLineOverlay instance tracking the nozzle assembly.")]
    public LeaderLineOverlay nozzleOverlay;

    [Header("Screen Layout Configuration")]
    [Tooltip("Screen-space pixel offset for the guide rail overlay tag.")]
    public Vector2 railScreenOffset = new Vector2(100f, 60f);

    [Tooltip("Screen-space pixel offset for the nozzle height overlay tag.")]
    public Vector2 nozzleScreenOffset = new Vector2(100f, 60f);

    private void Awake()
    {
        ValidateAndCacheReferences();
    }

    private void Start()
    {
        ValidateAndCacheReferences();
        InitializeOverlays();
    }

    /// <summary>
    /// Finds and caches ChangeoverSequencer and anchor transforms if not already assigned.
    /// </summary>
    public void ValidateAndCacheReferences()
    {
        if (sequencer == null)
        {
            sequencer = GetComponent<ChangeoverSequencer>();
            if (sequencer == null)
            {
                sequencer = Object.FindAnyObjectByType<ChangeoverSequencer>();
            }
        }

        if (guideRailAssembly == null)
        {
            if (sequencer != null && sequencer.guideRailAssembly != null)
            {
                guideRailAssembly = sequencer.guideRailAssembly;
            }
            else
            {
                GameObject railObj = GameObject.Find("GuideRailAssembly_X");
                if (railObj != null) guideRailAssembly = railObj.transform;
            }
        }

        if (nozzleAssembly == null)
        {
            if (sequencer != null && sequencer.nozzleAssembly != null)
            {
                nozzleAssembly = sequencer.nozzleAssembly;
            }
            else
            {
                GameObject nozzleObj = GameObject.Find("NozzleAssembly_Z");
                if (nozzleObj != null) nozzleAssembly = nozzleObj.transform;
            }
        }
    }

    /// <summary>
    /// Creates and configures the two LeaderLineOverlay instances on the default Screen Space - Overlay UI Canvas.
    /// Idempotent: reuses existing instances if already created.
    /// </summary>
    public void InitializeOverlays()
    {
        Canvas canvas = LeaderLineOverlay.GetOrCreateDefaultCanvas();
        Transform parentTransform = (canvas != null) ? canvas.transform : null;

        // 1. Guide Rail Width Overlay
        if (railOverlay == null)
        {
            if (parentTransform != null)
            {
                Transform existing = parentTransform.Find("LeaderLineOverlay_GuideRail");
                if (existing != null)
                {
                    railOverlay = existing.GetComponent<LeaderLineOverlay>();
                }
            }

            if (railOverlay == null)
            {
                railOverlay = LeaderLineOverlay.Create(
                    anchor: guideRailAssembly,
                    text: "GUIDE RAIL WIDTH\n0.0 mm",
                    screenOffset: railScreenOffset,
                    parentCanvas: parentTransform
                );
                railOverlay.gameObject.name = "LeaderLineOverlay_GuideRail";
            }
        }

        if (railOverlay != null)
        {
            if (guideRailAssembly != null && railOverlay.anchor == null)
            {
                railOverlay.SetAnchor(guideRailAssembly);
            }
            railOverlay.SetActive(false);
        }

        // 2. Nozzle Height Overlay
        if (nozzleOverlay == null)
        {
            if (parentTransform != null)
            {
                Transform existing = parentTransform.Find("LeaderLineOverlay_Nozzle");
                if (existing != null)
                {
                    nozzleOverlay = existing.GetComponent<LeaderLineOverlay>();
                }
            }

            if (nozzleOverlay == null)
            {
                nozzleOverlay = LeaderLineOverlay.Create(
                    anchor: nozzleAssembly,
                    text: "NOZZLE HEIGHT\n0.0 mm",
                    screenOffset: nozzleScreenOffset,
                    parentCanvas: parentTransform
                );
                nozzleOverlay.gameObject.name = "LeaderLineOverlay_Nozzle";
            }
        }

        if (nozzleOverlay != null)
        {
            if (nozzleAssembly != null && nozzleOverlay.anchor == null)
            {
                nozzleOverlay.SetAnchor(nozzleAssembly);
            }
            nozzleOverlay.SetActive(false);
        }
    }

    private void Update()
    {
        if (sequencer == null)
        {
            ValidateAndCacheReferences();
            if (sequencer == null) return;
        }

        // Ensure anchor references are wired to overlays
        if (railOverlay != null && railOverlay.anchor == null && guideRailAssembly != null)
        {
            railOverlay.SetAnchor(guideRailAssembly);
        }
        if (nozzleOverlay != null && nozzleOverlay.anchor == null && nozzleAssembly != null)
        {
            nozzleOverlay.SetAnchor(nozzleAssembly);
        }

        ChangeoverSequencer.ChangeoverState state = sequencer.CurrentState;

        // Visibility rules based on ChangeoverState:
        // - Rail Width overlay: active ONLY during S7_AdjustRailWidth
        // - Nozzle Height overlay: active during S6_RetractNozzleToHome through S9_FirstArticleCheck
        bool isRailActive = (state == ChangeoverSequencer.ChangeoverState.S7_AdjustRailWidth);
        bool isNozzleActive = (state >= ChangeoverSequencer.ChangeoverState.S6_RetractNozzleToHome &&
                               state <= ChangeoverSequencer.ChangeoverState.S9_FirstArticleCheck);

        // Update Rail Width Overlay
        if (railOverlay != null)
        {
            if (railOverlay.isOverlayActive != isRailActive)
            {
                railOverlay.SetActive(isRailActive);
            }

            if (isRailActive)
            {
                float railGapMm = sequencer.CurrentRailGapMm;
                string railText = $"GUIDE RAIL WIDTH\n{railGapMm:F1} mm";
                if (railOverlay.text != railText)
                {
                    railOverlay.SetText(railText);
                }
            }
        }

        // Update Nozzle Height Overlay
        if (nozzleOverlay != null)
        {
            if (nozzleOverlay.isOverlayActive != isNozzleActive)
            {
                nozzleOverlay.SetActive(isNozzleActive);
            }

            if (isNozzleActive)
            {
                float nozzleHeightMm = sequencer.CurrentNozzleHeightMm;
                string nozzleText = $"NOZZLE HEIGHT\n{nozzleHeightMm:F1} mm";
                if (nozzleOverlay.text != nozzleText)
                {
                    nozzleOverlay.SetText(nozzleText);
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (railOverlay != null && railOverlay.gameObject != null)
        {
            Destroy(railOverlay.gameObject);
        }
        if (nozzleOverlay != null && nozzleOverlay.gameObject != null)
        {
            Destroy(nozzleOverlay.gameObject);
        }
    }
}
