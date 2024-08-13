# LandmarksR Unity Structure

This page describes the current scene layout used by the framework and the conventions that tasks rely on at runtime.

## Minimal Runtime Scene

The smallest practical LandmarksR scene has these objects:

| GameObject or Asset | Required | Purpose |
| --- | --- | --- |
| `LandmarksR` with `Experiment` | Yes | Scene bootstrapper. Starts the task graph once the player systems are ready. |
| `PlayerController` prefab | Yes | Contains the desktop rig, VR rig, `HUD`, and `PlayerEventController`. |
| `RootTask` tagged `RootTask` | Yes | Entry point for the task hierarchy. |
| `Environment` tagged `Environment` | Usually | Parent for experiment geometry and the object moved by calibration. |
| `ExperimentLogger` | Strongly recommended | Creates run-scoped event and dataset output. |
| `Settings` | Strongly recommended | Stores subject metadata, display mode, logging options, and calibration state. |
| `PointableCanvasModule` prefab | VR scenes | Enables world-space UI interaction in Quest scenes. |
| `Calibration Space` prefab tagged `Calibration` | VR calibration scenes | Visual reference space used by calibration tasks. |

`Settings` and `ExperimentLogger` are written as singleton scene services. When present, they are kept alive across scene loads with `DontDestroyOnLoad`.

## Required Tags

Several systems use tag lookup instead of serialized references. The current runtime expects these tags when the related features are used:

| Tag | Used By | Notes |
| --- | --- | --- |
| `RootTask` | `Experiment` | Required in every playable experiment scene. |
| `Environment` | `SpaceSettings.ApplyToEnvironment`, interactive tasks | Recommended for all experiment scenes. |
| `Calibration` | calibration flow | Required if you use `CalibrateTask` or `ApplyCalibrationTask`. |
| `Target` | `NavigationTask` | Applied to navigable target objects. |
| `PlayerCollider` | `GoToFootprintTask` | Used to detect when the participant is on the footprint. |
| `Floor` | `GoToFootprintTask` | Floor objects remain visible while other environment objects are hidden. |

## Typical Hierarchy Shape

The demo scenes follow this general layout:

```text
LandmarksR
|- Experiment
|- PlayerController
|- Calibration Space (VR scenes only)
Environment
ExperimentLogger
Settings
RootTask
|- SubjectRegistryTask / InstructionTask / CollectionTask / RepeatTask ...
```

The exact object names vary by scene, but the service roles above are stable.

## Scene Assets in the Repository

Current LandmarksR scenes on disk:

- `Assets/LandmarksR/Scenes/Start Screen.unity`
- `Assets/LandmarksR/Scenes/Calibration.unity`
- `Assets/LandmarksR/Scenes/Demo/LandmarksR Demo.unity`
- `Assets/LandmarksR/Scenes/Demo/NBackText.unity`
- `Assets/LandmarksR/Scenes/Demo/StroopText.unity`

Important detail: the current Build Settings do not include every LandmarksR scene. If you want the build to advance from a demo scene to another experiment scene, update Build Settings explicitly.

## Creating a New Scene

1. Create an empty GameObject named `LandmarksR`.
2. Add the `Experiment` component to it.
3. Drag `Assets/LandmarksR/Prefabs/Core/PlayerController.prefab` under `LandmarksR`.
4. Assign that prefab instance to `Experiment.playerController`.
5. Create an `Environment` GameObject and tag it `Environment`.
6. Create a `RootTask` from `GameObject/Experiment/Tasks/1. RootTask` and tag it `RootTask`.
7. Add `Settings` and `ExperimentLogger` objects unless another bootstrap scene is responsible for them.
8. For VR scenes, add `Assets/LandmarksR/Prefabs/Core/PointableCanvasModule.prefab`.
9. For VR calibration scenes, also add `Assets/LandmarksR/Prefabs/Calibration/Calibration Space.prefab`.

## Where to Place Your Own Objects

- Put world-space experiment geometry under `Environment` so calibration can reposition the whole setup.
- Put HUD-facing UI inside the `PlayerController` HUD hierarchy, or drive it through `Hud.cs`.
- Put task objects under `RootTask` or under other structural tasks.
- Put trigger objects for navigation-style tasks in the scene with the expected tags, usually under `Environment`.
- Put reusable runtime prefabs in `Assets/LandmarksR/Prefabs` or your own prefab folder, not inside a scene-specific hierarchy.

## Start Screen Notes

`Start Screen.unity` is not a generic magic bootstrap scene. Its `StartScreen` script expects one or more `Settings` assets available through `Resources.LoadAll<Settings>("Settings")`. If you want to use that scene in practice, create a `Settings` prefab or asset under `Assets/**/Resources/Settings/`.
