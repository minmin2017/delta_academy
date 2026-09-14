using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EquipmentIntroSequencer - Camera and glow sequencer for Clip C (Hardware Intro).
/// Implements the 6-shot storyboard specified in CLIP_C_HARDWARE_INTRO_PLAN.md:
///   Shot 0 (0-5s, 5s): Wide angle of control cabinet
///   Shot 1 (5-14s, 9s): PLC AS320T-B - pan in + pulsing cyan glow
///   Shot 2 (14-23s, 9s): HMI DOP-100WS / 103WQ - pan to door screen + glow
///   Shot 3 (23-32s, 9s): VFD MS300 - pan to lower cabinet row + glow
///   Shot 4 (32-43s, 11s): SERVO ASD-A3 x2 - servo drives in cabinet -> cut to real axes (Guide Rail X + Nozzle Z), both glow
///   Shot 5 (43-50s, 7s): Full line pull-back closing shot
/// Total duration: 50.0s (1500 frames @ 30fps).
/// </summary>
public class EquipmentIntroSequencer : MonoBehaviour
{
    [System.Serializable]
    public struct ShotDefinition
    {
        public string shotName;
        public float duration;
        public Vector3 startPos;
        public Vector3 startLookAt;
        public Vector3 endPos;
        public Vector3 endLookAt;
        public string[] highlightTargetNames;
    }

    [Header("Camera Setup")]
    public Camera introCamera;
    [SerializeField] private RenderTexture sharedRenderTexture;

    [Header("Live Playback State")]
    [SerializeField] private int currentShotIndex = 0;
    [SerializeField] private float shotElapsed = 0f;
    [SerializeField] private float totalElapsed = 0f;
    [SerializeField] private bool isPlaying = false;
    [SerializeField] private bool isComplete = false;
    public bool autoPlayOnStart = true;

    private static readonly Color HighlightColor = new Color(0.10f, 0.95f, 1.0f, 1f); // cyan hologram glow
    private const string EmissionKeyword = "_EMISSION";
    private const string EmissionProperty = "_EmissionColor";
    private const float PulseSpeed = 3.5f;
    private const float PulseMin = 0.6f;
    private const float PulseMax = 2.4f;

    private ShotDefinition[] shots;
    private Dictionary<string, Renderer[]> cachedRenderers = new Dictionary<string, Renderer[]>();
    private List<Renderer> activeGlowRenderers = new List<Renderer>();

    public int CurrentShotIndex => currentShotIndex;
    public string CurrentShotName => (shots != null && currentShotIndex >= 0 && currentShotIndex < shots.Length) ? shots[currentShotIndex].shotName : "None";
    public float CurrentShotElapsed => shotElapsed;
    public float TotalElapsed => totalElapsed;
    public bool IsPlaying => isPlaying;
    public bool IsComplete => isComplete;
    public const float TotalSequenceDuration = 50.0f;

    private void Awake()
    {
        InitializeShots();
    }

    private void Start()
    {
        InitializeShots();
        EnsureCamera();
        CacheAllTargetRenderers();
        if (autoPlayOnStart)
        {
            Play();
        }
    }

    private void InitializeShots()
    {
        shots = new ShotDefinition[]
        {
            // Shot 0: 0-5s (5s) - Wide angle of control cabinet
            new ShotDefinition
            {
                shotName = "Shot0_CabinetWide",
                duration = 5.0f,
                startPos = new Vector3(0.35f, 1.95f, 1.65f),
                startLookAt = new Vector3(-1.05f, 1.10f, 0.35f),
                endPos = new Vector3(0.20f, 1.85f, 1.45f),
                endLookAt = new Vector3(-1.05f, 1.10f, 0.35f),
                highlightTargetNames = new string[0]
            },
            // Shot 1: 5-14s (9s) - PLC AS320T-B (pan in + glow)
            new ShotDefinition
            {
                shotName = "Shot1_PLC_AS320T_B",
                duration = 9.0f,
                startPos = new Vector3(0.10f, 1.65f, 1.15f),
                startLookAt = new Vector3(-1.23f, 1.22f, 0.26f),
                endPos = new Vector3(-0.35f, 1.40f, 0.65f),
                endLookAt = new Vector3(-1.23f, 1.22f, 0.26f),
                highlightTargetNames = new string[] { "Delta_AS320T_B_PLC" }
            },
            // Shot 2: 14-23s (9s) - HMI DOP-100WS / 103WQ
            new ShotDefinition
            {
                shotName = "Shot2_HMI_DOP100WS",
                duration = 9.0f,
                startPos = new Vector3(0.10f, 1.50f, 1.50f),
                startLookAt = new Vector3(-0.82f, 1.25f, 1.03f),
                endPos = new Vector3(-0.25f, 1.35f, 1.30f),
                endLookAt = new Vector3(-0.82f, 1.25f, 1.03f),
                highlightTargetNames = new string[] { "Delta_DOP_100WS_HMI" }
            },
            // Shot 3: 23-32s (9s) - VFD MS300 -> Conveyor Drive Motor
            new ShotDefinition
            {
                shotName = "Shot3_VFD_MS300",
                duration = 9.0f,
                startPos = new Vector3(-0.10f, 1.20f, 0.80f),
                startLookAt = new Vector3(-1.22f, 0.85f, 0.26f),
                endPos = new Vector3(0.55f, 1.15f, 1.10f),
                endLookAt = new Vector3(-0.22f, 0.78f, 1.25f),
                highlightTargetNames = new string[] {
                    "Delta_MS300_VFD", "Conveyor_DriveMotor_140x200mm", "Conveyor_Gearbox_120x120x100mm"
                }
            },
            // Shot 4: 32-43s (11s) - SERVO ASD-A3 x2 drives -> ECMA Servo Motors (X & Z)
            new ShotDefinition
            {
                shotName = "Shot4_SERVO_ASD_A3",
                duration = 11.0f,
                startPos = new Vector3(-0.20f, 1.45f, 0.65f),
                startLookAt = new Vector3(-1.14f, 1.22f, 0.09f),
                endPos = new Vector3(0.70f, 1.45f, 0.35f),
                endLookAt = new Vector3(-0.26f, 1.35f, 0.40f),
                highlightTargetNames = new string[] {
                    "Delta_ASD_A3_X_Rail", "Delta_ASD_A3_Z_Nozzle",
                    "ServoMotor_GuideRail_X_ECMA", "ServoMotor_Nozzle_Z_ECMA",
                    "GuideRailAssembly_X", "NozzleAssembly_Z"
                }
            },
            // Shot 5: 43-50s (7s) - Closing wide system view
            new ShotDefinition
            {
                shotName = "Shot5_SystemWideClose",
                duration = 7.0f,
                startPos = new Vector3(2.20f, 2.10f, -1.80f),
                startLookAt = new Vector3(-0.20f, 1.10f, 0.50f),
                endPos = new Vector3(2.80f, 2.40f, -2.40f),
                endLookAt = new Vector3(-0.20f, 1.10f, 0.50f),
                highlightTargetNames = new string[] {
                    "Delta_AS320T_B_PLC", "Delta_DOP_100WS_HMI", "Delta_MS300_VFD",
                    "Delta_ASD_A3_X_Rail", "Delta_ASD_A3_Z_Nozzle",
                    "ServoMotor_GuideRail_X_ECMA", "ServoMotor_Nozzle_Z_ECMA"
                }
            }
        };
    }

    public void EnsureCamera()
    {
        if (introCamera == null)
        {
            GameObject camObj = GameObject.Find("EquipmentIntroCamera");
            if (camObj == null)
            {
                camObj = new GameObject("EquipmentIntroCamera");
                introCamera = camObj.AddComponent<Camera>();
                introCamera.fieldOfView = 45f;
                introCamera.nearClipPlane = 0.05f;
            }
            else
            {
                introCamera = camObj.GetComponent<Camera>();
            }
        }

        introCamera.enabled = true;
        if (sharedRenderTexture != null)
        {
            introCamera.targetTexture = sharedRenderTexture;
        }
    }

    public void SetSharedRenderTexture(RenderTexture rt)
    {
        sharedRenderTexture = rt;
        if (introCamera != null)
        {
            introCamera.targetTexture = rt;
        }
    }

    private void CacheAllTargetRenderers()
    {
        cachedRenderers.Clear();
        string[] allTargets = new string[]
        {
            "Delta_AS320T_B_PLC", "Delta_DOP_100WS_HMI", "Delta_MS300_VFD",
            "Delta_ASD_A3_X_Rail", "Delta_ASD_A3_Z_Nozzle",
            "Conveyor_DriveMotor_140x200mm", "Conveyor_Gearbox_120x120x100mm",
            "ServoMotor_GuideRail_X_ECMA", "ServoMotor_Nozzle_Z_ECMA",
            "GuideRailAssembly_X", "NozzleAssembly_Z"
        };

        foreach (var tName in allTargets)
        {
            GameObject go = GameObject.Find(tName);
            if (go != null)
            {
                Renderer[] rends = go.GetComponentsInChildren<Renderer>();
                cachedRenderers[tName] = rends;
            }
        }
    }

    public void Play()
    {
        isPlaying = true;
        isComplete = false;
        currentShotIndex = 0;
        shotElapsed = 0f;
        totalElapsed = 0f;
        ActivateShot(0);
    }

    public void Pause()
    {
        isPlaying = false;
        ClearActiveGlow();
    }

    public void Restart()
    {
        ClearActiveGlow();
        Play();
    }

    private void Update()
    {
        if (!isPlaying || isComplete) return;
        if (shots == null || shots.Length == 0) return;

        shotElapsed += Time.deltaTime;
        totalElapsed += Time.deltaTime;

        UpdateCabinetDoorAnimation();

        ShotDefinition currentShot = shots[currentShotIndex];
        float progress = Mathf.Clamp01(shotElapsed / currentShot.duration);
        float smoothT = Mathf.SmoothStep(0f, 1f, progress);

        UpdateCameraTransform(currentShotIndex, progress, smoothT);
        UpdateGlowPulse();

        if (shotElapsed >= currentShot.duration)
        {
            AdvanceShot();
        }
    }

    private void UpdateCabinetDoorAnimation()
    {
        GameObject cab = GameObject.Find("DeltaControlCabinet");
        if (cab == null) return;
        Transform doorHinge = cab.transform.Find("Cabinet_Door_Hinge");
        if (doorHinge == null) return;

        if (currentShotIndex == 0)
        {
            // Shot 0: Cabinet fully closed and assembled
            doorHinge.localRotation = Quaternion.identity;
        }
        else if (currentShotIndex == 1)
        {
            // Shot 1: Door smoothly swings open in first 1.5s
            float openT = Mathf.Clamp01(shotElapsed / 1.5f);
            float smoothOpen = Mathf.SmoothStep(0f, 1f, openT);
            doorHinge.localRotation = Quaternion.Euler(0f, Mathf.Lerp(0f, -85f, smoothOpen), 0f);
        }
        else if (currentShotIndex >= 2 && currentShotIndex <= 4)
        {
            // Shots 2-4: Door open for direct internal view
            doorHinge.localRotation = Quaternion.Euler(0f, -85f, 0f);
        }
        else if (currentShotIndex == 5)
        {
            // Shot 5: Wide close - door smoothly swings back closed in first 2.0s
            float closeT = Mathf.Clamp01(shotElapsed / 2.0f);
            float smoothClose = Mathf.SmoothStep(0f, 1f, closeT);
            doorHinge.localRotation = Quaternion.Euler(0f, Mathf.Lerp(-85f, 0f, smoothClose), 0f);
        }
    }

    private void UpdateCameraTransform(int shotIdx, float progress, float smoothT)
    {
        if (introCamera == null) return;

        ShotDefinition shot = shots[shotIdx];

        if (shotIdx == 3)
        {
            // Shot 3: 0-4.5s VFD in cabinet, 4.5-9s cut/pan to Conveyor Drive Motor on machine
            if (shotElapsed < 4.5f)
            {
                float subT = Mathf.SmoothStep(0f, 1f, shotElapsed / 4.5f);
                Vector3 p = Vector3.Lerp(new Vector3(-0.10f, 1.20f, 0.80f), new Vector3(-0.55f, 0.95f, 0.50f), subT);
                Vector3 look = new Vector3(-1.22f, 0.85f, 0.26f);
                introCamera.transform.position = p;
                introCamera.transform.rotation = Quaternion.LookRotation((look - p).normalized, Vector3.up);
            }
            else
            {
                float subT = Mathf.SmoothStep(0f, 1f, (shotElapsed - 4.5f) / 4.5f);
                Vector3 p = Vector3.Lerp(new Vector3(0.30f, 1.15f, 0.85f), new Vector3(0.55f, 1.15f, 1.10f), subT);
                Vector3 look = new Vector3(-0.22f, 0.78f, 1.25f);
                introCamera.transform.position = p;
                introCamera.transform.rotation = Quaternion.LookRotation((look - p).normalized, Vector3.up);
            }
        }
        else if (shotIdx == 4)
        {
            // Shot 4: 0-5s ASD-A3 drives in cabinet, 5-11s cut/pan to physical Servo Motors on machine
            if (shotElapsed < 5.0f)
            {
                float subT = Mathf.SmoothStep(0f, 1f, shotElapsed / 5.0f);
                Vector3 p = Vector3.Lerp(new Vector3(-0.20f, 1.45f, 0.65f), new Vector3(-0.50f, 1.35f, 0.40f), subT);
                Vector3 look = new Vector3(-1.14f, 1.22f, 0.09f);
                introCamera.transform.position = p;
                introCamera.transform.rotation = Quaternion.LookRotation((look - p).normalized, Vector3.up);
            }
            else
            {
                float subT = Mathf.SmoothStep(0f, 1f, (shotElapsed - 5.0f) / 6.0f);
                Vector3 p = Vector3.Lerp(new Vector3(0.95f, 1.55f, 0.35f), new Vector3(0.70f, 1.45f, 0.35f), subT);
                Vector3 look = new Vector3(-0.26f, 1.35f, 0.40f);
                introCamera.transform.position = p;
                introCamera.transform.rotation = Quaternion.LookRotation((look - p).normalized, Vector3.up);
            }
        }
        else
        {
            Vector3 p = Vector3.Lerp(shot.startPos, shot.endPos, smoothT);
            Vector3 look = Vector3.Lerp(shot.startLookAt, shot.endLookAt, smoothT);
            introCamera.transform.position = p;
            introCamera.transform.rotation = Quaternion.LookRotation((look - p).normalized, Vector3.up);
        }
    }

    private void ActivateShot(int index)
    {
        if (shots == null || index < 0 || index >= shots.Length) return;

        ClearActiveGlow();

        ShotDefinition shot = shots[index];
        activeGlowRenderers.Clear();

        foreach (var tName in shot.highlightTargetNames)
        {
            if (cachedRenderers.TryGetValue(tName, out Renderer[] rends))
            {
                foreach (var r in rends)
                {
                    if (r != null) activeGlowRenderers.Add(r);
                }
            }
        }

        UpdateCameraTransform(index, 0f, 0f);
    }

    private void AdvanceShot()
    {
        int next = currentShotIndex + 1;
        if (next < shots.Length)
        {
            currentShotIndex = next;
            shotElapsed = 0f;
            ActivateShot(currentShotIndex);
        }
        else
        {
            isPlaying = false;
            isComplete = true;
            ClearActiveGlow();
            Debug.Log(string.Format("[EquipmentIntroSequencer] Completed all {0} shots in {1:F2}s.", shots.Length, totalElapsed));
        }
    }

    private void UpdateGlowPulse()
    {
        if (activeGlowRenderers.Count == 0) return;

        float pulse = Mathf.Lerp(PulseMin, PulseMax, (Mathf.Sin(Time.time * PulseSpeed) + 1f) * 0.5f);

        foreach (var r in activeGlowRenderers)
        {
            if (r == null) continue;
            Material mat = r.material;
            mat.EnableKeyword(EmissionKeyword);
            mat.SetColor(EmissionProperty, HighlightColor * pulse);
        }
    }

    private void ClearActiveGlow()
    {
        foreach (var r in activeGlowRenderers)
        {
            if (r == null) continue;
            Material mat = r.material;
            mat.SetColor(EmissionProperty, Color.black);
            mat.DisableKeyword(EmissionKeyword);
        }
        activeGlowRenderers.Clear();
    }

    private void OnDisable()
    {
        ClearActiveGlow();
    }
}
