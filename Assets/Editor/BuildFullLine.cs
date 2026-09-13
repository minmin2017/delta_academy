using UnityEditor;
using UnityEngine;

/// <summary>
/// Master orchestration for the whole line. "Build Full Cell" (in DeltaControlPropsBuilder.cs)
/// starts with CellBuilder.BuildCell() - the ONE destructive "wipe every root GameObject" step -
/// so any additive zone builder (Infeed/Filling/Capping/EndOfLine) that ran before it gets wiped
/// too if Build Full Cell runs again afterward. This single menu item chains the correct order
/// every time, so nobody (agent or human) has to remember the sequence.
/// </summary>
public static class BuildFullLine
{
    [MenuItem("Tools/Delta/Build Full Line (Everything)")]
    public static void BuildEverything()
    {
        // Build Full Cell first - this is the destructive rebuild (cell + cabinet + sequencer + camera director).
        DeltaControlPropsBuilder.BuildFullCell();

        // Then every additive zone builder, in line order (upstream to downstream).
        InfeedZoneBuilder.AddInfeedZoneProps();
        FillingZonePropsBuilder.AddFillingZoneProps();
        CappingZoneBuilder.AddCappingZoneProps();
        EndOfLineBuilder.AddEndOfLineProps();

        Debug.Log("[BuildFullLine] Full line built: Cell + Control Cabinet + Sequencer + Camera Director + Infeed + Filling + Capping + EndOfLine.");
    }
}
