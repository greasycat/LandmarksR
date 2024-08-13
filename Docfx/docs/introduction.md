# Introduction to LandmarksR

LandmarksR is a lightweight Unity framework for building spatial cognition and perception experiments on desktop and Meta Quest. The framework keeps the experiment flow in the Unity hierarchy, uses small reusable task components, and treats trial definitions as table data instead of hard-coded logic.

## Current Project Baseline

- Unity editor version: `2022.3.62f3`
- Primary XR stack: `com.meta.xr.sdk.all` `62.0.0`
- Oculus XR package: `com.unity.xr.oculus` `4.5.1`
- UI stack: TextMesh Pro plus UGUI
- Logging model: structured event logs plus dataset exports

## Core Idea

A LandmarksR scene is usually composed from four layers:

1. Scene services such as `Settings`, `ExperimentLogger`, `Experiment`, and `PlayerController`.
2. A tagged `RootTask` that serves as the entry point for the experiment flow.
3. Structural tasks such as `CollectionTask` and `RepeatTask` that organize execution.
4. Interactive or functional tasks that show instructions, collect responses, manipulate the player, or present trial stimuli.

`Experiment` waits for the player systems to be ready, finds the GameObject tagged `RootTask`, and starts the task tree by calling `ExecuteAll()` on that root task.

## What Ships in This Repository

The repository currently includes these LandmarksR scenes:

- `Assets/LandmarksR/Scenes/Start Screen.unity`
- `Assets/LandmarksR/Scenes/Calibration.unity`
- `Assets/LandmarksR/Scenes/Demo/LandmarksR Demo.unity`
- `Assets/LandmarksR/Scenes/Demo/NBackText.unity`
- `Assets/LandmarksR/Scenes/Demo/StroopText.unity`

The current Build Settings only enable `Start Screen` and `Demo/NBackText` from the LandmarksR scene set. If you want to build another demo or the calibration scene, update Build Settings before creating a player build.

## Recommended Starting Points

- Use `Demo/NBackText` to study a current table-driven cognitive task.
- Use `Demo/StroopText` to see a second cognitive task with the same repeat-and-log pattern.
- Use `Calibration.unity` to inspect the VR calibration flow in isolation.
- Use `Start Screen.unity` only if you also provide `Settings` assets under `Resources/Settings`, because the start screen loads profiles from that location.

## Getting Started

1. Open the project in Unity `2022.3.62f3`.
2. Open one of the demo scenes under `Assets/LandmarksR/Scenes/Demo/`.
3. Confirm the scene contains a `LandmarksR` object with `Experiment`, a `PlayerController`, an `ExperimentLogger`, and a tagged `RootTask`.
4. Press Play and inspect the task hierarchy as it advances.
5. Duplicate a demo scene when starting a new study instead of building from scratch.

## Documentation Map

- `landmarks_r_structure.md`: scene objects, tags, and layout conventions
- `core_components.md`: runtime services shared by tasks
- `tasks.md`: current task catalog and when to use each task type
- `data.md`: tables, repeat-driven trial data, and logging outputs
- `Quick Introduction to Quest.md`: current Quest-specific setup notes
- `Quest Calibration.md`: VR calibration flow and required scene objects
- `Tasks/BaseTask.md`: lifecycle details for writing custom tasks
