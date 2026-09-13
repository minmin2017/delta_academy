using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;

/// <summary>
/// FillingZonePropsBuilder - Additive, idempotent Editor builder for the Filling Zone visual props.
/// Builds Product Tank, Product Pump, Flow Meter, Anti-Drip Valve, and continuous sanitary Pipe Run
/// connecting into the real existing NozzleAssembly_Z on the conveyor.
/// Follows the exact additive pattern, naming conventions, and material palette from DeltaMaterials and DeltaControlPropsBuilder.
/// </summary>
public static class FillingZonePropsBuilder
{
    private const string RootName = "Cell";
    private const string FillingZoneRootName = "FillingZoneProps";

    [MenuItem("Tools/Delta/Add Filling Zone Props")]
    public static void AddFillingZoneProps()
    {
        // 1. Locate root Cell
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = GameObject.Find("IndustrialCell_Delta");
        }

        if (root == null)
        {
            Debug.LogError($"[FillingZonePropsBuilder] Scene root '{RootName}' not found! Run Tools/Delta/Build Cell first.");
            return;
        }

        // 2. Delete existing FillingZoneProps child if present (idempotent rebuild pattern)
        Transform existingProps = root.transform.Find(FillingZoneRootName);
        if (existingProps != null)
        {
            Undo.DestroyObjectImmediate(existingProps.gameObject);
        }

        // 3. Materials - Reused directly from DeltaMaterials palette
        Material frameSteel = DeltaMaterials.PaintedSteel(new Color(0.38f, 0.39f, 0.41f, 1f));
        Material darkSteel = DeltaMaterials.PaintedSteel(new Color(0.22f, 0.24f, 0.26f, 1f));
        Material baseplateSteel = DeltaMaterials.PaintedSteel(new Color(0.25f, 0.27f, 0.29f, 1f));
        Material tankSteel = DeltaMaterials.PaintedSteel(new Color(0.35f, 0.37f, 0.40f, 1f));
        Material transmitterSteel = DeltaMaterials.PaintedSteel(new Color(0.18f, 0.19f, 0.21f, 1f));
        Material actuatorCapSteel = DeltaMaterials.PaintedSteel(new Color(0.15f, 0.45f, 0.70f, 1f));
        Material fanAccentSteel = DeltaMaterials.PaintedSteel(new Color(0.15f, 0.50f, 0.80f, 1f));
        Material stainlessMat = DeltaMaterials.BrushedStainless();
        Material petMat = DeltaMaterials.ClearPET();
        Material liquidMat = DeltaMaterials.Liquid(new Color(0.95f, 0.85f, 0.45f, 1f));

        // Emissive LED / Display materials (ShadowCastingMode.Off ensures GPU Resident Drawer SHADOWCASTER pass ignores them)
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        Material ledGreen = new Material(urpLit) { name = "M_LED_Green_FZ" };
        ledGreen.SetColor("_BaseColor", new Color(0.2f, 1.0f, 0.3f, 1f));
        ledGreen.EnableKeyword("_EMISSION");
        ledGreen.SetColor("_EmissionColor", new Color(0.1f, 1.0f, 0.2f) * 3.5f);

        Material screenBlue = new Material(urpLit) { name = "M_Screen_Blue_FZ" };
        screenBlue.SetColor("_BaseColor", new Color(0.04f, 0.10f, 0.22f, 1f));
        screenBlue.EnableKeyword("_EMISSION");
        screenBlue.SetColor("_EmissionColor", new Color(0.05f, 0.25f, 0.60f) * 1.5f);

        TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (fontAsset == null)
        {
            fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }

        // 4. Create Filling Zone Root GameObject under Cell
        GameObject fillingZoneObj = new GameObject(FillingZoneRootName);
        fillingZoneObj.transform.SetParent(root.transform);
        fillingZoneObj.transform.localPosition = Vector3.zero;
        fillingZoneObj.transform.localRotation = Quaternion.identity;

        // =========================================================================
        // 1. PRODUCT TANK (FillingZone_ProductTank)
        // Positioned at X = -0.70m, Z = 0.60m (upstream/behind existing nozzle at Z = 0.60m)
        // Main cylinder: 0.50m diameter x 0.70m tall, elevated on 4 square support legs
        // =========================================================================
        GameObject tankRoot = new GameObject("FillingZone_ProductTank");
        tankRoot.transform.SetParent(fillingZoneObj.transform);
        tankRoot.transform.position = new Vector3(-0.70f, 0f, 0.60f);

        // 4 Support Legs (40x40mm) with 80x80mm Baseplate Feet & Cross-Bracing
        float[] legOffsetsX = { -0.18f, 0.18f };
        float[] legOffsetsZ = { -0.18f, 0.18f };
        float legHeight = 0.95f;

        for (int i = 0; i < legOffsetsX.Length; i++)
        {
            for (int j = 0; j < legOffsetsZ.Length; j++)
            {
                string sideX = legOffsetsX[i] < 0 ? "Left" : "Right";
                string sideZ = legOffsetsZ[j] < 0 ? "Upstream" : "Downstream";

                // Baseplate foot
                GameObject foot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                foot.name = $"Tank_Foot_{sideX}_{sideZ}_80x80mm";
                foot.transform.SetParent(tankRoot.transform);
                foot.transform.localPosition = new Vector3(legOffsetsX[i], 0.005f, legOffsetsZ[j]);
                foot.transform.localScale = new Vector3(0.08f, 0.01f, 0.08f);
                foot.GetComponent<Renderer>().sharedMaterial = frameSteel;

                // Square leg column
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = $"Tank_Leg_{sideX}_{sideZ}_40x40mm";
                leg.transform.SetParent(tankRoot.transform);
                leg.transform.localPosition = new Vector3(legOffsetsX[i], 0.01f + legHeight * 0.5f, legOffsetsZ[j]);
                leg.transform.localScale = new Vector3(0.04f, legHeight, 0.04f);
                leg.GetComponent<Renderer>().sharedMaterial = frameSteel;
            }
        }

        // Horizontal cross-bracing between tank legs at Y = 0.30m
        GameObject braceUpstream = GameObject.CreatePrimitive(PrimitiveType.Cube);
        braceUpstream.name = "Tank_CrossBrace_Upstream_300mm";
        braceUpstream.transform.SetParent(tankRoot.transform);
        braceUpstream.transform.localPosition = new Vector3(0f, 0.30f, -0.18f);
        braceUpstream.transform.localScale = new Vector3(0.32f, 0.03f, 0.03f);
        braceUpstream.GetComponent<Renderer>().sharedMaterial = frameSteel;

        GameObject braceDownstream = GameObject.CreatePrimitive(PrimitiveType.Cube);
        braceDownstream.name = "Tank_CrossBrace_Downstream_300mm";
        braceDownstream.transform.SetParent(tankRoot.transform);
        braceDownstream.transform.localPosition = new Vector3(0f, 0.30f, 0.18f);
        braceDownstream.transform.localScale = new Vector3(0.32f, 0.03f, 0.03f);
        braceDownstream.GetComponent<Renderer>().sharedMaterial = frameSteel;

        GameObject braceLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        braceLeft.name = "Tank_CrossBrace_Left_300mm";
        braceLeft.transform.SetParent(tankRoot.transform);
        braceLeft.transform.localPosition = new Vector3(-0.18f, 0.30f, 0f);
        braceLeft.transform.localScale = new Vector3(0.03f, 0.03f, 0.32f);
        braceLeft.GetComponent<Renderer>().sharedMaterial = frameSteel;

        GameObject braceRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        braceRight.name = "Tank_CrossBrace_Right_300mm";
        braceRight.transform.SetParent(tankRoot.transform);
        braceRight.transform.localPosition = new Vector3(0.18f, 0.30f, 0f);
        braceRight.transform.localScale = new Vector3(0.03f, 0.03f, 0.32f);
        braceRight.GetComponent<Renderer>().sharedMaterial = frameSteel;

        // Tank Bottom Conical / Dish Funnel (0.30m diameter x 0.12m tall)
        GameObject tankBottom = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tankBottom.name = "Tank_BottomFunnel_Cone";
        tankBottom.transform.SetParent(tankRoot.transform);
        tankBottom.transform.localPosition = new Vector3(0f, 0.93f, 0f);
        tankBottom.transform.localScale = new Vector3(0.30f, 0.06f, 0.30f);
        tankBottom.GetComponent<Renderer>().sharedMaterial = tankSteel;

        // Bottom Outlet Step Reducer
        GameObject tankOutlet = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tankOutlet.name = "Tank_BottomOutlet_Reducer";
        tankOutlet.transform.SetParent(tankRoot.transform);
        tankOutlet.transform.localPosition = new Vector3(0f, 0.86f, 0f);
        tankOutlet.transform.localScale = new Vector3(0.10f, 0.02f, 0.10f);
        tankOutlet.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Main Cylindrical Tank Body (0.50m diameter x 0.70m tall, Y: 0.99m to 1.69m)
        GameObject tankBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tankBody.name = "Tank_Body_Cylinder_500x700mm";
        tankBody.transform.SetParent(tankRoot.transform);
        tankBody.transform.localPosition = new Vector3(0f, 1.34f, 0f);
        tankBody.transform.localScale = new Vector3(0.50f, 0.35f, 0.50f);
        tankBody.GetComponent<Renderer>().sharedMaterial = tankSteel;

        // Top Rim Flange / Lid (0.52m diameter x 0.03m tall)
        GameObject topLid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        topLid.name = "Tank_TopLid_Flange";
        topLid.transform.SetParent(tankRoot.transform);
        topLid.transform.localPosition = new Vector3(0f, 1.70f, 0f);
        topLid.transform.localScale = new Vector3(0.52f, 0.015f, 0.52f);
        topLid.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Top Inspection Manhole Port (0.18m diameter x 0.05m tall)
        GameObject manhole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        manhole.name = "Tank_Manhole_Port";
        manhole.transform.SetParent(tankRoot.transform);
        manhole.transform.localPosition = new Vector3(0f, 1.74f, 0f);
        manhole.transform.localScale = new Vector3(0.18f, 0.025f, 0.18f);
        manhole.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Top Breather / Vent Filter
        GameObject ventFilter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ventFilter.name = "Tank_Vent_Filter";
        ventFilter.transform.SetParent(tankRoot.transform);
        ventFilter.transform.localPosition = new Vector3(0.14f, 1.76f, 0.08f);
        ventFilter.transform.localScale = new Vector3(0.06f, 0.035f, 0.06f);
        ventFilter.GetComponent<Renderer>().sharedMaterial = darkSteel;

        // External Sight Level Gauge (Visible level tube on front +X face)
        GameObject gaugeTopFitting = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        gaugeTopFitting.name = "Tank_SightGauge_TopFitting";
        gaugeTopFitting.transform.SetParent(tankRoot.transform);
        gaugeTopFitting.transform.localPosition = new Vector3(0.24f, 1.59f, 0f);
        gaugeTopFitting.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        gaugeTopFitting.transform.localScale = new Vector3(0.022f, 0.025f, 0.022f);
        gaugeTopFitting.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        GameObject gaugeBottomFitting = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        gaugeBottomFitting.name = "Tank_SightGauge_BottomFitting";
        gaugeBottomFitting.transform.SetParent(tankRoot.transform);
        gaugeBottomFitting.transform.localPosition = new Vector3(0.24f, 1.09f, 0f);
        gaugeBottomFitting.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        gaugeBottomFitting.transform.localScale = new Vector3(0.022f, 0.025f, 0.022f);
        gaugeBottomFitting.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        GameObject gaugeGlassTube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        gaugeGlassTube.name = "Tank_SightGauge_GlassTube";
        gaugeGlassTube.transform.SetParent(tankRoot.transform);
        gaugeGlassTube.transform.localPosition = new Vector3(0.26f, 1.34f, 0f);
        gaugeGlassTube.transform.localScale = new Vector3(0.018f, 0.25f, 0.018f);
        gaugeGlassTube.GetComponent<Renderer>().sharedMaterial = petMat;

        GameObject gaugeLiquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        gaugeLiquid.name = "Tank_SightGauge_LiquidLevel";
        gaugeLiquid.transform.SetParent(tankRoot.transform);
        gaugeLiquid.transform.localPosition = new Vector3(0.26f, 1.25f, 0f);
        gaugeLiquid.transform.localScale = new Vector3(0.014f, 0.16f, 0.014f);
        gaugeLiquid.GetComponent<Renderer>().sharedMaterial = liquidMat;

        // Product Tank TMP Nameplate Label (Facing front camera view at -Z)
        CreateWorldLabel(tankRoot.transform, "Label_ProductTank",
            new Vector3(0f, 1.45f, -0.26f), Vector3.zero,
            "<b><color=#00D8FF>DELTA</color> PRODUCT TANK</b>\n<size=75%>TK-101 | 150L SUPPLY</size>",
            0.038f, Color.white, TextAlignmentOptions.Center, new Vector2(0.35f, 0.08f), fontAsset);

        // =========================================================================
        // 2. PRODUCT PUMP (FillingZone_ProductPump)
        // Positioned at X = -0.45m, Y = 0.78m, Z = 0.60m (between tank base & conveyor)
        // Compact sanitary pump with electric drive motor, lobe pump head, mounting stand,
        // and a slowly rotating idle cooling fan / coupling disc
        // =========================================================================
        GameObject pumpRoot = new GameObject("FillingZone_ProductPump");
        pumpRoot.transform.SetParent(fillingZoneObj.transform);
        pumpRoot.transform.position = new Vector3(-0.45f, 0.78f, 0.60f);

        // Mounting Baseplate (0.26m x 0.18m x 0.02m)
        GameObject pumpBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pumpBase.name = "Pump_MountingBase";
        pumpBase.transform.SetParent(pumpRoot.transform);
        pumpBase.transform.localPosition = new Vector3(0f, -0.07f, 0f);
        pumpBase.transform.localScale = new Vector3(0.26f, 0.02f, 0.18f);
        pumpBase.GetComponent<Renderer>().sharedMaterial = baseplateSteel;

        // Stand Support Legs to Floor
        float[] pumpLegX = { -0.08f, 0.08f };
        for (int p = 0; p < pumpLegX.Length; p++)
        {
            GameObject pLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pLeg.name = $"Pump_StandLeg_{p}";
            pLeg.transform.SetParent(pumpRoot.transform);
            pLeg.transform.localPosition = new Vector3(pumpLegX[p], -0.425f, 0f);
            pLeg.transform.localScale = new Vector3(0.04f, 0.69f, 0.04f);
            pLeg.GetComponent<Renderer>().sharedMaterial = frameSteel;

            GameObject pFoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pFoot.name = $"Pump_StandFoot_{p}";
            pFoot.transform.SetParent(pumpRoot.transform);
            pFoot.transform.localPosition = new Vector3(pumpLegX[p], -0.775f, 0f);
            pFoot.transform.localScale = new Vector3(0.08f, 0.01f, 0.08f);
            pFoot.GetComponent<Renderer>().sharedMaterial = frameSteel;
        }

        // Electric Drive Motor Body (Cylinder along X axis, diameter 0.11m x length 0.12m)
        GameObject pumpMotor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pumpMotor.name = "Pump_Motor_Housing";
        pumpMotor.transform.SetParent(pumpRoot.transform);
        pumpMotor.transform.localPosition = new Vector3(-0.05f, 0f, 0f);
        pumpMotor.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        pumpMotor.transform.localScale = new Vector3(0.11f, 0.06f, 0.11f);
        pumpMotor.GetComponent<Renderer>().sharedMaterial = darkSteel;

        // Motor Terminal Box on top
        GameObject motorTerm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        motorTerm.name = "Pump_Motor_TerminalBox";
        motorTerm.transform.SetParent(pumpRoot.transform);
        motorTerm.transform.localPosition = new Vector3(-0.05f, 0.075f, 0f);
        motorTerm.transform.localScale = new Vector3(0.05f, 0.04f, 0.05f);
        motorTerm.GetComponent<Renderer>().sharedMaterial = darkSteel;

        // Sanitary Lobe Impeller Pump Head (Stainless steel housing at discharge end)
        GameObject pumpHead = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pumpHead.name = "Pump_ImpellerHead_Stainless";
        pumpHead.transform.SetParent(pumpRoot.transform);
        pumpHead.transform.localPosition = new Vector3(0.05f, 0f, 0f);
        pumpHead.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        pumpHead.transform.localScale = new Vector3(0.13f, 0.035f, 0.13f);
        pumpHead.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Suction Inlet Flange (Rear of pump head)
        GameObject pumpSuctionFlange = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pumpSuctionFlange.name = "Pump_SuctionFlange";
        pumpSuctionFlange.transform.SetParent(pumpRoot.transform);
        pumpSuctionFlange.transform.localPosition = new Vector3(-0.015f, 0f, 0f);
        pumpSuctionFlange.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        pumpSuctionFlange.transform.localScale = new Vector3(0.06f, 0.01f, 0.06f);
        pumpSuctionFlange.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Discharge Outlet Flange (Top of pump head)
        GameObject pumpDischargeFlange = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pumpDischargeFlange.name = "Pump_DischargeFlange";
        pumpDischargeFlange.transform.SetParent(pumpRoot.transform);
        pumpDischargeFlange.transform.localPosition = new Vector3(0.05f, 0.065f, 0f);
        pumpDischargeFlange.transform.localScale = new Vector3(0.05f, 0.01f, 0.05f);
        pumpDischargeFlange.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Idle Visual Interest: Rotating Cooling Fan / Coupling Disc
        GameObject fanRotatorObj = new GameObject("Pump_CouplingFan_Rotating");
        fanRotatorObj.transform.SetParent(pumpRoot.transform);
        fanRotatorObj.transform.localPosition = new Vector3(-0.12f, 0f, 0f);
        fanRotatorObj.transform.localRotation = Quaternion.identity;

        // Hub Disc
        GameObject fanHub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fanHub.name = "Fan_HubDisc";
        fanHub.transform.SetParent(fanRotatorObj.transform);
        fanHub.transform.localPosition = Vector3.zero;
        fanHub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        fanHub.transform.localScale = new Vector3(0.08f, 0.006f, 0.08f);
        fanHub.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // 4 Radial Vane Blades
        GameObject fanBladeV = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fanBladeV.name = "Fan_Vane_Vertical";
        fanBladeV.transform.SetParent(fanRotatorObj.transform);
        fanBladeV.transform.localPosition = Vector3.zero;
        fanBladeV.transform.localScale = new Vector3(0.008f, 0.070f, 0.010f);
        fanBladeV.GetComponent<Renderer>().sharedMaterial = fanAccentSteel;

        GameObject fanBladeH = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fanBladeH.name = "Fan_Vane_Horizontal";
        fanBladeH.transform.SetParent(fanRotatorObj.transform);
        fanBladeH.transform.localPosition = Vector3.zero;
        fanBladeH.transform.localScale = new Vector3(0.008f, 0.010f, 0.070f);
        fanBladeH.GetComponent<Renderer>().sharedMaterial = fanAccentSteel;

        // Attach reusable slow rotator component
        FillingPumpRotator rotator = fanRotatorObj.AddComponent<FillingPumpRotator>();
        rotator.rotationAxis = new Vector3(1f, 0f, 0f);
        rotator.rotationSpeed = 45f;

        // Product Pump TMP Nameplate Label
        CreateWorldLabel(pumpRoot.transform, "Label_ProductPump",
            new Vector3(0f, -0.07f, -0.10f), Vector3.zero,
            "<b>PRODUCT PUMP</b>\n<color=#FFCC00>P-101 | SANITARY LOBE</color>",
            0.028f, Color.white, TextAlignmentOptions.Center, new Vector2(0.24f, 0.05f), fontAsset);

        // =========================================================================
        // 3. FLOW METER (FillingZone_FlowMeter)
        // Positioned inline along the pipe at X = -0.22m, Y = 1.43m, Z = 0.60m
        // Cylindrical stainless sensor spool + transmitter neck & electronics head with display
        // =========================================================================
        GameObject flowMeterRoot = new GameObject("FillingZone_FlowMeter");
        flowMeterRoot.transform.SetParent(fillingZoneObj.transform);
        flowMeterRoot.transform.position = new Vector3(-0.22f, 1.43f, 0.60f);

        // Cylindrical Inline Sensor Body (Cylinder along X axis, diameter 0.065m x length 0.09m)
        GameObject fmSensor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fmSensor.name = "FlowMeter_SensorBody_Stainless";
        fmSensor.transform.SetParent(flowMeterRoot.transform);
        fmSensor.transform.localPosition = Vector3.zero;
        fmSensor.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        fmSensor.transform.localScale = new Vector3(0.065f, 0.045f, 0.065f);
        fmSensor.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Inlet Sanitary Flange (X = -0.045m local)
        GameObject fmInletFlange = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fmInletFlange.name = "FlowMeter_Flange_Inlet";
        fmInletFlange.transform.SetParent(flowMeterRoot.transform);
        fmInletFlange.transform.localPosition = new Vector3(-0.045f, 0f, 0f);
        fmInletFlange.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        fmInletFlange.transform.localScale = new Vector3(0.085f, 0.006f, 0.085f);
        fmInletFlange.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Outlet Sanitary Flange (X = +0.045m local)
        GameObject fmOutletFlange = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fmOutletFlange.name = "FlowMeter_Flange_Outlet";
        fmOutletFlange.transform.SetParent(flowMeterRoot.transform);
        fmOutletFlange.transform.localPosition = new Vector3(0.045f, 0f, 0f);
        fmOutletFlange.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        fmOutletFlange.transform.localScale = new Vector3(0.085f, 0.006f, 0.085f);
        fmOutletFlange.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Transmitter Mounting Neck (Vertical cylinder)
        GameObject fmNeck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fmNeck.name = "FlowMeter_Transmitter_Neck";
        fmNeck.transform.SetParent(flowMeterRoot.transform);
        fmNeck.transform.localPosition = new Vector3(0f, 0.055f, 0f);
        fmNeck.transform.localScale = new Vector3(0.025f, 0.025f, 0.025f);
        fmNeck.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Transmitter Electronics Enclosure (Charcoal industrial box)
        GameObject fmHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fmHead.name = "FlowMeter_Transmitter_Head";
        fmHead.transform.SetParent(flowMeterRoot.transform);
        fmHead.transform.localPosition = new Vector3(0f, 0.115f, 0f);
        fmHead.transform.localScale = new Vector3(0.070f, 0.070f, 0.060f);
        fmHead.GetComponent<Renderer>().sharedMaterial = transmitterSteel;

        // Transmitter Display Window (Emissive blue screen facing front)
        GameObject fmDisplay = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fmDisplay.name = "FlowMeter_Display_Window";
        fmDisplay.transform.SetParent(fmHead.transform);
        fmDisplay.transform.localPosition = new Vector3(0f, 0.05f, -0.51f);
        fmDisplay.transform.localScale = new Vector3(0.65f, 0.35f, 0.05f);
        Renderer fmDispRend = fmDisplay.GetComponent<Renderer>();
        fmDispRend.sharedMaterial = screenBlue;
        fmDispRend.shadowCastingMode = ShadowCastingMode.Off;
        fmDispRend.receiveShadows = false;

        // Flow Meter TextMeshPro Label (Nameplate format matching Delta device style)
        CreateWorldLabel(flowMeterRoot.transform, "Label_FlowMeter",
            new Vector3(0f, 0.170f, -0.035f), Vector3.zero,
            "<b>FLOW METER</b>\n<color=#00D8FF>FM-101</color>",
            0.028f, Color.white, TextAlignmentOptions.Center, new Vector2(0.14f, 0.05f), fontAsset);

        // =========================================================================
        // 4. ANTI-DRIP VALVE (FillingZone_AntiDripValve)
        // Positioned at X = -0.09m, Y = 1.43m, Z = 0.60m (immediately before nozzle valve block)
        // Horizontal valve body + vertical pneumatic actuator cylinder with top indicator cap
        // =========================================================================
        GameObject valveRoot = new GameObject("FillingZone_AntiDripValve");
        valveRoot.transform.SetParent(fillingZoneObj.transform);
        valveRoot.transform.position = new Vector3(-0.09f, 1.43f, 0.60f);

        // Valve Body Housing (Cylinder along X axis, diameter 0.050m x length 0.070m)
        GameObject valveBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        valveBody.name = "Valve_Body_Housing_Stainless";
        valveBody.transform.SetParent(valveRoot.transform);
        valveBody.transform.localPosition = Vector3.zero;
        valveBody.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        valveBody.transform.localScale = new Vector3(0.050f, 0.035f, 0.050f);
        valveBody.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Inlet Flange (X = -0.035m local)
        GameObject valveInletFlange = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        valveInletFlange.name = "Valve_Flange_Inlet";
        valveInletFlange.transform.SetParent(valveRoot.transform);
        valveInletFlange.transform.localPosition = new Vector3(-0.035f, 0f, 0f);
        valveInletFlange.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        valveInletFlange.transform.localScale = new Vector3(0.065f, 0.005f, 0.065f);
        valveInletFlange.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Outlet Connection Flange (X = +0.035m local)
        GameObject valveOutletFlange = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        valveOutletFlange.name = "Valve_Flange_Outlet";
        valveOutletFlange.transform.SetParent(valveRoot.transform);
        valveOutletFlange.transform.localPosition = new Vector3(0.035f, 0f, 0f);
        valveOutletFlange.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        valveOutletFlange.transform.localScale = new Vector3(0.065f, 0.005f, 0.065f);
        valveOutletFlange.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Perpendicular Pneumatic Actuator Cylinder (Vertical, diameter 0.042m x height 0.070m)
        GameObject valveActuator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        valveActuator.name = "Valve_Actuator_Cylinder";
        valveActuator.transform.SetParent(valveRoot.transform);
        valveActuator.transform.localPosition = new Vector3(0f, 0.065f, 0f);
        valveActuator.transform.localScale = new Vector3(0.042f, 0.035f, 0.042f);
        valveActuator.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Actuator Top Cap / Solenoid Housing (Delta blue accent)
        GameObject actuatorCap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        actuatorCap.name = "Valve_Actuator_TopCap";
        actuatorCap.transform.SetParent(valveRoot.transform);
        actuatorCap.transform.localPosition = new Vector3(0f, 0.105f, 0f);
        actuatorCap.transform.localScale = new Vector3(0.046f, 0.008f, 0.046f);
        actuatorCap.GetComponent<Renderer>().sharedMaterial = actuatorCapSteel;

        // Valve Position Indicator LED
        GameObject valveIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        valveIndicator.name = "Valve_Position_Indicator_LED";
        valveIndicator.transform.SetParent(valveRoot.transform);
        valveIndicator.transform.localPosition = new Vector3(0f, 0.120f, 0f);
        valveIndicator.transform.localScale = new Vector3(0.016f, 0.016f, 0.016f);
        Renderer vIndRend = valveIndicator.GetComponent<Renderer>();
        vIndRend.sharedMaterial = ledGreen;
        vIndRend.shadowCastingMode = ShadowCastingMode.Off;
        vIndRend.receiveShadows = false;

        // Anti-Drip Valve TextMeshPro Label (Nameplate format matching Delta device style)
        CreateWorldLabel(valveRoot.transform, "Label_AntiDripValve",
            new Vector3(0f, 0.155f, -0.025f), Vector3.zero,
            "<b>ANTI-DRIP VALVE</b>\n<color=#00FF66>ADV-101</color>",
            0.026f, Color.white, TextAlignmentOptions.Center, new Vector2(0.16f, 0.05f), fontAsset);

        // =========================================================================
        // 5. CONTINUOUS PIPE RUN (FillingZone_PipeRun)
        // Visually connects: Tank Bottom -> Pump -> Vertical Riser -> Flow Meter -> Anti-Drip Valve -> Real Nozzle
        // All pipe segments use BrushedStainless sanitary tubing (0.028m diameter)
        // Real Nozzle Valve Block is at X = 0.00m, Y = 1.43m, Z = 0.60m (spans X: -0.04 to +0.04)
        // =========================================================================
        GameObject pipeRoot = new GameObject("FillingZone_PipeRun");
        pipeRoot.transform.SetParent(fillingZoneObj.transform);
        pipeRoot.transform.localPosition = Vector3.zero;
        pipeRoot.transform.localRotation = Quaternion.identity;

        const float pipeDia = 0.028f;
        const float elbowDia = 0.034f;
        const float clampDia = 0.038f;
        const float clampThick = 0.006f;

        // 1. Tank Bottom Drop: from Y = 0.86m down to Y = 0.78m at X = -0.70m, Z = 0.60m
        GameObject pipeTankDrop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pipeTankDrop.name = "Pipe_Tank_BottomDrop";
        pipeTankDrop.transform.SetParent(pipeRoot.transform);
        pipeTankDrop.transform.position = new Vector3(-0.70f, 0.82f, 0.60f);
        pipeTankDrop.transform.localScale = new Vector3(pipeDia, 0.040f, pipeDia);
        pipeTankDrop.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Elbow Joint 1: Tank drop to horizontal run at (-0.70, 0.78, 0.60)
        GameObject elbow1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        elbow1.name = "Pipe_Elbow_Tank_To_Pump";
        elbow1.transform.SetParent(pipeRoot.transform);
        elbow1.transform.position = new Vector3(-0.70f, 0.78f, 0.60f);
        elbow1.transform.localScale = new Vector3(elbowDia, elbowDia, elbowDia);
        elbow1.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // 2. Tank to Pump Suction: from X = -0.70m to X = -0.50m at Y = 0.78m, Z = 0.60m
        GameObject pipeTankToPump = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pipeTankToPump.name = "Pipe_Tank_To_Pump_Horizontal";
        pipeTankToPump.transform.SetParent(pipeRoot.transform);
        pipeTankToPump.transform.position = new Vector3(-0.60f, 0.78f, 0.60f);
        pipeTankToPump.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        pipeTankToPump.transform.localScale = new Vector3(pipeDia, 0.100f, pipeDia);
        pipeTankToPump.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // 3. Pump Discharge to Riser: from X = -0.40m to X = -0.34m at Y = 0.845m, Z = 0.60m
        GameObject pipePumpToRiser = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pipePumpToRiser.name = "Pipe_Pump_To_Riser_Horizontal";
        pipePumpToRiser.transform.SetParent(pipeRoot.transform);
        pipePumpToRiser.transform.position = new Vector3(-0.37f, 0.845f, 0.60f);
        pipePumpToRiser.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        pipePumpToRiser.transform.localScale = new Vector3(pipeDia, 0.030f, pipeDia);
        pipePumpToRiser.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Elbow Joint 2: Pump discharge to vertical riser at (-0.34, 0.845, 0.60)
        GameObject elbow2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        elbow2.name = "Pipe_Elbow_Pump_To_Riser";
        elbow2.transform.SetParent(pipeRoot.transform);
        elbow2.transform.position = new Vector3(-0.34f, 0.845f, 0.60f);
        elbow2.transform.localScale = new Vector3(elbowDia, elbowDia, elbowDia);
        elbow2.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // 4. Vertical Riser Pipe: from Y = 0.845m to Y = 1.43m at X = -0.34m, Z = 0.60m (height 0.585m)
        GameObject pipeRiser = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pipeRiser.name = "Pipe_Pump_Riser_Vertical";
        pipeRiser.transform.SetParent(pipeRoot.transform);
        pipeRiser.transform.position = new Vector3(-0.34f, 1.1375f, 0.60f);
        pipeRiser.transform.localScale = new Vector3(pipeDia, 0.2925f, pipeDia);
        pipeRiser.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Elbow Joint 3: Vertical riser to horizontal bridge at (-0.34, 1.43, 0.60)
        GameObject elbow3 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        elbow3.name = "Pipe_Elbow_Riser_To_FlowMeter";
        elbow3.transform.SetParent(pipeRoot.transform);
        elbow3.transform.position = new Vector3(-0.34f, 1.43f, 0.60f);
        elbow3.transform.localScale = new Vector3(elbowDia, elbowDia, elbowDia);
        elbow3.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // 5. Riser to Flow Meter Inlet: from X = -0.34m to X = -0.265m at Y = 1.43m, Z = 0.60m (length 0.075m)
        GameObject pipeRiserToFM = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pipeRiserToFM.name = "Pipe_Riser_To_FlowMeter_Horizontal";
        pipeRiserToFM.transform.SetParent(pipeRoot.transform);
        pipeRiserToFM.transform.position = new Vector3(-0.3025f, 1.43f, 0.60f);
        pipeRiserToFM.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        pipeRiserToFM.transform.localScale = new Vector3(pipeDia, 0.0375f, pipeDia);
        pipeRiserToFM.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // 6. Flow Meter Outlet to Anti-Drip Valve: from X = -0.175m to X = -0.125m at Y = 1.43m, Z = 0.60m (length 0.050m)
        GameObject pipeFMToValve = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pipeFMToValve.name = "Pipe_FlowMeter_To_Valve_Horizontal";
        pipeFMToValve.transform.SetParent(pipeRoot.transform);
        pipeFMToValve.transform.position = new Vector3(-0.150f, 1.43f, 0.60f);
        pipeFMToValve.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        pipeFMToValve.transform.localScale = new Vector3(pipeDia, 0.025f, pipeDia);
        pipeFMToValve.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // 7. Anti-Drip Valve Outlet to Real Nozzle: from X = -0.055m to X = -0.040m (left face of Filling_ValveBlock_Stainless)
        GameObject pipeValveToNozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pipeValveToNozzle.name = "Pipe_Valve_To_Nozzle_Coupling";
        pipeValveToNozzle.transform.SetParent(pipeRoot.transform);
        pipeValveToNozzle.transform.position = new Vector3(-0.0475f, 1.43f, 0.60f);
        pipeValveToNozzle.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        pipeValveToNozzle.transform.localScale = new Vector3(pipeDia, 0.0075f, pipeDia);
        pipeValveToNozzle.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Sanitary Tri-Clamp Ferrules at pipe connection junctions
        Vector3[] clampPositions = {
            new Vector3(-0.70f, 0.855f, 0.60f), // Tank outlet clamp
            new Vector3(-0.51f, 0.78f, 0.60f),  // Pump suction clamp
            new Vector3(-0.34f, 1.00f, 0.60f),  // Riser mid clamp
            new Vector3(-0.265f, 1.43f, 0.60f), // Flow meter inlet clamp
            new Vector3(-0.175f, 1.43f, 0.60f), // Flow meter outlet clamp
            new Vector3(-0.125f, 1.43f, 0.60f), // Valve inlet clamp
            new Vector3(-0.040f, 1.43f, 0.60f)  // Nozzle valve block connection clamp
        };

        bool[] clampIsVertical = {
            true,  // Tank outlet (Y)
            false, // Pump suction (X)
            true,  // Riser (Y)
            false, // FM inlet (X)
            false, // FM outlet (X)
            false, // Valve inlet (X)
            false  // Nozzle block (X)
        };

        for (int c = 0; c < clampPositions.Length; c++)
        {
            GameObject clamp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            clamp.name = $"Pipe_SanitaryClamp_{c}";
            clamp.transform.SetParent(pipeRoot.transform);
            clamp.transform.position = clampPositions[c];
            if (!clampIsVertical[c])
            {
                clamp.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            }
            clamp.transform.localScale = new Vector3(clampDia, clampThick, clampDia);
            clamp.GetComponent<Renderer>().sharedMaterial = stainlessMat;
        }

        // =========================================================================
        // 6. CLEAN UP PRIMITIVE COLLIDERS
        // Remove box/capsule colliders generated by GameObject.CreatePrimitive
        // =========================================================================
        Collider[] colliders = fillingZoneObj.GetComponentsInChildren<Collider>();
        for (int c = 0; c < colliders.Length; c++)
        {
            Undo.DestroyObjectImmediate(colliders[c]);
        }

        EditorUtility.SetDirty(root);
        Selection.activeGameObject = fillingZoneObj;
        Debug.Log("[FillingZonePropsBuilder] Filling Zone Props (Product Tank, Pump, Flow Meter, Anti-Drip Valve, Continuous Pipe Run) added successfully under 'Cell'.");
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

/// <summary>
/// Simple reusable idle rotator script for small mechanical visual interest (e.g. pump fan/coupling disc).
/// </summary>
[ExecuteAlways]
public class FillingPumpRotator : MonoBehaviour
{
    [Tooltip("Rotation axis in local coordinates")]
    public Vector3 rotationAxis = new Vector3(1f, 0f, 0f);

    [Tooltip("Rotation speed in degrees per second")]
    public float rotationSpeed = 45f;

    private void Update()
    {
        if (Application.isPlaying)
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}
