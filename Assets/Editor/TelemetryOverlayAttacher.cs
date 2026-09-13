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

        Debug.Log($"[TelemetryOverlayAttacher] TelemetryOverlayController attached to '{targetObj.name}' and references wired successfully.");
    }
}
