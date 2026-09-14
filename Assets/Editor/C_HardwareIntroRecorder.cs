using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

public static class C_HardwareIntroRecorder
{
    [MenuItem("Tools/Delta/Record Hardware Intro")]
    public static void RecordIntro()
    {
        GameObject root = GameObject.Find("Cell");
        if (root != null && root.GetComponent<HardwareIntroDirector>() == null)
        {
            root.AddComponent<HardwareIntroDirector>();
        }

        if (EditorApplication.isPlaying)
        {
            StartRecording();
        }
        else
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.isPlaying = true;
        }
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            StartRecording();
        }
    }

    private static void StartRecording()
    {
        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var recorderController = new RecorderController(controllerSettings);

        var videoSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        videoSettings.name = "HardwareIntroRecorder";
        videoSettings.Enabled = true;
        // Suppress warning by using the correct enum if needed, but keeping as is for now
#pragma warning disable CS0618
        videoSettings.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        videoSettings.VideoBitRateMode = VideoBitrateMode.High;
#pragma warning restore CS0618
        videoSettings.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 1920,
            OutputHeight = 1080
        };
        videoSettings.AudioInputSettings.PreserveAudio = false;
        
        string outFile = "D:/unity_project/delta_academy/Recordings/C_HardwareIntro_RAW_v3";
        videoSettings.OutputFile = outFile;

        controllerSettings.AddRecorderSettings(videoSettings);
        controllerSettings.SetRecordModeToTimeInterval(0, 12);
        controllerSettings.FrameRate = 30;

        recorderController.PrepareRecording();
        recorderController.StartRecording();
        
        Debug.Log("[C_HardwareIntroRecorder] Started recording to: " + outFile + ".mp4");
    }
}
