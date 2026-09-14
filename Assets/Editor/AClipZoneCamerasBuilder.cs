using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Idempotent Editor builder for the locked zone-callout cameras and time-driven switcher
/// for the A_LineOverview.mp4 clip's post-establishing-shot section.
/// 
/// Creates 4 locked-angle cameras additively under 'Cell':
/// 1. InfeedZoneCam (depth 80) - frames Unscrambler + Infeed Conveyor + Star Wheel + Bottle Sensor + Stopper
/// 2. FillingZoneCam (depth 81) - frames Product Tank + Pump + Flow Meter + Anti-Drip Valve + Nozzle/Bottle
/// 3. CappingZoneCam (depth 82) - frames Cap Feeder + Capping Head + Cap Present Sensor
/// 4. EndOfLineCam (depth 83) - frames Checkweigher + Reject Station + Labeling Machine + Outfeed Conveyor
/// 
/// Follows the exact camera creation pattern of CellCam_Hero / RailTopCam / ControlCam_Close:
/// charcoal solid color background, ACES post-processing enabled, disabled by default, depths below 90.
/// 
/// Also provides 'Attach A-Clip Shot Switcher' to wire TimedShotSwitcher with the sequence:
/// InfeedZoneCam (14s) -> FillingZoneCam (24s) -> CappingZoneCam (10s) -> EndOfLineCam (12s) -> ControlCam_Close (20s).
/// </summary>
public static class AClipZoneCamerasBuilder
{
    private const string RootName = "Cell";
    private const string FallbackRootName = "IndustrialCell_Delta";

    public const string InfeedCamName = "InfeedZoneCam";
    public const string FillingCamName = "FillingZoneCam";
    public const string CappingCamName = "CappingZoneCam";
    public const string EndOfLineCamName = "EndOfLineCam";
    public const string ControlCamName = "ControlCam_Close";

    [MenuItem("Tools/Delta/Add A-Clip Zone Cameras")]
    public static void AddAClipZoneCameras()
    {
        // 1. Locate root Cell
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = GameObject.Find(FallbackRootName);
        }

        if (root == null)
        {
            Debug.LogError($"[AClipZoneCamerasBuilder] Scene root '{RootName}' not found! Run Tools/Delta/Build Cell first.");
            return;
        }

        Color charcoalBg = new Color(0.05f, 0.05f, 0.06f, 1f);

        // =========================================================================
        // 1. INFEED ZONE CAMERA (InfeedZoneCam)
        // Three-quarter angle framing Unscrambler (Z = -3.20m), Infeed Conveyor
        // (Z = -2.70m to -1.50m), Star Wheel (Z = -2.35m), Sensor (Z = -1.90m),
        // and Stopper Cylinder (Z = -2.10m).
        // Target center: (0.00, 0.85, -2.55).
        // Camera pos: (1.90, 1.75, -1.35), looking upstream-inwards at ~22° pitch.
        // =========================================================================
        CreateOrReplaceCamera(root, InfeedCamName,
            new Vector3(1.90f, 1.75f, -1.35f),
            new Vector3(0.00f, 0.85f, -2.55f),
            fieldOfView: 40f,
            depth: 80f,
            charcoalBg);

        // =========================================================================
        // 2. FILLING ZONE CAMERA (FillingZoneCam)
        // Three-quarter angle framing Product Tank (X = -0.70m, Z = -1.20m),
        // Pump (X = -0.45m, Z = -1.20m), Flow Meter (X = -0.22m, Z = -1.20m),
        // Anti-Drip Valve (X = -0.09m, Z = -1.20m), Pipe Run, and the real
        // NozzleAssembly_Z (X = 0.00m, Z = +0.60m) / PET Bottle on conveyor.
        // Target center: (-0.35, 1.15, -0.35).
        // Camera pos: (1.80, 1.80, 0.40), looking upstream-across at ~16° pitch.
        // =========================================================================
        CreateOrReplaceCamera(root, FillingCamName,
            new Vector3(1.80f, 1.80f, 0.40f),
            new Vector3(-0.35f, 1.15f, -0.35f),
            fieldOfView: 40f,
            depth: 81f,
            charcoalBg);

        // =========================================================================
        // 3. CAPPING ZONE CAMERA (CappingZoneCam)
        // Second repositioning (2026-09-14) - the previous overhead angle (0.90,2.30,1.15)
        // ->(0.00,1.00,1.15) fixed the DeltaControlCabinet overlap but still visually
        // overlapped the leftover Filling-zone demo bottle (parked at Z=0.60, belongs to
        // the B_Changeover nozzle demo, not this zone) with Gantry_VerticalColumn_60x60mm
        // (X=0.00, Z=0.75-0.81, Y=0.88-1.72) - Min flagged this as the bottle "clipping
        // through a post" around t=44s of A_LineOverview_ZONES. Real capping equipment
        // spans Z=0.89 (CapFeeder_Hopper near edge) to Z=1.35 (CappingHead_Chuck far edge).
        // Fix: moved the camera further downstream (Z 1.15->1.75) and closer/steeper so the
        // frustum's near side no longer reaches back to the Z=0.6-0.8 column/bottle area -
        // verified clean via live screenshot (bottle fully isolated on belt, no overlap).
        // Target center: (0.00, 1.25, 1.05) - centered on CapFeeder_Hopper/CappingHead.
        // Camera pos: (0.55, 2.10, 1.75), steep downward pitch, elevated overhead angle.
        // =========================================================================
        CreateOrReplaceCamera(root, CappingCamName,
            new Vector3(0.55f, 2.10f, 1.75f),
            new Vector3(0.00f, 1.25f, 1.05f),
            fieldOfView: 40f,
            depth: 82f,
            charcoalBg);

        // =========================================================================
        // 4. END OF LINE CAMERA (EndOfLineCam)
        // Three-quarter angle framing Checkweigher (Z = 1.95m), Reject Station
        // (Z = 2.50m), Labeler Machine (Z = 3.20m), and Outfeed Conveyor (Z = 1.50m to 3.90m).
        // Target center: (-0.05, 0.95, 2.70).
        // Camera pos: (1.85, 1.75, 4.30), looking upstream-back at ~18° pitch.
        // =========================================================================
        CreateOrReplaceCamera(root, EndOfLineCamName,
            new Vector3(1.85f, 1.75f, 4.30f),
            new Vector3(-0.05f, 0.95f, 2.70f),
            fieldOfView: 40f,
            depth: 83f,
            charcoalBg);

        EditorUtility.SetDirty(root);
        Debug.Log("[AClipZoneCamerasBuilder] All 4 A-Clip Zone Cameras (InfeedZoneCam, FillingZoneCam, CappingZoneCam, EndOfLineCam) created successfully under 'Cell'.");
    }

    /// <summary>
    /// Idempotently destroys any existing camera with the given name and creates a new locked camera.
    /// </summary>
    private static GameObject CreateOrReplaceCamera(GameObject root, string camName, Vector3 worldPos, Vector3 lookTarget,
        float fieldOfView, float depth, Color bgColor)
    {
        // 1. Destroy any existing instance across scene
        Camera[] existingCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (int i = 0; i < existingCams.Length; i++)
        {
            if (existingCams[i] != null && existingCams[i].gameObject.name == camName)
            {
                Undo.DestroyObjectImmediate(existingCams[i].gameObject);
            }
        }

        Transform existingCamChild = root.transform.Find(camName);
        if (existingCamChild != null)
        {
            Undo.DestroyObjectImmediate(existingCamChild.gameObject);
        }

        // 2. Create camera GameObject parented under root
        GameObject camObj = new GameObject(camName);
        Undo.RegisterCreatedObjectUndo(camObj, $"Create {camName}");
        camObj.transform.SetParent(root.transform);
        camObj.transform.position = worldPos;
        camObj.transform.LookAt(lookTarget);

        // 3. Configure Camera component
        Camera cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = fieldOfView;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 50f;
        cam.depth = depth;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = bgColor;
        cam.enabled = false; // Disabled by default per specification

        // 4. Enable URP Post-Processing
        UniversalAdditionalCameraData camData = camObj.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;

        return camObj;
    }

    [MenuItem("Tools/Delta/Attach A-Clip Shot Switcher")]
    public static void AttachAClipShotSwitcher()
    {
        // 1. Locate root Cell
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = GameObject.Find(FallbackRootName);
        }

        if (root == null)
        {
            Debug.LogError($"[AClipZoneCamerasBuilder] Scene root '{RootName}' not found! Run Tools/Delta/Build Cell first.");
            return;
        }

        // 2. Locate or add TimedShotSwitcher component on root
        TimedShotSwitcher switcher = root.GetComponent<TimedShotSwitcher>();
        if (switcher == null)
        {
            switcher = Object.FindAnyObjectByType<TimedShotSwitcher>();
        }

        if (switcher == null)
        {
            switcher = Undo.AddComponent<TimedShotSwitcher>(root);
        }

        // 3. Find camera GameObjects by name (ControlCam_Close is reused from DeltaControlCabinet, NOT recreated)
        GameObject infeedObj = GameObject.Find(InfeedCamName);
        GameObject fillingObj = GameObject.Find(FillingCamName);
        GameObject cappingObj = GameObject.Find(CappingCamName);
        GameObject eolObj = GameObject.Find(EndOfLineCamName);
        GameObject controlObj = GameObject.Find(ControlCamName);

        if (infeedObj == null || fillingObj == null || cappingObj == null || eolObj == null)
        {
            Debug.LogWarning("[AClipZoneCamerasBuilder] One or more A-Clip zone cameras not found. Run 'Tools/Delta/Add A-Clip Zone Cameras' first.");
        }

        if (controlObj == null)
        {
            Debug.LogWarning($"[AClipZoneCamerasBuilder] Existing '{ControlCamName}' camera not found in scene. Run 'Tools/Delta/Add Control Props' to create the Delta Control Cabinet and HMI camera.");
        }

        Camera infeedCam = (infeedObj != null) ? infeedObj.GetComponent<Camera>() : null;
        Camera fillingCam = (fillingObj != null) ? fillingObj.GetComponent<Camera>() : null;
        Camera cappingCam = (cappingObj != null) ? cappingObj.GetComponent<Camera>() : null;
        Camera eolCam = (eolObj != null) ? eolObj.GetComponent<Camera>() : null;
        Camera controlCam = (controlObj != null) ? controlObj.GetComponent<Camera>() : null;

        // 4. Configure shot list in specified order with exact durations
        switcher.shots = new TimedShotSwitcher.TimedShot[]
        {
            new TimedShotSwitcher.TimedShot
            {
                shotName = "Infeed Zone",
                camera = infeedCam,
                holdDuration = 14.0f
            },
            new TimedShotSwitcher.TimedShot
            {
                shotName = "Filling Zone",
                camera = fillingCam,
                holdDuration = 24.0f
            },
            new TimedShotSwitcher.TimedShot
            {
                shotName = "Capping Zone",
                camera = cappingCam,
                holdDuration = 10.0f
            },
            new TimedShotSwitcher.TimedShot
            {
                shotName = "End Of Line Zone",
                camera = eolCam,
                holdDuration = 12.0f
            },
            new TimedShotSwitcher.TimedShot
            {
                shotName = "Control Cabinet & HMI",
                camera = controlCam,
                holdDuration = 20.0f
            }
        };

        switcher.loopSequence = false;
        switcher.autoPlayOnStart = true;

        EditorUtility.SetDirty(switcher);
        EditorUtility.SetDirty(root);
        Selection.activeGameObject = root;

        Debug.Log($"[AClipZoneCamerasBuilder] TimedShotSwitcher attached to '{root.name}' and wired with 5 shots (Total duration: 80.0s): InfeedZoneCam (14s) -> FillingZoneCam (24s) -> CappingZoneCam (10s) -> EndOfLineCam (12s) -> ControlCam_Close (20s).");
    }
}
