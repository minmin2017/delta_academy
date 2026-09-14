using System;
using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// DeltaHMIDisplay - Drives the live screen display on the Delta DOP-100WS / 103WQ HMI panel.
/// Fully implements the 3 required visual modes for the 90s Changeover sequence:
///
/// Mode 1 (S0A_SelectRecipe, 0-12s):
///   - Proportional bottle silhouettes comparing 250ml (Ø55x125), 500ml (Ø70x155), 1000ml (Ø90x200)
///   - Operator touch selection of 500ml recipe
///   - Flashing confirmation banner ">>> RECIPE 500ml LOADED <<<"
///
/// Mode 2 (S0B_LoadParameters, 12-25s):
///   - 5 recipe parameters streaming into AS320T-B PLC registers (D100-D120) via Modbus TCP
///   - Live sequential [OK] checkmarks and animated progress bar (0% -> 100%)
///
/// Mode 3 (S1-S10, 25-89s):
///   - 10-phase Safe Changeover Checklist with real-time green checkmarks
/// </summary>
public class DeltaHMIDisplay : MonoBehaviour
{
    [Header("Dependencies")]
    public ChangeoverSequencer sequencer;
    public TextMeshPro textMesh;

    private readonly StringBuilder sb = new StringBuilder(512);

    private void Awake()
    {
        ValidateAndCacheReferences();
    }

    private void Start()
    {
        ValidateAndCacheReferences();
        AlignAndFitDisplay();
    }

    public void ValidateAndCacheReferences()
    {
        if (sequencer == null) sequencer = FindAnyObjectByType<ChangeoverSequencer>();

        if (textMesh == null)
        {
            GameObject tmpObj = GameObject.Find("HMI_Display_TextMeshPro");
            if (tmpObj != null) textMesh = tmpObj.GetComponent<TextMeshPro>();
            if (textMesh == null) textMesh = GetComponent<TextMeshPro>();
        }

        // Clean up any legacy BottleSilhouettesRoot objects in scene
        GameObject legacy = GameObject.Find("BottleSilhouettesRoot");
        if (legacy != null) Destroy(legacy);
        Transform childLegacy = transform.Find("BottleSilhouettesRoot");
        if (childLegacy != null) Destroy(childLegacy.gameObject);
        if (transform.parent != null)
        {
            Transform pLegacy = transform.parent.Find("BottleSilhouettesRoot");
            if (pLegacy != null) Destroy(pLegacy.gameObject);
        }
    }

    public void AlignAndFitDisplay()
    {
        if (textMesh != null)
        {
            textMesh.transform.position = new Vector3(-0.82f, 1.25f, 1.055f);
            textMesh.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            textMesh.rectTransform.sizeDelta = new Vector2(0.118f, 0.076f);
        }
    }

    private void LateUpdate()
    {
        if (sequencer == null || textMesh == null)
        {
            ValidateAndCacheReferences();
            if (sequencer == null || textMesh == null) return;
        }

        UpdateHmiText();
    }

    private void UpdateHmiText()
    {
        sb.Clear();

        ChangeoverSequencer.ChangeoverState state = sequencer.CurrentState;
        float progress = sequencer.StateProgress;
        float t = sequencer.stateTimer;

        int targetIdx = Mathf.Clamp(sequencer.targetRecipeIndex, 0, ChangeoverSequencer.Recipes.Length - 1);
        int initialIdx = Mathf.Clamp(sequencer.initialRecipeIndex, 0, ChangeoverSequencer.Recipes.Length - 1);
        ChangeoverSequencer.Recipe targetR = ChangeoverSequencer.Recipes[targetIdx];
        ChangeoverSequencer.Recipe initialR = ChangeoverSequencer.Recipes[initialIdx];

        // =====================================================================
        // MODE 1: S0A_SelectRecipe — Recipe Selection Screen
        // =====================================================================
        if (state == ChangeoverSequencer.ChangeoverState.S0A_SelectRecipe)
        {
            textMesh.fontSize = 0.024f;
            textMesh.alignment = TextAlignmentOptions.Top;

            sb.AppendLine("<b><color=#00D8FF>DELTA DOP-100WS</color></b> | <color=#00FFCC>RECIPE SELECT</color>");
            sb.AppendLine("<size=65%><color=#8899AA>KMITL DELTA ACADEMY · PACKAGING DEMO</color></size>");
            sb.AppendLine("<color=#335577>───────────────────────────────────</color>");
            sb.AppendLine("<b>  [250ml]          <color=#00FF88>[500ml] ★</color>         [1000ml]</b>");
            sb.AppendLine(" <size=70%><color=#88AACC>Ø55×125mm</color>        <color=#00FF88>Ø70×155mm</color>        <color=#88AACC>Ø90×200mm</color></size>");
            sb.AppendLine();
            sb.AppendLine("    <color=#557799>█</color>                 <color=#00FF88>█</color>                 <color=#557799>██</color>");
            sb.AppendLine("   <color=#557799>███</color>               <color=#00FF88>████</color>              <color=#557799>██████</color>");
            sb.AppendLine("   <color=#557799>███</color>               <color=#00FF88>████</color>              <color=#557799>██████</color>");
            sb.AppendLine("   <color=#557799>███</color>               <color=#00FF88>████</color>              <color=#557799>██████</color>");
            sb.AppendLine("                     <color=#00FF88>████</color>              <color=#557799>██████</color>");
            sb.AppendLine("                                       <color=#557799>██████</color>");
            sb.AppendLine("<color=#335577>───────────────────────────────────</color>");

            if (t < 6.5f)
            {
                bool blink = (Mathf.Sin(Time.time * 6f) > 0);
                string cursor = blink ? "<color=#00FFCC>▲ TOUCH SELECT: RECIPE 2 (500ml)</color>" : "<color=#335577>▲ TOUCH SELECT: RECIPE 2 (500ml)</color>";
                sb.AppendLine($"<size=75%><b>{cursor}</b></size>");
                sb.AppendLine("<size=60%><color=#FFDD55>PRESS TO LOAD 5-PARAMETER RECIPE PROFILE</color></size>");
            }
            else
            {
                bool pulse = (Mathf.Sin(Time.time * 8f) > 0);
                string flashCol = pulse ? "#00FF66" : "#00D8FF";
                sb.AppendLine($"<b><size=85%><color={flashCol}>>>> RECIPE {targetR.name} LOADED <<<</color></size></b>");
                sb.AppendLine("<size=60%><color=#E0E8F0>AS320T-B PLC REGISTERS D100-D120 SYNC READY</color></size>");
            }

            textMesh.text = sb.ToString();
            return;
        }

        // =====================================================================
        // MODE 2: S0B_LoadParameters — 5 Parameters Streaming Into PLC
        // =====================================================================
        if (state == ChangeoverSequencer.ChangeoverState.S0B_LoadParameters)
        {
            textMesh.fontSize = 0.020f;
            textMesh.alignment = TextAlignmentOptions.TopLeft;

            sb.AppendLine("<b><color=#00D8FF>DELTA DOP-100WS</color></b> | <color=#00FF88>PARAM BUFFER</color>");
            sb.AppendLine("<size=70%><color=#8899AA>PLC AS320T-B REGISTERS D100-D120 [MODBUS TCP]</color></size>");
            sb.AppendLine("<color=#335577>───────────────────────────────────</color>");

            float railGapMm = targetR.railGap * 1000f;
            float nozzleHtMm = targetR.nozzleClearHeight * 1000f;

            bool p1 = progress >= 0.15f;
            bool p2 = progress >= 0.35f;
            bool p3 = progress >= 0.55f;
            bool p4 = progress >= 0.75f;
            bool p5 = progress >= 0.92f;

            sb.AppendLine(p1 ? $"<color=#00FF66>[OK] 1. RAIL GAP   (ASD-A3 X) : {railGapMm:F1} mm</color>" : "<color=#445566>[..] 1. RAIL GAP   (ASD-A3 X) : STREAMING...</color>");
            sb.AppendLine(p2 ? $"<color=#00FF66>[OK] 2. NOZZLE HT  (ASD-A3 Z) : {nozzleHtMm:F1} mm</color>" : "<color=#445566>[..] 2. NOZZLE HT  (ASD-A3 Z) : STREAMING...</color>");
            sb.AppendLine(p3 ? $"<color=#00FF66>[OK] 3. BELT SPEED (MS300)    : {targetR.beltSpeed:F2} m/s</color>" : "<color=#445566>[..] 3. BELT SPEED (MS300)    : STREAMING...</color>");
            sb.AppendLine(p4 ? $"<color=#00FF66>[OK] 4. FILL VOL   (PUMP)     : {targetR.fillVolume:F0} ml</color>" : "<color=#445566>[..] 4. FILL VOL   (PUMP)     : STREAMING...</color>");
            sb.AppendLine(p5 ? $"<color=#00FF66>[OK] 5. FILL TIME  (PROFILE)  : {targetR.fillDuration:F1} s</color>" : "<color=#445566>[..] 5. FILL TIME  (PROFILE)  : STREAMING...</color>");

            sb.AppendLine("<color=#335577>───────────────────────────────────</color>");

            int totalBars = 20;
            int fillBars = Mathf.Clamp(Mathf.RoundToInt(progress * totalBars), 0, totalBars);
            string barStr = new string('█', fillBars) + new string('░', totalBars - fillBars);
            int pct = Mathf.Clamp(Mathf.RoundToInt(progress * 100f), 0, 100);

            sb.AppendLine($"BUFFER: [{barStr}] {pct}%");
            if (p5)
            {
                sb.AppendLine("<color=#00FF88>ALL REGISTERS WRITTEN & VERIFIED [OK]</color>");
            }
            else
            {
                sb.AppendLine("<color=#FFCC00>BUFFERING RECIPE PARAMETERS INTO AS320T-B...</color>");
            }

            textMesh.text = sb.ToString();
            return;
        }

        // =====================================================================
        // MODE 3: S1-S10 — Changeover Operational Checklist
        // =====================================================================
        textMesh.fontSize = 0.019f;
        textMesh.alignment = TextAlignmentOptions.TopLeft;

        sb.AppendLine("<b><color=#00D8FF>DELTA DOP-100WS</color></b> | <color=#FFDD55>CHECKLIST</color>");
        sb.AppendLine($"<size=70%><color=#8899AA>RECIPE: {initialR.name} -> {targetR.name} | STEP: {sequencer.CurrentStateName}</color></size>");
        sb.AppendLine("<color=#335577>───────────────────────────────────</color>");

        string Check(bool ok, string label) => ok ? $"<color=#00FF88>[OK] {label}</color>" : $"<color=#445566>[..] {label}</color>";

        sb.AppendLine(Check(state >= ChangeoverSequencer.ChangeoverState.S2_CompleteInFlightFill, "1. INFEED STOPPED"));
        sb.AppendLine(Check(state >= ChangeoverSequencer.ChangeoverState.S4_ClearBottlesFromZone, "2. LAST BOTTLE DISPENSED"));
        sb.AppendLine(Check(state >= ChangeoverSequencer.ChangeoverState.S6_RetractNozzleToHome,  "3. ZONE CLEARED & BELT STOPPED"));
        sb.AppendLine(Check(state >= ChangeoverSequencer.ChangeoverState.S7_AdjustRailWidth,      "4. NOZZLE RETRACT TO HOME (1250mm)"));
        sb.AppendLine(Check(state >= ChangeoverSequencer.ChangeoverState.S8_ConfirmInPosition,    $"5. RAIL GAP ADJUSTED ({targetR.railGap*1000f:F1}mm)"));
        sb.AppendLine(Check(state >= ChangeoverSequencer.ChangeoverState.S9_FirstArticleCheck,    $"6. VFD SPEED SYNC ({targetR.beltSpeed:F2} m/s)"));
        sb.AppendLine(Check(state >= ChangeoverSequencer.ChangeoverState.S10_ResumeProduction,   "7. FIRST ARTICLE TEST VERIFIED"));

        sb.AppendLine("<color=#335577>───────────────────────────────────</color>");
        if (state == ChangeoverSequencer.ChangeoverState.S10_ResumeProduction)
        {
            sb.AppendLine("<b><color=#00FF88>>>> PRODUCTION RESUMED @ FULL SPEED <<<</color></b>");
        }
        else
        {
            sb.AppendLine($"<color=#00E5FF>EXECUTING: {sequencer.CurrentStateName}</color>");
        }

        textMesh.text = sb.ToString();
    }
}
