# Tasks in LandmarksR

Tasks are the units of experiment flow in LandmarksR. Every task inherits from `BaseTask`, which means every task participates in the same prepare-run-finish lifecycle and can be arranged directly in the Unity hierarchy.

## How Task Execution Works

- `RootTask` is the entry point for a scene.
- Structural tasks decide how child tasks are executed.
- Functional tasks perform one-off actions and finish immediately.
- Interactive tasks stay active until a timer expires or they call `StopCurrentTask()`.
- `RepeatTask` is the bridge between tables and trials, because it advances rows and writes dataset output.

## Commonly Created Tasks

The current editor hierarchy menu exposes these entries under `GameObject/Experiment/Tasks/`:

- `RootTask`
- `CollectionTask`
- `InstructionTask`
- `SubjectRegistryTask`
- `RepeatTask`
- `ExploreTask`
- `NBackTask`
- `StroopTask`
- `FlankerTask`
- `PrimitiveSequenceTask2D`
- `PrimitiveSequenceTask3D`

Several other tasks exist in code and in demo scenes even though they are not currently exposed in that creation menu.

## Structural Tasks

| Task | Purpose | Notes |
| --- | --- | --- |
| `RootTask` | Starts the task hierarchy for the scene. | When it finishes, it loads the next build scene if one exists, otherwise it quits play mode or the app. |
| `CollectionTask` | Runs child tasks in order. | Also supports moving back to the previous task node during calibration flows. |
| `RepeatTask` | Repeats child tasks by count or by table row. | Writes dataset output and exposes `CurrentData` plus `Context`. |
| `CalibrateTask` | Specialized structural flow for VR room calibration. | Inherits from `CollectionTask` and coordinates floor, pole, and confirmation steps. |

## Functional Tasks

| Task | Purpose | Notes |
| --- | --- | --- |
| `TeleportTask` | Teleports the player to a fixed position and rotation. | Mainly useful in desktop scenes. |
| `ApplyCalibrationTask` | Applies saved calibration data to the environment and calibration space. | Only acts if `Settings.space.calibrated` is true. |
| `SetStimulusTextColorTask` | Changes the color used by the cognitive stimulus presenter. | Must live under a `RepeatTask` and reads its color value from the current trial row. |

## Interactive Tasks

| Task | Purpose | Notes |
| --- | --- | --- |
| `InstructionTask` | Shows instruction text and waits for confirm. | Uses the shared HUD and confirm input. |
| `SubjectRegistryTask` | Captures the subject ID inside the scene. | Also shows the current run session ID if enabled. |
| `ExploreTask` | Lets the participant explore for a timed period. | Enables movement and hides the HUD after a delay. |
| `NavigationTask` | Waits until the participant reaches a tagged target. | Usually placed under a `RepeatTask` so it can read the current target from table data. |
| `GoToFootprintTask` | Requires the participant to stand on a footprint and align orientation. | Uses the `Footprint` prefab and `PlayerCollider` trigger checks. |

## Cognitive Trial Tasks

These tasks all inherit from `CognitiveTrialTaskBase` and are intended to live under a `RepeatTask`.

| Task | Stimulus Type | Expected Trial Data |
| --- | --- | --- |
| `NBackTask` | Centered text stimulus | `stimulus`, `correct_response`, optional timing columns, optional `n_value` |
| `StroopTask` | Centered word stimulus | `word`, `ink_color`, `correct_response`, optional timing columns |
| `FlankerTask` | Flanker layout | `center_symbol`, `flanker_symbol`, `correct_response`, optional timing columns |

Shared behavior provided by `CognitiveTrialTaskBase`:

- fixation period
- timed stimulus presentation
- timed response window
- key-to-response mapping
- evaluation of correctness and response presence
- automatic addition of trial output columns to the parent `RepeatTask`

## Calibration Tasks

The current VR calibration flow is built from these task classes:

- `PlaceFloor`
- `PlacePoll`
- `ConfirmCalibration`
- `ReconfirmCalibration`

These tasks depend on Quest button handlers from `PlayerEventController`, the `Calibration Space` prefab, and the `Environment` plus `Calibration` tags.

## Primitive Sequence Tasks

`PrimitiveSequenceTask2D` and `PrimitiveSequenceTask3D` run scripted sequences against scene targets identified by `target_id`.

Supported operations currently include:

- `Translate`
- `Rotate`
- `Scale`
- `Color`
- `Visibility`
- `Custom`

Sequence tables must include at least:

- `sequence`
- `timestamp_ms`
- `target_id`
- `operation`

## Debug and Support Tasks

There are also debug-oriented scripts such as `DummyTask`, `TestingTask`, `HUDTestingTask`, and `VRInputTestingTask`. They are useful for local development, but they are not core building blocks for experiment design and are not documented here as production workflow components.

## Choosing the Right Container

- Use `CollectionTask` when you want a fixed ordered list of tasks.
- Use `RepeatTask` when child tasks should run once per trial row or once per repeat count.
- Use cognitive tasks only under a `RepeatTask`, because they require `CurrentData` and write into `RepeatTask.Context`.
- Use calibration tasks only inside a `CalibrateTask`.

## Writing Your Own Task

If you need behavior that is not covered by the built-in tasks:

1. Create a new component that inherits from `BaseTask`.
2. Set the task type in `Prepare()`.
3. Call `base.Prepare()` to resolve `Settings`, `Player`, `PlayerEvent`, `HUD`, and `Logger`.
4. Register any input or trigger handlers you need.
5. Call `StopCurrentTask()` when the task should complete.
6. Clean up handlers and HUD state in `Finish()`.

See `Tasks/BaseTask.md` for the lifecycle details.
