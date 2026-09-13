using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;

public static class DeltaControlPropsBuilder
{
    private const string RootName = "Cell";
    private const string CabinetRootName = "DeltaControlCabinet";

    [MenuItem("Tools/Delta/Add Control Props")]
    public static void AddControlProps()
    {
        // 1. Locate root Cell
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = GameObject.Find("IndustrialCell_Delta");
        }

        if (root == null)
        {
            Debug.LogError($"[DeltaControlPropsBuilder] Scene root '{RootName}' not found! Run Tools/Delta/Build Cell first.");
            return;
        }

        // 2. Delete existing DeltaControlCabinet child if present
        Transform existingCabinet = root.transform.Find(CabinetRootName);
        if (existingCabinet != null)
        {
            Undo.DestroyObjectImmediate(existingCabinet.gameObject);
        }

        // 3. Remove/hide any stale bottle cap on static scene bottle if present
        GameObject sceneBottle = GameObject.Find("Bottle_500ml_PET");
        if (sceneBottle != null)
        {
            Transform cap = sceneBottle.transform.Find("Bottle_Cap_Blue");
            if (cap == null) cap = sceneBottle.transform.Find("Bottle_Cap");
            if (cap != null)
            {
                Undo.DestroyObjectImmediate(cap.gameObject);
            }
        }

        // 4. Materials
        Material frameSteel = DeltaMaterials.PaintedSteel(new Color(0.32f, 0.33f, 0.35f, 1f));
        Material backpanelMat = DeltaMaterials.PaintedSteel(new Color(0.72f, 0.74f, 0.76f, 1f));
        Material plinthMat = DeltaMaterials.PaintedSteel(new Color(0.18f, 0.19f, 0.20f, 1f));
        Material stainlessMat = DeltaMaterials.BrushedStainless();
        Material ductMat = DeltaMaterials.PaintedSteel(new Color(0.48f, 0.50f, 0.53f, 1f));

        Material deviceDark = DeltaMaterials.PaintedSteel(new Color(0.14f, 0.15f, 0.16f, 1f));
        Material deviceCharcoal = DeltaMaterials.PaintedSteel(new Color(0.18f, 0.19f, 0.21f, 1f));
        Material terminalGreen = DeltaMaterials.PaintedSteel(new Color(0.12f, 0.38f, 0.22f, 1f));
        Material psuMetal = DeltaMaterials.PaintedSteel(new Color(0.62f, 0.64f, 0.67f, 1f));

        // Emissive LED materials (ShadowCastingMode.Off ensures GPU Resident Drawer SHADOWCASTER pass ignores them)
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        Material ledGreen = new Material(urpLit) { name = "M_LED_Green" };
        ledGreen.SetColor("_BaseColor", new Color(0.2f, 1.0f, 0.3f, 1f));
        ledGreen.EnableKeyword("_EMISSION");
        ledGreen.SetColor("_EmissionColor", new Color(0.1f, 1.0f, 0.2f) * 3.5f);

        Material ledRed = new Material(urpLit) { name = "M_LED_Red" };
        ledRed.SetColor("_BaseColor", new Color(1.0f, 0.2f, 0.2f, 1f));
        ledRed.EnableKeyword("_EMISSION");
        ledRed.SetColor("_EmissionColor", new Color(1.0f, 0.1f, 0.1f) * 3.5f);

        Material ledAmber = new Material(urpLit) { name = "M_LED_Amber" };
        ledAmber.SetColor("_BaseColor", new Color(1.0f, 0.7f, 0.1f, 1f));
        ledAmber.EnableKeyword("_EMISSION");
        ledAmber.SetColor("_EmissionColor", new Color(1.0f, 0.6f, 0.1f) * 3.0f);

        Material screenBlue = new Material(urpLit) { name = "M_HMI_Screen" };
        screenBlue.SetColor("_BaseColor", new Color(0.04f, 0.10f, 0.22f, 1f));
        screenBlue.EnableKeyword("_EMISSION");
        screenBlue.SetColor("_EmissionColor", new Color(0.05f, 0.25f, 0.60f) * 1.5f);

        TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (fontAsset == null)
        {
            fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }

        // 5. Cabinet Root GameObject
        // Placed next to the line at X = -1.05, Z = 0.15 where CellCam_Hero sees it without blocking the conveyor
        GameObject cabinetObj = new GameObject(CabinetRootName);
        cabinetObj.transform.SetParent(root.transform);
        cabinetObj.transform.position = new Vector3(-1.05f, 0.90f, 0.15f);
        cabinetObj.transform.rotation = Quaternion.Euler(0f, 70f, 0f);

        // =========================================================================
        // CABINET STRUCTURE (1.0 W x 1.8 H x 0.45 D m)
        // =========================================================================

        // Plinth / Base (flush on floor at Y = 0 to 0.10m, local Y = -0.85m)
        GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plinth.name = "Cabinet_Plinth_Base";
        plinth.transform.SetParent(cabinetObj.transform);
        plinth.transform.localPosition = new Vector3(0f, -0.85f, 0f);
        plinth.transform.localScale = new Vector3(1.00f, 0.10f, 0.45f);
        plinth.GetComponent<Renderer>().sharedMaterial = plinthMat;

        // Back Wall
        GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backWall.name = "Cabinet_BackWall";
        backWall.transform.SetParent(cabinetObj.transform);
        backWall.transform.localPosition = new Vector3(0f, 0.05f, -0.215f);
        backWall.transform.localScale = new Vector3(1.00f, 1.70f, 0.02f);
        backWall.GetComponent<Renderer>().sharedMaterial = frameSteel;

        // Roof / Top
        GameObject topRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topRoof.name = "Cabinet_TopRoof";
        topRoof.transform.SetParent(cabinetObj.transform);
        topRoof.transform.localPosition = new Vector3(0f, 0.89f, 0f);
        topRoof.transform.localScale = new Vector3(1.00f, 0.02f, 0.45f);
        topRoof.GetComponent<Renderer>().sharedMaterial = frameSteel;

        // Left Side Wall
        GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWall.name = "Cabinet_SideWall_Left";
        leftWall.transform.SetParent(cabinetObj.transform);
        leftWall.transform.localPosition = new Vector3(-0.49f, 0.05f, 0f);
        leftWall.transform.localScale = new Vector3(0.02f, 1.70f, 0.45f);
        leftWall.GetComponent<Renderer>().sharedMaterial = frameSteel;

        // Right Side Wall
        GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWall.name = "Cabinet_SideWall_Right";
        rightWall.transform.SetParent(cabinetObj.transform);
        rightWall.transform.localPosition = new Vector3(0.49f, 0.05f, 0f);
        rightWall.transform.localScale = new Vector3(0.02f, 1.70f, 0.45f);
        rightWall.GetComponent<Renderer>().sharedMaterial = frameSteel;

        // Internal Mounting Subpanel (Galvanized steel backplate)
        GameObject subpanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        subpanel.name = "Cabinet_MountingSubpanel";
        subpanel.transform.SetParent(cabinetObj.transform);
        subpanel.transform.localPosition = new Vector3(0f, 0.05f, -0.195f);
        subpanel.transform.localScale = new Vector3(0.92f, 1.55f, 0.015f);
        subpanel.GetComponent<Renderer>().sharedMaterial = backpanelMat;

        // Cabinet Top Banner Label
        CreateWorldLabel(cabinetObj.transform, "Label_CabinetHeader",
            new Vector3(0f, 0.84f, 0.15f), Vector3.zero,
            "<b><color=#00D8FF>DELTA</color> INDUSTRIAL AUTOMATION</b> | RECIPE CONTROL SYSTEM",
            0.045f, Color.white, TextAlignmentOptions.Center, new Vector2(0.95f, 0.06f), fontAsset);

        // =========================================================================
        // DIN RAILS & WIREWAY DUCTS
        // =========================================================================

        // Upper DIN Rail (Row 1: PLC + PSU + 2x Servos)
        GameObject dinUpper = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dinUpper.name = "DIN_Rail_Upper_35mm";
        dinUpper.transform.SetParent(cabinetObj.transform);
        dinUpper.transform.localPosition = new Vector3(0f, 0.32f, -0.180f);
        dinUpper.transform.localScale = new Vector3(0.86f, 0.035f, 0.010f);
        dinUpper.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Lower DIN Rail (Row 2: MS300 VFD below)
        GameObject dinLower = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dinLower.name = "DIN_Rail_Lower_35mm";
        dinLower.transform.SetParent(cabinetObj.transform);
        dinLower.transform.localPosition = new Vector3(0f, -0.05f, -0.180f);
        dinLower.transform.localScale = new Vector3(0.86f, 0.035f, 0.010f);
        dinLower.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Horizontal Wireway Ducts (Slotted PVC cable trunking)
        float[] ductY = { 0.48f, 0.14f, -0.32f };
        for (int d = 0; d < ductY.Length; d++)
        {
            GameObject duct = GameObject.CreatePrimitive(PrimitiveType.Cube);
            duct.name = $"Wireway_Duct_H_{d}";
            duct.transform.SetParent(cabinetObj.transform);
            duct.transform.localPosition = new Vector3(0f, ductY[d], -0.165f);
            duct.transform.localScale = new Vector3(0.86f, 0.045f, 0.035f);
            duct.GetComponent<Renderer>().sharedMaterial = ductMat;
        }

        // =========================================================================
        // ROW 1: PLC + PSU + SERVO DRIVES (IN ONE ROW ACROSS UPPER DIN RAIL)
        // =========================================================================

        // 1. CliQ-M 24V PSU: 0.040 W x 0.124 H x 0.117 D m
        GameObject psu = GameObject.CreatePrimitive(PrimitiveType.Cube);
        psu.name = "Delta_CliQ_M_PSU";
        psu.transform.SetParent(cabinetObj.transform);
        psu.transform.localPosition = new Vector3(-0.30f, 0.32f, -0.1215f);
        psu.transform.localScale = new Vector3(0.040f, 0.124f, 0.117f);
        psu.GetComponent<Renderer>().sharedMaterial = psuMetal;

        // PSU DC OK LED (Emissive indicator - ShadowCastingMode.Off)
        GameObject psuLed = GameObject.CreatePrimitive(PrimitiveType.Cube);
        psuLed.name = "LED_DC_OK";
        psuLed.transform.SetParent(psu.transform);
        psuLed.transform.localPosition = new Vector3(0f, 0.30f, 0.51f);
        psuLed.transform.localScale = new Vector3(0.18f, 0.08f, 0.05f);
        Renderer psuLedRend = psuLed.GetComponent<Renderer>();
        psuLedRend.sharedMaterial = ledGreen;
        psuLedRend.shadowCastingMode = ShadowCastingMode.Off;
        psuLedRend.receiveShadows = false;

        CreateWorldLabel(cabinetObj.transform, "Label_CliQ_M",
            new Vector3(-0.30f, 0.41f, -0.05f), Vector3.zero,
            "<b>CliQ-M</b>\n<color=#AACCFF>24 VDC</color>",
            0.034f, Color.white, TextAlignmentOptions.Center, new Vector2(0.10f, 0.05f), fontAsset);

        // 2. AS320T-B PLC: 0.088 W x 0.088 H x 0.095 D m
        GameObject plc = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plc.name = "Delta_AS320T_B_PLC";
        plc.transform.SetParent(cabinetObj.transform);
        plc.transform.localPosition = new Vector3(-0.16f, 0.32f, -0.1325f);
        plc.transform.localScale = new Vector3(0.088f, 0.088f, 0.095f);
        plc.GetComponent<Renderer>().sharedMaterial = deviceCharcoal;

        // PLC Terminals: Top and Bottom Rows
        GameObject plcTermTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plcTermTop.name = "PLC_Terminals_Top";
        plcTermTop.transform.SetParent(plc.transform);
        plcTermTop.transform.localPosition = new Vector3(0f, 0.40f, 0.20f);
        plcTermTop.transform.localScale = new Vector3(0.92f, 0.18f, 0.40f);
        plcTermTop.GetComponent<Renderer>().sharedMaterial = terminalGreen;

        GameObject plcTermBot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plcTermBot.name = "PLC_Terminals_Bottom";
        plcTermBot.transform.SetParent(plc.transform);
        plcTermBot.transform.localPosition = new Vector3(0f, -0.40f, 0.20f);
        plcTermBot.transform.localScale = new Vector3(0.92f, 0.18f, 0.40f);
        plcTermBot.GetComponent<Renderer>().sharedMaterial = terminalGreen;

        // PLC Central LEDs: RUN (green), ERR (red), BAT (amber), COM (green)
        Material[] plcLedMats = { ledGreen, ledRed, ledAmber, ledGreen };
        string[] plcLedNames = { "LED_RUN", "LED_ERR", "LED_BAT", "LED_COM" };
        for (int l = 0; l < 4; l++)
        {
            GameObject led = GameObject.CreatePrimitive(PrimitiveType.Cube);
            led.name = plcLedNames[l];
            led.transform.SetParent(plc.transform);
            led.transform.localPosition = new Vector3(-0.30f + l * 0.20f, 0.05f, 0.51f);
            led.transform.localScale = new Vector3(0.12f, 0.08f, 0.05f);
            Renderer ledRend = led.GetComponent<Renderer>();
            ledRend.sharedMaterial = plcLedMats[l];
            ledRend.shadowCastingMode = ShadowCastingMode.Off;
            ledRend.receiveShadows = false;
        }

        CreateWorldLabel(cabinetObj.transform, "Label_AS320T",
            new Vector3(-0.16f, 0.41f, -0.06f), Vector3.zero,
            "<b>AS320T-B</b>\n<color=#AACCFF>PLC</color>",
            0.034f, Color.white, TextAlignmentOptions.Center, new Vector2(0.12f, 0.05f), fontAsset);

        // 3. ASD-A3 AC Servo Drive X (GUIDE RAIL): 0.040 W x 0.150 H x 0.163 D m
        GameObject servoX = GameObject.CreatePrimitive(PrimitiveType.Cube);
        servoX.name = "Delta_ASD_A3_X_Rail";
        servoX.transform.SetParent(cabinetObj.transform);
        servoX.transform.localPosition = new Vector3(-0.02f, 0.32f, -0.0985f);
        servoX.transform.localScale = new Vector3(0.040f, 0.150f, 0.163f);
        servoX.GetComponent<Renderer>().sharedMaterial = deviceDark;

        // Servo X 5-digit Green LED Display (Emissive indicator)
        GameObject servoXDisp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        servoXDisp.name = "ServoX_Display";
        servoXDisp.transform.SetParent(servoX.transform);
        servoXDisp.transform.localPosition = new Vector3(0f, 0.30f, 0.51f);
        servoXDisp.transform.localScale = new Vector3(0.75f, 0.15f, 0.05f);
        Renderer sxRend = servoXDisp.GetComponent<Renderer>();
        sxRend.sharedMaterial = ledGreen;
        sxRend.shadowCastingMode = ShadowCastingMode.Off;
        sxRend.receiveShadows = false;

        CreateWorldLabel(cabinetObj.transform, "Label_ServoX",
            new Vector3(-0.02f, 0.43f, -0.01f), Vector3.zero,
            "<b>ASD-A3</b>\n<color=#00FF66>X — RAIL</color>",
            0.030f, Color.white, TextAlignmentOptions.Center, new Vector2(0.12f, 0.05f), fontAsset);

        // 4. ASD-A3 AC Servo Drive Z (NOZZLE): 0.040 W x 0.150 H x 0.163 D m
        GameObject servoZ = GameObject.CreatePrimitive(PrimitiveType.Cube);
        servoZ.name = "Delta_ASD_A3_Z_Nozzle";
        servoZ.transform.SetParent(cabinetObj.transform);
        servoZ.transform.localPosition = new Vector3(0.08f, 0.32f, -0.0985f);
        servoZ.transform.localScale = new Vector3(0.040f, 0.150f, 0.163f);
        servoZ.GetComponent<Renderer>().sharedMaterial = deviceDark;

        // Servo Z 5-digit Green LED Display (Emissive indicator)
        GameObject servoZDisp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        servoZDisp.name = "ServoZ_Display";
        servoZDisp.transform.SetParent(servoZ.transform);
        servoZDisp.transform.localPosition = new Vector3(0f, 0.30f, 0.51f);
        servoZDisp.transform.localScale = new Vector3(0.75f, 0.15f, 0.05f);
        Renderer szRend = servoZDisp.GetComponent<Renderer>();
        szRend.sharedMaterial = ledGreen;
        szRend.shadowCastingMode = ShadowCastingMode.Off;
        szRend.receiveShadows = false;

        CreateWorldLabel(cabinetObj.transform, "Label_ServoZ",
            new Vector3(0.08f, 0.43f, -0.01f), Vector3.zero,
            "<b>ASD-A3</b>\n<color=#00FF66>Z — NOZZLE</color>",
            0.030f, Color.white, TextAlignmentOptions.Center, new Vector2(0.14f, 0.05f), fontAsset);

        // =========================================================================
        // ROW 2: VFD BELOW (MOUNTED DIRECTLY UNDER PLC ON LOWER DIN RAIL)
        // =========================================================================

        // 5. MS300 VFD Frame A: 0.068 W x 0.128 H x 0.110 D m
        GameObject vfd = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vfd.name = "Delta_MS300_VFD";
        vfd.transform.SetParent(cabinetObj.transform);
        vfd.transform.localPosition = new Vector3(-0.16f, -0.05f, -0.125f);
        vfd.transform.localScale = new Vector3(0.068f, 0.128f, 0.110f);
        vfd.GetComponent<Renderer>().sharedMaterial = deviceDark;

        // VFD Red 4-digit LED Display
        GameObject vfdDisplay = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vfdDisplay.name = "VFD_Keypad_Display";
        vfdDisplay.transform.SetParent(vfd.transform);
        vfdDisplay.transform.localPosition = new Vector3(0f, 0.28f, 0.51f);
        vfdDisplay.transform.localScale = new Vector3(0.70f, 0.22f, 0.05f);
        Renderer vfdDispRend = vfdDisplay.GetComponent<Renderer>();
        vfdDispRend.sharedMaterial = ledRed;
        vfdDispRend.shadowCastingMode = ShadowCastingMode.Off;
        vfdDispRend.receiveShadows = false;

        // VFD Potentiometer Knob
        GameObject vfdKnob = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        vfdKnob.name = "VFD_Speed_Knob";
        vfdKnob.transform.SetParent(vfd.transform);
        vfdKnob.transform.localPosition = new Vector3(0f, -0.05f, 0.53f);
        vfdKnob.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        vfdKnob.transform.localScale = new Vector3(0.24f, 0.08f, 0.24f);
        vfdKnob.GetComponent<Renderer>().sharedMaterial = plinthMat;

        CreateWorldLabel(cabinetObj.transform, "Label_MS300",
            new Vector3(-0.16f, 0.04f, -0.05f), Vector3.zero,
            "<b>MS300</b>\n<color=#FFCC00>CONVEYOR</color>",
            0.032f, Color.white, TextAlignmentOptions.Center, new Vector2(0.12f, 0.05f), fontAsset);

        // Lower Terminal Blocks along Row 2
        GameObject termStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        termStrip.name = "Terminal_Blocks_Distribution";
        termStrip.transform.SetParent(cabinetObj.transform);
        termStrip.transform.localPosition = new Vector3(0.04f, -0.05f, -0.165f);
        termStrip.transform.localScale = new Vector3(0.22f, 0.060f, 0.040f);
        termStrip.GetComponent<Renderer>().sharedMaterial = terminalGreen;

        // =========================================================================
        // OPEN CABINET DOOR WITH HMI (DOP-100WS) ON THE FRONT DOOR
        // Real external dimensions: 0.137 W x 0.103 H x 0.037 D m (cutout 119 x 93 mm)
        // Door is hinged on the left and swung open into aisle at local -130 degrees (world 300 degrees), clearing all device sightlines
        // =========================================================================

        GameObject doorHinge = new GameObject("Cabinet_Door_Hinge");
        doorHinge.transform.SetParent(cabinetObj.transform);
        doorHinge.transform.localPosition = new Vector3(-0.48f, 0.05f, 0.220f);
        doorHinge.transform.localRotation = Quaternion.Euler(0f, -130f, 0f);

        // Door Panel Slab (0.76 W x 1.65 H x 0.025 D m)
        GameObject doorPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorPanel.name = "Cabinet_Door_Panel";
        doorPanel.transform.SetParent(doorHinge.transform);
        doorPanel.transform.localPosition = new Vector3(0.38f, 0f, 0f);
        doorPanel.transform.localScale = new Vector3(0.76f, 1.65f, 0.025f);
        doorPanel.GetComponent<Renderer>().sharedMaterial = frameSteel;

        // Door Perimeter Bevel Trim / Handle
        GameObject doorHandle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorHandle.name = "Cabinet_Door_Handle";
        doorHandle.transform.SetParent(doorHinge.transform);
        doorHandle.transform.localPosition = new Vector3(0.72f, 0f, 0.025f);
        doorHandle.transform.localScale = new Vector3(0.03f, 0.22f, 0.035f);
        doorHandle.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // DOP-100WS HMI Enclosure / Bezel mounted on the door panel at eye level
        GameObject hmiBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hmiBody.name = "Delta_DOP_100WS_HMI";
        hmiBody.transform.SetParent(doorHinge.transform);
        hmiBody.transform.localPosition = new Vector3(0.40f, 0.30f, 0.015f);
        hmiBody.transform.localScale = new Vector3(0.137f, 0.103f, 0.037f);
        hmiBody.GetComponent<Renderer>().sharedMaterial = deviceDark;

        // HMI Active Screen Plane (0.125 x 0.085 m)
        GameObject hmiScreen = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hmiScreen.name = "HMI_ActiveScreen";
        hmiScreen.transform.SetParent(hmiBody.transform);
        hmiScreen.transform.localPosition = new Vector3(0f, 0.04f, 0.51f);
        hmiScreen.transform.localScale = new Vector3(0.91f, 0.82f, 0.02f);
        Renderer screenRend = hmiScreen.GetComponent<Renderer>();
        screenRend.sharedMaterial = screenBlue;
        screenRend.shadowCastingMode = ShadowCastingMode.Off;
        screenRend.receiveShadows = false;

        // Lower Bezel Delta Label
        CreateWorldLabel(hmiBody.transform, "Label_DeltaLogo",
            new Vector3(0f, -0.42f, 0.52f), Vector3.zero,
            "<size=120%><b><color=#00D8FF>DELTA</color></b></size>  DOP-100WS",
            0.022f, Color.white, TextAlignmentOptions.Center, new Vector2(0.13f, 0.025f), fontAsset);

        // Runtime HMI Display TextMeshPro
        GameObject hmiTextObj = new GameObject("HMI_Display_TextMeshPro");
        hmiTextObj.transform.SetParent(hmiScreen.transform);
        hmiTextObj.transform.localPosition = new Vector3(0f, 0f, 0.55f);
        hmiTextObj.transform.localRotation = Quaternion.identity;

        TextMeshPro hmiTmp = hmiTextObj.AddComponent<TextMeshPro>();
        if (fontAsset != null)
        {
            hmiTmp.font = fontAsset;
        }
        hmiTmp.fontSize = 0.050f;
        hmiTmp.alignment = TextAlignmentOptions.TopLeft;
        hmiTmp.color = Color.white;
        hmiTmp.rectTransform.sizeDelta = new Vector2(0.122f, 0.082f);
        MeshRenderer hmiRend = hmiTextObj.GetComponent<MeshRenderer>();
        if (hmiRend != null)
        {
            hmiRend.shadowCastingMode = ShadowCastingMode.Off;
            hmiRend.receiveShadows = false;
        }

        // Attach and wire DeltaHMIDisplay runtime script
        DeltaHMIDisplay hmiDisplay = hmiScreen.AddComponent<DeltaHMIDisplay>();
        hmiDisplay.textMesh = hmiTmp;
        hmiDisplay.sequencer = root.GetComponent<ChangeoverSequencer>();

        // =========================================================================
        // CLOSE-UP CAMERA (ControlCam_Close)
        // Clean three-quarter hero framing of the open cabinet door showing the
        // labeled devices, with the HMI screen prominent and clearly readable.
        // =========================================================================
        // Robust cleanup: destroy ANY existing ControlCam_Close across the scene to guarantee no duplicates
        Camera[] existingCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (int i = 0; i < existingCams.Length; i++)
        {
            if (existingCams[i] != null && existingCams[i].gameObject.name == "ControlCam_Close")
            {
                Undo.DestroyObjectImmediate(existingCams[i].gameObject);
            }
        }
        Transform existingCamChild = root.transform.Find("ControlCam_Close");
        if (existingCamChild != null)
        {
            Undo.DestroyObjectImmediate(existingCamChild.gameObject);
        }

        GameObject camObj = new GameObject("ControlCam_Close");
        camObj.transform.SetParent(root.transform);

        // Confirmed live camera framing (tested and verified in Unity editor):
        // Position sits higher and closer, looking across into the open door at the DIN devices
        Vector3 camWorldPos = new Vector3(0.15f, 1.85f, 1.35f);
        Vector3 targetWorldPos = new Vector3(-1.05f, 1.10f, 0.35f);

        camObj.transform.position = camWorldPos;
        camObj.transform.rotation = Quaternion.LookRotation((targetWorldPos - camWorldPos).normalized, Vector3.up);

        Camera cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 40f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 50f;
        cam.depth = 101f; // Above Wide (99) and Hero (100)
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.06f, 1f);

        UniversalAdditionalCameraData camData = camObj.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;

        // Clean up colliders on all visual primitive props
        Collider[] colliders = cabinetObj.GetComponentsInChildren<Collider>();
        for (int c = 0; c < colliders.Length; c++)
        {
            Undo.DestroyObjectImmediate(colliders[c]);
        }

        EditorUtility.SetDirty(root);
        Selection.activeGameObject = cabinetObj;
        Debug.Log("[DeltaControlPropsBuilder] DeltaControlCabinet, HMI on open door, panel layout (Row 1: PLC+PSU+Servos, Row 2: VFD), and reframed ControlCam_Close added successfully under 'Cell'.");
    }

    [MenuItem("Tools/Delta/Attach Camera Director")]
    public static void AttachCameraDirector()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = GameObject.Find("IndustrialCell_Delta");
        }

        if (root == null)
        {
            Debug.LogError($"[DeltaControlPropsBuilder] Scene root '{RootName}' not found! Run Tools/Delta/Build Cell first.");
            return;
        }

        Color charcoalBg = new Color(0.05f, 0.05f, 0.06f, 1f);

        // Ensure RailTopCam exists
        GameObject railTopObj = GameObject.Find("RailTopCam");
        if (railTopObj == null)
        {
            railTopObj = new GameObject("RailTopCam");
            railTopObj.transform.SetParent(root.transform);
            railTopObj.transform.position = new Vector3(0.0f, 2.35f, -0.15f);
            railTopObj.transform.LookAt(new Vector3(0f, 0.96f, 0.15f));

            Camera cam = railTopObj.AddComponent<Camera>();
            cam.fieldOfView = 48f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 50f;
            cam.depth = 90f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = charcoalBg;
            cam.enabled = false;

            UniversalAdditionalCameraData data = railTopObj.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            Undo.RegisterCreatedObjectUndo(railTopObj, "Create RailTopCam");
        }

        // Ensure NozzleSideCam exists
        GameObject nozzleSideObj = GameObject.Find("NozzleSideCam");
        if (nozzleSideObj == null)
        {
            nozzleSideObj = new GameObject("NozzleSideCam");
            nozzleSideObj.transform.SetParent(root.transform);
            nozzleSideObj.transform.position = new Vector3(1.10f, 1.25f, 0.55f);
            nozzleSideObj.transform.LookAt(new Vector3(0f, 1.18f, 0.60f));

            Camera cam = nozzleSideObj.AddComponent<Camera>();
            cam.fieldOfView = 36f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 50f;
            cam.depth = 91f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = charcoalBg;
            cam.enabled = false;

            UniversalAdditionalCameraData data = nozzleSideObj.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            Undo.RegisterCreatedObjectUndo(nozzleSideObj, "Create NozzleSideCam");
        }

        CameraDirector director = root.GetComponent<CameraDirector>();
        if (director == null)
        {
            director = Undo.AddComponent<CameraDirector>(root);
        }

        // Wire sequencer reference
        ChangeoverSequencer sequencer = root.GetComponent<ChangeoverSequencer>();
        if (sequencer == null)
        {
            sequencer = Object.FindAnyObjectByType<ChangeoverSequencer>();
        }
        director.sequencer = sequencer;

        // Wire camera references by scene name
        GameObject heroObj = GameObject.Find("CellCam_Hero");
        GameObject wideObj = GameObject.Find("CellCam_Wide");
        GameObject controlObj = GameObject.Find("ControlCam_Close");

        if (heroObj != null) director.heroCam = heroObj.GetComponent<Camera>();
        if (wideObj != null) director.wideCam = wideObj.GetComponent<Camera>();
        if (controlObj != null) director.controlCamClose = controlObj.GetComponent<Camera>();
        if (railTopObj != null) director.railTopCam = railTopObj.GetComponent<Camera>();
        if (nozzleSideObj != null) director.nozzleSideCam = nozzleSideObj.GetComponent<Camera>();

        director.ValidateAndCacheReferences();
        EditorUtility.SetDirty(root);
        Debug.Log($"[DeltaControlPropsBuilder] CameraDirector attached to '{RootName}' and camera references wired.");
    }

    [MenuItem("Tools/Delta/Build Full Cell")]
    public static void BuildFullCell()
    {
        // 1. Build base cell machine, room, lighting, and cameras
        CellBuilder.BuildCell();

        // 2. Add industrial control cabinet, Delta devices, HMI display, and close-up camera
        AddControlProps();

        // 3. Attach changeover sequencer and wire scene references
        CellBuilder.AttachSequencer();

        // 4. Attach camera director and wire camera references
        AttachCameraDirector();

        // 5. Re-render reflection probe so brushed steel catches cabinet reflections
        GameObject probeObj = GameObject.Find("ReflectionProbe_Cell");
        if (probeObj != null)
        {
            ReflectionProbe probe = probeObj.GetComponent<ReflectionProbe>();
            if (probe != null)
            {
                probe.RenderProbe();
            }
        }

        Debug.Log("[DeltaControlPropsBuilder] Build Full Cell completed: Verified Cell + Control Props + Sequencer + Camera Director + Reflection Probe baked.");
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
