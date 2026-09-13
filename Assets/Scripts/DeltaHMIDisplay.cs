using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// Runtime display driver for Delta DOP-100WS HMI screen.
/// Renders active recipe status, human-readable PLC state, progress, and safe-changeover checklist.
/// Emphasizes PLC AS320T-B control mediation (HMI requests recipe, PLC executes motion).
/// </summary>
public class DeltaHMIDisplay : MonoBehaviour
{
    [Header("References")]
    public ChangeoverSequencer sequencer;
    public TextMeshPro textMesh;

    private readonly StringBuilder sb = new StringBuilder(512);

    private void Awake()
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshPro>();
        }
    }

    private void Start()
    {
        if (sequencer == null)
        {
            sequencer = FindAnyObjectByType<ChangeoverSequencer>();
        }

        if (textMesh != null && textMesh.font == null)
        {
            TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (defaultFont != null)
            {
                textMesh.font = defaultFont;
            }
        }
    }

    private void LateUpdate()
    {
        if (sequencer == null)
        {
            sequencer = FindAnyObjectByType<ChangeoverSequencer>();
            if (sequencer == null) return;
        }

        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshPro>();
            if (textMesh == null) return;
        }

        UpdateHmiText();
    }

    private void UpdateHmiText()
    {
        sb.Clear();

        ChangeoverSequencer.ChangeoverState state = sequencer.CurrentState;
        int stateIdx = (int)state;
        float progress = sequencer.StateProgress;

        string targetRecipeName = (sequencer.targetRecipeIndex >= 0 && sequencer.targetRecipeIndex < ChangeoverSequencer.Recipes.Length)
            ? ChangeoverSequencer.Recipes[sequencer.targetRecipeIndex].name
            : "1000ml";

        string initialRecipeName = (sequencer.initialRecipeIndex >= 0 && sequencer.initialRecipeIndex < ChangeoverSequencer.Recipes.Length)
            ? ChangeoverSequencer.Recipes[sequencer.initialRecipeIndex].name
            : "500ml";

        // Header
        sb.AppendLine("<b><color=#00D8FF>DELTA DOP-100WS</color></b> | <color=#AAAAAA>RECIPE REQUEST</color>");
        sb.AppendLine("<size=80%><color=#8899AA>PLC AS320T-B SEQUENCE CONTROLLER</color></size>");
        sb.AppendLine("<color=#335577>────────────────────────────────────</color>");

        // Recipe & State Status
        sb.Append("<color=#E0E8F0>RECIPE:</color> <color=#FFDD55><b>").Append(initialRecipeName).Append(" → ").Append(targetRecipeName).Append("</b></color>");
        sb.Append("  <color=#88AACC>[PHASE ").Append(stateIdx + 1).Append("/10]</color>\n");

        sb.Append("<color=#E0E8F0>STATUS:</color> <color=#FFFFFF><b>").Append(GetHumanReadableState(state)).Append("</b></color>\n");
        sb.Append("<color=#E0E8F0>STEP PROG:</color> <color=#66FFBB>").Append((progress * 100f).ToString("F0")).Append("%</color>  ");
        sb.Append(GetProgressBar(progress, 14)).Append("\n");

        sb.AppendLine("<color=#335577>────────────────────────────────────</color>");
        sb.AppendLine("<b><color=#A0B8D0>SAFE CHANGEOVER CHECKLIST:</color></b>");

        // Checklist Items
        // 1. Infeed stopped
        AppendChecklistItem("Infeed stopped", stateIdx >= 1, stateIdx == 0);
        // 2. Valve/pump safe
        AppendChecklistItem("Valve/pump safe", stateIdx >= 3, stateIdx == 1 || stateIdx == 2);
        // 3. Zone cleared
        AppendChecklistItem("Zone cleared", stateIdx >= 4, stateIdx == 3);
        // 4. Z axis home
        AppendChecklistItem("Z axis home (interlock)", stateIdx >= 6, stateIdx == 5);
        // 5. X axis in position
        AppendChecklistItem("X axis in position", stateIdx >= 7, stateIdx == 6);
        // 6. Guard/sensor permissive
        AppendChecklistItem("Guard/sensor permissive", stateIdx >= 7, false);
        // 7. Flow totalizer reset
        AppendChecklistItem("Flow totalizer reset", stateIdx >= 7, false);
        // 8. Drives ready
        AppendChecklistItem("Drives ready", stateIdx >= 8, stateIdx == 7);
        // 9. First article passed
        AppendChecklistItem("First article passed", stateIdx >= 9, stateIdx == 8);

        textMesh.text = sb.ToString();
    }

    private void AppendChecklistItem(string label, bool completed, bool inProgress)
    {
        if (completed)
        {
            sb.Append(" <color=#00FF66>[✓] ").Append(label).Append("</color>\n");
        }
        else if (inProgress)
        {
            sb.Append(" <color=#FFCC00>[▶] ").Append(label).Append("...</color>\n");
        }
        else
        {
            sb.Append(" <color=#667788>[ ] ").Append(label).Append("</color>\n");
        }
    }

    private static string GetProgressBar(float norm, int length)
    {
        int filled = Mathf.RoundToInt(norm * length);
        filled = Mathf.Clamp(filled, 0, length);
        return $"[<color=#00FF88>{new string('|', filled)}</color><color=#445566>{new string('.', length - filled)}</color>]";
    }

    private static string GetHumanReadableState(ChangeoverSequencer.ChangeoverState state)
    {
        switch (state)
        {
            case ChangeoverSequencer.ChangeoverState.S1_StopInfeed:
                return "S1: Stop Infeed (Admitting Halted)";
            case ChangeoverSequencer.ChangeoverState.S2_CompleteInFlightFill:
                return "S2: Complete In-Flight Fill Cycle";
            case ChangeoverSequencer.ChangeoverState.S3_CloseValveStopPump:
                return "S3: Close Dispense Valve & Stop Pump";
            case ChangeoverSequencer.ChangeoverState.S4_ClearBottlesFromZone:
                return "S4: Evacuate Conveyor Working Zone";
            case ChangeoverSequencer.ChangeoverState.S5_StopBelt:
                return "S5: Conveyor Standstill & S-Curve Stop";
            case ChangeoverSequencer.ChangeoverState.S6_RetractNozzleToHome:
                return "S6: Retract Z Nozzle to Home Height";
            case ChangeoverSequencer.ChangeoverState.S7_AdjustRailWidth:
                return "S7: Adjust X Guide Rails (Servo S-Curve)";
            case ChangeoverSequencer.ChangeoverState.S8_ConfirmInPosition:
                return "S8: Verify In-Position & Drive Ready";
            case ChangeoverSequencer.ChangeoverState.S9_FirstArticleCheck:
                return "S9: First-Article Test Bottle Fill";
            case ChangeoverSequencer.ChangeoverState.S10_ResumeProduction:
                return "S10: Full-Speed Steady Production";
            default:
                return state.ToString();
        }
    }
}
