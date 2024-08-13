# Core Components

LandmarksR scenes are driven by a small set of scene-level runtime components. `BaseTask` resolves these references before a task runs, so custom tasks can treat them as shared services.

| Component | Role | Responsibilities |
| --- | --- | --- |
| `Experiment` | Runtime bootstrapper | Waits for the player systems to become ready, finds the tagged `RootTask`, and starts the task graph. |
| `Settings` | Configuration hub | Stores subject and run metadata, display profiles, interaction settings, calibration state, logging options, and UI defaults. |
| `PlayerController` | Active rig coordinator | Switches between desktop and VR rigs, exposes the active camera, and provides access to HUD plus input routing. |
| `PlayerEventController` | Input routing layer | Registers keyboard handlers, VR button handlers, timed hold handlers, and collision or trigger callbacks. |
| `HUD` | Participant-facing UI | Shows instructions, confirmation prompts, progress feedback, and manages HUD placement modes. |
| `ExperimentLogger` | Structured logging layer | Creates run folders, writes event logs, opens dataset exports, and can forward logs to a remote endpoint. |

## Runtime Order

The current startup sequence is:

1. `Settings` becomes the active singleton and selects the display profile from `defaultDisplayMode`.
2. `PlayerController` switches to the matching rig and connects `HUD` plus `PlayerEventController`.
3. `Experiment` waits until the player event layer and HUD are initialized.
4. `Experiment` finds the GameObject tagged `RootTask` and starts the experiment flow.
5. `ExperimentLogger` records run/session metadata, task events, and dataset rows while tasks execute.

## `Experiment`

`Experiment` owns scene startup. It asserts that `playerController` is assigned, waits for `playerEvent` and `hud` to be ready, then starts `RootTask.ExecuteAll()`.

Use this component when:

- a scene starts but no task runs
- the wrong `PlayerController` is assigned
- a scene is missing the `RootTask` tag

## `Settings`

`Settings` is the main configuration object for the framework. The current class contains these groups:

- `experiment`: `subjectId` and `runSessionId`
- `defaultDisplayMode`: selects desktop or VR on startup
- `vrDisplay` and `desktopDisplay`: HUD mode, distance, and screen size
- `interaction`: HUD collider thickness and related interaction settings
- `space`: calibrated corners, center, forward direction, and ground height
- `calibration`: currently includes `controllerHeight`
- `logging`: local or remote logging plus CSV, TSV, and JSONL export options
- `ui`: currently includes `calibrationTriggerTime`

Two implementation details matter for current users:

- `experiment.StartNewRunSession()` generates a new GUID-backed run session ID.
- The default placeholder participant IDs from older versions are normalized away when a new run session starts.

## `PlayerController`

`PlayerController` coordinates the participant rig.

Current behavior:

- switches between desktop and VR according to `Settings.defaultDisplayMode`
- activates either the desktop controller reference or VR controller reference
- exposes the active camera through `GetMainCamera()`
- exposes `hud` and `playerEvent` for task access
- supports `Teleport(position, rotation)` for desktop locomotion
- can start and stop periodic player-state logging

In desktop mode, it also manages the `FirstPersonController` enable state so tasks can temporarily lock movement.

## `PlayerEventController`

`PlayerEventController` is the input registration layer tasks should use instead of scattering input polling across many scripts.

Supported handler types in the current implementation:

- keyboard handlers via `RegisterKeyHandler`
- VR button handlers via `RegisterVRInputHandler`
- timed VR hold handlers via `RegisterTimedVRInputHandler`
- shared confirm handlers via `RegisterConfirmHandler`
- trigger enter handlers
- collision enter handlers

By default, confirm is wired to:

- `Return` on keyboard
- `OVRInput.Button.PrimaryIndexTrigger` in VR

## `HUD`

`HUD` is the participant-facing presentation service.

Current capabilities include:

- title and content text
- panel opacity and visibility
- confirm button label and visibility
- progress bar display
- layer masking for hiding scene layers during instructions
- three placement modes: `Follow`, `Fixed`, and `Overlay`
- recentering for fixed and follow modes

Tasks such as `InstructionTask`, calibration tasks, and subject registration tasks all depend on `HUD`.

## `ExperimentLogger`

`ExperimentLogger` now supports a structured logging workflow rather than only legacy CSV-style output.

Current behavior:

- creates a run directory under `Application.persistentDataPath/<runSessionId>/`
- writes an event log with a stable base name, defaulting to `events`
- manages named dataset loggers opened by `RepeatTask`
- writes envelope metadata such as timestamps, `run_session_id`, and `subject_id`
- can export both TSV and CSV tabular files
- can optionally mirror records to a remote endpoint if remote logging is enabled

Tasks usually access it through the `Logger` reference resolved in `BaseTask`.

## Setup for an Empty Scene

### Step 1: Create `LandmarksR`

- Create an empty GameObject named `LandmarksR`.
- Add the `Experiment` component.

![Create LandmarksR](images/create_landmarks_r.png)

### Step 2: Add `PlayerController`

- Drag `Assets/LandmarksR/Prefabs/Core/PlayerController.prefab` under `LandmarksR`.
- Assign it to `Experiment.playerController`.
- This prefab also provides the runtime `HUD` and `PlayerEventController` references used by tasks.

### Step 3: Create `Environment`

- Create an empty GameObject named `Environment`.
- Tag it `Environment`.

### Step 4: Create `RootTask`

- Use `GameObject/Experiment/Tasks/1. RootTask`.
- Tag the created GameObject `RootTask`.

![Create RootTask](images/create_root_task.png)

### Step 5: Add `Settings` and `ExperimentLogger`

- Add a `Settings` object if the scene should be self-contained.
- Add an `ExperimentLogger` object if the scene should create structured output.
- If you use a separate bootstrap flow, make sure those objects still exist somewhere before the experiment scene begins.

### Step 6: Add VR-only Support Objects

- Add `Assets/LandmarksR/Prefabs/Core/PointableCanvasModule.prefab` to the scene root for VR UI interaction.
- Add `Assets/LandmarksR/Prefabs/Calibration/Calibration Space.prefab` if the scene includes calibration tasks.

![Calibration Prefabs](images/calibration_prefabs.png)
