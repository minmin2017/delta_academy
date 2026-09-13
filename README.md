# Delta Academy — Zero-Downtime Changeover Cell

Automated recipe-driven packaging cell developed for the **Delta Academy KMITL** competition.
Simulates high-speed zero-downtime bottle changeover (250ml / 500ml / 1000ml) on a liquid packaging line, reducing manual changeover time from 45 minutes to under 5 minutes.

## Simulated Delta Industrial Hardware

| Device | Type | Role in Cell | Dimensions (W×H×D mm) |
|---|---|---|---|
| **AS320T-B** | PLC | Recipe management, high-speed counter, anti-drip valve control | 88 × 88 × 95 |
| **DOP-100WS** | HMI | Operator station, recipe selection, axis position telemetry, CIP checklist | 137 × 103 × 37 |
| **ASD-A3** (×2) | AC Servo Drive | Axis X (conveyor guide rail width) & Axis Z (filling nozzle height & dive) | 40 × 150 × 163 |
| **MS300** | VFD | Variable speed bottle conveyor with S-curve ramp acceleration | 68 × 128 × 110 |
| **CliQ-M 24V** | DIN PSU | Clean 24VDC control power distribution | 40 × 124 × 117 |

## Project Architecture

- **Engine**: Unity 6000.6.0f1 (Universal Render Pipeline - URP)
- **Target Platform**: Industrial Digital Twin / Presentation Video
- **Scene Assembly**: Fully procedural editor scripts via `[MenuItem]` for 100% reproducible millimetric placement.

### Editor Menu Tools (`Tools/Delta/...`)

- `Tools/Delta/Build Full Cell`: Builds complete cell (factory room, conveyor, gantry, control cabinet, HMI, sequencer, and baked reflection probe).
- `Tools/Delta/Add Control Props`: Generates the industrial control cabinet, DIN rails, Delta hardware, unlit reference photo cards, and close-up camera (`ControlCam_Close`).
- `Tools/Delta/Build Cell`: Assembles base cell machine, conveyor, lighting, and hero cameras.
- `Tools/Delta/Attach Sequencer`: Attaches runtime changeover sequencer and wires bottle trigger events.

## Repository Structure

```
Assets/
├── Editor/
│   ├── CellBuilder.cs                # Procedural cell, conveyor, gantry & room builder
│   ├── DeltaControlPropsBuilder.cs   # Control cabinet, Delta devices, HMI & reference cards
│   ├── DeltaMaterials.cs             # Industrial PBR shader & material factory
│   └── LightingTestBuilder.cs        # Lighting and material look-dev reference
├── Scripts/
│   ├── ChangeoverSequencer.cs        # Runtime changeover animation sequencer
│   └── DeltaHMIDisplay.cs            # Live HMI display simulation controller
├── ReferenceImages/                  # Real Delta product reference photos (Unlit cards)
├── Screenshots/                      # Visual verification milestones (v1-v15)
└── Scenes/
    └── SampleScene.unity             # Primary production cell scene
```
