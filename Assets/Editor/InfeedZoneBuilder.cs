using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using TMPro;

/// <summary>
/// Runtime idle rotator for infeed zone elements (e.g. Unscrambler rotary disc).
/// Provides subtle visual motion to avoid static "dead" shots.
/// </summary>
public class InfeedRotator : MonoBehaviour
{
    [Tooltip("Rotation speed in degrees per second.")]
    public float rotationSpeed = 15.0f;
    public Vector3 rotationAxis = Vector3.up;

    private void Update()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
    }
}

/// <summary>
/// Additive, idempotent builder for the Infeed Zone visual props.
/// Builds low-detail, high-realism visual context props (Unscrambler, Infeed Conveyor,
/// Star Wheel, Bottle Present Sensor, Stopper Cylinder) upstream (-Z) of the main conveyor.
/// </summary>
public static class InfeedZoneBuilder
{
    private const string RootName = "Cell";
    private const string InfeedRootName = "InfeedZone";

    [MenuItem("Tools/Delta/Add Infeed Zone Props")]
    public static void AddInfeedZoneProps()
    {
        // 1. Locate root Cell
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = GameObject.Find("IndustrialCell_Delta");
        }

        if (root == null)
        {
            Debug.LogError($"[InfeedZoneBuilder] Scene root '{RootName}' not found! Run Tools/Delta/Build Cell first.");
            return;
        }

        // 2. Delete existing InfeedZone child if present (Idempotent additive rebuild)
        Transform existingInfeed = root.transform.Find(InfeedRootName);
        if (existingInfeed != null)
        {
            Undo.DestroyObjectImmediate(existingInfeed.gameObject);
        }

        // 3. Materials reused directly from DeltaMaterials palette
        Material frameSteel = DeltaMaterials.PaintedSteel(new Color(0.38f, 0.39f, 0.41f, 1f));
        Material rotaryDiscSteel = DeltaMaterials.PaintedSteel(new Color(0.32f, 0.33f, 0.35f, 1f));
        Material pedestalSteel = DeltaMaterials.PaintedSteel(new Color(0.25f, 0.27f, 0.29f, 1f));
        Material basePlinthSteel = DeltaMaterials.PaintedSteel(new Color(0.18f, 0.19f, 0.20f, 1f));
        Material deviceDark = DeltaMaterials.PaintedSteel(new Color(0.14f, 0.15f, 0.16f, 1f));
        Material cylinderBodySteel = DeltaMaterials.PaintedSteel(new Color(0.22f, 0.24f, 0.26f, 1f));
        Material stainlessMat = DeltaMaterials.BrushedStainless();
        Material darkRubberMat = DeltaMaterials.DarkRubber();
        Material opticalRed = DeltaMaterials.PaintedSteel(new Color(0.85f, 0.15f, 0.15f, 1f));

        // Emissive LED indicator material for photoelectric sensor
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        Material ledGreen = new Material(urpLit) { name = "M_LED_Infeed_Green" };
        ledGreen.SetColor("_BaseColor", new Color(0.2f, 1.0f, 0.3f, 1f));
        ledGreen.EnableKeyword("_EMISSION");
        ledGreen.SetColor("_EmissionColor", new Color(0.1f, 1.0f, 0.2f) * 3.5f);

        // Load TextMeshPro Font Asset
        TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (fontAsset == null)
        {
            fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }

        // 4. Create InfeedZone Root GameObject
        GameObject infeedObj = new GameObject(InfeedRootName);
        Undo.RegisterCreatedObjectUndo(infeedObj, "Add Infeed Zone Props");
        infeedObj.transform.SetParent(root.transform);
        infeedObj.transform.localPosition = Vector3.zero;
        infeedObj.transform.localRotation = Quaternion.identity;

        // =========================================================================
        // 1. BOTTLE UNSCRAMBLER (ROTARY ACCUMULATION TABLE)
        // Located at X = 0.0, Z = -3.20m (upstream of infeed conveyor).
        // Diameter ~0.75m, working height at Y ~ 0.90m matching conveyor belt top.
        // Slowly rotates continuously via InfeedRotator.
        // =========================================================================
        GameObject unscramblerRoot = new GameObject("Bottle_Unscrambler");
        unscramblerRoot.transform.SetParent(infeedObj.transform);
        unscramblerRoot.transform.localPosition = new Vector3(0f, 0f, -3.20f);

        // Base Plinth / Floor Plate (0.60 x 0.02 x 0.60 m)
        GameObject unscramblerBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        unscramblerBase.name = "Unscrambler_BasePlate";
        unscramblerBase.transform.SetParent(unscramblerRoot.transform);
        unscramblerBase.transform.localPosition = new Vector3(0f, 0.01f, 0f);
        unscramblerBase.transform.localScale = new Vector3(0.60f, 0.02f, 0.60f);
        unscramblerBase.GetComponent<Renderer>().sharedMaterial = basePlinthSteel;

        // Central Pedestal Column (Cylinder, 0.22m diameter x 0.84m tall)
        GameObject unscramblerColumn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        unscramblerColumn.name = "Unscrambler_Pedestal_Column";
        unscramblerColumn.transform.SetParent(unscramblerRoot.transform);
        unscramblerColumn.transform.localPosition = new Vector3(0f, 0.44f, 0f);
        unscramblerColumn.transform.localScale = new Vector3(0.22f, 0.42f, 0.22f);
        unscramblerColumn.GetComponent<Renderer>().sharedMaterial = pedestalSteel;

        // Rotary Disc Bearing Hub Housing
        GameObject unscramblerHubHousing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        unscramblerHubHousing.name = "Unscrambler_HubHousing";
        unscramblerHubHousing.transform.SetParent(unscramblerRoot.transform);
        unscramblerHubHousing.transform.localPosition = new Vector3(0f, 0.86f, 0f);
        unscramblerHubHousing.transform.localScale = new Vector3(0.35f, 0.02f, 0.35f);
        unscramblerHubHousing.GetComponent<Renderer>().sharedMaterial = pedestalSteel;

        // Rotating Disc (Diameter 0.75m, Top Surface Y = 0.90m)
        GameObject discObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        discObj.name = "Unscrambler_RotaryDisc";
        discObj.transform.SetParent(unscramblerRoot.transform);
        discObj.transform.localPosition = new Vector3(0f, 0.89f, 0f);
        discObj.transform.localScale = new Vector3(0.75f, 0.010f, 0.75f);
        discObj.GetComponent<Renderer>().sharedMaterial = rotaryDiscSteel;

        // Center Stainless Cone / Cap on disc
        GameObject discCone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        discCone.name = "Unscrambler_CenterCap";
        discCone.transform.SetParent(discObj.transform);
        discCone.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        discCone.transform.localScale = new Vector3(0.24f, 0.8f, 0.24f);
        discCone.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Attach idle rotation component to the rotary disc
        InfeedRotator rotator = discObj.AddComponent<InfeedRotator>();
        rotator.rotationSpeed = 15.0f;
        rotator.rotationAxis = Vector3.up;

        // Stationary Outer Funnel Guide Arm (deflects bottles onto infeed conveyor)
        GameObject funnelGuide = GameObject.CreatePrimitive(PrimitiveType.Cube);
        funnelGuide.name = "Unscrambler_FunnelGuide";
        funnelGuide.transform.SetParent(unscramblerRoot.transform);
        funnelGuide.transform.localPosition = new Vector3(-0.16f, 0.94f, 0.32f);
        funnelGuide.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
        funnelGuide.transform.localScale = new Vector3(0.015f, 0.06f, 0.38f);
        funnelGuide.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Outer Deflector Rim Bracket
        GameObject guideBracket = GameObject.CreatePrimitive(PrimitiveType.Cube);
        guideBracket.name = "Unscrambler_GuideBracket";
        guideBracket.transform.SetParent(unscramblerRoot.transform);
        guideBracket.transform.localPosition = new Vector3(-0.32f, 0.90f, 0.15f);
        guideBracket.transform.localScale = new Vector3(0.12f, 0.03f, 0.03f);
        guideBracket.GetComponent<Renderer>().sharedMaterial = frameSteel;

        // =========================================================================
        // 2. SHORT INDEPENDENT INFEED CONVEYOR SEGMENT
        // Spans Z = -2.70m to -1.50m (Length 1.20m, Center Z = -2.10m).
        // Belt top at Y = 0.90m, matching main conveyor height.
        // =========================================================================
        GameObject infeedConveyorRoot = new GameObject("Infeed_Conveyor_Segment");
        infeedConveyorRoot.transform.SetParent(infeedObj.transform);
        infeedConveyorRoot.transform.localPosition = new Vector3(0f, 0f, -2.10f);

        // Dark rubber flat belt (400mm wide x 1.20m long, top surface Y = 0.90m)
        GameObject infeedBelt = GameObject.CreatePrimitive(PrimitiveType.Cube);
        infeedBelt.name = "Infeed_Belt_DarkRubber";
        infeedBelt.transform.SetParent(infeedConveyorRoot.transform);
        infeedBelt.transform.localPosition = new Vector3(0f, 0.89f, 0f);
        infeedBelt.transform.localScale = new Vector3(0.40f, 0.02f, 1.20f);
        infeedBelt.GetComponent<Renderer>().sharedMaterial = darkRubberMat;

        // Welded Box Frame Side Beams (80mm tall x 40mm wide x 1.20m long)
        GameObject infeedBeamLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        infeedBeamLeft.name = "Infeed_SideBeam_Left_80x40mm";
        infeedBeamLeft.transform.SetParent(infeedConveyorRoot.transform);
        infeedBeamLeft.transform.localPosition = new Vector3(-0.22f, 0.88f, 0f);
        infeedBeamLeft.transform.localScale = new Vector3(0.04f, 0.08f, 1.20f);
        infeedBeamLeft.GetComponent<Renderer>().sharedMaterial = frameSteel;

        GameObject infeedBeamRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        infeedBeamRight.name = "Infeed_SideBeam_Right_80x40mm";
        infeedBeamRight.transform.SetParent(infeedConveyorRoot.transform);
        infeedBeamRight.transform.localPosition = new Vector3(0.22f, 0.88f, 0f);
        infeedBeamRight.transform.localScale = new Vector3(0.04f, 0.08f, 1.20f);
        infeedBeamRight.GetComponent<Renderer>().sharedMaterial = frameSteel;

        // End Beams
        GameObject infeedEndUp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        infeedEndUp.name = "Infeed_EndBeam_Upstream";
        infeedEndUp.transform.SetParent(infeedConveyorRoot.transform);
        infeedEndUp.transform.localPosition = new Vector3(0f, 0.88f, -0.58f);
        infeedEndUp.transform.localScale = new Vector3(0.40f, 0.08f, 0.04f);
        infeedEndUp.GetComponent<Renderer>().sharedMaterial = frameSteel;

        GameObject infeedEndDown = GameObject.CreatePrimitive(PrimitiveType.Cube);
        infeedEndDown.name = "Infeed_EndBeam_Downstream";
        infeedEndDown.transform.SetParent(infeedConveyorRoot.transform);
        infeedEndDown.transform.localPosition = new Vector3(0f, 0.88f, 0.58f);
        infeedEndDown.transform.localScale = new Vector3(0.40f, 0.08f, 0.04f);
        infeedEndDown.GetComponent<Renderer>().sharedMaterial = frameSteel;

        // Legs & Foot Plates (2 pairs: Z = -0.40m and +0.40m relative to infeed center)
        float[] legX = { -0.20f, 0.20f };
        float[] legZ = { -0.40f, 0.40f };
        float legH = 0.83f;

        for (int i = 0; i < legX.Length; i++)
        {
            for (int j = 0; j < legZ.Length; j++)
            {
                string legSide = legX[i] < 0 ? "Left" : "Right";
                string legEnd = legZ[j] < 0 ? "Upstream" : "Downstream";

                // Foot Baseplate (120x120x10mm)
                GameObject foot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                foot.name = $"Infeed_Foot_{legSide}_{legEnd}_120x120mm";
                foot.transform.SetParent(infeedConveyorRoot.transform);
                foot.transform.localPosition = new Vector3(legX[i], 0.005f, legZ[j]);
                foot.transform.localScale = new Vector3(0.12f, 0.01f, 0.12f);
                foot.GetComponent<Renderer>().sharedMaterial = frameSteel;

                // Square Leg Column (60x60mm)
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = $"Infeed_Leg_{legSide}_{legEnd}_60x60mm";
                leg.transform.SetParent(infeedConveyorRoot.transform);
                leg.transform.localPosition = new Vector3(legX[i], 0.01f + legH * 0.5f, legZ[j]);
                leg.transform.localScale = new Vector3(0.06f, legH, 0.06f);
                leg.GetComponent<Renderer>().sharedMaterial = frameSteel;
            }
        }

        // Cross-bracing at Y = 0.30m
        for (int j = 0; j < legZ.Length; j++)
        {
            string legEnd = legZ[j] < 0 ? "Upstream" : "Downstream";
            GameObject crossBrace = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossBrace.name = $"Infeed_CrossBrace_{legEnd}_300mm";
            crossBrace.transform.SetParent(infeedConveyorRoot.transform);
            crossBrace.transform.localPosition = new Vector3(0f, 0.30f, legZ[j]);
            crossBrace.transform.localScale = new Vector3(0.34f, 0.04f, 0.04f);
            crossBrace.GetComponent<Renderer>().sharedMaterial = frameSteel;
        }

        // Side Guide Rails along Infeed Conveyor (visual continuity)
        GameObject infeedRailL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        infeedRailL.name = "Infeed_GuideRail_Left_20mm";
        infeedRailL.transform.SetParent(infeedConveyorRoot.transform);
        infeedRailL.transform.localPosition = new Vector3(-0.065f, 0.94f, 0f);
        infeedRailL.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        infeedRailL.transform.localScale = new Vector3(0.02f, 0.60f, 0.02f);
        infeedRailL.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        GameObject infeedRailR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        infeedRailR.name = "Infeed_GuideRail_Right_20mm";
        infeedRailR.transform.SetParent(infeedConveyorRoot.transform);
        infeedRailR.transform.localPosition = new Vector3(0.065f, 0.94f, 0f);
        infeedRailR.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        infeedRailR.transform.localScale = new Vector3(0.02f, 0.60f, 0.02f);
        infeedRailR.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // =========================================================================
        // 3. STAR WHEEL (INDEXING DISC WITH POCKETS)
        // Positioned along the infeed conveyor at X = -0.15m, Y = 0.93m, Z = -2.35m.
        // Approximated with a stainless disc and radial pocket notches.
        // =========================================================================
        GameObject starWheelRoot = new GameObject("Star_Wheel_Assembly");
        starWheelRoot.transform.SetParent(infeedObj.transform);
        starWheelRoot.transform.localPosition = new Vector3(-0.15f, 0.92f, -2.35f);

        // Vertical Spindle / Shaft
        GameObject starSpindle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        starSpindle.name = "StarWheel_Spindle";
        starSpindle.transform.SetParent(starWheelRoot.transform);
        starSpindle.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        starSpindle.transform.localScale = new Vector3(0.025f, 0.035f, 0.025f);
        starSpindle.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Central Indexing Disc (Diameter 0.20m, Thickness 0.012m)
        GameObject starDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        starDisc.name = "StarWheel_Disc";
        starDisc.transform.SetParent(starWheelRoot.transform);
        starDisc.transform.localPosition = new Vector3(0f, 0.015f, 0f);
        starDisc.transform.localScale = new Vector3(0.20f, 0.006f, 0.20f);
        starDisc.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // 6 Radial Teeth / Pocket Lugs around rim (radius ~0.085m)
        for (int t = 0; t < 6; t++)
        {
            float angle = t * 60f * Mathf.Deg2Rad;
            float tx = Mathf.Cos(angle) * 0.085f;
            float tz = Mathf.Sin(angle) * 0.085f;

            GameObject tooth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tooth.name = $"StarWheel_Tooth_{t}";
            tooth.transform.SetParent(starDisc.transform);
            tooth.transform.localPosition = new Vector3(tx / 0.20f, 0f, tz / 0.20f);
            tooth.transform.localRotation = Quaternion.Euler(0f, -t * 60f, 0f);
            tooth.transform.localScale = new Vector3(0.16f, 1.20f, 0.16f);
            tooth.GetComponent<Renderer>().sharedMaterial = stainlessMat;
        }

        // Star Wheel Frame Mounting Bracket (attached to side frame)
        GameObject starBracket = GameObject.CreatePrimitive(PrimitiveType.Cube);
        starBracket.name = "StarWheel_MountBracket";
        starBracket.transform.SetParent(starWheelRoot.transform);
        starBracket.transform.localPosition = new Vector3(-0.04f, -0.02f, 0f);
        starBracket.transform.localScale = new Vector3(0.08f, 0.025f, 0.04f);
        starBracket.GetComponent<Renderer>().sharedMaterial = frameSteel;

        // =========================================================================
        // 4. BOTTLE PRESENT SENSOR (PHOTOELECTRIC SENSOR)
        // Mounted on bracket beside infeed conveyor at X = -0.24m, Y = 0.95m, Z = -1.90m.
        // Includes optical barrel, status LED, and TextMeshPro nameplate label.
        // =========================================================================
        GameObject sensorRoot = new GameObject("Bottle_Present_Sensor");
        sensorRoot.transform.SetParent(infeedObj.transform);
        sensorRoot.transform.localPosition = new Vector3(-0.24f, 0.95f, -1.90f);

        // Mounting Bracket on Conveyor Side Beam
        GameObject sensorBracket = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sensorBracket.name = "Sensor_MountBracket";
        sensorBracket.transform.SetParent(sensorRoot.transform);
        sensorBracket.transform.localPosition = new Vector3(0.015f, -0.03f, 0f);
        sensorBracket.transform.localScale = new Vector3(0.035f, 0.070f, 0.030f);
        sensorBracket.GetComponent<Renderer>().sharedMaterial = frameSteel;

        // Photoelectric Sensor Housing (Dark rectangular enclosure)
        GameObject sensorBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sensorBody.name = "Sensor_Housing";
        sensorBody.transform.SetParent(sensorRoot.transform);
        sensorBody.transform.localPosition = new Vector3(-0.015f, 0.01f, 0f);
        sensorBody.transform.localScale = new Vector3(0.035f, 0.050f, 0.035f);
        sensorBody.GetComponent<Renderer>().sharedMaterial = deviceDark;

        // Optical Lens Barrel (pointing across conveyor belt +X)
        GameObject sensorLens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        sensorLens.name = "Sensor_OpticLens";
        sensorLens.transform.SetParent(sensorBody.transform);
        sensorLens.transform.localPosition = new Vector3(0.55f, 0.1f, 0f);
        sensorLens.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        sensorLens.transform.localScale = new Vector3(0.35f, 0.20f, 0.35f);
        sensorLens.GetComponent<Renderer>().sharedMaterial = opticalRed;

        // Sensor Active Status LED Indicator
        GameObject sensorLed = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sensorLed.name = "Sensor_StatusLED";
        sensorLed.transform.SetParent(sensorBody.transform);
        sensorLed.transform.localPosition = new Vector3(0f, 0.52f, 0f);
        sensorLed.transform.localScale = new Vector3(0.30f, 0.10f, 0.30f);
        Renderer sLedRend = sensorLed.GetComponent<Renderer>();
        sLedRend.sharedMaterial = ledGreen;
        sLedRend.shadowCastingMode = ShadowCastingMode.Off;
        sLedRend.receiveShadows = false;

        // Sensor Nameplate Backing Plate
        GameObject sensorPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sensorPlate.name = "Sensor_Nameplate_Backing";
        sensorPlate.transform.SetParent(sensorRoot.transform);
        sensorPlate.transform.localPosition = new Vector3(-0.022f, 0.075f, 0f);
        sensorPlate.transform.localRotation = Quaternion.Euler(15f, -90f, 0f);
        sensorPlate.transform.localScale = new Vector3(0.18f, 0.045f, 0.005f);
        sensorPlate.GetComponent<Renderer>().sharedMaterial = deviceDark;

        // TextMeshPro Nameplate Label: "BOTTLE PRESENT SENSOR"
        CreateWorldLabel(sensorPlate.transform, "Label_BottlePresentSensor",
            new Vector3(0f, 0f, 0.55f), Vector3.zero,
            "<b><color=#00D8FF>BOTTLE PRESENT SENSOR</color></b>\n<size=75%><color=#AACCFF>PHOTOELECTRIC PE-01</color></size>",
            0.022f, Color.white, TextAlignmentOptions.Center, new Vector2(0.18f, 0.042f), fontAsset);

        // =========================================================================
        // 5. STOPPER CYLINDER (PNEUMATIC BOTTLE GATING CYLINDER)
        // Positioned at X = 0.24m, Y = 0.95m, Z = -2.10m across infeed conveyor.
        // Features cylinder barrel, rod, stopper gate blade, and TextMeshPro label.
        // =========================================================================
        GameObject stopperRoot = new GameObject("Stopper_Cylinder");
        stopperRoot.transform.SetParent(infeedObj.transform);
        stopperRoot.transform.localPosition = new Vector3(0.24f, 0.95f, -2.10f);

        // Mounting Bracket on Conveyor Side Beam
        GameObject stopperBracket = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stopperBracket.name = "Stopper_MountBracket";
        stopperBracket.transform.SetParent(stopperRoot.transform);
        stopperBracket.transform.localPosition = new Vector3(-0.015f, -0.03f, 0f);
        stopperBracket.transform.localScale = new Vector3(0.035f, 0.070f, 0.040f);
        stopperBracket.GetComponent<Renderer>().sharedMaterial = frameSteel;

        // Pneumatic Cylinder Main Body (Frame/Barrel)
        GameObject stopperBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stopperBody.name = "Stopper_CylinderBody";
        stopperBody.transform.SetParent(stopperRoot.transform);
        stopperBody.transform.localPosition = new Vector3(0.025f, 0.01f, 0f);
        stopperBody.transform.localScale = new Vector3(0.055f, 0.042f, 0.042f);
        stopperBody.GetComponent<Renderer>().sharedMaterial = cylinderBodySteel;

        // Cylinder Rear End Cap
        GameObject stopperEndCap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stopperEndCap.name = "Stopper_CylinderEndCap";
        stopperEndCap.transform.SetParent(stopperRoot.transform);
        stopperEndCap.transform.localPosition = new Vector3(0.058f, 0.01f, 0f);
        stopperEndCap.transform.localScale = new Vector3(0.015f, 0.046f, 0.046f);
        stopperEndCap.GetComponent<Renderer>().sharedMaterial = basePlinthSteel;

        // Stainless Piston Rod (extending inward towards belt)
        GameObject stopperRod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stopperRod.name = "Stopper_PistonRod";
        stopperRod.transform.SetParent(stopperRoot.transform);
        stopperRod.transform.localPosition = new Vector3(-0.030f, 0.01f, 0f);
        stopperRod.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        stopperRod.transform.localScale = new Vector3(0.014f, 0.030f, 0.014f);
        stopperRod.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Stopper Gate Blade (blocking/gating position across belt edge)
        GameObject stopperBlade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stopperBlade.name = "Stopper_Blade";
        stopperBlade.transform.SetParent(stopperRoot.transform);
        stopperBlade.transform.localPosition = new Vector3(-0.075f, 0.01f, 0f);
        stopperBlade.transform.localScale = new Vector3(0.014f, 0.065f, 0.040f);
        stopperBlade.GetComponent<Renderer>().sharedMaterial = frameSteel;

        // Stopper Nameplate Backing Plate
        GameObject stopperPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stopperPlate.name = "Stopper_Nameplate_Backing";
        stopperPlate.transform.SetParent(stopperRoot.transform);
        stopperPlate.transform.localPosition = new Vector3(0.030f, 0.075f, 0f);
        stopperPlate.transform.localRotation = Quaternion.Euler(15f, 90f, 0f);
        stopperPlate.transform.localScale = new Vector3(0.18f, 0.045f, 0.005f);
        stopperPlate.GetComponent<Renderer>().sharedMaterial = deviceDark;

        // TextMeshPro Nameplate Label: "STOPPER CYLINDER"
        CreateWorldLabel(stopperPlate.transform, "Label_StopperCylinder",
            new Vector3(0f, 0f, 0.55f), Vector3.zero,
            "<b><color=#00D8FF>STOPPER CYLINDER</color></b>\n<size=75%><color=#AACCFF>PNEUMATIC CYL-01</color></size>",
            0.022f, Color.white, TextAlignmentOptions.Center, new Vector2(0.18f, 0.042f), fontAsset);

        // =========================================================================
        // CLEANUP & SELECTION
        // Remove primitive colliders (props are visual-only context)
        // =========================================================================
        Collider[] colliders = infeedObj.GetComponentsInChildren<Collider>();
        for (int c = 0; c < colliders.Length; c++)
        {
            Undo.DestroyObjectImmediate(colliders[c]);
        }

        EditorUtility.SetDirty(root);
        Selection.activeGameObject = infeedObj;
        Debug.Log("[InfeedZoneBuilder] Infeed Zone visual props (Bottle Unscrambler with idle rotator, Infeed Conveyor segment, Star Wheel, Bottle Present Sensor, Stopper Cylinder) added successfully under 'Cell'.");
    }

    private static TextMeshPro CreateWorldLabel(Transform parent, string name, Vector3 localPos, Vector3 localRot,
        string text, float fontSize, Color color, TextAlignmentOptions align, Vector2 size, TMP_FontAsset font)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(localRot);

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        if (font != null)
        {
            tmp.font = font;
        }
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        tmp.rectTransform.sizeDelta = size;

        // Explicitly set ShadowCastingMode.Off on TMP mesh renderers to avoid GPU Resident Drawer SHADOWCASTER pass registration
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        return tmp;
    }
}
