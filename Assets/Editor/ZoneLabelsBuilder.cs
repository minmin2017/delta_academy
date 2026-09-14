using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using TMPro;

/// <summary>
/// Additive, idempotent builder for equipment call-out labels (Min feedback item 3,
/// 2026-09-14: "อยากได้ text อธิบาย เหมือน UI ในหน้าจอ อธิบายว่าแต่ละอันคือไร").
///
/// CappingZone and EndOfLineZone already carry world-space TMP labels (built by their
/// own zone builders). This file fills the two zones that were still missing them
/// (InfeedZone, FillingZone) using the exact same world-space TextMeshPro pattern
/// (see CappingZoneBuilder.CreateWorldLabel) so all 4 A-Clip zone shots are consistently
/// labeled. It also adds a size-change callout label (item 2: "ทำให้ชัดเจนกว่านี้ว่าขวด
/// มันเปลี่ยนรูปร่าง") near the nozzle, read live from ChangeoverSequencer's recipe pair
/// so it never goes stale if the recipe indices change.
///
/// All labels use localRotation (0, 90, 0) to face the +X direction, matching every
/// zone camera in this project (InfeedZoneCam/FillingZoneCam/CappingZoneCam/EndOfLineCam/
/// NozzleSideCam all sit on the +X side of the line looking back toward -X) - this is
/// the same convention CappingZoneBuilder already established for Label_CappingHead.
/// </summary>
public static class ZoneLabelsBuilder
{
    private const string RootName = "Cell";
    private const string FallbackRootName = "IndustrialCell_Delta";
    private const string LabelsRootName = "ZoneLabels";

    [MenuItem("Tools/Delta/Add Zone Equipment Labels")]
    public static void AddZoneEquipmentLabels()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null) root = GameObject.Find(FallbackRootName);
        if (root == null)
        {
            Debug.LogError("[ZoneLabelsBuilder] Scene root 'Cell' not found! Run Tools/Delta/Build Full Line first.");
            return;
        }

        // Idempotent rebuild: destroy any previous ZoneLabels root
        Transform existing = root.transform.Find(LabelsRootName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        GameObject labelsRoot = new GameObject(LabelsRootName);
        Undo.RegisterCreatedObjectUndo(labelsRoot, "Create ZoneLabels");
        labelsRoot.transform.SetParent(root.transform);
        labelsRoot.transform.localPosition = Vector3.zero;

        TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (fontAsset == null)
        {
            fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }

        Color labelColor = Color.white;
        // NOTE (2026-09-14): Y=+90 rendered mirrored/backwards text when viewed from these
        // cameras (confirmed via live screenshot - "250ml -> 500ml" read as reversed
        // characters) - the +X-facing cameras actually see the BACK of a +90-rotated TMP
        // plane. Y=-90 is the correct facing rotation for this project's +X camera convention.
        Vector3 faceCamera = new Vector3(0f, -90f, 0f);
        // NOTE (2026-09-14): InfeedZoneCam/FillingZoneCam sit ~2.2-2.4m from their subjects
        // (roughly 2x CappingZoneCam's ~1.2m), so CappingZoneBuilder's original 0.035 fontSize
        // rendered as near-invisible specks here - confirmed via live screenshot before this
        // fix. Scaled up ~2.5x to stay legible at this camera distance.
        Vector2 size = new Vector2(0.55f, 0.10f);
        float fontSize = 0.085f;

        int count = 0;

        // =========================================================================
        // INFEED ZONE LABELS
        // Real object positions (execute_code, live scene, 2026-09-14):
        //   Bottle_Unscrambler top=0.97 center=(0.00,0.49,-3.14)
        //   Star_Wheel_Assembly top=0.98 pos=(-0.15,0.92,-2.35)
        //   Bottle_Present_Sensor top=1.05 pos=(-0.24,0.95,-1.90)
        //   Stopper_Cylinder top=1.05 pos=(0.23,0.97,-2.10)
        // Labels offset +0.20m in X (toward camera side) and sit just above each part's top.
        // =========================================================================
        if (TryFindWorld(root, "Bottle_Unscrambler", out Vector3 pUnscrambler))
        {
            CreateWorldLabel(labelsRoot.transform, "Label_Unscrambler",
                new Vector3(0.22f, 1.05f, pUnscrambler.z),
                faceCamera, "<b>BOTTLE UNSCRAMBLER</b>",
                fontSize, labelColor, TextAlignmentOptions.Center, size, fontAsset);
            count++;
        }
        if (TryFindWorld(root, "Star_Wheel_Assembly", out Vector3 pStarWheel))
        {
            CreateWorldLabel(labelsRoot.transform, "Label_StarWheel",
                new Vector3(0.22f, 1.10f, pStarWheel.z),
                faceCamera, "<b>STAR WHEEL</b>",
                fontSize, labelColor, TextAlignmentOptions.Center, size, fontAsset);
            count++;
        }
        if (TryFindWorld(root, "Bottle_Present_Sensor", out Vector3 pSensor))
        {
            CreateWorldLabel(labelsRoot.transform, "Label_InfeedSensor",
                new Vector3(0.22f, 1.18f, pSensor.z),
                faceCamera, "<b>BOTTLE PRESENT SENSOR</b>",
                fontSize, labelColor, TextAlignmentOptions.Center, size, fontAsset);
            count++;
        }
        if (TryFindWorld(root, "Stopper_Cylinder", out Vector3 pStopper))
        {
            CreateWorldLabel(labelsRoot.transform, "Label_Stopper",
                new Vector3(0.22f, 1.18f, pStopper.z),
                faceCamera, "<b>STOPPER CYLINDER</b>",
                fontSize, labelColor, TextAlignmentOptions.Center, size, fontAsset);
            count++;
        }

        // =========================================================================
        // FILLING ZONE LABELS
        // Real object positions (execute_code, live scene, 2026-09-14):
        //   FillingZone_ProductTank top=1.80 pos=(-0.70,0.00,-1.20)
        //   FillingZone_ProductPump top=0.87 pos=(-0.45,0.78,-1.20)
        //   FillingZone_FlowMeter top=1.60 pos=(-0.22,1.43,-1.20)
        //   FillingZone_AntiDripValve top=1.59 pos=(-0.09,1.43,-1.20)
        // =========================================================================
        if (TryFindWorld(root, "FillingZone_ProductTank", out Vector3 pTank))
        {
            CreateWorldLabel(labelsRoot.transform, "Label_ProductTank",
                new Vector3(pTank.x, 1.90f, pTank.z),
                faceCamera, "<b>PRODUCT TANK</b>",
                fontSize, labelColor, TextAlignmentOptions.Center, size, fontAsset);
            count++;
        }
        if (TryFindWorld(root, "FillingZone_ProductPump", out Vector3 pPump))
        {
            CreateWorldLabel(labelsRoot.transform, "Label_Pump",
                new Vector3(pPump.x, 0.98f, pPump.z),
                faceCamera, "<b>PUMP</b>",
                fontSize, labelColor, TextAlignmentOptions.Center, size, fontAsset);
            count++;
        }
        if (TryFindWorld(root, "FillingZone_FlowMeter", out Vector3 pFlow))
        {
            CreateWorldLabel(labelsRoot.transform, "Label_FlowMeter",
                new Vector3(pFlow.x, 1.70f, pFlow.z),
                faceCamera, "<b>FLOW METER</b>",
                fontSize, labelColor, TextAlignmentOptions.Center, size, fontAsset);
            count++;
        }
        if (TryFindWorld(root, "FillingZone_AntiDripValve", out Vector3 pValve))
        {
            CreateWorldLabel(labelsRoot.transform, "Label_AntiDripValve",
                new Vector3(pValve.x, 1.70f, pValve.z + 0.10f),
                faceCamera, "<b>ANTI-DRIP VALVE</b>",
                fontSize, labelColor, TextAlignmentOptions.Center, size, fontAsset);
            count++;
        }

        // =========================================================================
        // ITEM 2 CALLOUT: recipe size-change label near the nozzle/HMI, read live
        // from ChangeoverSequencer so it always matches whatever recipe pair is set
        // (currentRecipe/targetRecipe use Recipes[initialRecipeIndex]/[targetRecipeIndex]).
        // Placed above the fill station (FillStationZ = 0.60, matches NozzleAssembly_Z)
        // so it reads clearly during S1 (HMI request) and S9 (First Article Check) shots.
        // =========================================================================
        var sequencer = Object.FindAnyObjectByType<ChangeoverSequencer>();
        string recipeText = "<b>RECIPE CHANGE</b>";
        if (sequencer != null)
        {
            var recipes = ChangeoverSequencer.Recipes;
            int fromIdx = Mathf.Clamp(sequencer.initialRecipeIndex, 0, recipes.Length - 1);
            int toIdx = Mathf.Clamp(sequencer.targetRecipeIndex, 0, recipes.Length - 1);
            recipeText = "<b>" + recipes[fromIdx].name + " → " + recipes[toIdx].name + "</b>";
        }
        CreateWorldLabel(labelsRoot.transform, "Label_RecipeSizeCallout",
            new Vector3(0.30f, 1.75f, 0.60f),
            faceCamera, recipeText,
            0.11f, new Color(1f, 0.87f, 0.33f, 1f), TextAlignmentOptions.Center,
            new Vector2(0.70f, 0.14f), fontAsset);
        count++;

        EditorUtility.SetDirty(labelsRoot);
        EditorUtility.SetDirty(root);
        Debug.Log($"[ZoneLabelsBuilder] Created {count} world-space equipment labels under 'ZoneLabels' (Infeed x4, Filling x4, Recipe callout x1).");
    }

    private static bool TryFindWorld(GameObject root, string name, out Vector3 worldPos)
    {
        Transform t = FindDeep(root.transform, name);
        if (t == null)
        {
            worldPos = Vector3.zero;
            Debug.LogWarning($"[ZoneLabelsBuilder] Could not find '{name}' under '{root.name}' - skipping its label. Run the relevant zone builder first.");
            return false;
        }
        worldPos = t.position;
        return true;
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// Exact same world-space TMP label pattern as CappingZoneBuilder.CreateWorldLabel,
    /// duplicated here (not shared) to keep each Editor builder file independently
    /// self-contained per this project's existing convention (see AClipZoneCamerasBuilder,
    /// CappingZoneBuilder, EndOfLineBuilder - none of them share a common label helper).
    /// </summary>
    private static TextMeshPro CreateWorldLabel(Transform parent, string name, Vector3 localPos, Vector3 localRot,
        string text, float fontSize, Color color, TextAlignmentOptions align, Vector2 size, TMP_FontAsset font)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
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

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        return tmp;
    }
}
