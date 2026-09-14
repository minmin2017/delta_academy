using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility for attaching and wiring the TelemetryOverlayController component.
/// Provides Tools/Delta/Attach Telemetry Overlay menu item following the standard cell attachment pattern.
/// </summary>
public static class TelemetryOverlayAttacher
{
    private const string RootName = "IndustrialCell_Delta";

    [MenuItem("Tools/Delta/Attach Telemetry Overlay")]
    public static void AttachTelemetryOverlay()
    {
        // 1. Locate the target GameObject (prefer ChangeoverSequencer's host, fallback to scene root)
        GameObject targetObj = null;
        ChangeoverSequencer sequencer = Object.FindAnyObjectByType<ChangeoverSequencer>();
        if (sequencer != null)
        {
            targetObj = sequencer.gameObject;
        }
        else
        {
            targetObj = GameObject.Find(RootName);
        }

        if (targetObj == null)
        {
            Debug.LogError($"[TelemetryOverlayAttacher] Neither ChangeoverSequencer nor '{RootName}' found in scene! Run Tools/Delta/Build Cell or Tools/Delta/Attach Sequencer first.");
            return;
        }

        // 2. Add or get TelemetryOverlayController component (idempotent)
        TelemetryOverlayController controller = targetObj.GetComponent<TelemetryOverlayController>();
        if (controller == null)
        {
            controller = Undo.AddComponent<TelemetryOverlayController>(targetObj);
        }

        // 3. Wire references
        if (sequencer == null)
        {
            sequencer = targetObj.GetComponent<ChangeoverSequencer>();
            if (sequencer == null)
            {
                sequencer = Object.FindAnyObjectByType<ChangeoverSequencer>();
            }
        }
        controller.sequencer = sequencer;

        if (sequencer != null)
        {
            if (sequencer.guideRailAssembly != null)
            {
                controller.guideRailAssembly = sequencer.guideRailAssembly;
            }
            if (sequencer.nozzleAssembly != null)
            {
                controller.nozzleAssembly = sequencer.nozzleAssembly;
            }
        }

        if (controller.guideRailAssembly == null)
        {
            GameObject railObj = GameObject.Find("GuideRailAssembly_X");
            if (railObj != null) controller.guideRailAssembly = railObj.transform;
        }

        if (controller.nozzleAssembly == null)
        {
            GameObject nozzleObj = GameObject.Find("NozzleAssembly_Z");
            if (nozzleObj != null) controller.nozzleAssembly = nozzleObj.transform;
        }

        // 4. Ensure Screen Space - Overlay UI Canvas exists
        Canvas canvas = LeaderLineOverlay.GetOrCreateDefaultCanvas();
        if (canvas != null)
        {
            Undo.RegisterCreatedObjectUndo(canvas.gameObject, "Ensure LeaderLineCanvas");
        }

        controller.ValidateAndCacheReferences();
        EditorUtility.SetDirty(targetObj);

        Debug.Log($"[TelemetryOverlayAttacher] TelemetryOverlayController attached to '{targetObj.name}' and references wired successfully. NOTE: this uses a Screen Space - Overlay canvas which is NOT captured by the RenderTexture-based Recorder pipeline - use 'Attach Motion Highlights' below for a render-safe version.");
    }

    /// <summary>
    /// Min's feedback (2026-09-14): rail/nozzle motion is too subtle to notice in a locked wide
    /// shot; wants a highlight/hologram effect. MotionHighlightController does this with a
    /// pulsing emissive glow + world-space live mm readout - unlike TelemetryOverlayController
    /// above (Screen Space - Overlay), this one IS captured by the Recorder pipeline because it
    /// uses world-space TextMeshPro, the same pattern already proven in every render this
    /// session (ZoneLabelsBuilder, CappingZoneBuilder).
    /// </summary>
    [MenuItem("Tools/Delta/Attach Motion Highlights")]
    public static void AttachMotionHighlights()
    {
        ChangeoverSequencer sequencer = Object.FindAnyObjectByType<ChangeoverSequencer>();
        GameObject targetObj = (sequencer != null) ? sequencer.gameObject : GameObject.Find(RootName);
        if (targetObj == null)
        {
            Debug.LogError($"[TelemetryOverlayAttacher] Neither ChangeoverSequencer nor '{RootName}' found in scene! Run Tools/Delta/Build Full Line first.");
            return;
        }

        MotionHighlightController controller = targetObj.GetComponent<MotionHighlightController>();
        if (controller == null)
        {
            controller = Undo.AddComponent<MotionHighlightController>(targetObj);
        }

        controller.sequencer = sequencer;
        if (sequencer != null)
        {
            if (sequencer.guideRailAssembly != null) controller.guideRailAssembly = sequencer.guideRailAssembly;
            if (sequencer.nozzleAssembly != null) controller.nozzleAssembly = sequencer.nozzleAssembly;
        }
        if (controller.guideRailAssembly == null)
        {
            GameObject railObj = GameObject.Find("GuideRailAssembly_X");
            if (railObj != null) controller.guideRailAssembly = railObj.transform;
        }
        if (controller.nozzleAssembly == null)
        {
            GameObject nozzleObj = GameObject.Find("NozzleAssembly_Z");
            if (nozzleObj != null) controller.nozzleAssembly = nozzleObj.transform;
        }

        controller.ValidateAndCacheReferences();
        EditorUtility.SetDirty(targetObj);

        Debug.Log($"[TelemetryOverlayAttacher] MotionHighlightController attached to '{targetObj.name}' - pulsing glow + live mm readout on rail (S7) and nozzle (S6-S9), render-safe (world-space, not Screen Space Overlay).");
    }
}
