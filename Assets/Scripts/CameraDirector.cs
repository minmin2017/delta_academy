using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// State-driven camera switcher for KMITL Delta Academy changeover demonstration.
/// Observes ChangeoverSequencer state transitions and activates locked-angle cameras
/// corresponding to key phases of the changeover sequence:
/// - S6 Retract Nozzle To Home -> NozzleSideCam (side profile of Z-axis gantry and nozzle retract)
/// - S7 Adjust Rail Width -> RailTopCam (top-down view of X-axis guide rail widening)
/// - S1-S5, S8-S10 -> CellCam_Hero (default hero machine view)
/// </summary>
public class CameraDirector : MonoBehaviour
{
    [Header("Changeover Sequencer Reference")]
    public ChangeoverSequencer sequencer;

    [Header("Camera References")]
    [Tooltip("Default hero overview camera framing the full conveyor and cell")]
    public Camera heroCam;

    [Tooltip("Top-down locked camera framing GuideRailAssembly_X for rail width adjustment (S7)")]
    public Camera railTopCam;

    [Tooltip("Side profile locked camera framing NozzleAssembly_Z for nozzle retract & fill operations (S6/fill)")]
    public Camera nozzleSideCam;

    [Tooltip("Close-up locked camera framing the DeltaControlCabinet and HMI screen")]
    public Camera controlCamClose;

    [Tooltip("Wide overview camera framing the entire industrial cell")]
    public Camera wideCam;

    [Header("Live Director State")]
    [SerializeField] private Camera activeCamera;
    public string activeCameraName = "";
    public ChangeoverSequencer.ChangeoverState lastObservedState = (ChangeoverSequencer.ChangeoverState)(-1);

    [Header("Configuration")]
    [Tooltip("Automatically switch cameras on sequencer state change")]
    public bool autoSwitchOnState = true;

    public Camera ActiveCamera => activeCamera;

    private void Awake()
    {
        ValidateAndCacheReferences();
    }

    private void Start()
    {
        ValidateAndCacheReferences();
        if (sequencer != null)
        {
            UpdateCameraForState(sequencer.CurrentState, force: true);
        }
        else if (heroCam != null)
        {
            SwitchToCamera(heroCam);
        }
    }

    private void LateUpdate()
    {
        if (sequencer == null)
        {
            sequencer = GetComponent<ChangeoverSequencer>();
            if (sequencer == null)
            {
                sequencer = Object.FindAnyObjectByType<ChangeoverSequencer>();
            }
            if (sequencer == null) return;
        }

        if (!autoSwitchOnState) return;

        ChangeoverSequencer.ChangeoverState curState = sequencer.CurrentState;
        if (curState != lastObservedState || activeCamera == null)
        {
            UpdateCameraForState(curState, force: false);
        }
    }

    /// <summary>
    /// Finds and caches all camera references and sequencer if not assigned.
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

        if (heroCam == null)
        {
            GameObject obj = GameObject.Find("CellCam_Hero");
            if (obj != null) heroCam = obj.GetComponent<Camera>();
        }

        if (railTopCam == null)
        {
            GameObject obj = GameObject.Find("RailTopCam");
            if (obj != null) railTopCam = obj.GetComponent<Camera>();
        }

        if (nozzleSideCam == null)
        {
            GameObject obj = GameObject.Find("NozzleSideCam");
            if (obj != null) nozzleSideCam = obj.GetComponent<Camera>();
        }

        if (controlCamClose == null)
        {
            GameObject obj = GameObject.Find("ControlCam_Close");
            if (obj != null) controlCamClose = obj.GetComponent<Camera>();
        }

        if (wideCam == null)
        {
            GameObject obj = GameObject.Find("CellCam_Wide");
            if (obj != null) wideCam = obj.GetComponent<Camera>();
        }
    }

    /// <summary>
    /// Maps a ChangeoverState to the designated camera.
    /// </summary>
    public Camera GetCameraForState(ChangeoverSequencer.ChangeoverState state)
    {
        switch (state)
        {
            case ChangeoverSequencer.ChangeoverState.S6_RetractNozzleToHome:
                return (nozzleSideCam != null) ? nozzleSideCam : heroCam;

            case ChangeoverSequencer.ChangeoverState.S7_AdjustRailWidth:
                return (railTopCam != null) ? railTopCam : heroCam;

            case ChangeoverSequencer.ChangeoverState.S1_StopInfeed:
            case ChangeoverSequencer.ChangeoverState.S2_CompleteInFlightFill:
            case ChangeoverSequencer.ChangeoverState.S3_CloseValveStopPump:
            case ChangeoverSequencer.ChangeoverState.S4_ClearBottlesFromZone:
            case ChangeoverSequencer.ChangeoverState.S5_StopBelt:
            case ChangeoverSequencer.ChangeoverState.S8_ConfirmInPosition:
            case ChangeoverSequencer.ChangeoverState.S9_FirstArticleCheck:
            case ChangeoverSequencer.ChangeoverState.S10_ResumeProduction:
            default:
                return (heroCam != null) ? heroCam : null;
        }
    }

    /// <summary>
    /// Evaluates current state and switches to the corresponding camera.
    /// </summary>
    public void UpdateCameraForState(ChangeoverSequencer.ChangeoverState state, bool force = false)
    {
        if (state == lastObservedState && !force && activeCamera != null) return;

        lastObservedState = state;
        Camera targetCam = GetCameraForState(state);
        SwitchToCamera(targetCam);
    }

    /// <summary>
    /// Activates target camera and deactivates all other managed cameras cleanly.
    /// </summary>
    public void SwitchToCamera(Camera targetCam)
    {
        if (targetCam == null)
        {
            targetCam = heroCam;
        }

        Camera[] managedCameras = new Camera[]
        {
            heroCam,
            railTopCam,
            nozzleSideCam,
            controlCamClose,
            wideCam
        };

        for (int i = 0; i < managedCameras.Length; i++)
        {
            Camera cam = managedCameras[i];
            if (cam == null) continue;

            bool isTarget = (cam == targetCam);
            cam.enabled = isTarget;
        }

        activeCamera = targetCam;
        activeCameraName = (targetCam != null) ? targetCam.gameObject.name : "None";
    }

    /// <summary>
    /// Assigns (or clears, pass null) a shared RenderTexture as the targetTexture on every
    /// managed camera. Since only the currently-enabled camera actually renders each frame,
    /// whichever one CameraDirector switches to writes into this same texture - letting a
    /// Recorder session (RenderTextureInputSettings pointed at this RT) capture a genuine
    /// per-frame, state-driven multi-camera cut in ONE output file. This also sidesteps the
    /// Editor gizmo contamination issue found in GameViewInputSettings, for the same reason
    /// CameraInputSettings does: it's a direct render target, not the interactive Game View.
    /// </summary>
    public void SetSharedRenderTexture(RenderTexture rt)
    {
        Camera[] managedCameras = new Camera[]
        {
            heroCam,
            railTopCam,
            nozzleSideCam,
            controlCamClose,
            wideCam
        };

        for (int i = 0; i < managedCameras.Length; i++)
        {
            if (managedCameras[i] != null)
            {
                managedCameras[i].targetTexture = rt;
            }
        }
    }
}
