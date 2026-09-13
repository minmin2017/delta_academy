using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Idempotent Editor builder for the Traveling Establishing Camera (TravelingEstablishingCam).
/// Creates/rebuilds the camera additively under 'Cell' for the A_LineOverview.mp4 establishing shot.
/// Configures camera depth (98), disables it by default so it does not interfere with the
/// changeover cameras or CameraDirector, and attaches the TravelingCamera animation script.
/// </summary>
public static class TravelingCameraBuilder
{
    private const string RootName = "Cell";
    private const string CameraName = "TravelingEstablishingCam";

    [MenuItem("Tools/Delta/Add Traveling Establishing Camera")]
    public static void AddTravelingCamera()
    {
        // 1. Locate root Cell
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = GameObject.Find("IndustrialCell_Delta");
        }

        if (root == null)
        {
            Debug.LogError($"[TravelingCameraBuilder] Scene root '{RootName}' not found! Run Tools/Delta/Build Cell first.");
            return;
        }

        // 2. Delete existing camera instance if present (idempotent find-and-replace pattern)
        Camera[] existingCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (int i = 0; i < existingCams.Length; i++)
        {
            if (existingCams[i] != null && existingCams[i].gameObject.name == CameraName)
            {
                Undo.DestroyObjectImmediate(existingCams[i].gameObject);
            }
        }

        Transform existingCamChild = root.transform.Find(CameraName);
        if (existingCamChild != null)
        {
            Undo.DestroyObjectImmediate(existingCamChild.gameObject);
        }

        // 3. Create Camera GameObject parented under Cell
        GameObject camObj = new GameObject(CameraName);
        Undo.RegisterCreatedObjectUndo(camObj, "Add Traveling Establishing Camera");
        camObj.transform.SetParent(root.transform);

        // Real-world Z coordinates derived from line geometry:
        // - Upstream Infeed Unscrambler: Z = -3.20m (disc edge at Z = -3.575m). Start at Z = -4.80m for full line entry framing.
        // - Downstream EndOfLine Outfeed: Z = +2.70m (belt end at Z = +3.90m). End at Z = +4.50m for full exit sweep.
        // - Operator aisle offset: X = +2.20m (clear view of conveyor and cabinet/tanks on opposite side).
        // - Elevation: Y = 1.80m (elevated 3/4 angle above 0.90m conveyor belt).
        // Pulled back and raised from the original (2.20,1.80,-4.80) start - live-verified via
        // unityMCP that the original framing cropped the top of DeltaControlCabinet/ProductTank.
        Vector3 startPos = new Vector3(3.00f, 2.60f, -6.50f);
        Vector3 endPos = new Vector3(3.00f, 2.60f, 6.20f);
        Vector3 lookOffset = new Vector3(0.0f, 0.90f, 2.50f);
        float duration = 10.0f;

        camObj.transform.position = startPos;
        Vector3 initialTarget = new Vector3(lookOffset.x, lookOffset.y, startPos.z + lookOffset.z);
        camObj.transform.LookAt(initialTarget);

        // Configure Camera component matching project conventions
        Camera cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 45f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 50f;
        cam.depth = 98f; // Below Wide (99), Hero (100), ControlCam_Close (101); above RailTopCam (90), NozzleSideCam (91)
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.06f, 1f);
        cam.enabled = false; // DISABLED by default per specification (manual activation for clip A only)

        // Enable URP Post-Processing on camera
        UniversalAdditionalCameraData camData = camObj.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;

        // Attach and configure TravelingCamera runtime script
        TravelingCamera travelComp = camObj.AddComponent<TravelingCamera>();
        travelComp.startPosition = startPos;
        travelComp.endPosition = endPos;
        travelComp.lookOffset = lookOffset;
        travelComp.duration = duration;
        travelComp.autoPlayOnEnable = true;

        EditorUtility.SetDirty(root);
        Selection.activeGameObject = camObj;
        Debug.Log($"[TravelingCameraBuilder] '{CameraName}' created successfully under '{RootName}' (depth: 98, enabled: false, start: {startPos}, end: {endPos}, duration: {duration}s).");
    }
}
