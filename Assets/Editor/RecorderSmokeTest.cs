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
}
