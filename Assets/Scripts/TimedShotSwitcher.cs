using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TimedShotSwitcher - Time-driven camera switcher for KMITL Delta Academy Line Overview clip (A_LineOverview.mp4).
/// Sequences through locked zone-callout cameras in time order post-establishing shot:
/// 1. InfeedZoneCam (14s)
/// 2. FillingZoneCam (24s)
/// 3. CappingZoneCam (10s)
/// 4. EndOfLineCam (12s)
/// 5. ControlCam_Close (20s)
/// 
/// Uses accumulated Time.deltaTime in Update() (not Coroutines with WaitForSeconds) for frame-accurate
/// determinism across Unity Recorder frame-stepped capture, matching ChangeoverSequencer's timing architecture.
/// </summary>
public class TimedShotSwitcher : MonoBehaviour
{
    [System.Serializable]
    public struct TimedShot
    {
        [Tooltip("Descriptive name of the shot/zone")]
        public string shotName;

        [Tooltip("Camera GameObject to activate for this shot")]
        public Camera camera;

        [Tooltip("Hold duration in seconds")]
        public float holdDuration;
    }

    [Header("Shot List Configuration")]
    [Tooltip("Ordered sequence of cameras and hold durations")]
    public TimedShot[] shots;

    [Tooltip("Loop the shot sequence continuously (default false for single-pass A_LineOverview clip)")]
    public bool loopSequence = false;

    [Tooltip("Automatically start the sequence on Start()")]
    public bool autoPlayOnStart = true;

    [Header("Live Switcher Telemetry")]
    [SerializeField] private int currentShotIndex = 0;
    [SerializeField] private float shotElapsed = 0f;
    [SerializeField] private float totalElapsed = 0f;
    [SerializeField] private bool isPlaying = false;
    [SerializeField] private bool isComplete = false;
    [SerializeField] private Camera activeCamera;

    [Header("Shared Render Target (Multi-Camera Recorder Support)")]
    [SerializeField] private RenderTexture sharedRenderTexture;

    // Public Telemetry Properties (matching ChangeoverSequencer / CameraDirector conventions)
    public int CurrentShotIndex => currentShotIndex;
    public string CurrentShotName => (shots != null && currentShotIndex >= 0 && currentShotIndex < shots.Length)
        ? (string.IsNullOrEmpty(shots[currentShotIndex].shotName) ? (shots[currentShotIndex].camera != null ? shots[currentShotIndex].camera.name : $"Shot_{currentShotIndex}") : shots[currentShotIndex].shotName)
        : "None";
    public Camera CurrentCamera => (shots != null && currentShotIndex >= 0 && currentShotIndex < shots.Length)
        ? shots[currentShotIndex].camera
        : null;
    public Camera ActiveCamera => activeCamera;
    public float CurrentShotElapsed => shotElapsed;
    public float CurrentShotDuration => (shots != null && currentShotIndex >= 0 && currentShotIndex < shots.Length)
        ? shots[currentShotIndex].holdDuration
        : 0f;
    public float ShotProgress => (CurrentShotDuration > 0f)
        ? Mathf.Clamp01(shotElapsed / CurrentShotDuration)
        : 1f;
    public float TotalElapsed => totalElapsed;
    public bool IsPlaying => isPlaying;
    public bool IsComplete => isComplete;
    public int ShotCount => (shots != null) ? shots.Length : 0;

    private void Start()
    {
        if (autoPlayOnStart && shots != null && shots.Length > 0)
        {
            Play();
        }
    }

    private void Update()
    {
        if (!isPlaying || isComplete) return;
        if (shots == null || shots.Length == 0) return;

        shotElapsed += Time.deltaTime;
        totalElapsed += Time.deltaTime;

        float currentDuration = CurrentShotDuration;

        if (shotElapsed >= currentDuration)
        {
            AdvanceShot();
        }
    }

    /// <summary>
    /// Starts or resumes playback of the timed shot sequence.
    /// </summary>
    public void Play()
    {
        if (shots == null || shots.Length == 0)
        {
            Debug.LogWarning("[TimedShotSwitcher] Cannot Play: Shot list is empty!");
            return;
        }

        isPlaying = true;
        isComplete = false;

        // If starting fresh or index out of bounds, start at shot 0
        if (currentShotIndex < 0 || currentShotIndex >= shots.Length)
        {
            currentShotIndex = 0;
            shotElapsed = 0f;
            totalElapsed = 0f;
        }

        ActivateShot(currentShotIndex);
    }

    /// <summary>
    /// Pauses sequence playback at current shot and elapsed time.
    /// </summary>
    public void Pause()
    {
        isPlaying = false;
    }

    /// <summary>
    /// Restarts the sequence from shot 0.
    /// </summary>
    [ContextMenu("Restart Sequence")]
    public void Restart()
    {
        currentShotIndex = 0;
        shotElapsed = 0f;
        totalElapsed = 0f;
        isComplete = false;
        isPlaying = true;

        if (shots != null && shots.Length > 0)
        {
            ActivateShot(0);
        }
    }

    /// <summary>
    /// Jumps directly to a specific shot index.
    /// </summary>
    public void JumpToShot(int index)
    {
        if (shots == null || shots.Length == 0) return;

        if (index < 0 || index >= shots.Length)
        {
            Debug.LogWarning($"[TimedShotSwitcher] Invalid shot index {index}. Valid range: 0 to {shots.Length - 1}.");
            return;
        }

        currentShotIndex = index;
        shotElapsed = 0f;
        isComplete = false;
        ActivateShot(index);
    }

    /// <summary>
    /// Advances to the next shot in sequence.
    /// </summary>
    public void AdvanceShot()
    {
        if (shots == null || shots.Length == 0) return;

        int nextIndex = currentShotIndex + 1;
        if (nextIndex < shots.Length)
        {
            currentShotIndex = nextIndex;
            shotElapsed = 0f;
            ActivateShot(currentShotIndex);
        }
        else
        {
            if (loopSequence)
            {
                currentShotIndex = 0;
                shotElapsed = 0f;
                ActivateShot(0);
            }
            else
            {
                isPlaying = false;
                isComplete = true;
                Debug.Log($"[TimedShotSwitcher] Sequence completed all {shots.Length} shots in {totalElapsed:F2}s.");
            }
        }
    }

    /// <summary>
    /// Activates the designated camera at the given shot index and deactivates all other managed cameras.
    /// </summary>
    public void ActivateShot(int index)
    {
        if (shots == null || index < 0 || index >= shots.Length) return;

        Camera targetCam = shots[index].camera;

        // Disable all managed cameras across the shot list
        for (int i = 0; i < shots.Length; i++)
        {
            Camera cam = shots[i].camera;
            if (cam == null) continue;

            bool isTarget = (cam == targetCam);
            cam.enabled = isTarget;

            if (sharedRenderTexture != null)
            {
                cam.targetTexture = sharedRenderTexture;
            }
        }

        activeCamera = targetCam;
    }

    /// <summary>
    /// Assigns (or clears, pass null) a shared RenderTexture as targetTexture on all cameras in the shot list.
    /// Enables single-file multi-camera cuts via Unity Recorder (RenderTextureInputSettings).
    /// </summary>
    public void SetSharedRenderTexture(RenderTexture rt)
    {
        sharedRenderTexture = rt;
        if (shots == null) return;

        for (int i = 0; i < shots.Length; i++)
        {
            if (shots[i].camera != null)
            {
                shots[i].camera.targetTexture = rt;
            }
        }
    }
}
