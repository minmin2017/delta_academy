using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using TMPro;

public static class CappingZoneBuilder
{
    private const string RootName = "Cell";
    private const string ZoneRootName = "CappingZone";

    [MenuItem("Tools/Delta/Add Capping Zone Props")]
    public static void AddCappingZoneProps()
    {
        // 1. Locate root Cell
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = GameObject.Find("IndustrialCell_Delta");
        }

        if (root == null)
        {
            Debug.LogError($"[CappingZoneBuilder] Scene root '{RootName}' not found! Run Tools/Delta/Build Cell first.");
            return;
        }

        // 2. Delete existing CappingZone child if present (idempotent rebuild pattern)
        Transform existingZone = root.transform.Find(ZoneRootName);
        if (existingZone != null)
        {
            Undo.DestroyObjectImmediate(existingZone.gameObject);
        }

        // 3. Materials from DeltaMaterials
        Material frameSteel = DeltaMaterials.PaintedSteel(new Color(0.32f, 0.33f, 0.35f, 1f));
        Material stainlessMat = DeltaMaterials.BrushedStainless();

        TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (fontAsset == null)
        {
            fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }

        // 4. Zone Root GameObject under Cell
        GameObject zoneObj = new GameObject(ZoneRootName);
        zoneObj.transform.SetParent(root.transform);
        zoneObj.transform.localPosition = Vector3.zero;
        zoneObj.transform.localRotation = Quaternion.identity;

        // =========================================================================
        // CAP FEEDER (Funnel / hopper bowl and chute above conveyor downstream of filling nozzle at Z = 0.60m)
        // Positioned at Z = 1.05m, elevated above the line (Y = 1.35m - 1.55m)
        // =========================================================================
        GameObject hopper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hopper.name = "CapFeeder_Hopper";
        hopper.transform.SetParent(zoneObj.transform);
        hopper.transform.localPosition = new Vector3(0f, 1.55f, 1.05f);
        hopper.transform.localScale = new Vector3(0.32f, 0.12f, 0.32f);
        hopper.GetComponent<Renderer>().sharedMaterial = frameSteel;

        GameObject chute = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        chute.name = "CapFeeder_Chute";
        chute.transform.SetParent(zoneObj.transform);
        chute.transform.localPosition = new Vector3(0f, 1.35f, 1.05f);
        chute.transform.localScale = new Vector3(0.06f, 0.10f, 0.06f);
        chute.GetComponent<Renderer>().sharedMaterial = frameSteel;

        // =========================================================================
        // CAPPING HEAD (Cylinder tool & vertical actuator rod above conveyor at Z = 1.30m)
        // =========================================================================
        GameObject cappingActuator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cappingActuator.name = "CappingHead_Actuator";
        cappingActuator.transform.SetParent(zoneObj.transform);
        cappingActuator.transform.localPosition = new Vector3(0f, 1.32f, 1.30f);
        cappingActuator.transform.localScale = new Vector3(0.04f, 0.12f, 0.04f);
        cappingActuator.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        GameObject cappingChuck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cappingChuck.name = "CappingHead_Chuck";
        cappingChuck.transform.SetParent(zoneObj.transform);
        cappingChuck.transform.localPosition = new Vector3(0f, 1.15f, 1.30f);
        cappingChuck.transform.localScale = new Vector3(0.09f, 0.05f, 0.09f);
        cappingChuck.GetComponent<Renderer>().sharedMaterial = stainlessMat;

        // Small TextMeshPro label "CAPPING HEAD"
        CreateWorldLabel(zoneObj.transform, "Label_CappingHead",
            new Vector3(0.20f, 1.30f, 1.30f), new Vector3(0f, 90f, 0f),
            "<b>CAPPING HEAD</b>",
            0.035f, Color.white, TextAlignmentOptions.Center, new Vector2(0.28f, 0.06f), fontAsset);

        // =========================================================================
        // CAP PRESENT SENSOR (Simple optical box sensor beside the conveyor at Z = 1.18m)
        // =========================================================================
        GameObject sensor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sensor.name = "Sensor_CapPresent";
        sensor.transform.SetParent(zoneObj.transform);
        sensor.transform.localPosition = new Vector3(-0.16f, 1.08f, 1.18f);
        sensor.transform.localScale = new Vector3(0.04f, 0.04f, 0.05f);
        sensor.GetComponent<Renderer>().sharedMaterial = frameSteel;

        CreateWorldLabel(zoneObj.transform, "Label_Sensor",
            new Vector3(0.20f, 1.08f, 1.18f), new Vector3(0f, 90f, 0f),
            "<b>SENSOR</b>",
            0.026f, Color.white, TextAlignmentOptions.Center, new Vector2(0.18f, 0.05f), fontAsset);

        // 5. Clean up colliders on all visual primitive props
        Collider[] colliders = zoneObj.GetComponentsInChildren<Collider>();
        for (int c = 0; c < colliders.Length; c++)
        {
            Undo.DestroyObjectImmediate(colliders[c]);
        }

        EditorUtility.SetDirty(root);
        Selection.activeGameObject = zoneObj;
        Debug.Log("[CappingZoneBuilder] Capping zone props (Cap Feeder, Capping Head with label, Cap Present Sensor with label) added successfully under 'Cell'.");
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
