using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using System.Globalization;

public static class CellBuilder
{
    private const string RootName = "Cell";

    [MenuItem("Tools/Delta/Build Cell")]
    public static void BuildCell()
    {
        // 0. Destroy EVERY root GameObject in the active scene before building
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] existingRoots = activeScene.GetRootGameObjects();
        for (int i = existingRoots.Length - 1; i >= 0; i--)
        {
            if (existingRoots[i] != null)
            {
                Undo.DestroyObjectImmediate(existingRoots[i]);
            }
        }

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Cell");

        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");

        // Materials
        Material frameMat = DeltaMaterials.PaintedSteel(new Color(0.38f, 0.39f, 0.41f, 1f));
        Material stainlessMat = DeltaMaterials.BrushedStainless();
        Material darkRubberMat = DeltaMaterials.DarkRubber();

        // Walls and ceiling material: albedo (0.17, 0.175, 0.19), metallic 0.15, smoothness 0.30
        Material wallMat = new Material(urpLitShader)
        {
            name = "M_Delta_Wall_Ceiling"
        };
        wallMat.SetColor("_BaseColor", new Color(0.17f, 0.175f, 0.19f, 1f));
        wallMat.SetFloat("_Metallic", 0.15f);
        wallMat.SetFloat("_Smoothness", 0.30f);

        // Tuned floor material: albedo (0.17, 0.17, 0.18), smoothness 0.22, metallic 0
        Material tunedFloorMat = new Material(urpLitShader)
        {
            name = "M_Delta_Floor_Tuned"
        };
        tunedFloorMat.SetColor("_BaseColor", new Color(0.17f, 0.17f, 0.18f, 1f));
        tunedFloorMat.SetFloat("_Smoothness", 0.22f);
        tunedFloorMat.SetFloat("_Metallic", 0.0f);

        // =========================================================================
        // 1. FACTORY FLOOR (80m x 80m Concrete)
        // =========================================================================
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor_Concrete_80x80m";
        floor.transform.SetParent(root.transform);
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(8.0f, 1.0f, 8.0f);
        floor.GetComponent<Renderer>().sharedMaterial = tunedFloorMat;

        // =========================================================================
        // 2. BACKDROP ROOM ENCLOSURE (20m x 20m, 9m tall with ceiling)
        // Walls 9m tall (center y = 4.5m) and ceiling at y = 9m
        // =========================================================================
        GameObject backdropRoot = new GameObject("Backdrop_Room_20x20m");
        backdropRoot.transform.SetParent(root.transform);

        // North Wall (+Z = +10m, 20m wide x 9m high)
        GameObject wallNorth = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallNorth.name = "Wall_North_PosZ";
        wallNorth.transform.SetParent(backdropRoot.transform);
        wallNorth.transform.position = new Vector3(0f, 4.5f, 10f);
        wallNorth.transform.localScale = new Vector3(20f, 9f, 0.10f);
        wallNorth.GetComponent<Renderer>().sharedMaterial = wallMat;

        // South Wall (-Z = -10m, 20m wide x 9m high)
        GameObject wallSouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallSouth.name = "Wall_South_NegZ";
        wallSouth.transform.SetParent(backdropRoot.transform);
        wallSouth.transform.position = new Vector3(0f, 4.5f, -10f);
        wallSouth.transform.localScale = new Vector3(20f, 9f, 0.10f);
        wallSouth.GetComponent<Renderer>().sharedMaterial = wallMat;

        // East Wall (+X = +10m, 20m long x 9m high)
        GameObject wallEast = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallEast.name = "Wall_East_PosX";
        wallEast.transform.SetParent(backdropRoot.transform);
        wallEast.transform.position = new Vector3(10f, 4.5f, 0f);
        wallEast.transform.localScale = new Vector3(0.10f, 9f, 20f);
        wallEast.GetComponent<Renderer>().sharedMaterial = wallMat;

        // West Wall (-X = -10m, 20m long x 9m high)
        GameObject wallWest = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallWest.name = "Wall_West_NegX";
        wallWest.transform.SetParent(backdropRoot.transform);
        wallWest.transform.position = new Vector3(-10f, 4.5f, 0f);
        wallWest.transform.localScale = new Vector3(0.10f, 9f, 20f);
        wallWest.GetComponent<Renderer>().sharedMaterial = wallMat;

        // Ceiling Plane at y = 9m (20m x 20m)
        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.name = "Ceiling_20x20m";
        ceiling.transform.SetParent(backdropRoot.transform);
        ceiling.transform.position = new Vector3(0f, 9.05f, 0f);
        ceiling.transform.localScale = new Vector3(20f, 0.10f, 20f);
        ceiling.GetComponent<Renderer>().sharedMaterial = wallMat;

        // =========================================================================
        // 3. CEILING LIGHT FIXTURES (2 x 3 Grid at x = +-3.5, z = -4, 0, +4)
        // Emissive material created ONCE and shared across all 6 panels
        // =========================================================================
        GameObject ceilingLightsRoot = new GameObject("CeilingLights");
        ceilingLightsRoot.transform.SetParent(root.transform);

        Material ceilEmissiveMat = new Material(urpLitShader)
        {
            name = "M_Ceiling_Emissive"
        };
        ceilEmissiveMat.SetColor("_BaseColor", new Color(0.95f, 0.96f, 1.00f, 1f));
        ceilEmissiveMat.SetFloat("_Smoothness", 0.10f);
        ceilEmissiveMat.EnableKeyword("_EMISSION");
        ceilEmissiveMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        ceilEmissiveMat.SetColor("_EmissionColor", new Color(1.0f, 0.98f, 0.94f) * 6.0f);

        float[] fixtureX = { -3.5f, 3.5f };
        float[] fixtureZ = { -4.0f, 0.0f, 4.0f };

        for (int ix = 0; ix < fixtureX.Length; ix++)
        {
            for (int iz = 0; iz < fixtureZ.Length; iz++)
            {
                float fx = fixtureX[ix];
                float fz = fixtureZ[iz];
                string suffix = $"{fx.ToString(CultureInfo.InvariantCulture)}_{fz.ToString(CultureInfo.InvariantCulture)}";

                // a) Emissive panel Cube at (x, 8.60, z), scale (1.2, 0.08, 3.0)
                GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                panel.name = $"CeilPanel_{suffix}";
                panel.transform.SetParent(ceilingLightsRoot.transform);
                panel.transform.position = new Vector3(fx, 8.60f, fz);
                panel.transform.localScale = new Vector3(1.2f, 0.08f, 3.0f);
                panel.GetComponent<Renderer>().sharedMaterial = ceilEmissiveMat;

                // b) Point light at (x, 8.40, z): intensity 55, range 18, color (1.00, 0.98, 0.94)
                GameObject lightObj = new GameObject($"CeilLight_{suffix}");
                lightObj.transform.SetParent(ceilingLightsRoot.transform);
                lightObj.transform.position = new Vector3(fx, 8.40f, fz);
                Light ptLight = lightObj.AddComponent<Light>();
                ptLight.type = LightType.Point;
                ptLight.intensity = 55f;
                ptLight.range = 18f;
                ptLight.color = new Color(1.00f, 0.98f, 0.94f);
                ptLight.shadows = LightShadows.None;
            }
        }

        // =========================================================================
        // 4. CONVEYOR ASSEMBLY
        // Belt runs along +Z. Belt top Y = 0.90m, width 0.40m, length 3.0m
        // =========================================================================
        GameObject conveyorRoot = new GameObject("Conveyor");
        conveyorRoot.transform.SetParent(root.transform);

        // Welded Box Frame Side Beams (80mm tall x 40mm wide x 3.0m long)
        GameObject beamLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beamLeft.name = "SideBeam_Left_80x40mm";
        beamLeft.transform.SetParent(conveyorRoot.transform);
        beamLeft.transform.position = new Vector3(-0.22f, 0.88f, 0f);
        beamLeft.transform.localScale = new Vector3(0.04f, 0.08f, 3.0f);
        beamLeft.GetComponent<Renderer>().sharedMaterial = frameMat;

        GameObject beamRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beamRight.name = "SideBeam_Right_80x40mm";
        beamRight.transform.SetParent(conveyorRoot.transform);
        beamRight.transform.position = new Vector3(0.22f, 0.88f, 0f);
        beamRight.transform.localScale = new Vector3(0.04f, 0.08f, 3.0f);
        beamRight.GetComponent<Renderer>().sharedMaterial = frameMat;

        // End Beams and Center Tie
        GameObject endBeamUpstream = GameObject.CreatePrimitive(PrimitiveType.Cube);
        endBeamUpstream.name = "EndBeam_Upstream";
        endBeamUpstream.transform.SetParent(conveyorRoot.transform);
        endBeamUpstream.transform.position = new Vector3(0f, 0.88f, -1.48f);
        endBeamUpstream.transform.localScale = new Vector3(0.40f, 0.08f, 0.04f);
        endBeamUpstream.GetComponent<Renderer>().sharedMaterial = frameMat;

        GameObject endBeamDownstream = GameObject.CreatePrimitive(PrimitiveType.Cube);
        endBeamDownstream.name = "EndBeam_Downstream";
        endBeamDownstream.transform.SetParent(conveyorRoot.transform);
        endBeamDownstream.transform.position = new Vector3(0f, 0.88f, 1.48f);
        endBeamDownstream.transform.localScale = new Vector3(0.40f, 0.08f, 0.04f);
        endBeamDownstream.GetComponent<Renderer>().sharedMaterial = frameMat;

        GameObject crossTieCenter = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crossTieCenter.name = "CrossTie_Center";
        crossTieCenter.transform.SetParent(conveyorRoot.transform);
        crossTieCenter.transform.position = new Vector3(0f, 0.86f, 0f);
        crossTieCenter.transform.localScale = new Vector3(0.40f, 0.04f, 0.04f);
        crossTieCenter.GetComponent<Renderer>().sharedMaterial = frameMat;

        // Belt (DarkRubber, 400mm wide, top at Y = 0.90m)
        GameObject belt = GameObject.CreatePrimitive(PrimitiveType.Cube);
        belt.name = "Belt_DarkRubber_400mm";
        belt.transform.SetParent(conveyorRoot.transform);
        belt.transform.position = new Vector3(0f, 0.89f, 0f);
        belt.transform.localScale = new Vector3(0.40f, 0.02f, 3.0f);
        belt.GetComponent<Renderer>().sharedMaterial = darkRubberMat;

        // 4 Square Legs (60x60mm) with 120x120mm Feet and Cross-Bracing at 300mm
        float[] legX = { -0.20f, 0.20f };
        float[] legZ = { -1.10f, 1.10f };
        float legH = 0.83f;

        for (int i = 0; i < legX.Length; i++)
        {
            for (int j = 0; j < legZ.Length; j++)
            {
                string legSide = legX[i] < 0 ? "Left" : "Right";
                string legEnd = legZ[j] < 0 ? "Upstream" : "Downstream";

                // Foot baseplate (120x120x10mm)
                GameObject foot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                foot.name = $"Foot_{legSide}_{legEnd}_120x120mm";
                foot.transform.SetParent(conveyorRoot.transform);
                foot.transform.position = new Vector3(legX[i], 0.005f, legZ[j]);
                foot.transform.localScale = new Vector3(0.12f, 0.01f, 0.12f);
                foot.GetComponent<Renderer>().sharedMaterial = frameMat;

                // Square leg column (60x60mm)
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = $"Leg_{legSide}_{legEnd}_60x60mm";
                leg.transform.SetParent(conveyorRoot.transform);
                leg.transform.position = new Vector3(legX[i], 0.01f + legH * 0.5f, legZ[j]);
                leg.transform.localScale = new Vector3(0.06f, legH, 0.06f);
                leg.GetComponent<Renderer>().sharedMaterial = frameMat;
            }
        }

        // Horizontal cross-brace between leg pairs at 300mm (Y = 0.30m)
        GameObject crossBraceUp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crossBraceUp.name = "CrossBrace_Upstream_300mm";
        crossBraceUp.transform.SetParent(conveyorRoot.transform);
        crossBraceUp.transform.position = new Vector3(0f, 0.30f, -1.10f);
        crossBraceUp.transform.localScale = new Vector3(0.34f, 0.04f, 0.04f);
        crossBraceUp.GetComponent<Renderer>().sharedMaterial = frameMat;

        GameObject crossBraceDown = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crossBraceDown.name = "CrossBrace_Downstream_300mm";
        crossBraceDown.transform.SetParent(conveyorRoot.transform);
        crossBraceDown.transform.position = new Vector3(0f, 0.30f, 1.10f);
        crossBraceDown.transform.localScale = new Vector3(0.34f, 0.04f, 0.04f);
        crossBraceDown.GetComponent<Renderer>().sharedMaterial = frameMat;

        // Side stringers at 300mm height
        GameObject sideStrLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sideStrLeft.name = "SideStringer_Left_300mm";
        sideStrLeft.transform.SetParent(conveyorRoot.transform);
        sideStrLeft.transform.position = new Vector3(-0.20f, 0.30f, 0f);
        sideStrLeft.transform.localScale = new Vector3(0.04f, 0.04f, 2.14f);
        sideStrLeft.GetComponent<Renderer>().sharedMaterial = frameMat;

        GameObject sideStrRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sideStrRight.name = "SideStringer_Right_300mm";
        sideStrRight.transform.SetParent(conveyorRoot.transform);
        sideStrRight.transform.position = new Vector3(0.20f, 0.30f, 0f);
        sideStrRight.transform.localScale = new Vector3(0.04f, 0.04f, 2.14f);
        sideStrRight.GetComponent<Renderer>().sharedMaterial = frameMat;

        // Drive Motor & Gearbox
        GameObject gearbox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gearbox.name = "Conveyor_Gearbox_120x120x100mm";
        gearbox.transform.SetParent(conveyorRoot.transform);
        gearbox.transform.position = new Vector3(-0.22f, 0.78f, 1.35f);
        gearbox.transform.localScale = new Vector3(0.12f, 0.12f, 0.10f);
        Material gearboxMat = DeltaMaterials.PaintedSteel(new Color(0.25f, 0.27f, 0.29f, 1f));
        gearbox.GetComponent<Renderer>().sharedMaterial = gearboxMat;

        GameObject motor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        motor.name = "Conveyor_DriveMotor_140x200mm";
        motor.transform.SetParent(conveyorRoot.transform);
        motor.transform.position = new Vector3(-0.22f, 0.78f, 1.18f);
        motor.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        motor.transform.localScale = new Vector3(0.14f, 0.10f, 0.14f);
        Material motorMat = DeltaMaterials.PaintedSteel(new Color(0.22f, 0.24f, 0.26f, 1f));
        motor.GetComponent<Renderer>().sharedMaterial = motorMat;

        GameObject motorTermBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        motorTermBox.name = "Motor_TerminalBox";
        motorTermBox.transform.SetParent(conveyorRoot.transform);
        motorTermBox.transform.position = new Vector3(-0.22f, 0.86f, 1.18f);
        motorTermBox.transform.localScale = new Vector3(0.06f, 0.05f, 0.08f);
        motorTermBox.GetComponent<Renderer>().sharedMaterial = motorMat;

        // =========================================================================
        // 5. GUIDE RAILS (SERVO AXIS: GuideRailAssembly_X)
        // =========================================================================
        GameObject guideRailRoot = new GameObject("GuideRailAssembly_X");
        guideRailRoot.transform.SetParent(conveyorRoot.transform);
        guideRailRoot.transform.position = new Vector3(0f, 0.96f, 0f);

        float railXOffset = (0.073f + 0.030f) * 0.5f; // 0.0515m

        GameObject railLeft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        railLeft.name = "GuideRail_Left_30mm";
        railLeft.transform.SetParent(guideRailRoot.transform);
        railLeft.transform.localPosition = new Vector3(-railXOffset, 0f, 0f);
        railLeft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        railLeft.transform.localScale = new Vector3(0.03f, 1.50f, 0.03f);
        railLeft.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        GameObject railRight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        railRight.name = "GuideRail_Right_30mm";
        railRight.transform.SetParent(guideRailRoot.transform);
        railRight.transform.localPosition = new Vector3(railXOffset, 0f, 0f);
        railRight.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        railRight.transform.localScale = new Vector3(0.03f, 1.50f, 0.03f);
        railRight.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        float[] bracketZ = { -1.0f, 0.0f, 1.0f };
        for (int b = 0; b < bracketZ.Length; b++)
        {
            GameObject brkL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            brkL.name = $"GuideRailBracket_Left_{b}";
            brkL.transform.SetParent(conveyorRoot.transform);
            brkL.transform.position = new Vector3(-0.16f, 0.95f, bracketZ[b]);
            brkL.transform.localScale = new Vector3(0.12f, 0.03f, 0.03f);
            brkL.GetComponent<Renderer>().sharedMaterial = frameMat;

            GameObject brkR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            brkR.name = $"GuideRailBracket_Right_{b}";
            brkR.transform.SetParent(conveyorRoot.transform);
            brkR.transform.position = new Vector3(0.16f, 0.95f, bracketZ[b]);
            brkR.transform.localScale = new Vector3(0.12f, 0.03f, 0.03f);
            brkR.GetComponent<Renderer>().sharedMaterial = frameMat;
        }

        // =========================================================================
        // 6. FILLING HEAD & GANTRY (SERVO AXIS: NozzleAssembly_Z)
        // =========================================================================
        GameObject gantryRoot = new GameObject("FillingGantry_Stationary");
        gantryRoot.transform.SetParent(conveyorRoot.transform);

        GameObject gantryColumn = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gantryColumn.name = "Gantry_VerticalColumn_60x60mm";
        gantryColumn.transform.SetParent(gantryRoot.transform);
        gantryColumn.transform.position = new Vector3(0f, 1.30f, 0.78f);
        gantryColumn.transform.localScale = new Vector3(0.06f, 0.84f, 0.06f);
        gantryColumn.GetComponent<Renderer>().sharedMaterial = frameMat;

        GameObject gantryLinearRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gantryLinearRail.name = "Gantry_LinearRail_Stainless";
        gantryLinearRail.transform.SetParent(gantryRoot.transform);
        gantryLinearRail.transform.position = new Vector3(0f, 1.30f, 0.745f);
        gantryLinearRail.transform.localScale = new Vector3(0.02f, 0.80f, 0.01f);
        gantryLinearRail.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        GameObject nozzleAssemblyRoot = new GameObject("NozzleAssembly_Z");
        nozzleAssemblyRoot.transform.SetParent(conveyorRoot.transform);
        nozzleAssemblyRoot.transform.position = new Vector3(0f, 1.15f, 0.60f);

        GameObject nozzleTip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        nozzleTip.name = "Nozzle_DispenseTip_Stainless";
        nozzleTip.transform.SetParent(nozzleAssemblyRoot.transform);
        nozzleTip.transform.localPosition = new Vector3(0f, 0.015f, 0f);
        nozzleTip.transform.localScale = new Vector3(0.014f, 0.015f, 0.014f);
        nozzleTip.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        GameObject nozzleTube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        nozzleTube.name = "Nozzle_FeedTube_Stainless";
        nozzleTube.transform.SetParent(nozzleAssemblyRoot.transform);
        nozzleTube.transform.localPosition = new Vector3(0f, 0.13f, 0f);
        nozzleTube.transform.localScale = new Vector3(0.020f, 0.10f, 0.020f);
        nozzleTube.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        GameObject valveBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        valveBlock.name = "Filling_ValveBlock_Stainless";
        valveBlock.transform.SetParent(nozzleAssemblyRoot.transform);
        valveBlock.transform.localPosition = new Vector3(0f, 0.28f, 0f);
        valveBlock.transform.localScale = new Vector3(0.08f, 0.10f, 0.08f);
        valveBlock.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        GameObject carriageArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        carriageArm.name = "Nozzle_CarriageArm";
        carriageArm.transform.SetParent(nozzleAssemblyRoot.transform);
        carriageArm.transform.localPosition = new Vector3(0f, 0.28f, 0.07f);
        carriageArm.transform.localScale = new Vector3(0.04f, 0.04f, 0.06f);
        carriageArm.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        GameObject slideBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slideBlock.name = "Nozzle_LinearSlideBlock";
        slideBlock.transform.SetParent(nozzleAssemblyRoot.transform);
        slideBlock.transform.localPosition = new Vector3(0f, 0.28f, 0.14f);
        slideBlock.transform.localScale = new Vector3(0.05f, 0.08f, 0.03f);
        slideBlock.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // =========================================================================
        // 7. 500 ML PET BOTTLE ON BELT UPSTREAM
        // =========================================================================
        GameObject bottleRoot = new GameObject("Bottle_500ml_PET");
        bottleRoot.transform.SetParent(conveyorRoot.transform);
        bottleRoot.transform.position = new Vector3(0f, 0.90f, 0.0f);

        Material petMat = DeltaMaterials.ClearPET();
        Material liquidMat = DeltaMaterials.Liquid(new Color(0.95f, 0.85f, 0.45f, 1f));
        Material capMat = DeltaMaterials.PaintedSteel(new Color(0.12f, 0.45f, 0.70f, 1f));

        float bodyH = 0.155f;
        GameObject bottleBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bottleBody.name = "Bottle_Body_PET";
        bottleBody.transform.SetParent(bottleRoot.transform);
        bottleBody.transform.localPosition = new Vector3(0f, bodyH * 0.5f, 0f);
        bottleBody.transform.localScale = new Vector3(0.070f, bodyH * 0.5f, 0.070f);
        bottleBody.GetComponent<Renderer>().sharedMaterial = petMat;

        float neckH = 0.030f;
        GameObject bottleNeck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bottleNeck.name = "Bottle_Neck_PET";
        bottleNeck.transform.SetParent(bottleRoot.transform);
        bottleNeck.transform.localPosition = new Vector3(0f, bodyH + neckH * 0.5f, 0f);
        bottleNeck.transform.localScale = new Vector3(0.028f, neckH * 0.5f, 0.028f);
        bottleNeck.GetComponent<Renderer>().sharedMaterial = petMat;

        // Bottle cap omitted for filling station demo (neck remains open under dispense tip)
        // float capH = 0.015f;

        float liquidH = bodyH * 0.70f;
        GameObject liquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        liquid.name = "Liquid_Shampoo_Volume";
        liquid.transform.SetParent(bottleRoot.transform);
        liquid.transform.localPosition = new Vector3(0f, 0.002f + liquidH * 0.5f, 0f);
        liquid.transform.localScale = new Vector3(0.066f, liquidH * 0.5f, 0.066f);
        liquid.GetComponent<Renderer>().sharedMaterial = liquidMat;

        // =========================================================================
        // 8. BAKE VERIFIED AMBIENT & DIRECTIONAL LIGHTING VALUES
        // =========================================================================
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = new Color(0.30f, 0.31f, 0.34f);
        RenderSettings.ambientEquatorColor = new Color(0.21f, 0.215f, 0.235f);
        RenderSettings.ambientGroundColor  = new Color(0.10f, 0.10f, 0.11f);

        // Key Light: Neutral directional light, intensity 3.4, soft shadows (strength 0.9)
        GameObject keyObj = new GameObject("Light_Key_Directional");
        keyObj.transform.SetParent(root.transform);
        keyObj.transform.rotation = Quaternion.Euler(50f, 325f, 0f);
        Light keyLight = keyObj.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.intensity = 3.4f;
        keyLight.color = new Color(1.00f, 0.98f, 0.95f);
        keyLight.shadows = LightShadows.Soft;
        keyLight.shadowStrength = 0.9f;

        // Fill Light: Directional light, intensity 0.85, shadows None
        GameObject fillObj = new GameObject("Light_Fill_Directional");
        fillObj.transform.SetParent(root.transform);
        fillObj.transform.rotation = Quaternion.Euler(35f, 145f, 0f);
        Light fillLight = fillObj.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.intensity = 0.85f;
        fillLight.color = new Color(0.85f, 0.90f, 1.00f);
        fillLight.shadows = LightShadows.None;

        // Rim Light: Directional light, intensity 1.30, shadows None
        GameObject rimObj = new GameObject("Light_Rim_Directional");
        rimObj.transform.SetParent(root.transform);
        rimObj.transform.rotation = Quaternion.Euler(8f, 205f, 0f);
        Light rimLight = rimObj.AddComponent<Light>();
        rimLight.type = LightType.Directional;
        rimLight.intensity = 1.30f;
        rimLight.color = new Color(0.88f, 0.93f, 1.00f);
        rimLight.shadows = LightShadows.None;

        // =========================================================================
        // 9. POST-PROCESSING VOLUME (Global ACES)
        // =========================================================================
        GameObject volObj = new GameObject("PostProcessing_GlobalVolume");
        volObj.transform.SetParent(root.transform);
        Volume volume = volObj.AddComponent<Volume>();
        volume.isGlobal = true;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "Cell_VolumeProfile";

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.85f);
        bloom.intensity.Override(0.35f);
        bloom.scatter.Override(0.70f);

        Tonemapping tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.mode.Override(TonemappingMode.ACES);

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.22f);
        vignette.smoothness.Override(0.35f);

        volume.sharedProfile = profile;

        // =========================================================================
        // 10. CAMERAS
        // =========================================================================
        Color charcoalBg = new Color(0.05f, 0.05f, 0.06f, 1f);

        // Hero Camera (Re-framed to comfortably frame the full 3m machine)
        GameObject heroCamObj = new GameObject("CellCam_Hero");
        heroCamObj.transform.SetParent(root.transform);
        heroCamObj.transform.position = new Vector3(2.30f, 1.95f, -2.60f);
        heroCamObj.transform.LookAt(new Vector3(0f, 1.00f, 0.10f));

        Camera heroCam = heroCamObj.AddComponent<Camera>();
        heroCam.fieldOfView = 38f;
        heroCam.nearClipPlane = 0.05f;
        heroCam.depth = 100f;
        heroCam.clearFlags = CameraClearFlags.SolidColor;
        heroCam.backgroundColor = charcoalBg;

        UniversalAdditionalCameraData heroCamData = heroCamObj.AddComponent<UniversalAdditionalCameraData>();
        heroCamData.renderPostProcessing = true;

        // Wide Camera (Full cell view, depth 99)
        GameObject wideCamObj = new GameObject("CellCam_Wide");
        wideCamObj.transform.SetParent(root.transform);
        wideCamObj.transform.position = new Vector3(3.60f, 3.20f, -3.40f);
        wideCamObj.transform.LookAt(new Vector3(0f, 0.80f, 0.10f));

        Camera wideCam = wideCamObj.AddComponent<Camera>();
        wideCam.fieldOfView = 45f;
        wideCam.nearClipPlane = 0.05f;
        wideCam.depth = 99f;
        wideCam.clearFlags = CameraClearFlags.SolidColor;
        wideCam.backgroundColor = charcoalBg;

        UniversalAdditionalCameraData wideCamData = wideCamObj.AddComponent<UniversalAdditionalCameraData>();
        wideCamData.renderPostProcessing = true;

        // Rail Top Camera (Overhead angle framing GuideRailAssembly_X, depth 90, initially disabled)
        GameObject railTopCamObj = new GameObject("RailTopCam");
        railTopCamObj.transform.SetParent(root.transform);
        railTopCamObj.transform.position = new Vector3(0.0f, 2.35f, -0.15f);
        railTopCamObj.transform.LookAt(new Vector3(0f, 0.96f, 0.15f));

        Camera railTopCam = railTopCamObj.AddComponent<Camera>();
        railTopCam.fieldOfView = 48f;
        railTopCam.nearClipPlane = 0.05f;
        railTopCam.farClipPlane = 50f;
        railTopCam.depth = 90f;
        railTopCam.clearFlags = CameraClearFlags.SolidColor;
        railTopCam.backgroundColor = charcoalBg;
        railTopCam.enabled = false;

        UniversalAdditionalCameraData railTopCamData = railTopCamObj.AddComponent<UniversalAdditionalCameraData>();
        railTopCamData.renderPostProcessing = true;

        // Nozzle Side Camera (Side angle framing NozzleAssembly_Z & fill station, depth 91, initially disabled)
        GameObject nozzleSideCamObj = new GameObject("NozzleSideCam");
        nozzleSideCamObj.transform.SetParent(root.transform);
        nozzleSideCamObj.transform.position = new Vector3(1.10f, 1.25f, 0.55f);
        nozzleSideCamObj.transform.LookAt(new Vector3(0f, 1.18f, 0.60f));

        Camera nozzleSideCam = nozzleSideCamObj.AddComponent<Camera>();
        nozzleSideCam.fieldOfView = 36f;
        nozzleSideCam.nearClipPlane = 0.05f;
        nozzleSideCam.farClipPlane = 50f;
        nozzleSideCam.depth = 91f;
        nozzleSideCam.clearFlags = CameraClearFlags.SolidColor;
        nozzleSideCam.backgroundColor = charcoalBg;
        nozzleSideCam.enabled = false;

        UniversalAdditionalCameraData nozzleSideCamData = nozzleSideCamObj.AddComponent<UniversalAdditionalCameraData>();
        nozzleSideCamData.renderPostProcessing = true;

        // =========================================================================
        // 11. REFLECTION PROBE (LAST CALL after ceiling fixtures, walls, ceiling & lights)
        // Scaled to cover the 20x20x9m room and full conveyor
        // =========================================================================
        GameObject probeObj = new GameObject("ReflectionProbe_Cell");
        probeObj.transform.SetParent(root.transform);
        probeObj.transform.position = new Vector3(0f, 4.5f, 0f);
        ReflectionProbe probe = probeObj.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        probe.size = new Vector3(22.0f, 10.0f, 22.0f);
        probe.boxProjection = true;
        probe.RenderProbe();

        // =========================================================================
        // SELECTION & LOGGING
        // =========================================================================
        Selection.activeGameObject = root;
        Debug.Log("[CellBuilder] Industrial cell built successfully: 6 ceiling light fixtures, verified lighting baked, 80x80m floor, 20x20x9m room + ceiling, reflection probe baked last.");
    }

    // =========================================================================
    // 12. ATTACH SEQUENCER HELPER
    // =========================================================================
    [MenuItem("Tools/Delta/Attach Sequencer")]
    public static void AttachSequencer()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            Debug.LogError($"[CellBuilder] Root GameObject '{RootName}' not found!");
            return;
        }

        ChangeoverSequencer sequencer = root.GetComponent<ChangeoverSequencer>();
        if (sequencer == null)
        {
            sequencer = Undo.AddComponent<ChangeoverSequencer>(root);
        }

        // Find objects by scene names
        GameObject railObj = GameObject.Find("GuideRailAssembly_X");
        GameObject nozzleObj = GameObject.Find("NozzleAssembly_Z");
        GameObject beltObj = GameObject.Find("Belt_DarkRubber_400mm");
        GameObject bottleObj = GameObject.Find("Bottle_500ml_PET");

        if (railObj != null) sequencer.guideRailAssembly = railObj.transform;
        if (nozzleObj != null) sequencer.nozzleAssembly = nozzleObj.transform;
        if (beltObj != null) sequencer.beltRenderer = beltObj.GetComponent<Renderer>();
        if (bottleObj != null) sequencer.initialBottle = bottleObj;

        // Wire materials from DeltaMaterials
        sequencer.petMaterial = DeltaMaterials.ClearPET();
        sequencer.liquidMaterial = DeltaMaterials.Liquid(new Color(0.95f, 0.85f, 0.45f, 1f));
        sequencer.capMaterial = DeltaMaterials.PaintedSteel(new Color(0.12f, 0.45f, 0.70f, 1f));

        sequencer.ValidateAndCacheReferences();
        EditorUtility.SetDirty(root);
        Debug.Log($"[CellBuilder] ChangeoverSequencer attached to '{RootName}' and references wired.");
    }
}
