using UnityEngine;
using TMPro;

/// <summary>
/// MotionHighlightController - Min's feedback (2026-09-14): "ราวยืด ตำแหน่งเปลี่ยน ต้องใช้ตา
/// สังเกตมากเกินไป ถ้ามี highlight or holograms บอกจะสังเกตได้ดีมาก" - subtle millimeter-scale
/// rail/nozzle motion is easy to miss in a wide static shot. This makes it unmissable via two
/// combined effects, both driven live off ChangeoverSequencer's real state/telemetry (never
/// hand-tuned per-shot, so it stays correct if timings change):
///
/// 1. Pulsing cyan emissive "hologram" glow on whichever part is actively moving
///    (GuideRail_Left/Right during S7_AdjustRailWidth, NozzleAssembly_Z parts during
///    S6_RetractNozzleToHome..S9_FirstArticleCheck) via renderer.material (instanced copy,
///    NOT sharedMaterial - DeltaMaterials.BrushedStainless()/PaintedSteel() are shared across
///    many unrelated parts, so mutating sharedMaterial would highlight everything using that
///    same material asset, not just this one rail).
/// 2. A live-updating world-space TMP number readout next to the part (not a Screen Space -
///    Overlay canvas like TelemetryOverlayController/LeaderLineOverlay use - those are NOT
///    captured by the RenderTexture-based Recorder pipeline this project's renders depend on,
///    confirmed via LeaderLineOverlay.GetOrCreateDefaultCanvas() hardcoding
///    RenderMode.ScreenSpaceOverlay - so this uses the same world-space TextMeshPro pattern
///    already proven to work in every render this session (ZoneLabelsBuilder, CappingZoneBuilder).
/// </summary>
public class MotionHighlightController : MonoBehaviour
{
    public ChangeoverSequencer sequencer;
    public Transform guideRailAssembly;
    public Transform nozzleAssembly;

    private Renderer[] railRenderers;
    private Renderer[] nozzleRenderers;
    private TextMeshPro railLabel;
    private TextMeshPro nozzleLabel;

    private static readonly Color HighlightColor = new Color(0.10f, 0.95f, 1.0f, 1f); // cyan, reads as "hologram"
    private const string EmissionKeyword = "_EMISSION";
    private const string EmissionProperty = "_EmissionColor";
    private const float PulseSpeed = 3.5f;
    private const float PulseMin = 0.6f;
    private const float PulseMax = 2.4f;

    private bool railHighlightActive = false;
    private bool nozzleHighlightActive = false;

    private void Awake()
    {
        ValidateAndCacheReferences();
    }

    private void Start()
    {
        ValidateAndCacheReferences();
        CacheRenderers();
        CreateLabels();
    }

    public void ValidateAndCacheReferences()
    {
        if (sequencer == null) sequencer = GetComponent<ChangeoverSequencer>();
        if (sequencer == null) sequencer = Object.FindAnyObjectByType<ChangeoverSequencer>();

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
        railRenderers = (guideRailAssembly != null) ? guideRailAssembly.GetComponentsInChildren<Renderer>() : new Renderer[0];
        nozzleRenderers = (nozzleAssembly != null) ? nozzleAssembly.GetComponentsInChildren<Renderer>() : new Renderer[0];
    }

    private void CreateLabels()
    {
        TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (guideRailAssembly != null && railLabel == null)
        {
            // RailTopCam shoots straight down (not a 3/4 +X-side angle like the other locked
            // cameras), so this label must lie flat facing up (X=90) instead of the Y=-90
            // "face +X camera" convention used everywhere else in this project.
            railLabel = CreateRuntimeLabel(guideRailAssembly, "MotionLabel_Rail",
                new Vector3(0.22f, 0.02f, 0.35f), new Vector3(90f, 0f, 0f), fontAsset);
        }
        if (nozzleAssembly != null && nozzleLabel == null)
        {
            // Pushed further out in X and up in Y so the text clears the glowing carriage arm
            // itself (confirmed via live screenshot - original offset sat right on top of the
            // glow, hard to read against the bright emissive color).
            nozzleLabel = CreateRuntimeLabel(nozzleAssembly, "MotionLabel_Nozzle",
                new Vector3(0.55f, 0.30f, 0f), new Vector3(0f, -90f, 0f), fontAsset);
        }
    }

    private TextMeshPro CreateRuntimeLabel(Transform parent, string name, Vector3 localPos, Vector3 localRot, TMP_FontAsset font)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(localRot);

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        if (font != null) tmp.font = font;
        tmp.fontSize = 0.10f;
        tmp.color = HighlightColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = new Vector2(0.60f, 0.14f);

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        go.SetActive(false);
        return tmp;
    }

    private void Update()
    {
        if (sequencer == null)
        {
            ValidateAndCacheReferences();
            if (sequencer == null) return;
        }
        if (railRenderers == null) CacheRenderers();

        ChangeoverSequencer.ChangeoverState state = sequencer.CurrentState;
        bool isRailActive = (state == ChangeoverSequencer.ChangeoverState.S7_AdjustRailWidth);
        bool isNozzleActive = (state >= ChangeoverSequencer.ChangeoverState.S6_RetractNozzleToHome &&
                               state <= ChangeoverSequencer.ChangeoverState.S9_FirstArticleCheck);

        float pulse = Mathf.Lerp(PulseMin, PulseMax, (Mathf.Sin(Time.time * PulseSpeed) + 1f) * 0.5f);

        SetHighlight(railRenderers, isRailActive, pulse, ref railHighlightActive);
        SetHighlight(nozzleRenderers, isNozzleActive, pulse, ref nozzleHighlightActive);

        if (railLabel != null)
        {
            if (railLabel.gameObject.activeSelf != isRailActive) railLabel.gameObject.SetActive(isRailActive);
            if (isRailActive) railLabel.text = $"<b>RAIL WIDTH\n{sequencer.CurrentRailGapMm:F1} mm</b>";
        }
        if (nozzleLabel != null)
        {
            if (nozzleLabel.gameObject.activeSelf != isNozzleActive) nozzleLabel.gameObject.SetActive(isNozzleActive);
            if (isNozzleActive) nozzleLabel.text = $"<b>NOZZLE HEIGHT\n{sequencer.CurrentNozzleHeightMm:F1} mm</b>";
        }
    }

    private void SetHighlight(Renderer[] renderers, bool active, float pulseIntensity, ref bool currentlyActive)
    {
        if (renderers == null || renderers.Length == 0) return;
        if (!active && !currentlyActive) return; // nothing to do, avoid touching materials every frame when idle

        foreach (var r in renderers)
        {
            if (r == null) continue;
            Material mat = r.material; // instanced copy - safe to mutate, does not affect shared assets
            if (active)
            {
                mat.EnableKeyword(EmissionKeyword);
                mat.SetColor(EmissionProperty, HighlightColor * pulseIntensity);
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
        currentlyActive = active;
    }
}
