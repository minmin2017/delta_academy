using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class LightingTestBuilder
{
    private const string RootName = "LightingTest";

    [MenuItem("Tools/Delta/Build Lighting Test")]
    public static void BuildLightingTest()
    {
        // 0. Idempotence: Remove previously created root object
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Lighting Test");

        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLitShader == null)
        {
            Debug.LogError("Universal Render Pipeline/Lit shader not found! Make sure URP is active.");
            return;
        }

        // ==========================================
        // 1. SKYBOX & AMBIENT (Factory Interior)
        // Remove outdoor skybox, set neutral dark industrial Trilight ambient
        // ==========================================
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.12f, 0.13f, 0.15f);
        RenderSettings.ambientEquatorColor = new Color(0.08f, 0.08f, 0.09f);
        RenderSettings.ambientGroundColor = new Color(0.04f, 0.04f, 0.045f);

        // ==========================================
        // 2. FLOOR (12m x 12m plane, mid-dark concrete)
        // Unity plane is 10m x 10m -> scale (1.2, 1.0, 1.2) = 12m x 12m
        // ==========================================
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor_Concrete";
        floor.transform.SetParent(root.transform);
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(1.2f, 1f, 1.2f);

        Material floorMat = new Material(urpLitShader);
        floorMat.name = "M_ConcreteFloor";
        floorMat.SetColor("_BaseColor", new Color(0.22f, 0.22f, 0.23f, 1f));
        floorMat.SetFloat("_Metallic", 0f);
        floorMat.SetFloat("_Smoothness", 0.15f);
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;

        // ==========================================
        // 3. CONVEYOR SEGMENT & STAINLESS STEEL
        // Slab: 1.6m (X) x 0.3m (Z) x 0.04m (Y), top surface at Y = 0.9m
        // Center: Y = 0.9 - 0.02 = 0.88m
        // ==========================================
        GameObject conveyorSlab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        conveyorSlab.name = "Conveyor_BeltSlab";
        conveyorSlab.transform.SetParent(root.transform);
        conveyorSlab.transform.position = new Vector3(0f, 0.88f, 0f);
        conveyorSlab.transform.localScale = new Vector3(1.6f, 0.04f, 0.3f);

        // Procedural brushed normal & subtle smoothness maps to break up reflections
        Texture2D brushedNormal = GenerateBrushedNormalMap(256, 256);
        Texture2D smoothnessMask = GenerateSmoothnessVariationMap(256, 256, 0.60f, 0.05f);

        Material stainlessMat = new Material(urpLitShader);
        stainlessMat.name = "M_StainlessSteel";
        // Neutral grey albedo tints reflections without reading as flat white plastic
        stainlessMat.SetColor("_BaseColor", new Color(0.55f, 0.56f, 0.58f, 1f));
        stainlessMat.SetFloat("_Metallic", 1.0f);
        stainlessMat.SetFloat("_Smoothness", 0.60f);
        if (brushedNormal != null)
        {
            stainlessMat.SetTexture("_BumpMap", brushedNormal);
            stainlessMat.EnableKeyword("_NORMALMAP");
            stainlessMat.SetFloat("_BumpScale", 0.25f);
        }
        if (smoothnessMask != null)
        {
            stainlessMat.SetTexture("_MetallicGlossMap", smoothnessMask);
            stainlessMat.EnableKeyword("_METALLICSPECGLOSSMAP");
        }
        conveyorSlab.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Two side guide rails: 1.6m x 0.012m x 0.05m standing on belt top (Y = 0.9m)
        // Spaced 0.073m apart in Z (500ml bottle recipe width).
        // Center-to-center = 0.073 + 0.012 = 0.085m -> Z offsets = +-0.0425m
        float railZOffset = (0.073f + 0.012f) * 0.5f; // 0.0425m
        float railYCenter = 0.9f + 0.05f * 0.5f;     // 0.925m

        GameObject railFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
        railFront.name = "GuideRail_Front";
        railFront.transform.SetParent(root.transform);
        railFront.transform.position = new Vector3(0f, railYCenter, -railZOffset);
        railFront.transform.localScale = new Vector3(1.6f, 0.05f, 0.012f);
        railFront.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        GameObject railBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
        railBack.name = "GuideRail_Back";
        railBack.transform.SetParent(root.transform);
        railBack.transform.position = new Vector3(0f, railYCenter, railZOffset);
        railBack.transform.localScale = new Vector3(1.6f, 0.05f, 0.012f);
        railBack.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // 4 conveyor support legs down to the floor
        float[] legX = { -0.65f, 0.65f };
        float[] legZ = { -0.11f, 0.11f };
        float legHeight = 0.86f;
        Material legMat = new Material(urpLitShader);
        legMat.name = "M_ConveyorLegs";
        legMat.SetColor("_BaseColor", new Color(0.28f, 0.30f, 0.32f, 1f));
        legMat.SetFloat("_Metallic", 0.9f);
        legMat.SetFloat("_Smoothness", 0.45f);

        for (int i = 0; i < legX.Length; i++)
        {
            for (int j = 0; j < legZ.Length; j++)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = $"SupportLeg_{i}_{j}";
                leg.transform.SetParent(root.transform);
                leg.transform.position = new Vector3(legX[i], legHeight * 0.5f, legZ[j]);
                leg.transform.localScale = new Vector3(0.035f, legHeight * 0.5f, 0.035f);
                leg.GetComponent<Renderer>().sharedMaterial = legMat;
            }
        }

        // ==========================================
        // 4. BOTTLE (500 ml PET) & LIQUID
        // Diameter 0.070m, Body height 0.195m, standing at Y = 0.9m
        // ==========================================
        GameObject bottleRoot = new GameObject("Bottle_500ml_PET");
        bottleRoot.transform.SetParent(root.transform);
        bottleRoot.transform.position = new Vector3(0f, 0.9f, 0f);

        // Clear PET Plastic: URP Lit Transparent mode
        Material bottleMat = new Material(urpLitShader);
        bottleMat.name = "M_PET_Plastic_Transparent";
        bottleMat.SetFloat("_Surface", 1.0f); // 1 = Transparent
        bottleMat.SetFloat("_Blend", 0.0f);   // 0 = Alpha
        bottleMat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        bottleMat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        bottleMat.SetInt("_ZWrite", 0);
        bottleMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        bottleMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        bottleMat.SetOverrideTag("RenderType", "Transparent");
        bottleMat.renderQueue = (int)RenderQueue.Transparent;
        // Alpha ~0.25, smoothness ~0.90, metallic 0, slight blue-green tint
        bottleMat.SetColor("_BaseColor", new Color(0.85f, 0.95f, 0.94f, 0.25f));
        bottleMat.SetFloat("_Smoothness", 0.90f);
        bottleMat.SetFloat("_Metallic", 0.0f);
        bottleMat.SetShaderPassEnabled("ShadowCaster", false);

        // Main Body (diameter 0.070m, height 0.155m)
        float bodyH = 0.155f;
        GameObject bottleBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bottleBody.name = "Bottle_Body";
        bottleBody.transform.SetParent(bottleRoot.transform);
        bottleBody.transform.localPosition = new Vector3(0f, bodyH * 0.5f, 0f);
        bottleBody.transform.localScale = new Vector3(0.070f, bodyH * 0.5f, 0.070f);
        bottleBody.GetComponent<Renderer>().sharedMaterial = bottleMat;

        // Neck (diameter ~0.028m, height ~0.030m)
        float neckH = 0.030f;
        GameObject bottleNeck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bottleNeck.name = "Bottle_Neck";
        bottleNeck.transform.SetParent(bottleRoot.transform);
        bottleNeck.transform.localPosition = new Vector3(0f, bodyH + neckH * 0.5f, 0f);
        bottleNeck.transform.localScale = new Vector3(0.028f, neckH * 0.5f, 0.028f);
        bottleNeck.GetComponent<Renderer>().sharedMaterial = bottleMat;

        // Cap (opaque blue, diameter ~0.031m, height ~0.015m)
        float capH = 0.015f;
        GameObject bottleCap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bottleCap.name = "Bottle_Cap";
        bottleCap.transform.SetParent(bottleRoot.transform);
        bottleCap.transform.localPosition = new Vector3(0f, bodyH + neckH + capH * 0.5f, 0f);
        bottleCap.transform.localScale = new Vector3(0.031f, capH * 0.5f, 0.031f);

        Material capMat = new Material(urpLitShader);
        capMat.name = "M_BottleCap_Blue";
        capMat.SetColor("_BaseColor", new Color(0.12f, 0.45f, 0.70f, 1f));
        capMat.SetFloat("_Metallic", 0.1f);
        capMat.SetFloat("_Smoothness", 0.75f);
        bottleCap.GetComponent<Renderer>().sharedMaterial = capMat;

        // Liquid inside: Opaque pearly shampoo volume (70% of body height = ~0.1085m)
        // Closed cylinder mesh with visible top meniscus surface
        float liquidH = bodyH * 0.70f;
        GameObject liquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        liquid.name = "Liquid_Shampoo_Volume";
        liquid.transform.SetParent(bottleRoot.transform);
        liquid.transform.localPosition = new Vector3(0f, 0.002f + liquidH * 0.5f, 0f);
        liquid.transform.localScale = new Vector3(0.066f, liquidH * 0.5f, 0.066f);

        Material liquidMat = new Material(urpLitShader);
        liquidMat.name = "M_PearlyLiquid";
        liquidMat.SetColor("_BaseColor", new Color(0.95f, 0.85f, 0.45f, 1.0f));
        liquidMat.SetFloat("_Metallic", 0.0f);
        liquidMat.SetFloat("_Smoothness", 0.5f);
        liquid.GetComponent<Renderer>().sharedMaterial = liquidMat;

        // ==========================================
        // 5. LIGHTING — THREE-POINT SETUP WITH CLEAR SEPARATION & SHADOWS
        // ==========================================
        // Key Light: Directional light angled (50, -35, 0), Soft shadows, strength 0.8
        GameObject keyObj = new GameObject("Light_Key_Directional");
        keyObj.transform.SetParent(root.transform);
        keyObj.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        Light keyLight = keyObj.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.intensity = 2.0f;
        keyLight.color = new Color(1.0f, 0.97f, 0.92f);
        keyLight.shadows = LightShadows.Soft;
        keyLight.shadowStrength = 0.8f;

        // Fill Light: Spot light from opposite side, dimmer (~0.35 ratio = 0.70 intensity)
        GameObject fillObj = new GameObject("Light_Fill_Spot");
        fillObj.transform.SetParent(root.transform);
        fillObj.transform.position = new Vector3(-1.2f, 1.4f, -0.6f);
        Light fillLight = fillObj.AddComponent<Light>();
        fillLight.type = LightType.Spot;
        fillLight.intensity = 0.70f;
        fillLight.color = new Color(0.72f, 0.84f, 1.0f);
        fillLight.range = 5.0f;
        fillLight.spotAngle = 65f;
        fillObj.transform.LookAt(new Vector3(0f, 0.90f, 0f));

        // Rim/Back Light: Aimed to catch top edge of conveyor rail and bottle silhouette
        GameObject rimObj = new GameObject("Light_Rim_Spot");
        rimObj.transform.SetParent(root.transform);
        rimObj.transform.position = new Vector3(0f, 1.35f, 0.85f);
        Light rimLight = rimObj.AddComponent<Light>();
        rimLight.type = LightType.Spot;
        rimLight.intensity = 1.0f;
        rimLight.color = new Color(1.0f, 0.98f, 0.94f);
        rimLight.range = 4.0f;
        rimLight.spotAngle = 45f;
        rimObj.transform.LookAt(new Vector3(0f, 0.925f, 0f));

        // ==========================================
        // 6. CAMERAS
        // TestCam: Hero close-up along conveyor (depth 100)
        // TestCamWide: Three-quarter view of whole cell (depth 99)
        // ==========================================
        GameObject camObj = new GameObject("TestCam");
        camObj.transform.SetParent(root.transform);
        camObj.transform.position = new Vector3(0.55f, 1.12f, -0.65f);
        camObj.transform.LookAt(new Vector3(0f, 0.99f, 0f));

        Camera cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 35f;
        cam.nearClipPlane = 0.05f;
        cam.depth = 100f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.06f, 1f); // Dark charcoal background

        UniversalAdditionalCameraData camData = camObj.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;

        GameObject wideCamObj = new GameObject("TestCamWide");
        wideCamObj.transform.SetParent(root.transform);
        wideCamObj.transform.position = new Vector3(2.2f, 2.0f, -2.0f);
        wideCamObj.transform.LookAt(new Vector3(0f, 0.85f, 0f));

        Camera wideCam = wideCamObj.AddComponent<Camera>();
        wideCam.fieldOfView = 45f;
        wideCam.nearClipPlane = 0.05f;
        wideCam.depth = 99f;
        wideCam.clearFlags = CameraClearFlags.SolidColor;
        wideCam.backgroundColor = new Color(0.05f, 0.05f, 0.06f, 1f);

        UniversalAdditionalCameraData wideCamData = wideCamObj.AddComponent<UniversalAdditionalCameraData>();
        wideCamData.renderPostProcessing = true;

        // ==========================================
        // 7. POST-PROCESSING VOLUME
        // Global Volume: Bloom, ACES Tonemapping, Vignette
        // ==========================================
        GameObject volObj = new GameObject("PostProcessing_GlobalVolume");
        volObj.transform.SetParent(root.transform);
        Volume volume = volObj.AddComponent<Volume>();
        volume.isGlobal = true;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "LightingTest_VolumeProfile";

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

        // ==========================================
        // 8. REFLECTION PROBE (Rendered AFTER all geometry & lights exist)
        // Box size covers conveyor and floor area
        // ==========================================
        GameObject probeObj = new GameObject("ReflectionProbe_Central");
        probeObj.transform.SetParent(root.transform);
        probeObj.transform.position = new Vector3(0f, 1.0f, 0f);
        ReflectionProbe probe = probeObj.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        probe.size = new Vector3(12.0f, 4.0f, 12.0f);
        probe.boxProjection = true;
        probe.RenderProbe();

        // ==========================================
        // 9. SUMMARY & SELECTION
        // ==========================================
        Selection.activeGameObject = root;
        Debug.Log("[LightingTestBuilder] Scene built successfully: Industrial dark ambient, 12x12m floor, brushed stainless steel, clear PET bottle & liquid volume, 3-point lighting with soft shadows, reflection probe covering cell, TestCam + TestCamWide & ACES post-processing.");
    }

    private static Texture2D GenerateBrushedNormalMap(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.name = "Procedural_Brushed_Normal";
        tex.wrapMode = TextureWrapMode.Repeat;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Elongated horizontal noise for brushed metal look
                float n = Mathf.PerlinNoise(x * 0.04f, y * 0.5f);
                float nx = (Mathf.PerlinNoise((x + 1) * 0.04f, y * 0.5f) - n) * 0.5f;
                float ny = (Mathf.PerlinNoise(x * 0.04f, (y + 1) * 0.5f) - n) * 1.5f;

                Vector3 norm = new Vector3(-nx * 0.2f, -ny * 0.2f, 1.0f).normalized;
                Color c = new Color(norm.x * 0.5f + 0.5f, norm.y * 0.5f + 0.5f, norm.z * 0.5f + 0.5f, 1f);
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateSmoothnessVariationMap(int width, int height, float baseSmoothness, float variance)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.name = "Procedural_MetallicSmoothness";
        tex.wrapMode = TextureWrapMode.Repeat;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
                float s = Mathf.Clamp01(baseSmoothness + (n - 0.5f) * variance);
                // URP Standard Lit: R = Metallic, G = Occlusion, B = Detail Mask, A = Smoothness
                Color c = new Color(1.0f, 1.0f, 0f, s);
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }
}
