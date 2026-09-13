using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using TMPro;

public static class EndOfLineBuilder
{
    private const string RootName = "Cell";
    private const string ZoneRootName = "EndOfLineZone";

    [MenuItem("Tools/Delta/Add End Of Line Props")]
    public static void AddEndOfLineProps()
    {
        // 1. Locate root Cell
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = GameObject.Find("IndustrialCell_Delta");
        }

        if (root == null)
        {
            Debug.LogError($"[EndOfLineBuilder] Scene root '{RootName}' not found! Run Tools/Delta/Build Cell first.");
            return;
        }

        // 2. Delete existing EndOfLineZone child if present (idempotent rebuild pattern)
        Transform existingZone = root.transform.Find(ZoneRootName);
        if (existingZone != null)
        {
            Undo.DestroyObjectImmediate(existingZone.gameObject);
        }

        // 3. Materials from DeltaMaterials
        Material frameSteel = DeltaMaterials.PaintedSteel(new Color(0.32f, 0.33f, 0.35f, 1f));
        Material darkRubberMat = DeltaMaterials.DarkRubber();

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
        // OUTFEED CONVEYOR (Extending downstream from main conveyor exit at Z = 1.50m to Z = 3.90m)
        // Belt top at Y = 0.90m (slab center at Y = 0.89m, thickness 0.02m)
        // =========================================================================
        GameObject outfeedBelt = GameObject.CreatePrimitive(PrimitiveType.Cube);
        outfeedBelt.name = "Outfeed_Belt_DarkRubber";
        outfeedBelt.transform.SetParent(zoneObj.transform);
        outfeedBelt.transform.localPosition = new Vector3(0f, 0.89f, 2.70f);
        outfeedBelt.transform.localScale = new Vector3(0.40f, 0.02f, 2.40f);
        outfeedBelt.GetComponent<Renderer>().sharedMaterial = darkRubberMat;

        GameObject outfeedFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        outfeedFrame.name = "Outfeed_SideFrame_Bed";
        outfeedFrame.transform.SetParent(zoneObj.transform);
        outfeedFrame.transform.localPosition = new Vector3(0f, 0.84f, 2.70f);
        outfeedFrame.transform.localScale = new Vector3(0.44f, 0.08f, 2.40f);
        outfeedFrame.GetComponent<Renderer>().sharedMaterial = frameSteel;

        GameObject outfeedLegs = GameObject.CreatePrimitive(PrimitiveType.Cube);
        outfeedLegs.name = "Outfeed_SupportLegs";
        outfeedLegs.transform.SetParent(zoneObj.transform);
        outfeedLegs.transform.localPosition = new Vector3(0f, 0.40f, 3.70f);
        outfeedLegs.transform.localScale = new Vector3(0.40f, 0.80f, 0.06f);
        outfeedLegs.GetComponent<Renderer>().sharedMaterial = frameSteel;

        // =========================================================================
        // CHECKWEIGHER (Flat platform / scale on conveyor line at Z = 1.95m)
        // =========================================================================
        GameObject checkweigherPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        checkweigherPlatform.name = "Checkweigher_Platform";
        checkweigherPlatform.transform.SetParent(zoneObj.transform);
        checkweigherPlatform.transform.localPosition = new Vector3(0f, 0.905f, 1.95f);
        checkweigherPlatform.transform.localScale = new Vector3(0.38f, 0.015f, 0.35f);
        checkweigherPlatform.GetComponent<Renderer>().sharedMaterial = frameSteel;

        GameObject checkweigherTerminal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        checkweigherTerminal.name = "Checkweigher_Terminal";
        checkweigherTerminal.transform.SetParent(zoneObj.transform);
        checkweigherTerminal.transform.localPosition = new Vector3(-0.28f, 1.10f, 1.95f);
        checkweigherTerminal.transform.localScale = new Vector3(0.08f, 0.14f, 0.10f);
        checkweigherTerminal.GetComponent<Renderer>().sharedMaterial = frameSteel;

        CreateWorldLabel(zoneObj.transform, "Label_Checkweigher",
            new Vector3(0.24f, 0.98f, 1.95f), new Vector3(0f, 90f, 0f),
            "<b>CHECKWEIGHER</b>",
            0.032f, Color.white, TextAlignmentOptions.Center, new Vector2(0.30f, 0.06f), fontAsset);

        // =========================================================================
        // REJECT STATION (Angled paddle/pusher shape beside conveyor at Z = 2.50m)
        // =========================================================================
        GameObject rejectPaddle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rejectPaddle.name = "RejectStation_Paddle";
        rejectPaddle.transform.SetParent(zoneObj.transform);
        rejectPaddle.transform.localPosition = new Vector3(-0.12f, 0.95f, 2.50f);
        rejectPaddle.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
        rejectPaddle.transform.localScale = new Vector3(0.25f, 0.06f, 0.03f);
        rejectPaddle.GetComponent<Renderer>().sharedMaterial = frameSteel;

        GameObject rejectActuator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rejectActuator.name = "RejectStation_Actuator";
        rejectActuator.transform.SetParent(zoneObj.transform);
        rejectActuator.transform.localPosition = new Vector3(-0.28f, 0.95f, 2.45f);
        rejectActuator.transform.localScale = new Vector3(0.12f, 0.08f, 0.12f);
        rejectActuator.GetComponent<Renderer>().sharedMaterial = frameSteel;

        CreateWorldLabel(zoneObj.transform, "Label_Reject",
            new Vector3(0.24f, 0.98f, 2.50f), new Vector3(0f, 90f, 0f),
            "<b>REJECT</b>",
            0.032f, Color.white, TextAlignmentOptions.Center, new Vector2(0.20f, 0.06f), fontAsset);

        // =========================================================================
        // LABELING MACHINE (Box with label roll cylinder on top at Z = 3.20m)
        // =========================================================================
        GameObject labelerBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        labelerBox.name = "Labeler_Enclosure";
        labelerBox.transform.SetParent(zoneObj.transform);
        labelerBox.transform.localPosition = new Vector3(-0.26f, 1.10f, 3.20f);
        labelerBox.transform.localScale = new Vector3(0.16f, 0.28f, 0.26f);
        labelerBox.GetComponent<Renderer>().sharedMaterial = frameSteel;

        GameObject labelRoll = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        labelRoll.name = "Labeler_Roll";
        labelRoll.transform.SetParent(zoneObj.transform);
        labelRoll.transform.localPosition = new Vector3(-0.26f, 1.32f, 3.20f);
        labelRoll.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        labelRoll.transform.localScale = new Vector3(0.18f, 0.04f, 0.18f);
        labelRoll.GetComponent<Renderer>().sharedMaterial = frameSteel;

        CreateWorldLabel(zoneObj.transform, "Label_Labeler",
            new Vector3(0.24f, 1.10f, 3.20f), new Vector3(0f, 90f, 0f),
            "<b>LABELER</b>",
            0.032f, Color.white, TextAlignmentOptions.Center, new Vector2(0.22f, 0.06f), fontAsset);

        // 5. Clean up colliders on all visual primitive props
        Collider[] colliders = zoneObj.GetComponentsInChildren<Collider>();
        for (int c = 0; c < colliders.Length; c++)
        {
            Undo.DestroyObjectImmediate(colliders[c]);
        }

        EditorUtility.SetDirty(root);
        Selection.activeGameObject = zoneObj;
        Debug.Log("[EndOfLineBuilder] End-of-Line zone props (Checkweigher, Reject Station, Labeler, Outfeed Conveyor) added successfully under 'Cell'.");
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
