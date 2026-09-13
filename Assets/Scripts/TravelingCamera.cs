using UnityEngine;

/// <summary>
/// TravelingCamera - Smooth traveling establishing camera controller for KMITL Delta Academy Line Overview clip (A_LineOverview.mp4).
/// Animates camera position along the full industrial line from upstream Infeed to downstream EndOfLine
/// using the project's quintic S-Curve servo easing function (ChangeoverSequencer.SCurve).
/// </summary>
public class TravelingCamera : MonoBehaviour
{
    [Header("Travel Coordinates")]
    [Tooltip("Starting world position of camera at upstream infeed end")]
    public Vector3 startPosition = new Vector3(2.20f, 1.80f, -4.80f);

    [Tooltip("Ending world position of camera at downstream end-of-line exit")]
    public Vector3 endPosition = new Vector3(2.20f, 1.80f, 4.50f);

    [Tooltip("Look target coordinates: X = target centerline (0 = conveyor center), Y = target elevation (0.90 = belt surface), Z = forward look-ahead distance relative to camera Z")]
    public Vector3 lookOffset = new Vector3(0.0f, 0.90f, 2.50f);

    [Header("Timing")]
    [Tooltip("Travel duration in seconds matching the 0:00-0:10 establishing shot in A_LineOverview.mp4")]
    public float duration = 10.0f;

    [Tooltip("Automatically restart and play travel animation when GameObject or component is enabled")]
    public bool autoPlayOnEnable = true;

    [Header("Live Telemetry")]
    [SerializeField] private float elapsedTime = 0f;
    [SerializeField] private bool isPlaying = false;

    public float ElapsedTime => elapsedTime;
    public float Progress => (duration > 0f) ? Mathf.Clamp01(elapsedTime / duration) : 1f;
    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        // Initialize camera at starting pose
        UpdateCameraPose(0f);
    }

    private void OnEnable()
    {
        if (autoPlayOnEnable)
        {
            Replay();
        }
    }

    private void Start()
    {
        if (autoPlayOnEnable && !isPlaying)
        {
            Replay();
        }
    }

    private void Update()
    {
        if (!isPlaying) return;

        elapsedTime += Time.deltaTime;
        float t = (duration > 0f) ? Mathf.Clamp01(elapsedTime / duration) : 1f;

        UpdateCameraPose(t);

        if (t >= 1.0f)
        {
            isPlaying = false;
        }
    }

    /// <summary>
    /// Updates camera position along the travel path and orients it towards the look target
    /// using the quintic S-curve easing from ChangeoverSequencer.
    /// </summary>
    /// <param name="normalizedTime">Normalized progress between 0.0 (start) and 1.0 (end)</param>
    public void UpdateCameraPose(float normalizedTime)
    {
        float t = Mathf.Clamp01(normalizedTime);
        float easedT = ChangeoverSequencer.SCurve(t);

        Vector3 currentPos = Vector3.Lerp(startPosition, endPosition, easedT);
        transform.position = currentPos;

        Vector3 lookTarget = new Vector3(lookOffset.x, lookOffset.y, currentPos.z + lookOffset.z);
        transform.LookAt(lookTarget);
    }

    /// <summary>
    /// Restarts the traveling shot animation from the start position.
    /// Accessible via Inspector context menu for quick iteration in Play Mode.
    /// </summary>
    [ContextMenu("Replay Travel")]
    public void Replay()
    {
        elapsedTime = 0f;
        isPlaying = true;
        UpdateCameraPose(0f);
    }

    /// <summary>
    /// Plays or resumes travel animation from current position.
    /// </summary>
    public void Play()
    {
        isPlaying = true;
    }

    /// <summary>
    /// Pauses travel animation at current position.
    /// </summary>
    public void Pause()
    {
        isPlaying = false;
    }

    /// <summary>
    /// Sets travel progress to a specific normalized time [0, 1] and updates camera pose immediately.
    /// </summary>
    public void SetProgress(float progress)
    {
        float clamped = Mathf.Clamp01(progress);
        elapsedTime = clamped * duration;
        UpdateCameraPose(clamped);
    }

    [ContextMenu("Jump To Start")]
    public void JumpToStart()
    {
        SetProgress(0f);
    }

    [ContextMenu("Jump To End")]
    public void JumpToEnd()
    {
        SetProgress(1f);
    }
}
