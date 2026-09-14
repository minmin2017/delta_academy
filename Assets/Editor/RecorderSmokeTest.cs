using System;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEngine;

/// <summary>
/// Recorder smoke-test — proves the com.unity.recorder pipeline actually produces a playable
/// MP4 on this machine (GTX 1650, URP) before any real content depends on it. Zero prior
/// precedent in this project; this is the highest-risk untested milestone per the roadmap.
/// Deliberately tiny (short duration, low res) to iterate fast.
/// </summary>
public static class RecorderSmokeTest
{
    private static RecorderController s_Controller;
    public static string LastOutputPath;
    public static bool IsDone => s_Controller == null || !s_Controller.IsRecording();

    // ===================================================================================
    // GIZMO SUPPRESSION - discovered live via ffmpeg frame extraction that Recorder's
    // GameViewInputSettings captures whatever the Game View's "Gizmos" toggle is currently
    // set to, baking giant TextMeshPro/Camera/ReflectionProbe editor icons permanently into
    // the output video. Unity does not expose this toggle publicly - reflection into the
    // internal GameView EditorWindow type is required. Call ForceGizmosOff() before every
    // recording start.
    // ===================================================================================
    private static bool? s_SavedGizmoState;

    private static System.Type GameViewType => System.Type.GetType("UnityEditor.GameView,UnityEditor");

    public static void ForceGizmosOff()
    {
        var gameViewType = GameViewType;
        if (gameViewType == null) { Debug.LogWarning("[RecorderGizmoFix] Could not find internal GameView type via reflection."); return; }
        var window = EditorWindow.GetWindow(gameViewType, false, null, false);
        var prop = gameViewType.GetProperty("showGizmos", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (prop == null) { Debug.LogWarning("[RecorderGizmoFix] Could not find 'gizmos' property on GameView via reflection."); return; }
        s_SavedGizmoState = (bool)prop.GetValue(window);
        prop.SetValue(window, false);
        window.Repaint();
        Debug.Log("[RecorderGizmoFix] Game View gizmos forced OFF (was " + s_SavedGizmoState + ") before recording.");
    }

    public static void RestoreGizmoState()
    {
        if (s_SavedGizmoState == null) return;
        var gameViewType = GameViewType;
        if (gameViewType == null) return;
        var window = EditorWindow.GetWindow(gameViewType, false, null, false);
        var prop = gameViewType.GetProperty("showGizmos", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (prop != null)
        {
            prop.SetValue(window, s_SavedGizmoState.Value);
            window.Repaint();
        }
        s_SavedGizmoState = null;
    }

    [MenuItem("Tools/Delta/Recorder Smoke Test (Small, 2s)")]
    public static void RunSmokeTest()
    {
        // Recorder's PrepareRecording/StartRecording only work in Play Mode (discovered live -
        // this is the single biggest unknown-risk item in the whole remaining project).
        if (!EditorApplication.isPlaying)
        {
            Debug.Log("[RecorderSmokeTest] Not in Play Mode - entering Play Mode first, recording will start automatically once entered.");
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.isPlaying = true;
            return;
        }

        StartRecordingNow();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            StartRecordingNow();
        }
    }

    private static void StartRecordingNow()
    {
        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        controllerSettings.SetRecordModeToFrameInterval(0, 60); // 60 frames @ 30fps = 2s
        controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
        controllerSettings.FrameRate = 30f;
        controllerSettings.CapFrameRate = true;

        var movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieSettings.name = "SmokeTestMovieRecorder";
        movieSettings.Enabled = true;
#pragma warning disable CS0618 // Obsolete API - acceptable for this smoke test, revisit with EncoderSettings API before final render
        movieSettings.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        movieSettings.VideoBitRateMode = UnityEditor.VideoBitrateMode.High;
#pragma warning restore CS0618

        var imageInput = new GameViewInputSettings
        {
            OutputWidth = 640,
            OutputHeight = 360
        };
        movieSettings.ImageInputSettings = imageInput;
        movieSettings.CaptureAudio = false;

        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string outDir = Path.Combine(projectRoot, "Recordings");
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
        string fileBase = "SmokeTest_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        movieSettings.OutputFile = Path.Combine(outDir, fileBase);
        LastOutputPath = movieSettings.OutputFile + ".mp4";

        controllerSettings.AddRecorderSettings(movieSettings);

        ForceGizmosOff();
        s_Controller = new RecorderController(controllerSettings);
        s_Controller.PrepareRecording();
        s_Controller.StartRecording();

        Debug.Log("[RecorderSmokeTest] Started recording in Play Mode. Expecting output at: " + LastOutputPath);
    }

    [MenuItem("Tools/Delta/Recorder Smoke Test - Check Status")]
    public static void CheckStatus()
    {
        bool recording = s_Controller != null && s_Controller.IsRecording();
        bool fileExists = !string.IsNullOrEmpty(LastOutputPath) && File.Exists(LastOutputPath);
        long fileSize = fileExists ? new FileInfo(LastOutputPath).Length : 0;
        Debug.Log("[RecorderSmokeTest] recording=" + recording + " outputPath=" + LastOutputPath
            + " fileExists=" + fileExists + " fileSizeBytes=" + fileSize);
    }

    // ===================================================================================
    // REAL DRAFT RENDER - raw uncut footage of B_Changeover via CellCam_Hero (CameraDirector
    // auto-switches to RailTopCam/NozzleSideCam as the sequence progresses through its states).
    // This is NOT an edited/narrated final clip - it is real gameplay footage of the verified
    // scene running its actual changeover sequence, meant as raw material for an edit pass.
    // ===================================================================================
    [MenuItem("Tools/Delta/Render B_Changeover Draft (45s, 720p)")]
    public static void RenderChangeoverDraft()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.Log("[RecorderDraft] Not in Play Mode - entering Play Mode first, draft render will start automatically.");
            EditorApplication.playModeStateChanged += OnPlayModeChangedDraft;
            EditorApplication.isPlaying = true;
            return;
        }
        StartDraftRecordingNow();
    }

    private static void OnPlayModeChangedDraft(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChangedDraft;
            StartDraftRecordingNow();
        }
    }

    private static void StartDraftRecordingNow()
    {
        const float durationSeconds = 45f;
        const float fps = 30f;
        int totalFrames = Mathf.RoundToInt(durationSeconds * fps);

        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        controllerSettings.SetRecordModeToFrameInterval(0, totalFrames);
        controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
        controllerSettings.FrameRate = fps;
        controllerSettings.CapFrameRate = true;

        var movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieSettings.name = "BChangeoverDraftRecorder";
        movieSettings.Enabled = true;
#pragma warning disable CS0618
        movieSettings.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        movieSettings.VideoBitRateMode = UnityEditor.VideoBitrateMode.High;
#pragma warning restore CS0618

        // Switched from GameViewInputSettings to CameraInputSettings: GameView mirrors the
        // interactive Editor view, which bakes gizmo icons (giant TMP/Camera/ReflectionProbe
        // glyphs) permanently into the output - confirmed live via ffmpeg frame extraction,
        // and the GameView.showGizmos reflection toggle did not actually suppress them.
        // CameraInputSettings renders directly from a camera's own output with no gizmo
        // overlay. Trade-off: URP does not support CaptureUI on this input type, so the
        // Screen-Space Canvas overlay (LeaderLineOverlay/TelemetryOverlayController) will
        // NOT appear in this render - consistent with this project's own established decision
        // that overlays belong in 2D post-production compositing, not baked into the 3D render.
        // Scope note: TaggedCamera picks the first matching camera via FindGameObjectsWithTag
        // and does NOT follow CameraDirector's live enable/disable switching between shots, so
        // this draft renders CellCam_Hero only for its full duration (no automatic cutaways to
        // RailTopCam/NozzleSideCam) - a real per-frame camera-driven render is a follow-up task.
        GameObject heroCamObj = GameObject.Find("CellCam_Hero");
        if (heroCamObj != null) heroCamObj.tag = "MainCamera";

        var imageInput = new CameraInputSettings
        {
            Source = ImageSource.MainCamera,
            OutputWidth = 1280,
            OutputHeight = 720
        };
        movieSettings.ImageInputSettings = imageInput;
        movieSettings.CaptureAudio = false;

        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string outDir = Path.Combine(projectRoot, "Recordings");
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
        string fileBase = "B_Changeover_DRAFT_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        movieSettings.OutputFile = Path.Combine(outDir, fileBase);
        LastOutputPath = movieSettings.OutputFile + ".mp4";

        controllerSettings.AddRecorderSettings(movieSettings);

        s_Controller = new RecorderController(controllerSettings);
        s_Controller.PrepareRecording();
        s_Controller.StartRecording();

        Debug.Log("[RecorderDraft] Started " + durationSeconds + "s draft render at 1280x720. Expecting output at: " + LastOutputPath);
    }

    // ===================================================================================
    // A_LineOverview DRAFT - isolates TravelingEstablishingCam (normally disabled, not part
    // of CameraDirector's B-clip state switching) by temporarily disabling every other camera,
    // records its full travel duration, then restores every camera's prior enabled state.
    // ===================================================================================
    private static System.Collections.Generic.Dictionary<Camera, bool> s_SavedCameraStates;

    [MenuItem("Tools/Delta/Render A_LineOverview Draft (Traveling Cam)")]
    public static void RenderLineOverviewDraft()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.Log("[RecorderDraftA] Not in Play Mode - entering Play Mode first.");
            EditorApplication.playModeStateChanged += OnPlayModeChangedDraftA;
            EditorApplication.isPlaying = true;
            return;
        }
        StartDraftARecordingNow();
    }

    private static void OnPlayModeChangedDraftA(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChangedDraftA;
            StartDraftARecordingNow();
        }
    }

    private static void StartDraftARecordingNow()
    {
        GameObject travelCamObj = GameObject.Find("TravelingEstablishingCam");
        if (travelCamObj == null)
        {
            Debug.LogError("[RecorderDraftA] TravelingEstablishingCam not found - run Tools/Delta/Add Traveling Establishing Camera first.");
            return;
        }
        Camera travelCam = travelCamObj.GetComponent<Camera>();
        TravelingCamera travelScript = travelCamObj.GetComponent<TravelingCamera>();

        // Save and disable every other camera so only the traveling cam is visible in Game View.
        s_SavedCameraStates = new System.Collections.Generic.Dictionary<Camera, bool>();
        Camera[] allCams = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Camera c in allCams)
        {
            if (c == travelCam) continue;
            s_SavedCameraStates[c] = c.enabled;
            c.enabled = false;
        }
        travelCam.enabled = true;
        travelCamObj.tag = "MainCamera"; // CameraInputSettings.ImageSource.MainCamera needs this - see B-draft render comment for why GameViewInputSettings was dropped (bakes gizmo icons into output)
        if (travelScript != null) travelScript.Replay();

        float durationSeconds = (travelScript != null) ? travelScript.duration + 0.5f : 10.5f;
        const float fps = 30f;
        int totalFrames = Mathf.RoundToInt(durationSeconds * fps);

        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        controllerSettings.SetRecordModeToFrameInterval(0, totalFrames);
        controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
        controllerSettings.FrameRate = fps;
        controllerSettings.CapFrameRate = true;

        var movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieSettings.name = "ALineOverviewDraftRecorder";
        movieSettings.Enabled = true;
#pragma warning disable CS0618
        movieSettings.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        movieSettings.VideoBitRateMode = UnityEditor.VideoBitrateMode.High;
#pragma warning restore CS0618

        movieSettings.ImageInputSettings = new CameraInputSettings { Source = ImageSource.MainCamera, OutputWidth = 1280, OutputHeight = 720 };
        movieSettings.CaptureAudio = false;

        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string outDir = Path.Combine(projectRoot, "Recordings");
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
        string fileBase = "A_LineOverview_DRAFT_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        movieSettings.OutputFile = Path.Combine(outDir, fileBase);
        LastOutputPath = movieSettings.OutputFile + ".mp4";

        controllerSettings.AddRecorderSettings(movieSettings);
        ForceGizmosOff();
        s_Controller = new RecorderController(controllerSettings);
        s_Controller.PrepareRecording();
        s_Controller.StartRecording();

        Debug.Log("[RecorderDraftA] Started " + durationSeconds + "s traveling-cam draft render. Expecting output at: " + LastOutputPath);
    }

    [MenuItem("Tools/Delta/Restore Camera States After A-Draft")]
    public static void RestoreCameraStates()
    {
        if (s_SavedCameraStates == null) { Debug.Log("[RecorderDraftA] No saved camera states to restore."); return; }
        foreach (var kv in s_SavedCameraStates)
        {
            if (kv.Key != null) kv.Key.enabled = kv.Value;
        }
        s_SavedCameraStates = null;
        Debug.Log("[RecorderDraftA] Camera states restored.");
    }

    // ===================================================================================
    // B_Changeover REAL MULTI-CAMERA DRAFT - the actual per-frame, state-driven camera cut
    // render that GameViewInputSettings/CameraInputSettings couldn't do (see commit history:
    // GameView bakes gizmo icons; CameraInputSettings/TaggedCamera picks one camera statically
    // and can't follow CameraDirector's live switching). This renders CameraDirector's shared
    // RenderTexture directly - whichever camera the director enables each frame writes into
    // it, so the output genuinely cuts between CellCam_Hero / RailTopCam / NozzleSideCam as
    // the real changeover sequence progresses. Also gizmo-free for the same reason
    // CameraInputSettings is: it's a direct render target, not the interactive Game View.
    // ===================================================================================
    private static RenderTexture s_SharedRT;
    private static CameraDirector s_Director;
    private static MotionHighlightController s_MotionHighlight;
    private static EquipmentIntroSequencer s_EquipmentIntro;
    private static HardwareCommandVisualizer s_CommandVisualizer;

    [MenuItem("Tools/Delta/Render B_Changeover MULTICAM (45s, 720p)")]
    public static void RenderChangeoverMulticam()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.Log("[RecorderMulticam] Not in Play Mode - entering Play Mode first.");
            EditorApplication.playModeStateChanged += OnPlayModeChangedMulticam;
            EditorApplication.isPlaying = true;
            return;
        }
        StartMulticamRecordingNow();
    }

    private static void OnPlayModeChangedMulticam(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChangedMulticam;
            StartMulticamRecordingNow();
        }
    }

    private static void StartMulticamRecordingNow()
    {
        s_Director = UnityEngine.Object.FindAnyObjectByType<CameraDirector>();
        if (s_Director == null)
        {
            Debug.LogError("[RecorderMulticam] No CameraDirector found in scene - run Tools/Delta/Attach Camera Director first.");
            return;
        }

        // Symmetric counterpart to the fix in StartZoneRecordingNow: TimedShotSwitcher (A-clip's
        // time-driven switcher) has autoPlayOnStart = true, so once it exists in the scene it
        // starts switching cameras the moment Play Mode is entered - fighting CameraDirector
        // for this B-clip render exactly the way CameraDirector fought it for the A-clip render.
        // The earlier B multicam render predates TimedShotSwitcher existing, which is why it
        // wasn't hit at the time. Pause the switcher for the duration of this render.
        s_ShotSwitcher = UnityEngine.Object.FindAnyObjectByType<TimedShotSwitcher>();
        if (s_ShotSwitcher != null)
        {
            s_ShotSwitcher.autoPlayOnStart = false;
            s_ShotSwitcher.Pause();
        }
        s_EquipmentIntro = UnityEngine.Object.FindAnyObjectByType<EquipmentIntroSequencer>();
        if (s_EquipmentIntro != null)
        {
            s_EquipmentIntro.autoPlayOnStart = false;
            s_EquipmentIntro.Pause();
            s_EquipmentIntro.enabled = false;
        }
        if (s_Director != null) s_Director.autoSwitchOnState = true; // ensure B's own switcher is live

        const int width = 1280;
        const int height = 720;
        s_SharedRT = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        s_SharedRT.name = "MulticamSharedRT";
        s_SharedRT.Create();
        s_Director.SetSharedRenderTexture(s_SharedRT);

        const float durationSeconds = 45f;
        const float fps = 30f;
        int totalFrames = Mathf.RoundToInt(durationSeconds * fps);

        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        controllerSettings.SetRecordModeToFrameInterval(0, totalFrames);
        controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
        controllerSettings.FrameRate = fps;
        controllerSettings.CapFrameRate = true;

        var movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieSettings.name = "BChangeoverMulticamRecorder";
        movieSettings.Enabled = true;
#pragma warning disable CS0618
        movieSettings.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        movieSettings.VideoBitRateMode = UnityEditor.VideoBitrateMode.High;
#pragma warning restore CS0618

        // OutputWidth/OutputHeight are NOT set here deliberately - RenderTextureInputSettings'
        // setters for those forward directly to renderTexture.width/.height, which Unity does
        // not allow on an already-.Create()'d RenderTexture ("Setting width of already created
        // render texture is not supported!"). The getters already report s_SharedRT's own
        // dimensions (1280x720, set at creation above), so this is a no-op worth skipping.
        movieSettings.ImageInputSettings = new RenderTextureInputSettings
        {
            RenderTexture = s_SharedRT
        };
        movieSettings.CaptureAudio = false;

        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string outDir = Path.Combine(projectRoot, "Recordings");
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
        string fileBase = "B_Changeover_MULTICAM_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        movieSettings.OutputFile = Path.Combine(outDir, fileBase);
        LastOutputPath = movieSettings.OutputFile + ".mp4";

        controllerSettings.AddRecorderSettings(movieSettings);
        s_Controller = new RecorderController(controllerSettings);
        s_Controller.PrepareRecording();
        s_Controller.StartRecording();

        Debug.Log("[RecorderMulticam] Started " + durationSeconds + "s multicam draft render. Expecting output at: " + LastOutputPath);
    }

    // ===================================================================================
    // B_Changeover 90s EXPANDED RENDER - renders full ~90s Changeover demonstration:
    //   S0A_SelectRecipe (12s): HMI bottle silhouettes comparison & 500ml selection
    //   S0B_LoadParameters (13s): 5 recipe parameters streaming into AS320T-B registers
    //   S1-S5 (16.5s): Stop infeed, finish dive fill, clear bottles, belt stop
    //   S6 (6.5s): PLC -> Servo ASD-A3 (Z) pulse train homing retract (1250mm)
    //   S7 (8.5s): PLC -> Servo ASD-A3 (X) pulse train rail gap adjustment (58 -> 73mm)
    //   S8 (5.0s): PLC -> VFD MS300 Modbus RTU RS-485 speed preset (0.20 m/s) + HMI check
    //   S9 (18.0s): First article 500ml test bottle infeed, dive-fill & exit
    //   S10 (13.0s): Full speed production resume
    // Total duration: 89.0s (2670 frames @ 30fps).
    // ===================================================================================
    [MenuItem("Tools/Delta/Render B_Changeover (90s, 720p)")]
    public static void RenderChangeover90s()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.Log("[Recorder90s] Not in Play Mode - entering Play Mode first.");
            EditorApplication.playModeStateChanged += OnPlayModeChanged90s;
            EditorApplication.isPlaying = true;
            return;
        }
        StartChangeover90sRecordingNow();
    }

    private static void OnPlayModeChanged90s(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged90s;
            StartChangeover90sRecordingNow();
        }
    }

    private static void StartChangeover90sRecordingNow()
    {
        s_Director = UnityEngine.Object.FindAnyObjectByType<CameraDirector>();
        if (s_Director == null)
        {
            Debug.LogError("[Recorder90s] No CameraDirector found in scene - run Tools/Delta/Attach Camera Director first.");
            return;
        }

        s_ShotSwitcher = UnityEngine.Object.FindAnyObjectByType<TimedShotSwitcher>();
        if (s_ShotSwitcher != null)
        {
            s_ShotSwitcher.autoPlayOnStart = false;
            s_ShotSwitcher.Pause();
        }
        s_EquipmentIntro = UnityEngine.Object.FindAnyObjectByType<EquipmentIntroSequencer>();
        if (s_EquipmentIntro != null)
        {
            s_EquipmentIntro.autoPlayOnStart = false;
            s_EquipmentIntro.Pause();
            s_EquipmentIntro.enabled = false;
        }
        if (s_Director != null) s_Director.autoSwitchOnState = true;

        s_MotionHighlight = UnityEngine.Object.FindAnyObjectByType<MotionHighlightController>();
        if (s_MotionHighlight != null) s_MotionHighlight.enabled = true;

        s_CommandVisualizer = UnityEngine.Object.FindAnyObjectByType<HardwareCommandVisualizer>();
        if (s_CommandVisualizer != null) s_CommandVisualizer.enabled = true;

        const int width = 1280;
        const int height = 720;
        s_SharedRT = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        s_SharedRT.name = "Changeover90sSharedRT";
        s_SharedRT.Create();
        s_Director.SetSharedRenderTexture(s_SharedRT);

        const float durationSeconds = 89.0f;
        const float fps = 30f;
        int totalFrames = Mathf.RoundToInt(durationSeconds * fps);

        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        controllerSettings.SetRecordModeToFrameInterval(0, totalFrames);
        controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
        controllerSettings.FrameRate = fps;
        controllerSettings.CapFrameRate = true;

        var movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieSettings.name = "BChangeover90sRecorder";
        movieSettings.Enabled = true;
#pragma warning disable CS0618
        movieSettings.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        movieSettings.VideoBitRateMode = UnityEditor.VideoBitrateMode.High;
#pragma warning restore CS0618

        movieSettings.ImageInputSettings = new RenderTextureInputSettings
        {
            RenderTexture = s_SharedRT
        };
        movieSettings.CaptureAudio = false;

        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string outDir = Path.Combine(projectRoot, "Recordings");
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
        string fileBase = "B_Changeover_90s_RAW_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        movieSettings.OutputFile = Path.Combine(outDir, fileBase);
        LastOutputPath = movieSettings.OutputFile + ".mp4";

        controllerSettings.AddRecorderSettings(movieSettings);
        s_Controller = new RecorderController(controllerSettings);
        s_Controller.PrepareRecording();
        s_Controller.StartRecording();

        Debug.Log("[Recorder90s] Started " + durationSeconds + "s changeover 90s render. Expecting output at: " + LastOutputPath);
    }

    [MenuItem("Tools/Delta/Cleanup Multicam RenderTexture")]
    public static void CleanupMulticamRenderTexture()
    {
        if (s_Director != null)
        {
            s_Director.SetSharedRenderTexture(null);
        }
        if (s_ShotSwitcher != null)
        {
            s_ShotSwitcher.autoPlayOnStart = true; // restore A-clip switcher's own behaviour
        }
        if (s_EquipmentIntro != null)
        {
            s_EquipmentIntro.enabled = true;
        }
        if (s_CommandVisualizer != null)
        {
            s_CommandVisualizer.CleanupAll();
        }
        if (s_SharedRT != null)
        {
            s_SharedRT.Release();
            UnityEngine.Object.DestroyImmediate(s_SharedRT);
            s_SharedRT = null;
        }
        Debug.Log("[RecorderMulticam] Shared RenderTexture cleaned up, cameras restored to normal rendering.");
    }

    // ===================================================================================
    // A_LineOverview ZONE SEQUENCE - renders TimedShotSwitcher's full 80s zone-callout
    // sequence (Infeed -> Filling -> Capping -> EndOfLine -> ControlCam_Close) into one
    // file, same shared-RenderTexture technique as the B_Changeover multicam render.
    // ===================================================================================
    private static TimedShotSwitcher s_ShotSwitcher;

    [MenuItem("Tools/Delta/Render A_LineOverview ZONES (80s, 720p)")]
    public static void RenderZoneSequence()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.Log("[RecorderZones] Not in Play Mode - entering Play Mode first.");
            EditorApplication.playModeStateChanged += OnPlayModeChangedZones;
            EditorApplication.isPlaying = true;
            return;
        }
        StartZoneRecordingNow();
    }

    private static void OnPlayModeChangedZones(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChangedZones;
            StartZoneRecordingNow();
        }
    }

    private static void StartZoneRecordingNow()
    {
        s_ShotSwitcher = UnityEngine.Object.FindAnyObjectByType<TimedShotSwitcher>();
        if (s_ShotSwitcher == null)
        {
            Debug.LogError("[RecorderZones] No TimedShotSwitcher found - run Tools/Delta/Attach A-Clip Shot Switcher first.");
            return;
        }

        // Real bug found via frame extraction (t=55s/68s both showed ControlCam_Close when
        // EndOfLineCam's 48-60s window should have been active): CameraDirector (B_Changeover's
        // state-driven switcher) and TimedShotSwitcher (A's time-driven switcher) coexist on
        // the same Cell GameObject and both fight over ControlCam_Close's enabled state.
        // ChangeoverSequencer keeps looping/running regardless of which clip is being
        // rendered, so whenever its own cycle reaches S1/S8, CameraDirector.LateUpdate()
        // force-re-enables ControlCam_Close every frame from that point on, permanently
        // overriding TimedShotSwitcher since LateUpdate runs after Update. Disable
        // CameraDirector's switching for the duration of this A-clip render.
        s_Director = UnityEngine.Object.FindAnyObjectByType<CameraDirector>();
        if (s_Director != null) s_Director.autoSwitchOnState = false;

        // Same root cause as the CameraDirector fight above: ChangeoverSequencer keeps looping
        // during this A-clip render regardless of which camera is active, so MotionHighlightController
        // (glow effect built for B_Changeover's S6/S7/S9) can trigger mid-cycle and bleed its cyan
        // glow + live mm label into whatever A-clip zone camera happens to be recording at that
        // moment - confirmed via frame extraction (glow + "NOZZLE HEIGHT" label appeared in the
        // Filling Zone shot). Disable the whole component for the duration of this render.
        s_MotionHighlight = UnityEngine.Object.FindAnyObjectByType<MotionHighlightController>();
        if (s_MotionHighlight != null) s_MotionHighlight.enabled = false;

        s_CommandVisualizer = UnityEngine.Object.FindAnyObjectByType<HardwareCommandVisualizer>();
        if (s_CommandVisualizer != null)
        {
            s_CommandVisualizer.CleanupAll();
            s_CommandVisualizer.enabled = false;
        }

        s_CommandVisualizer = UnityEngine.Object.FindAnyObjectByType<HardwareCommandVisualizer>();
        if (s_CommandVisualizer != null)
        {
            s_CommandVisualizer.CleanupAll();
            s_CommandVisualizer.enabled = false;
        }

        s_EquipmentIntro = UnityEngine.Object.FindAnyObjectByType<EquipmentIntroSequencer>();
        if (s_EquipmentIntro != null)
        {
            s_EquipmentIntro.autoPlayOnStart = false;
            s_EquipmentIntro.Pause();
            s_EquipmentIntro.enabled = false;
        }

        const int width = 1280;
        const int height = 720;
        s_SharedRT = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        s_SharedRT.name = "ZoneSequenceSharedRT";
        s_SharedRT.Create();
        s_ShotSwitcher.SetSharedRenderTexture(s_SharedRT);
        s_ShotSwitcher.Restart();

        const float durationSeconds = 80f;
        const float fps = 30f;
        int totalFrames = Mathf.RoundToInt(durationSeconds * fps);

        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        controllerSettings.SetRecordModeToFrameInterval(0, totalFrames);
        controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
        controllerSettings.FrameRate = fps;
        controllerSettings.CapFrameRate = true;

        var movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieSettings.name = "ALineOverviewZonesRecorder";
        movieSettings.Enabled = true;
#pragma warning disable CS0618
        movieSettings.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        movieSettings.VideoBitRateMode = UnityEditor.VideoBitrateMode.High;
#pragma warning restore CS0618

        movieSettings.ImageInputSettings = new RenderTextureInputSettings { RenderTexture = s_SharedRT };
        movieSettings.CaptureAudio = false;

        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string outDir = Path.Combine(projectRoot, "Recordings");
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
        string fileBase = "A_LineOverview_ZONES_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        movieSettings.OutputFile = Path.Combine(outDir, fileBase);
        LastOutputPath = movieSettings.OutputFile + ".mp4";

        controllerSettings.AddRecorderSettings(movieSettings);
        s_Controller = new RecorderController(controllerSettings);
        s_Controller.PrepareRecording();
        s_Controller.StartRecording();

        Debug.Log("[RecorderZones] Started " + durationSeconds + "s zone-sequence render. Expecting output at: " + LastOutputPath);
    }

    [MenuItem("Tools/Delta/Cleanup Zone Sequence RenderTexture")]
    public static void CleanupZoneSequenceRenderTexture()
    {
        if (s_ShotSwitcher != null)
        {
            s_ShotSwitcher.SetSharedRenderTexture(null);
        }
        if (s_Director != null)
        {
            s_Director.autoSwitchOnState = true; // restore B_Changeover's own switching
        }
        if (s_MotionHighlight != null)
        {
            s_MotionHighlight.enabled = true; // restore glow highlight for B_Changeover renders
        }
        if (s_EquipmentIntro != null)
        {
            s_EquipmentIntro.enabled = true;
        }
        if (s_SharedRT != null)
        {
            s_SharedRT.Release();
            UnityEngine.Object.DestroyImmediate(s_SharedRT);
            s_SharedRT = null;
        }
        Debug.Log("[RecorderZones] Shared RenderTexture cleaned up, cameras restored to normal rendering.");
    }

    // ===================================================================================
    // C_HardwareIntro SEQUENCED RENDER - renders EquipmentIntroSequencer's full 50s
    // 6-shot storyboard (CabinetWide -> PLC -> HMI -> VFD -> SERVO+Axes -> SystemWide)
    // with pulsing cyan glow into one file via shared RenderTexture (RenderTextureInputSettings).
    // ===================================================================================
    [MenuItem("Tools/Delta/Render C_HardwareIntro (50s, 720p)")]
    public static void RenderHardwareIntro()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.Log("[RecorderHardwareIntro] Not in Play Mode - entering Play Mode first.");
            EditorApplication.playModeStateChanged += OnPlayModeChangedHardwareIntro;
            EditorApplication.isPlaying = true;
            return;
        }
        StartHardwareIntroRecordingNow();
    }

    private static void OnPlayModeChangedHardwareIntro(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChangedHardwareIntro;
            StartHardwareIntroRecordingNow();
        }
    }

    private static void StartHardwareIntroRecordingNow()
    {
        s_EquipmentIntro = UnityEngine.Object.FindAnyObjectByType<EquipmentIntroSequencer>();
        if (s_EquipmentIntro == null)
        {
            GameObject cell = GameObject.Find("Cell");
            if (cell != null)
            {
                s_EquipmentIntro = cell.AddComponent<EquipmentIntroSequencer>();
            }
            else
            {
                Debug.LogError("[RecorderHardwareIntro] No Cell GameObject found to attach EquipmentIntroSequencer.");
                return;
            }
        }
        s_EquipmentIntro.enabled = true;

        s_Director = UnityEngine.Object.FindAnyObjectByType<CameraDirector>();
        if (s_Director != null) s_Director.autoSwitchOnState = false;

        s_ShotSwitcher = UnityEngine.Object.FindAnyObjectByType<TimedShotSwitcher>();
        if (s_ShotSwitcher != null)
        {
            s_ShotSwitcher.autoPlayOnStart = false;
            s_ShotSwitcher.Pause();
        }

        s_MotionHighlight = UnityEngine.Object.FindAnyObjectByType<MotionHighlightController>();
        if (s_MotionHighlight != null) s_MotionHighlight.enabled = false;

        const int width = 1280;
        const int height = 720;
        s_SharedRT = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        s_SharedRT.name = "HardwareIntroSharedRT";
        s_SharedRT.Create();

        s_EquipmentIntro.EnsureCamera();
        s_EquipmentIntro.SetSharedRenderTexture(s_SharedRT);
        s_EquipmentIntro.Restart();

        const float durationSeconds = 50f;
        const float fps = 30f;
        int totalFrames = Mathf.RoundToInt(durationSeconds * fps);

        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        controllerSettings.SetRecordModeToFrameInterval(0, totalFrames);
        controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
        controllerSettings.FrameRate = fps;
        controllerSettings.CapFrameRate = true;

        var movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieSettings.name = "CHardwareIntroRecorder";
        movieSettings.Enabled = true;
#pragma warning disable CS0618
        movieSettings.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        movieSettings.VideoBitRateMode = UnityEditor.VideoBitrateMode.High;
#pragma warning restore CS0618

        movieSettings.ImageInputSettings = new RenderTextureInputSettings { RenderTexture = s_SharedRT };
        movieSettings.CaptureAudio = false;

        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string outDir = Path.Combine(projectRoot, "Recordings");
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
        string fileBase = "C_HardwareIntro_RAW_v4_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        movieSettings.OutputFile = Path.Combine(outDir, fileBase);
        LastOutputPath = movieSettings.OutputFile + ".mp4";

        controllerSettings.AddRecorderSettings(movieSettings);
        s_Controller = new RecorderController(controllerSettings);
        s_Controller.PrepareRecording();
        s_Controller.StartRecording();

        Debug.Log("[RecorderHardwareIntro] Started " + durationSeconds + "s hardware-intro render. Expecting output at: " + LastOutputPath);
    }

    [MenuItem("Tools/Delta/Cleanup Hardware Intro RenderTexture")]
    public static void CleanupHardwareIntroRenderTexture()
    {
        if (s_EquipmentIntro != null)
        {
            s_EquipmentIntro.SetSharedRenderTexture(null);
            s_EquipmentIntro.Pause();
        }
        if (s_Director != null) s_Director.autoSwitchOnState = true;
        if (s_ShotSwitcher != null) s_ShotSwitcher.autoPlayOnStart = true;
        if (s_MotionHighlight != null) s_MotionHighlight.enabled = true;
        if (s_SharedRT != null)
        {
            s_SharedRT.Release();
            UnityEngine.Object.DestroyImmediate(s_SharedRT);
            s_SharedRT = null;
        }
        Debug.Log("[RecorderHardwareIntro] Shared RenderTexture cleaned up, cameras restored.");
    }
}
