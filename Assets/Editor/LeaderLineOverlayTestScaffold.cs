using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor test scaffold for verifying LeaderLineOverlay component functioning.
/// Provides Tools/Delta menu items to create an idempotent test instance or remove it cleanly.
/// </summary>
public static class LeaderLineOverlayTestScaffold
{
    private const string TestOverlayName = "TestLeaderLineOverlay";
    private const string CanvasName = "LeaderLineCanvas";

    /// <summary>
    /// Creates or refreshes a single test LeaderLineOverlay instance in the scene.
    /// Anchors to "GuideRail_Left_30mm" if present; otherwise falls back to any Camera or scene object.
    /// </summary>
    [MenuItem("Tools/Delta/Test Leader Line Overlay")]
    public static void TestLeaderLineOverlay()
    {
        // 1. Remove any existing test instance first to ensure idempotence
        ClearTestLeaderLineOverlay();

        // 2. Find anchor Transform: prioritize GuideRail_Left_30mm, then other Delta props, then Camera
        Transform anchorTransform = null;

        GameObject railObj = GameObject.Find("GuideRail_Left_30mm");
        if (railObj != null)
        {
            anchorTransform = railObj.transform;
        }

        if (anchorTransform == null)
        {
            GameObject plcObj = GameObject.Find("Delta_AS320T_B_PLC");
            if (plcObj != null) anchorTransform = plcObj.transform;
        }

        if (anchorTransform == null)
        {
            GameObject nozzleObj = GameObject.Find("NozzleAssembly_Z");
            if (nozzleObj != null) anchorTransform = nozzleObj.transform;
        }

        if (anchorTransform == null)
        {
            Camera anyCam = Object.FindAnyObjectByType<Camera>();
            if (anyCam != null) anchorTransform = anyCam.transform;
        }

        if (anchorTransform == null)
        {
            Debug.LogWarning("[LeaderLineOverlayTestScaffold] No scene objects found. Creating a temporary anchor object at (0, 1, 0).");
            GameObject dummy = new GameObject("Dummy_Anchor_Target");
            dummy.transform.position = new Vector3(0f, 1f, 0f);
            Undo.RegisterCreatedObjectUndo(dummy, "Create Dummy Anchor Target");
            anchorTransform = dummy.transform;
        }

        // 3. Ensure Screen Space - Overlay UI Canvas exists
        Canvas canvas = LeaderLineOverlay.GetOrCreateDefaultCanvas();
        Undo.RegisterCreatedObjectUndo(canvas.gameObject, "Create LeaderLine Canvas");

        // 4. Create and configure the test overlay instance
        string labelText = $"<b><color=#00D8FF>{anchorTransform.name}</color></b>\n<size=80%><color=#D0E0F0>TEST LABEL · LeaderLineOverlay</color></size>";
        Vector2 testOffset = new Vector2(120f, 70f);

        LeaderLineOverlay overlay = LeaderLineOverlay.Create(
            anchor: anchorTransform,
            text: labelText,
            screenOffset: testOffset,
            parentCanvas: canvas.transform
        );

        overlay.gameObject.name = TestOverlayName;
        Undo.RegisterCreatedObjectUndo(overlay.gameObject, "Create Test Leader Line Overlay");

        Selection.activeGameObject = overlay.gameObject;
        EditorUtility.SetDirty(overlay.gameObject);

        Debug.Log($"[LeaderLineOverlayTestScaffold] Test overlay '{TestOverlayName}' successfully created, anchored to '{anchorTransform.name}'. Offset: {testOffset}. Select Game/Scene view to verify.");
    }

    /// <summary>
    /// Removes the test overlay instance and cleans up empty canvas if unused.
    /// </summary>
    [MenuItem("Tools/Delta/Clear Test Leader Line Overlay")]
    public static void ClearTestLeaderLineOverlay()
    {
        GameObject existingTestObj = GameObject.Find(TestOverlayName);
        if (existingTestObj != null)
        {
            Undo.DestroyObjectImmediate(existingTestObj);
            Debug.Log($"[LeaderLineOverlayTestScaffold] Removed '{TestOverlayName}'.");
        }

        // Clean up empty canvas if no other overlays remain
        GameObject canvasObj = GameObject.Find(CanvasName);
        if (canvasObj != null && canvasObj.transform.childCount == 0)
        {
            Undo.DestroyObjectImmediate(canvasObj);
            Debug.Log($"[LeaderLineOverlayTestScaffold] Removed empty '{CanvasName}'.");
        }
    }
}
