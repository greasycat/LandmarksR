# Quick Introduction to Quest

This page summarizes the current Quest-specific setup in LandmarksR.

## Current Package Baseline

The project currently targets Quest with:

- Unity `2022.3.62f3`
- `com.meta.xr.sdk.all` `62.0.0`
- `com.meta.xr.sdk.interaction.ovr.samples` `62.0.0`
- `com.unity.xr.oculus` `4.5.1`

The codebase still uses `OVRInput` directly for several VR interactions, especially calibration and confirm actions.

## How VR Mode Is Selected

Quest mode is selected through `Settings.defaultDisplayMode = DisplayMode.VR`.

At runtime:

1. `Settings` selects the VR display profile.
2. `PlayerController` disables the desktop rig and enables the VR rig.
3. `PlayerController` starts the XR subsystems.
4. `HUD` switches to the VR display mode configured in `Settings.vrDisplay`.

The default VR display profile currently uses:

- `displayMode = VR`
- `hudMode = Fixed`
- `hudDistance = 3f`

## Required Scene Pieces for Quest

For a Quest-ready scene, include:

- `Experiment`
- `PlayerController` prefab
- `RootTask` tagged `RootTask`
- `Environment` tagged `Environment`
- `Settings`
- `ExperimentLogger`
- `PointableCanvasModule` prefab

For VR calibration scenes, also include:

- `Calibration Space` prefab tagged `Calibration`

## Input Conventions

Current VR mappings in the framework include:

- confirm: `OVRInput.Button.PrimaryIndexTrigger`
- calibration back or reset actions: `OVRInput.Button.One`
- timed holds for calibration: `RegisterTimedVRInputHandler(...)`

Because these mappings live in code, make sure Quest-specific tasks use `PlayerEventController` rather than reading `OVRInput` in many separate places unless the task has a good reason.

## HUD Behavior in Quest

The HUD supports three modes overall, but Quest scenes usually rely on:

- `Fixed`: world-space HUD positioned in front of the participant
- `Follow`: HUD that can be recentered relative to the headset

`Overlay` is meant for desktop use.

## Current Demo Guidance

Quest support exists in the runtime and prefabs, but not every scene in the repository is configured for a Quest build by default. The current Build Settings only include:

- `Start Screen`
- `Demo/NBackText`

Before building to Quest:

1. Open Build Settings.
2. Add the experiment scene you actually want to run.
3. Make sure the scene contains VR support objects such as `PointableCanvasModule`.
4. Confirm `Settings.defaultDisplayMode` is `VR`.

## Known Practical Notes

- The framework expects Meta/Oculus packages to be present; removing them will break `OVRInput` usage and VR rig behavior.
- The Unity editor console in this workspace currently reports Meta XR DLL issues while the project is open on Linux. That does not change the intended Quest-side package setup, but it is relevant if you test in this editor environment.
- The cleanest way to start a Quest experiment is usually to duplicate an existing VR-capable demo scene and replace only the task graph and table data.
