using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;

public class HardwareIntroManualRecorder : MonoBehaviour
{
    private int framesToCapture = 360;
    private int currentFrame = 0;
    private string folder;
    
    void Start()
    {
        folder = Application.dataPath + "/../Recordings/frames_v3";
        if (!Directory.Exists(folder)) {
            Directory.CreateDirectory(folder);
        }
        Time.captureFramerate = 30; // Force Unity to render exactly 30fps internally
        Debug.Log("[ManualRecorder] Started recording to: " + folder);
    }

    void LateUpdate()
    {
        if (currentFrame < framesToCapture)
        {
            string name = string.Format("{0}/frame_{1:D4}.png", folder, currentFrame);
            ScreenCapture.CaptureScreenshot(name);
            if (currentFrame % 30 == 0) Debug.Log("[ManualRecorder] Captured frame " + currentFrame);
            currentFrame++;
        }
        else if (currentFrame == framesToCapture)
        {
            currentFrame++;
            Debug.Log("[ManualRecorder] Finished capturing 360 frames.");
            EditorApplication.isPlaying = false; // Stop playmode when done
        }
    }
}
