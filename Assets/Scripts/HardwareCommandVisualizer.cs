using System;
using UnityEngine;
using TMPro;

/// <summary>
/// HardwareCommandVisualizer - Visualizes the live PLC -> Hardware communication command path
/// during the changeover sequence (Step 5 of Clip B expansion):
/// 1. S6_RetractNozzleToHome:
///    PLC AS320T-B ──(Pulse Train 200kHz)──► Servo ASD-A3 Z ──► Retract Nozzle to Home 1,250.0 mm
/// 2. S7_AdjustRailWidth:
///    PLC AS320T-B ──(Pulse Train 200kHz)──► Servo ASD-A3 X ──► Lead Screw Rail Gap fromRecipe ➔ toRecipe mm
/// 3. S8_ConfirmInPosition:
///    PLC AS320T-B ──(Modbus RTU RS-485)──► VFD MS300 ──► Conveyor Speed Preset toRecipe m/s
/// </summary>
public class HardwareCommandVisualizer : MonoBehaviour
{
    [Header("Sequencer Reference")]
    public ChangeoverSequencer sequencer;

    [Header("Hardware Object References")]
    public Transform plcTransform;
    public Transform vfdTransform;
    public Transform servoXTransform;
    public Transform servoZTransform;
    public Transform guideRailAssembly;
    public Transform nozzleAssembly;

    // Cached Renderers for synchronized glow
    private Renderer[] plcRenderers;
    private Renderer[] vfdRenderers;
    private Renderer[] servoXRenderers;
    private Renderer[] servoZRenderers;

    // Signal Lines
    private LineRenderer lineS6; // PLC -> Servo Z -> Nozzle
    private LineRenderer lineS7; // PLC -> Servo X -> Rail
    private LineRenderer lineS8; // PLC -> VFD
    private Material lineMaterial;

    // Command Callout Banners (World-Space TextMeshPro)
    private TextMeshPro bannerS6;
    private TextMeshPro bannerS7;
    private TextMeshPro bannerS8;

    private static readonly Color CyanGlowColor = new Color(0.10f, 0.95f, 1.0f, 1f);
    private const string EmissionKeyword = "_EMISSION";
    private const string EmissionProperty = "_EmissionColor";
    private const float PulseSpeed = 4.5f;

    private bool s6Highlighted = false;
    private bool s7Highlighted = false;
    private bool s8Highlighted = false;

    private void Awake()
    {
        ValidateAndCacheReferences();
    }

    private void Start()
    {
        ValidateAndCacheReferences();
        CacheRenderers();
        CreateSignalLines();
        CreateBanners();
    }

    public void ValidateAndCacheReferences()
    {
        if (sequencer == null) sequencer = GetComponent<ChangeoverSequencer>();
        if (sequencer == null) sequencer = UnityEngine.Object.FindAnyObjectByType<ChangeoverSequencer>();

        if (plcTransform == null)
        {
            GameObject go = GameObject.Find("Delta_AS320T_B_PLC");
            if (go != null) plcTransform = go.transform;
        }
        if (vfdTransform == null)
        {
            GameObject go = GameObject.Find("Delta_MS300_VFD");
            if (go != null) vfdTransform = go.transform;
        }
        if (servoXTransform == null)
        {
            GameObject go = GameObject.Find("Delta_ASD_A3_X_Rail");
            if (go != null) servoXTransform = go.transform;
        }
        if (servoZTransform == null)
        {
            GameObject go = GameObject.Find("Delta_ASD_A3_Z_Nozzle");
            if (go != null) servoZTransform = go.transform;
        }
        if (guideRailAssembly == null)
        {
            if (sequencer != null && sequencer.guideRailAssembly != null) guideRailAssembly = sequencer.guideRailAssembly;
            else
            {
                GameObject go = GameObject.Find("GuideRailAssembly_X");
                if (go != null) guideRailAssembly = go.transform;
            }
        }
        if (nozzleAssembly == null)
        {
            if (sequencer != null && sequencer.nozzleAssembly != null) nozzleAssembly = sequencer.nozzleAssembly;
            else
            {
                GameObject go = GameObject.Find("NozzleAssembly_Z");
                if (go != null) nozzleAssembly = go.transform;
            }
        }
    }

    private void CacheRenderers()
    {
        plcRenderers = (plcTransform != null) ? plcTransform.GetComponentsInChildren<Renderer>() : new Renderer[0];
        vfdRenderers = (vfdTransform != null) ? vfdTransform.GetComponentsInChildren<Renderer>() : new Renderer[0];
        servoXRenderers = (servoXTransform != null) ? servoXTransform.GetComponentsInChildren<Renderer>() : new Renderer[0];
        servoZRenderers = (servoZTransform != null) ? servoZTransform.GetComponentsInChildren<Renderer>() : new Renderer[0];
    }

    private void CreateSignalLines()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        lineMaterial = new Material(shader);
        lineMaterial.name = "SignalPulse_Mat";
        if (lineMaterial.HasProperty("_BaseColor")) lineMaterial.SetColor("_BaseColor", CyanGlowColor);
        else if (lineMaterial.HasProperty("_Color")) lineMaterial.SetColor("_Color", CyanGlowColor);

        // Line S6: PLC -> Servo Z -> Nozzle
        Vector3[] s6Points = new Vector3[]
        {
            new Vector3(-1.23f, 1.22f, 0.26f), // PLC
            new Vector3(-1.18f, 1.22f, 0.15f), // Conduit
            new Vector3(-1.12f, 1.22f, 0.04f), // Servo Z
            new Vector3(-0.70f, 1.45f, 0.30f), // Gantry run
            new Vector3(-0.28f, 1.35f, 0.60f)  // Nozzle carriage
        };
        lineS6 = CreateLine("SignalLine_S6_NozzlePulse", s6Points, lineMaterial);

        // Line S7: PLC -> Servo X -> Rail
        Vector3[] s7Points = new Vector3[]
        {
            new Vector3(-1.23f, 1.22f, 0.26f), // PLC
            new Vector3(-1.19f, 1.22f, 0.20f), // Conduit
            new Vector3(-1.15f, 1.22f, 0.14f), // Servo X
            new Vector3(-0.60f, 0.98f, 0.10f), // Lead screw drag chain
            new Vector3(0.00f, 0.96f, 0.00f)   // Guide rail center
        };
        lineS7 = CreateLine("SignalLine_S7_RailPulse", s7Points, lineMaterial);

        // Line S8: PLC -> VFD
        Vector3[] s8Points = new Vector3[]
        {
            new Vector3(-1.23f, 1.15f, 0.26f), // PLC RS-485
            new Vector3(-1.23f, 1.00f, 0.26f), // DIN wireway
            new Vector3(-1.22f, 0.85f, 0.26f)  // VFD RS-485
        };
        lineS8 = CreateLine("SignalLine_S8_ModbusRTU", s8Points, lineMaterial);
    }

    private LineRenderer CreateLine(string name, Vector3[] points, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(this.transform);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = mat;
        lr.useWorldSpace = true;
        lr.positionCount = points.Length;
        lr.SetPositions(points);
        lr.startWidth = 0.012f;
        lr.endWidth = 0.012f;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.enabled = false;
        return lr;
    }

    private void CreateBanners()
    {
        TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // Banner S6: Positioned above nozzle facing NozzleSideCam (+X direction)
        if (bannerS6 == null)
        {
            GameObject bObj = new GameObject("Banner_S6_NozzlePulse");
            bObj.transform.SetParent(this.transform);
            bObj.transform.position = new Vector3(0.15f, 1.48f, 0.55f);
            bObj.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

            bannerS6 = bObj.AddComponent<TextMeshPro>();
            if (fontAsset != null) bannerS6.font = fontAsset;
            bannerS6.fontSize = 0.11f;
            bannerS6.alignment = TextAlignmentOptions.Center;
            bannerS6.rectTransform.sizeDelta = new Vector2(1.10f, 0.25f);
            bObj.SetActive(false);
        }

        // Banner S7: Positioned on side floor facing straight up (+Y) into RailTopCam
        if (bannerS7 == null)
        {
            GameObject bObj = new GameObject("Banner_S7_RailPulse");
            bObj.transform.SetParent(this.transform);
            bObj.transform.position = new Vector3(0.48f, 1.05f, -0.15f);
            bObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            bannerS7 = bObj.AddComponent<TextMeshPro>();
            if (fontAsset != null) bannerS7.font = fontAsset;
            bannerS7.fontSize = 0.11f;
            bannerS7.alignment = TextAlignmentOptions.Center;
            bannerS7.rectTransform.sizeDelta = new Vector2(1.10f, 0.30f);
            bObj.SetActive(false);
        }

        // Banner S8: Positioned directly above HMI bezel facing ControlCam_Close (-Z direction)
        if (bannerS8 == null)
        {
            GameObject bObj = new GameObject("Banner_S8_VFDModbus");
            bObj.transform.SetParent(this.transform);
            bObj.transform.position = new Vector3(-0.82f, 1.298f, 1.055f);
            bObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            bannerS8 = bObj.AddComponent<TextMeshPro>();
            if (fontAsset != null) bannerS8.font = fontAsset;
            bannerS8.fontSize = 0.009f;
            bannerS8.alignment = TextAlignmentOptions.Center;
            bannerS8.rectTransform.sizeDelta = new Vector2(0.18f, 0.04f);
            bObj.SetActive(false);
        }
    }

    private void Update()
    {
        if (sequencer == null)
        {
            ValidateAndCacheReferences();
            if (sequencer == null) return;
        }

        ChangeoverSequencer.ChangeoverState state = sequencer.CurrentState;
        bool isS6 = (state == ChangeoverSequencer.ChangeoverState.S6_RetractNozzleToHome);
        bool isS7 = (state == ChangeoverSequencer.ChangeoverState.S7_AdjustRailWidth);
        bool isS8 = (state == ChangeoverSequencer.ChangeoverState.S8_ConfirmInPosition);

        float pulse = Mathf.Lerp(0.8f, 2.6f, (Mathf.Sin(Time.time * PulseSpeed) + 1f) * 0.5f);

        // Fetch dynamic recipes
        ChangeoverSequencer.Recipe fromRec = (ChangeoverSequencer.Recipes != null && sequencer.initialRecipeIndex >= 0 && sequencer.initialRecipeIndex < ChangeoverSequencer.Recipes.Length)
            ? ChangeoverSequencer.Recipes[sequencer.initialRecipeIndex] : default;
        ChangeoverSequencer.Recipe toRec = (ChangeoverSequencer.Recipes != null && sequencer.targetRecipeIndex >= 0 && sequencer.targetRecipeIndex < ChangeoverSequencer.Recipes.Length)
            ? ChangeoverSequencer.Recipes[sequencer.targetRecipeIndex] : default;

        // 1. Manage S6: PLC -> Servo Z -> Retract Nozzle
        if (isS6)
        {
            SetHighlight(plcRenderers, true, pulse, CyanGlowColor);
            SetHighlight(servoZRenderers, true, pulse, CyanGlowColor);
            s6Highlighted = true;

            if (lineS6 != null && !lineS6.enabled) lineS6.enabled = true;
            if (bannerS6 != null)
            {
                if (!bannerS6.gameObject.activeSelf) bannerS6.gameObject.SetActive(true);
                bannerS6.text = $"<color=#00E5FF><b>PLC AS320T-B  ──[ Pulse Train 200 kHz ]──►  ASD-A3 (AXIS Z)</b></color>\n" +
                                $"<color=#FFFFFF>COMMAND: HOMING RETRACT -> 1,250.0 mm  |  POS: {sequencer.CurrentNozzleHeightMm:F1} mm  [SYNC]</color>";
            }
        }
        else if (s6Highlighted)
        {
            SetHighlight(plcRenderers, false, 0f, Color.black);
            SetHighlight(servoZRenderers, false, 0f, Color.black);
            s6Highlighted = false;
            if (lineS6 != null && lineS6.enabled) lineS6.enabled = false;
            if (bannerS6 != null && bannerS6.gameObject.activeSelf) bannerS6.gameObject.SetActive(false);
        }

        // 2. Manage S7: PLC -> Servo X -> Adjust Rail Gap
        if (isS7)
        {
            SetHighlight(plcRenderers, true, pulse, CyanGlowColor);
            SetHighlight(servoXRenderers, true, pulse, CyanGlowColor);
            s7Highlighted = true;

            if (lineS7 != null && !lineS7.enabled) lineS7.enabled = true;
            if (bannerS7 != null)
            {
                if (!bannerS7.gameObject.activeSelf) bannerS7.gameObject.SetActive(true);
                float fromGapMm = fromRec.railGap * 1000f;
                float toGapMm = toRec.railGap * 1000f;
                bannerS7.text = $"<color=#00E5FF><b>PLC AS320T-B  ──[ Pulse Train 200 kHz ]──►  ASD-A3 (AXIS X)</b></color>\n" +
                                $"<color=#FFFFFF>COMMAND: RAIL GAP {fromGapMm:F1}mm -> {toGapMm:F1}mm  |  LIVE: {sequencer.CurrentRailGapMm:F1} mm  [SYNC]</color>";
            }
        }
        else if (s7Highlighted)
        {
            SetHighlight(plcRenderers, false, 0f, Color.black);
            SetHighlight(servoXRenderers, false, 0f, Color.black);
            s7Highlighted = false;
            if (lineS7 != null && lineS7.enabled) lineS7.enabled = false;
            if (bannerS7 != null && bannerS7.gameObject.activeSelf) bannerS7.gameObject.SetActive(false);
        }

        // 3. Manage S8: PLC -> VFD MS300 -> Conveyor Speed Preset
        if (isS8)
        {
            SetHighlight(plcRenderers, true, pulse, CyanGlowColor);
            SetHighlight(vfdRenderers, true, pulse, CyanGlowColor);
            s8Highlighted = true;

            if (lineS8 != null && !lineS8.enabled) lineS8.enabled = true;
            if (bannerS8 != null)
            {
                if (!bannerS8.gameObject.activeSelf) bannerS8.gameObject.SetActive(true);
                bannerS8.text = $"<color=#00E5FF><b>PLC AS320T-B  ──[ Modbus RTU RS-485 ]──►  VFD MS300</b></color>\n" +
                                $"<color=#FFFFFF>COMMAND: SET BELT SPEED -> {toRec.beltSpeed:F2} m/s  |  S-CURVE RAMP  [OK]</color>";
            }
        }
        else if (s8Highlighted)
        {
            SetHighlight(plcRenderers, false, 0f, Color.black);
            SetHighlight(vfdRenderers, false, 0f, Color.black);
            s8Highlighted = false;
            if (lineS8 != null && lineS8.enabled) lineS8.enabled = false;
            if (bannerS8 != null && bannerS8.gameObject.activeSelf) bannerS8.gameObject.SetActive(false);
        }
    }

    private void SetHighlight(Renderer[] renderers, bool active, float pulseIntensity, Color baseColor)
    {
        if (renderers == null || renderers.Length == 0) return;

        foreach (var r in renderers)
        {
            if (r == null) continue;
            Material mat = r.material; // safe instanced copy
            if (active)
            {
                mat.EnableKeyword(EmissionKeyword);
                mat.SetColor(EmissionProperty, baseColor * pulseIntensity);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
            }
            else
            {
                mat.SetColor(EmissionProperty, Color.black);
                mat.DisableKeyword(EmissionKeyword);
            }
        }
    }

    private void OnDisable()
    {
        CleanupAll();
    }

    private void OnDestroy()
    {
        CleanupAll();
    }

    public void CleanupAll()
    {
        if (lineS6 != null) lineS6.enabled = false;
        if (lineS7 != null) lineS7.enabled = false;
        if (lineS8 != null) lineS8.enabled = false;

        if (bannerS6 != null && bannerS6.gameObject != null) bannerS6.gameObject.SetActive(false);
        if (bannerS7 != null && bannerS7.gameObject != null) bannerS7.gameObject.SetActive(false);
        if (bannerS8 != null && bannerS8.gameObject != null) bannerS8.gameObject.SetActive(false);

        SetHighlight(plcRenderers, false, 0f, Color.black);
        SetHighlight(vfdRenderers, false, 0f, Color.black);
        SetHighlight(servoXRenderers, false, 0f, Color.black);
        SetHighlight(servoZRenderers, false, 0f, Color.black);
        s6Highlighted = false;
        s7Highlighted = false;
        s8Highlighted = false;
    }
}
