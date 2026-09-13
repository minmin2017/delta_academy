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

    [MenuItem("Tools/Delta/Cleanup Multicam RenderTexture")]
    public static void CleanupMulticamRenderTexture()
    {
        if (s_Director != null)
        {
            s_Director.SetSharedRenderTexture(null);
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
        if (s_SharedRT != null)
        {
            s_SharedRT.Release();
            UnityEngine.Object.DestroyImmediate(s_SharedRT);
            s_SharedRT = null;
        }
        Debug.Log("[RecorderZones] Shared RenderTexture cleaned up, cameras restored to normal rendering.");
    }
}
