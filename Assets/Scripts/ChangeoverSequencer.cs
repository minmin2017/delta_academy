using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ChangeoverSequencer - Drives the recipe-based changeover sequence animation for KMITL Delta Academy.
/// Demonstrates Delta AS320T-B PLC, DOP-100WS HMI, ASD-A3 servos (X rail width, Z nozzle height),
/// and MS300 VFD conveyor control.
/// </summary>
public class ChangeoverSequencer : MonoBehaviour
{
    // =========================================================================
    // PART 1 — THE RECIPE TABLE
    // PLACEHOLDER - awaiting real values from Min
    // Put every number in this ONE table; nothing downstream may hardcode a value.
    // =========================================================================
    [System.Serializable]
    public struct Recipe
    {
        public string name;               // e.g. "250ml", "500ml", "1000ml"
        public float bottleDia;           // meters
        public float bottleHeight;        // meters
        public float railGap;             // meters (guide rail opening width)
        public float nozzleClearHeight;   // meters above belt surface (belt top = 0.90m)
        public float fillVolume;          // ml
        public float beltSpeed;           // m/s
        public float fillDuration;        // fill cycle time in seconds
    }

    // PLACEHOLDER - awaiting real values from Min
    public static readonly Recipe[] Recipes = new Recipe[]
    {
        // PLACEHOLDER - awaiting real values from Min
        new Recipe
        {
            name = "250ml",
            bottleDia = 0.055f,
            bottleHeight = 0.125f,
            railGap = 0.058f,
            nozzleClearHeight = 0.165f,
            fillVolume = 250f,
            beltSpeed = 0.25f,
            fillDuration = 1.8f
        },
        // PLACEHOLDER - awaiting real values from Min
        new Recipe
        {
            name = "500ml",
            bottleDia = 0.070f,
            bottleHeight = 0.155f,
            railGap = 0.073f,
            nozzleClearHeight = 0.195f,
            fillVolume = 500f,
            beltSpeed = 0.20f,
            fillDuration = 2.2f
        },
        // PLACEHOLDER - awaiting real values from Min
        new Recipe
        {
            name = "1000ml",
            bottleDia = 0.090f,
            bottleHeight = 0.200f,
            railGap = 0.093f,
            nozzleClearHeight = 0.240f,
            fillVolume = 1000f,
            beltSpeed = 0.15f,
            fillDuration = 3.0f
        }
    };

    // =========================================================================
    // PART 2 — THE CHANGEOVER SEQUENCE (STATE MACHINE ENUM)
    // =========================================================================
    public enum ChangeoverState
    {
        S0A_SelectRecipe,        // Pre-roll Step 2: HMI Recipe Selection & confirmation
        S0B_LoadParameters,      // Pre-roll Step 3: Loading 5 parameters animation
        S1_StopInfeed,           // Stop admitting new bottles
        S2_CompleteInFlightFill, // Finish the fill already in progress
        S3_CloseValveStopPump,   // Close fill valve & stop pump
        S4_ClearBottlesFromZone, // Run belt until collision zone is cleared
        S5_StopBelt,             // Bring conveyor belt to standstill
        S6_RetractNozzleToHome,  // Z axis returns to home height (1.25m) FIRST
        S7_AdjustRailWidth,      // X axis moves symmetrically to new railGap
        S8_ConfirmInPosition,    // Dwell & verify drives in-position / ready
        S9_FirstArticleCheck,    // Admit 1 test bottle, fill, and verify
        S10_ResumeProduction     // Full speed production on new recipe
    }

    [Header("Changeover Configuration")]
    [Tooltip("Index into Recipes table for initial production (default 0 = 250ml)")]
    public int initialRecipeIndex = 0;

    [Tooltip("Index into Recipes table for changeover target (default 1 = 500ml) - matches the B_Changeover.mp4 shot list (250ml -> 500ml)")]
    public int targetRecipeIndex = 1;

    [Tooltip("Loop the entire sequence continuously for demo video recording")]
    public bool loopSequence = true;

    [Header("Live State (Visible for On-Screen Captions)")]
    [SerializeField] private ChangeoverState currentState = ChangeoverState.S1_StopInfeed;
    public string currentStateName = "S1_StopInfeed";
    public float stateTimer = 0f;
    public float stateDuration = 3.0f;
    public float accumulatedTime = 0f;

    public ChangeoverState CurrentState => currentState;
    public string CurrentStateName => currentState.ToString();
    public float StateProgress => (stateDuration > 0f) ? Mathf.Clamp01(stateTimer / stateDuration) : 1f;

    [Header("Telemetry (Live Read-Only in Millimetres)")]
    /// <summary>
    /// Current live guide rail opening gap in millimetres, computed from GuideRailAssembly_X children transforms.
    /// </summary>
    public float CurrentRailGapMm
    {
        get
        {
            if (railLeft != null && railRight != null)
            {
                return Mathf.Abs(railRight.localPosition.x - railLeft.localPosition.x) * 1000f;
            }
            if (guideRailAssembly != null)
            {
                Transform l = guideRailAssembly.Find("GuideRail_Left_30mm");
                Transform r = guideRailAssembly.Find("GuideRail_Right_30mm");
                if (l != null && r != null)
                {
                    return Mathf.Abs(r.localPosition.x - l.localPosition.x) * 1000f;
                }
            }
            return currentRecipe.railGap * 1000f;
        }
    }

    /// <summary>
    /// Current live nozzle height in millimetres - clearance ABOVE the belt surface (matches the
    /// real Delta spec sheet's "Nozzle Height" parameter, ~165-240mm depending on recipe), not the
    /// nozzle's raw world Y. Bug found 2026-09-14 via on-screen readout showing "1123.7 mm" in the
    /// rendered B_Changeover clip - was returning nozzleAssembly.position.y directly (its absolute
    /// world-space height, ~1.1m, since the assembly sits high up in the scene), not the clearance
    /// above BeltSurfaceY that the label and the real spec both mean by "nozzle height".
    /// </summary>
    public float CurrentNozzleHeightMm
    {
        get
        {
            if (nozzleAssembly != null)
            {
                return (nozzleAssembly.position.y - BeltSurfaceY) * 1000f;
            }
            return currentRecipe.nozzleClearHeight * 1000f;
        }
    }

    public float LiveRailGapMm => CurrentRailGapMm;
    public float LiveNozzleHeightMm => CurrentNozzleHeightMm;

    [Header("Scene References (Assigned by Attach Sequencer)")]
    public Transform guideRailAssembly;
    public Transform nozzleAssembly;
    public Renderer beltRenderer;
    public GameObject initialBottle;

    [Header("Materials (Assigned by Attach Sequencer)")]
    public Material petMaterial;
    public Material liquidMaterial;
    public Material capMaterial;

    // Internal constants matching scene geometry
    private const float BeltSurfaceY = 0.90f;
    private const float NozzleHomeY = 1.25f; // 0.90 + 0.35m home height above belt
    private const float FillStationZ = 0.60f;
    private const float InitialBottleStartZ = 0.0f; // Position from CellBuilder line 404
    private const float FirstArticleSpawnZ = -0.60f; // Upstream spawn point for test bottle
    private const float InfeedSpawnZ = -1.40f;
    private const float OutfeedExitZ = 1.55f;

    // Physical dive-filling constants (meters)
    private const float DiveStartAboveBelt = 0.025f; // 25 mm above conveyor belt surface (near bottle bottom)
    private const float SubmergedOffset = 0.010f;    // 10 mm below rising liquid surface during dispensing

    // Rail child transforms
    private Transform railLeft;
    private Transform railRight;

    // Runtime state tracking
    private Recipe currentRecipe;
    private Recipe targetRecipe;
    public Recipe CurrentRecipe => currentRecipe;
    public Recipe TargetRecipe => targetRecipe;
    private float currentBeltSpeed = 0f;
    private float beltUvOffset = 0f;
    private bool s6CompleteConfirmed = false;

    // S2 timing sub-phases (in-flight bottle dive & fill completion)
    private float s2_tDive = 1.0f;
    private float s2_tFill = 1.5f;

    // S9 timing sub-phases (derived dynamically from distance and recipe speed)
    private float s9_tTravel = 0f;
    private float s9_tLower = 1.5f;
    private float s9_tFill = 2.5f;
    private float s9_tRaise = 1.5f;
    private float s9_tExit = 3.0f;

    // Tracked bottles in the scene
    public class BottleTracker : MonoBehaviour
    {
        public Transform liquidTransform;
        public Renderer liquidRenderer;
        public float maxLiquidHeight;
        public float liquidDiameter;
        public float fillFraction;
        public Recipe recipe;
        public bool isFirstArticle;
        public bool isFilled;

        public void SetFill(float frac)
        {
            float newFill = Mathf.Clamp01(frac);
            // Once filled, liquid level must never be reduced
            if (isFilled && newFill < fillFraction)
            {
                return;
            }

            fillFraction = newFill;
            if (liquidTransform != null)
            {
                if (liquidRenderer != null)
                {
                    liquidRenderer.enabled = fillFraction > 0.001f;
                }
                float curH = Mathf.Max(0.0002f, maxLiquidHeight * fillFraction);
                // Grown strictly from base: pivot offset at bottom (0.002m)
                liquidTransform.localPosition = new Vector3(0f, 0.002f + curH * 0.5f, 0f);
                liquidTransform.localScale = new Vector3(liquidDiameter, curH * 0.5f, liquidDiameter);
            }
        }
    }

    private readonly List<BottleTracker> activeBottles = new List<BottleTracker>();
    private BottleTracker inFlightBottle;
    private BottleTracker firstArticleBottle;

    private void Awake()
    {
        ValidateAndCacheReferences();
    }

    private void Start()
    {
        ValidateAndCacheReferences();
        SetupInitialState();
    }

    /// <summary>
    /// Finds and caches scene references if not already wired by the Attach Sequencer menu item.
    /// </summary>
    public void ValidateAndCacheReferences()
    {
        if (guideRailAssembly == null)
        {
            GameObject obj = GameObject.Find("GuideRailAssembly_X");
            if (obj != null) guideRailAssembly = obj.transform;
        }

        if (guideRailAssembly != null)
        {
            Transform l = guideRailAssembly.Find("GuideRail_Left_30mm");
            Transform r = guideRailAssembly.Find("GuideRail_Right_30mm");
            railLeft = l != null ? l : (guideRailAssembly.childCount > 0 ? guideRailAssembly.GetChild(0) : null);
            railRight = r != null ? r : (guideRailAssembly.childCount > 1 ? guideRailAssembly.GetChild(1) : null);
        }

        if (nozzleAssembly == null)
        {
            GameObject obj = GameObject.Find("NozzleAssembly_Z");
            if (obj != null) nozzleAssembly = obj.transform;
        }

        if (beltRenderer == null)
        {
            GameObject obj = GameObject.Find("Belt_DarkRubber_400mm");
            if (obj != null) beltRenderer = obj.GetComponent<Renderer>();
        }

        if (initialBottle == null)
        {
            initialBottle = GameObject.Find("Bottle_500ml_PET");
        }
        HideBottleCap(initialBottle);

        EnsureBeltTexture();
    }

    /// <summary>
    /// Ensures the belt material has visible texture coordinates so UV scrolling is visibly apparent.
    /// </summary>
    private void EnsureBeltTexture()
    {
        if (beltRenderer == null) return;
        Material mat = Application.isPlaying ? beltRenderer.material : beltRenderer.sharedMaterial;
        if (mat != null && mat.mainTexture == null)
        {
            Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false)
            {
                name = "Procedural_ConveyorTreads",
                wrapMode = TextureWrapMode.Repeat
            };
            for (int y = 0; y < 64; y++)
            {
                float rib = (y % 8 < 2) ? 0.82f : 1.0f;
                Color col = new Color(0.06f * rib, 0.06f * rib, 0.07f * rib, 1f);
                for (int x = 0; x < 64; x++)
                {
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            mat.mainTexture = tex;
            mat.SetTextureScale("_BaseMap", new Vector2(1f, 15f));
        }
    }

    private void SetupInitialState()
    {
        activeBottles.Clear();

        initialRecipeIndex = Mathf.Clamp(initialRecipeIndex, 0, Recipes.Length - 1);
        targetRecipeIndex = Mathf.Clamp(targetRecipeIndex, 0, Recipes.Length - 1);

        currentRecipe = Recipes[initialRecipeIndex];
        targetRecipe = Recipes[targetRecipeIndex];

        // Apply initial geometry from recipe table
        SetRailGap(currentRecipe.railGap);
        SetNozzleWorldY(GetRecipeClearY(currentRecipe));
        currentBeltSpeed = currentRecipe.beltSpeed;

        // Adopt initial scene bottle if present
        if (initialBottle != null)
        {
            initialBottle.SetActive(true);
            HideBottleCap(initialBottle);
            initialBottle.transform.position = new Vector3(0f, BeltSurfaceY, InitialBottleStartZ);
            BottleTracker tracker = initialBottle.GetComponent<BottleTracker>();
            if (tracker == null)
            {
                tracker = initialBottle.AddComponent<BottleTracker>();
                Transform liq = initialBottle.transform.Find("Liquid_Shampoo_Volume");
                if (liq != null)
                {
                    tracker.liquidTransform = liq;
                    tracker.liquidRenderer = liq.GetComponent<Renderer>();
                }
                tracker.maxLiquidHeight = currentRecipe.bottleHeight * 0.70f;
                tracker.liquidDiameter = currentRecipe.bottleDia * 0.94f;
                tracker.recipe = currentRecipe;
            }
            tracker.isFilled = false;
            tracker.isFirstArticle = false;
            tracker.SetFill(0.40f); // In-flight bottle partially filled
            inFlightBottle = tracker;
            activeBottles.Add(tracker);
        }

        s6CompleteConfirmed = false;
        EnterState(ChangeoverState.S0A_SelectRecipe);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        accumulatedTime += dt;
        stateTimer += dt;

        UpdateState(dt);
        UpdateConveyorMotion(dt);
        UpdateBottlePositions(dt); // ALL bottle translation along +Z occurs strictly here

        currentStateName = currentState.ToString();

        // Sequential state machine transition check
        if (stateTimer >= stateDuration)
        {
            AdvanceToNextState();
        }
    }

    private void EnterState(ChangeoverState nextState)
    {
        currentState = nextState;
        currentStateName = nextState.ToString();
        stateTimer = 0f;

        switch (currentState)
        {
            case ChangeoverState.S0A_SelectRecipe:
                // Pre-roll Step 2: HMI Recipe Selection (12.0s total: 0-6.5s selection, 6.5-12.0s confirmation)
                stateDuration = 12.0f;
                currentBeltSpeed = 0f;
                SetNozzleWorldY(GetRecipeClearY(currentRecipe));
                SetRailGap(currentRecipe.railGap);
                break;

            case ChangeoverState.S0B_LoadParameters:
                // Pre-roll Step 3: Loading 5 recipe parameters sequentially into AS320T-B PLC registers (13.0s)
                stateDuration = 13.0f;
                currentBeltSpeed = 0f;
                SetNozzleWorldY(GetRecipeClearY(currentRecipe));
                SetRailGap(currentRecipe.railGap);
                break;

            case ChangeoverState.S1_StopInfeed:
                // S1: Derive exact duration to carry inFlightBottle from InitialBottleStartZ (0.0) to FillStationZ (0.60)
                // at single belt speed without double speed reliance
                float s1Dist = Mathf.Max(0.01f, FillStationZ - InitialBottleStartZ);
                stateDuration = s1Dist / Mathf.Max(0.05f, currentRecipe.beltSpeed);
                currentBeltSpeed = currentRecipe.beltSpeed;
                SetNozzleWorldY(GetRecipeClearY(currentRecipe));
                break;

            case ChangeoverState.S2_CompleteInFlightFill:
                // Conveyor stops while completing in-flight bottle fill with true dive-filling motion
                s2_tDive = 1.0f;
                s2_tFill = Mathf.Max(1.5f, currentRecipe.fillDuration * 0.60f);
                stateDuration = s2_tDive + s2_tFill;
                currentBeltSpeed = 0f;
                if (inFlightBottle != null)
                {
                    inFlightBottle.transform.position = new Vector3(0f, BeltSurfaceY, FillStationZ);
                }
                break;

            case ChangeoverState.S3_CloseValveStopPump:
                // Close valve, stop pump dwell, and visibly retract nozzle from dive height to recipe-clear height
                stateDuration = 1.2f;
                currentBeltSpeed = 0f;
                if (inFlightBottle != null)
                {
                    inFlightBottle.transform.position = new Vector3(0f, BeltSurfaceY, FillStationZ);
                    inFlightBottle.SetFill(1.0f);
                    inFlightBottle.isFilled = true;
                }
                break;

            case ChangeoverState.S4_ClearBottlesFromZone:
                // Run belt to clear in-flight bottle past downstream collision zone.
                // Safety invariant: nozzle is verified at recipe-clear height before conveyor movement.
                stateDuration = 3.5f;
                currentBeltSpeed = currentRecipe.beltSpeed;
                SetNozzleWorldY(GetRecipeClearY(currentRecipe));
                break;

            case ChangeoverState.S5_StopBelt:
                // Decelerate belt to zero; enforce nozzle at recipe-clear height
                stateDuration = 0.8f;
                SetNozzleWorldY(GetRecipeClearY(currentRecipe));
                break;

            case ChangeoverState.S6_RetractNozzleToHome:
                // Z axis servo returns from recipe-clear height to Home height (1.25m) FIRST. Rails remain locked.
                stateDuration = 6.5f;
                currentBeltSpeed = 0f;
                s6CompleteConfirmed = false;
                break;

            case ChangeoverState.S7_AdjustRailWidth:
                // X axis servo adjusts rails symmetrically to target railGap.
                // HARDWARE INTERLOCK ENFORCEMENT:
                stateDuration = 8.5f;
                currentBeltSpeed = 0f;

                if (nozzleAssembly == null)
                {
                    Debug.LogError("[Delta Interlock Error] S7 Rail Motion Blocked! NozzleAssembly_Z reference is null.");
                    enabled = false;
                    return;
                }
                if (guideRailAssembly == null)
                {
                    Debug.LogError("[Delta Interlock Error] S7 Rail Motion Blocked! GuideRailAssembly_X reference is null.");
                    enabled = false;
                    return;
                }
                if (!s6CompleteConfirmed || nozzleAssembly.position.y < (NozzleHomeY - 0.005f))
                {
                    Debug.LogError($"[Delta Interlock Violation] S7 Rail Motion Blocked! Nozzle is not confirmed at home height (1.25m). Current Y: {nozzleAssembly.position.y:F3}m, confirmed: {s6CompleteConfirmed}");
                    enabled = false;
                    return;
                }
                break;

            case ChangeoverState.S8_ConfirmInPosition:
                // Both axes dwell & verify drive-ready + VFD speed preset via Modbus RTU
                stateDuration = 5.0f;
                currentBeltSpeed = 0f;
                break;

            case ChangeoverState.S9_FirstArticleCheck:
                // S9: Spawn exactly ONE test bottle with target recipe dimensions at FirstArticleSpawnZ.
                // Dynamically derive travel phase duration from physical distance and target belt speed.
                float s9Dist = Mathf.Max(0.01f, FillStationZ - FirstArticleSpawnZ);
                s9_tTravel = s9Dist / Mathf.Max(0.05f, targetRecipe.beltSpeed);
                s9_tLower = 1.5f;
                s9_tFill = targetRecipe.fillDuration;
                s9_tRaise = 1.5f;
                s9_tExit = 3.0f;
                stateDuration = s9_tTravel + s9_tLower + s9_tFill + s9_tRaise + s9_tExit;

                currentBeltSpeed = targetRecipe.beltSpeed;
                GameObject testObj = CreateBottle(targetRecipe, new Vector3(0f, BeltSurfaceY, FirstArticleSpawnZ), 0f);
                firstArticleBottle = testObj.GetComponent<BottleTracker>();
                firstArticleBottle.isFirstArticle = true;
                firstArticleBottle.isFilled = false;
                activeBottles.Add(firstArticleBottle);
                break;

            case ChangeoverState.S10_ResumeProduction:
                currentRecipe = targetRecipe;
                currentBeltSpeed = targetRecipe.beltSpeed;
                stateDuration = 22.0f;
                productionSpawnTimer = 0f;
                break;
        }
    }

    private void UpdateState(float dt)
    {
        float norm = StateProgress;
        float eased = SCurve(norm);

        switch (currentState)
        {
            case ChangeoverState.S0A_SelectRecipe:
            case ChangeoverState.S0B_LoadParameters:
                currentBeltSpeed = 0f;
                SetNozzleWorldY(GetRecipeClearY(currentRecipe));
                SetRailGap(currentRecipe.railGap);
                break;

            case ChangeoverState.S1_StopInfeed:
                // In-flight bottle advances ONLY via UpdateBottlePositions at currentRecipe.beltSpeed
                SetNozzleWorldY(GetRecipeClearY(currentRecipe));
                break;

            case ChangeoverState.S2_CompleteInFlightFill:
                // Stopped in-flight bottle receives nozzle down from recipe-clear, then completes fill 40% -> 100% as nozzle rises with liquid
                currentBeltSpeed = 0f;
                if (inFlightBottle != null)
                {
                    inFlightBottle.transform.position = new Vector3(0f, BeltSurfaceY, FillStationZ);
                    float clearY = GetRecipeClearY(currentRecipe);
                    float dive40Y = GetDiveFollowY(currentRecipe, 0.40f);

                    if (stateTimer < s2_tDive)
                    {
                        // Sub-phase 1: Nozzle servo lowers from recipe-clear height into bottle down to initial dive level
                        float diveT = SCurve(stateTimer / s2_tDive);
                        SetNozzleWorldY(Mathf.Lerp(clearY, dive40Y, diveT));
                        inFlightBottle.SetFill(0.40f);
                    }
                    else
                    {
                        // Sub-phase 2: Dispense completes 40% -> 100% while nozzle rises with liquid surface
                        float fillProgress = Mathf.Clamp01((stateTimer - s2_tDive) / s2_tFill);
                        float fillFrac = Mathf.Lerp(0.40f, 1.0f, SCurve(fillProgress));
                        inFlightBottle.SetFill(fillFrac);
                        SetNozzleWorldY(GetDiveFollowY(currentRecipe, fillFrac));
                    }
                }
                break;

            case ChangeoverState.S3_CloseValveStopPump:
                // Valve closed & pump stopped. Nozzle retracts from full-fill dive height to recipe-clear height before S4 conveyor motion.
                currentBeltSpeed = 0f;
                float startRetractY = GetDiveFollowY(currentRecipe, 1.0f);
                float endClearY = GetRecipeClearY(currentRecipe);
                SetNozzleWorldY(Mathf.Lerp(startRetractY, endClearY, SCurve(StateProgress)));
                break;

            case ChangeoverState.S4_ClearBottlesFromZone:
                // Belt runs to clear bottles past downstream outfeed; enforce nozzle at recipe-clear height
                SetNozzleWorldY(GetRecipeClearY(currentRecipe));
                break;

            case ChangeoverState.S5_StopBelt:
                // S-curve belt deceleration; nozzle remains strictly at recipe-clear height
                currentBeltSpeed = Mathf.Lerp(currentRecipe.beltSpeed, 0f, eased);
                SetNozzleWorldY(GetRecipeClearY(currentRecipe));
                break;

            case ChangeoverState.S6_RetractNozzleToHome:
                // S6: DRIVE Z AXIS TO HOME HEIGHT FIRST. Rails remain strictly stationary.
                float startY = GetRecipeClearY(currentRecipe);
                float targetY = NozzleHomeY;
                float curNozzleY = Mathf.Lerp(startY, targetY, eased);
                SetNozzleWorldY(curNozzleY);

                if (norm >= 0.999f)
                {
                    SetNozzleWorldY(NozzleHomeY);
                    s6CompleteConfirmed = true;
                }
                break;

            case ChangeoverState.S7_AdjustRailWidth:
                // S7: STRUCTURALLY INTERLOCKED. Drive X axis symmetrically to target railGap.
                // Nozzle is locked at Home height throughout this entire state.
                SetNozzleWorldY(NozzleHomeY);

                float startGap = currentRecipe.railGap;
                float endGap = targetRecipe.railGap;
                float curGap = Mathf.Lerp(startGap, endGap, eased);
                SetRailGap(curGap);
                break;

            case ChangeoverState.S8_ConfirmInPosition:
                // S8: Both axes report in-position and drive-ready
                SetNozzleWorldY(NozzleHomeY);
                SetRailGap(targetRecipe.railGap);
                break;

            case ChangeoverState.S9_FirstArticleCheck:
                UpdateFirstArticleCheck(dt);
                break;

            case ChangeoverState.S10_ResumeProduction:
                UpdateProductionLoop(dt);
                break;
        }
    }

    private void UpdateFirstArticleCheck(float dt)
    {
        if (firstArticleBottle == null) return;

        float t = stateTimer;
        float diveStartY = GetDiveStartY(targetRecipe);

        // Sub-phase 1: Physical continuous conveyor travel to station at target belt speed
        if (t < s9_tTravel)
        {
            currentBeltSpeed = targetRecipe.beltSpeed;
            SetNozzleWorldY(NozzleHomeY);
        }
        // Sub-phase 2: Conveyor stops, nozzle lowers from Home to near bottle bottom via S-curve servo profile
        else if (t < s9_tTravel + s9_tLower)
        {
            currentBeltSpeed = 0f;
            firstArticleBottle.transform.position = new Vector3(0f, BeltSurfaceY, FillStationZ);
            firstArticleBottle.SetFill(0f);
            float phaseT = (t - s9_tTravel) / s9_tLower;
            SetNozzleWorldY(Mathf.Lerp(NozzleHomeY, diveStartY, SCurve(phaseT)));
        }
        // Sub-phase 3: Dispense liquid from base while nozzle rises with liquid via dive-follow profile
        else if (t < s9_tTravel + s9_tLower + s9_tFill)
        {
            currentBeltSpeed = 0f;
            firstArticleBottle.transform.position = new Vector3(0f, BeltSurfaceY, FillStationZ);
            float phaseT = (t - (s9_tTravel + s9_tLower)) / s9_tFill;
            float fillFrac = SCurve(phaseT);
            firstArticleBottle.SetFill(fillFrac);
            SetNozzleWorldY(GetDiveFollowY(targetRecipe, fillFrac));
        }
        // Sub-phase 4: Nozzle retracts back to Home height via S-curve; test bottle marked permanently filled
        else if (t < s9_tTravel + s9_tLower + s9_tFill + s9_tRaise)
        {
            currentBeltSpeed = 0f;
            firstArticleBottle.SetFill(1.0f);
            firstArticleBottle.isFilled = true;
            float phaseT = (t - (s9_tTravel + s9_tLower + s9_tFill)) / s9_tRaise;
            float fullFillDiveY = GetDiveFollowY(targetRecipe, 1.0f);
            SetNozzleWorldY(Mathf.Lerp(fullFillDiveY, NozzleHomeY, SCurve(phaseT)));
        }
        // Sub-phase 5: Conveyor restarts only after nozzle reaches home; verified first article conveys downstream
        else
        {
            SetNozzleWorldY(NozzleHomeY);
            firstArticleBottle.isFilled = true;
            currentBeltSpeed = targetRecipe.beltSpeed;
        }
    }

    private float productionSpawnTimer = 0f;
    private void UpdateProductionLoop(float dt)
    {
        currentBeltSpeed = targetRecipe.beltSpeed;
        productionSpawnTimer += dt;

        // Spawn periodic production bottles according to recipe dimensions
        float spawnInterval = (targetRecipe.bottleDia * 2.5f) / Mathf.Max(0.05f, targetRecipe.beltSpeed);
        if (productionSpawnTimer >= spawnInterval)
        {
            productionSpawnTimer = 0f;
            GameObject bObj = CreateBottle(targetRecipe, new Vector3(0f, BeltSurfaceY, InfeedSpawnZ), 0f);
            BottleTracker tracker = bObj.GetComponent<BottleTracker>();
            activeBottles.Add(tracker);
        }

        // Fill bottles passing under nozzle (never reducing already filled bottles)
        for (int i = 0; i < activeBottles.Count; i++)
        {
            BottleTracker b = activeBottles[i];
            if (b == null || b.isFilled) continue;

            float z = b.transform.position.z;
            if (z >= FillStationZ - 0.05f && z <= FillStationZ + 0.15f)
            {
                float fillRatio = Mathf.Clamp01((z - (FillStationZ - 0.05f)) / 0.20f);
                b.SetFill(fillRatio);
                if (fillRatio >= 0.99f) b.isFilled = true;
            }
        }
    }

    private void AdvanceToNextState()
    {
        // Interlock: enforce S6 strictly complete before advancing to S7
        if (currentState == ChangeoverState.S6_RetractNozzleToHome)
        {
            if (!s6CompleteConfirmed || (nozzleAssembly != null && nozzleAssembly.position.y < NozzleHomeY - 0.005f))
            {
                SetNozzleWorldY(NozzleHomeY);
                s6CompleteConfirmed = true;
            }
        }

        if (currentState == ChangeoverState.S10_ResumeProduction)
        {
            if (loopSequence)
            {
                ResetForLoop();
            }
            return;
        }

        EnterState((ChangeoverState)((int)currentState + 1));
    }

    /// <summary>
    /// Resets full state machine, geometry, and bottle instances for clean repeated demonstration loop.
    /// Prevents target->target loops by restoring currentRecipe from initialRecipeIndex.
    /// </summary>
    private void ResetForLoop()
    {
        // Destroy all dynamically spawned bottles
        for (int i = activeBottles.Count - 1; i >= 0; i--)
        {
            if (activeBottles[i] != null && activeBottles[i].gameObject != initialBottle)
            {
                Destroy(activeBottles[i].gameObject);
            }
        }
        activeBottles.Clear();
        firstArticleBottle = null;

        // Restore currentRecipe from initialRecipeIndex (avoids target->target loops!)
        initialRecipeIndex = Mathf.Clamp(initialRecipeIndex, 0, Recipes.Length - 1);
        targetRecipeIndex = Mathf.Clamp(targetRecipeIndex, 0, Recipes.Length - 1);
        currentRecipe = Recipes[initialRecipeIndex];
        targetRecipe = Recipes[targetRecipeIndex];

        // Reset hardware geometry to initial recipe
        SetRailGap(currentRecipe.railGap);
        SetNozzleWorldY(GetRecipeClearY(currentRecipe));
        currentBeltSpeed = currentRecipe.beltSpeed;

        // Reactivate and reset the original bottle
        if (initialBottle != null)
        {
            initialBottle.SetActive(true);
            HideBottleCap(initialBottle);
            initialBottle.transform.position = new Vector3(0f, BeltSurfaceY, InitialBottleStartZ);
            BottleTracker trk = initialBottle.GetComponent<BottleTracker>();
            if (trk != null)
            {
                trk.isFilled = false;
                trk.isFirstArticle = false;
                trk.recipe = currentRecipe;
                trk.SetFill(0.40f);
            }
            inFlightBottle = trk;
            activeBottles.Add(trk);
        }

        s6CompleteConfirmed = false;
        accumulatedTime = 0f;
        EnterState(ChangeoverState.S0A_SelectRecipe);
    }

    private void UpdateConveyorMotion(float dt)
    {
        if (beltRenderer == null || Mathf.Abs(currentBeltSpeed) < 0.0001f) return;

        // Scroll UV offset at recipe beltSpeed
        float scrollDelta = (currentBeltSpeed / 3.0f) * dt;
        beltUvOffset += scrollDelta;

        Material mat = Application.isPlaying ? beltRenderer.material : beltRenderer.sharedMaterial;
        if (mat != null && mat.HasProperty("_BaseMap"))
        {
            mat.SetTextureOffset("_BaseMap", new Vector2(0f, -beltUvOffset));
        }
    }

    /// <summary>
    /// Single authoritative place where all bottles are advanced along +Z by belt speed.
    /// </summary>
    private void UpdateBottlePositions(float dt)
    {
        if (Mathf.Abs(currentBeltSpeed) < 0.0001f) return;

        for (int i = activeBottles.Count - 1; i >= 0; i--)
        {
            BottleTracker b = activeBottles[i];
            if (b == null)
            {
                activeBottles.RemoveAt(i);
                continue;
            }

            // Advance bottle along +Z at current belt speed (SINGLE PLACE)
            Vector3 pos = b.transform.position;
            pos.z += currentBeltSpeed * dt;
            b.transform.position = pos;

            // Handle downstream outfeed exit
            if (pos.z > OutfeedExitZ)
            {
                if (b.gameObject == initialBottle)
                {
                    b.gameObject.SetActive(false);
                }
                else
                {
                    Destroy(b.gameObject);
                }
                activeBottles.RemoveAt(i);
            }
        }
    }

    // =========================================================================
    // PART 3 — MOTION HELPERS (S-CURVE & HARDWARE DRIVERS)
    // =========================================================================

    /// <summary>
    /// S-curve easing helper: quintic smootherstep with 0 acceleration and 0 jerk at boundaries.
    /// Shared by both X rail width and Z nozzle height axes to match real servo profiles.
    /// </summary>
    public static float SCurve(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    /// <summary>
    /// Symmetrically sets guide rail children in local X to +-(railGap / 2).
    /// Guarded against null references with explicit logging.
    /// </summary>
    public void SetRailGap(float gap)
    {
        if (guideRailAssembly == null)
        {
            Debug.LogError("[ChangeoverSequencer] guideRailAssembly is null! Cannot set rail gap.");
            return;
        }

        if (railLeft != null)
        {
            railLeft.localPosition = new Vector3(-gap * 0.5f, railLeft.localPosition.y, railLeft.localPosition.z);
        }
        else
        {
            Debug.LogWarning("[ChangeoverSequencer] railLeft reference is null on guideRailAssembly.");
        }

        if (railRight != null)
        {
            railRight.localPosition = new Vector3(gap * 0.5f, railRight.localPosition.y, railRight.localPosition.z);
        }
        else
        {
            Debug.LogWarning("[ChangeoverSequencer] railRight reference is null on guideRailAssembly.");
        }
    }

    /// <summary>
    /// Sets NozzleAssembly_Z world Y position.
    /// Guarded against null references with explicit logging.
    /// </summary>
    public void SetNozzleWorldY(float y)
    {
        if (nozzleAssembly == null)
        {
            Debug.LogError("[ChangeoverSequencer] nozzleAssembly is null! Cannot set nozzle height.");
            return;
        }

        Vector3 p = nozzleAssembly.position;
        nozzleAssembly.position = new Vector3(p.x, y, p.z);
    }

    // =========================================================================
    // DIVE-FILLING & ELEVATION HELPERS
    // =========================================================================

    /// <summary>
    /// Recipe clearance height in world coordinates (safe clearance above bottle top).
    /// Used for bottle transit through station (S4/S5) and baseline start for S6 gantry home retract.
    /// </summary>
    public float GetRecipeClearY(Recipe recipe)
    {
        return BeltSurfaceY + recipe.nozzleClearHeight;
    }

    /// <summary>
    /// Dive start height in world coordinates near bottle bottom (25 mm above conveyor belt).
    /// Ensures nozzle tip is safely inserted near bottom before dispensing starts.
    /// </summary>
    public float GetDiveStartY(Recipe recipe)
    {
        return BeltSurfaceY + DiveStartAboveBelt;
    }

    /// <summary>
    /// Dive follow height in world coordinates: rises with liquid level during dispensing while
    /// keeping nozzle tip approximately 10 mm submerged beneath liquid surface to prevent foaming,
    /// clamped above dive-start height and strictly below bottle neck shoulder.
    /// </summary>
    public float GetDiveFollowY(Recipe recipe, float fillFraction)
    {
        float maxLiqH = recipe.bottleHeight * 0.70f;
        float curLiqH = Mathf.Clamp01(fillFraction) * maxLiqH;
        float liquidSurfaceY = BeltSurfaceY + 0.002f + curLiqH;

        float diveStartY = GetDiveStartY(recipe);
        float neckBottomY = BeltSurfaceY + recipe.bottleHeight * 0.76f;

        // Submerged 10 mm beneath surface, bounded between dive start and bottle neck shoulder
        float targetY = liquidSurfaceY - SubmergedOffset;
        return Mathf.Clamp(targetY, diveStartY, neckBottomY - 0.005f);
    }

    // =========================================================================
    // PROCEDURAL BOTTLE BUILDER (FROM RECIPE TABLE & DELTAMATERIALS)
    // =========================================================================

    public GameObject CreateBottle(Recipe recipe, Vector3 spawnPos, float initialFillFraction)
    {
        GameObject bottleRoot = new GameObject($"Bottle_{recipe.name}_PET");
        bottleRoot.transform.position = spawnPos;

        Material petMat = GetPetMaterial();
        Material liqMat = GetLiquidMaterial(new Color(0.95f, 0.85f, 0.45f, 1f));
        Material capMat = GetCapMaterial();

        float totalH = recipe.bottleHeight;
        float dia = recipe.bottleDia;

        float bodyH = totalH * 0.76f;
        float neckH = totalH * 0.16f;
        float capH = totalH * 0.08f;

        // Bottle Body (Cylinder)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Bottle_Body_PET";
        body.transform.SetParent(bottleRoot.transform);
        body.transform.localPosition = new Vector3(0f, bodyH * 0.5f, 0f);
        body.transform.localScale = new Vector3(dia, bodyH * 0.5f, dia);
        if (petMat != null) body.GetComponent<Renderer>().sharedMaterial = petMat;
        Collider colB = body.GetComponent<Collider>();
        if (colB != null) Destroy(colB);

        // Bottle Neck (Cylinder)
        float neckDia = dia * 0.40f;
        GameObject neck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        neck.name = "Bottle_Neck_PET";
        neck.transform.SetParent(bottleRoot.transform);
        neck.transform.localPosition = new Vector3(0f, bodyH + neckH * 0.5f, 0f);
        neck.transform.localScale = new Vector3(neckDia, neckH * 0.5f, neckDia);
        if (petMat != null) neck.GetComponent<Renderer>().sharedMaterial = petMat;
        Collider colN = neck.GetComponent<Collider>();
        if (colN != null) Destroy(colN);

        // Bottle cap omitted for filling station demo (neck remains open under dispense tip)
        // Cap material and dimensions preserved for future capping station integration

        // Liquid Volume (Cylinder growing strictly from base)
        float maxLiquidH = bodyH * 0.85f;
        float liquidDia = dia * 0.92f;
        GameObject liquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        liquid.name = "Liquid_Volume";
        liquid.transform.SetParent(bottleRoot.transform);

        float curH = Mathf.Max(0.0002f, maxLiquidH * initialFillFraction);
        liquid.transform.localPosition = new Vector3(0f, 0.002f + curH * 0.5f, 0f);
        liquid.transform.localScale = new Vector3(liquidDia, curH * 0.5f, liquidDia);
        if (liqMat != null) liquid.GetComponent<Renderer>().sharedMaterial = liqMat;
        Collider colL = liquid.GetComponent<Collider>();
        if (colL != null) Destroy(colL);

        Renderer liqRend = liquid.GetComponent<Renderer>();
        if (initialFillFraction <= 0.001f)
        {
            liqRend.enabled = false;
        }

        BottleTracker tracker = bottleRoot.AddComponent<BottleTracker>();
        tracker.liquidTransform = liquid.transform;
        tracker.liquidRenderer = liqRend;
        tracker.maxLiquidHeight = maxLiquidH;
        tracker.liquidDiameter = liquidDia;
        tracker.recipe = recipe;
        tracker.SetFill(initialFillFraction);

        return bottleRoot;
    }

    /// <summary>
    /// Hides and removes any bottle cap from bottles travelling through or waiting at the filling nozzle.
    /// Preserves open bottle neck for dispensing.
    /// </summary>
    private static void HideBottleCap(GameObject bottle)
    {
        if (bottle == null) return;
        Transform cap = bottle.transform.Find("Bottle_Cap_Blue");
        if (cap == null) cap = bottle.transform.Find("Bottle_Cap");
        if (cap != null)
        {
            cap.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(cap.gameObject);
            else DestroyImmediate(cap.gameObject);
        }
    }

    private Material GetPetMaterial()
    {
        if (petMaterial != null) return petMaterial;
        petMaterial = ResolveEditorMaterial("ClearPET");
        if (petMaterial != null) return petMaterial;

        // Fallback procedural URP Lit transparent
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        Material m = new Material(s) { name = "M_Delta_ClearPET_Fallback" };
        m.SetFloat("_Surface", 1.0f);
        m.SetFloat("_Blend", 0.0f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.SetOverrideTag("RenderType", "Transparent");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        m.SetShaderPassEnabled("ShadowCaster", false);
        m.SetColor("_BaseColor", new Color(0.85f, 0.95f, 0.94f, 0.25f));
        m.SetFloat("_Smoothness", 0.90f);
        m.SetFloat("_Metallic", 0.0f);
        petMaterial = m;
        return petMaterial;
    }

    private Material GetLiquidMaterial(Color tint)
    {
        if (liquidMaterial != null) return liquidMaterial;
        liquidMaterial = ResolveEditorMaterialWithColor("Liquid", tint);
        if (liquidMaterial != null) return liquidMaterial;

        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        Material m = new Material(s) { name = "M_Delta_Liquid_Fallback" };
        m.SetColor("_BaseColor", tint);
        m.SetFloat("_Metallic", 0.0f);
        m.SetFloat("_Smoothness", 0.50f);
        liquidMaterial = m;
        return liquidMaterial;
    }

    private Material GetCapMaterial()
    {
        if (capMaterial != null) return capMaterial;
        capMaterial = ResolveEditorMaterialWithColor("PaintedSteel", new Color(0.12f, 0.45f, 0.70f, 1f));
        if (capMaterial != null) return capMaterial;

        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        Material m = new Material(s) { name = "M_Delta_Cap_Fallback" };
        m.SetColor("_BaseColor", new Color(0.12f, 0.45f, 0.70f, 1f));
        m.SetFloat("_Metallic", 0.15f);
        m.SetFloat("_Smoothness", 0.30f);
        capMaterial = m;
        return capMaterial;
    }

    private static Material ResolveEditorMaterial(string methodName)
    {
        try
        {
            Type t = Type.GetType("DeltaMaterials, Assembly-CSharp-Editor");
            if (t != null)
            {
                var method = t.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, Type.EmptyTypes, null);
                if (method != null) return (Material)method.Invoke(null, null);
            }
        }
        catch { }
        return null;
    }

    private static Material ResolveEditorMaterialWithColor(string methodName, Color c)
    {
        try
        {
            Type t = Type.GetType("DeltaMaterials, Assembly-CSharp-Editor");
            if (t != null)
            {
                var method = t.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, new Type[] { typeof(Color) }, null);
                if (method != null) return (Material)method.Invoke(null, new object[] { c });
            }
        }
        catch { }
        return null;
    }
}
