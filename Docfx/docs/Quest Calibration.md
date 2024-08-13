# Quest Calibration

LandmarksR includes a dedicated VR calibration flow that lets the participant define the physical play area and align the in-game environment to it.

## What Calibration Writes

Calibration populates `Settings.space` with:

- `groundY`
- `leftTop`
- `rightTop`
- `rightBottom`
- `leftBottom`
- `center`
- `forward`
- `calibrated`

After calibration is confirmed, `Settings.space.ApplyToEnvironment()` moves both the `Environment` object and the `Calibration` space object so the virtual setup matches the measured physical space.

## Required Scene Objects

To use the built-in calibration flow, the scene needs:

- an `Environment` GameObject tagged `Environment`
- a calibration-space object tagged `Calibration`
- a `PlayerController` with the VR rig available
- a `CalibrateTask` in the task tree
- the calibration prefabs under `Assets/LandmarksR/Prefabs/Calibration/`

The current prefabs are:

- `Calibration Space.prefab`
- `Calibration.prefab`
- `Floor Indicator.prefab`
- `Pole.prefab`

## Current Calibration Flow

The built-in flow is implemented as a `CalibrateTask` with child tasks. The typical sequence is:

1. `PlaceFloor`
2. `PlacePoll` for each corner
3. `ConfirmCalibration`
4. `ReconfirmCalibration`

`CalibrateTask` inherits from `CollectionTask`, so it can move backward through the flow when the participant chooses to revise a step.

## Step Behavior

### `PlaceFloor`

- Spawns the floor indicator prefab.
- Tracks the right-hand anchor position.
- Uses `Settings.calibration.controllerHeight` to offset the measured floor point.
- Uses a timed hold on the index trigger to confirm.
- Uses button `One` as a back action.

### `PlacePoll`

- Places one pole at the current controller position.
- Prompts the participant to place poles in the configured order.
- Uses the current `polePositions` list from `CalibrateTask`.
- Uses button `One` to step back and remove the previous pole.

### `ConfirmCalibration`

- Computes the floor height, corner positions, center, and forward direction.
- Spawns a calibration-result indicator.
- Uses a timed trigger hold to accept the result.
- Uses button `One` to reject the result and return to the previous step.

### `ReconfirmCalibration`

- Applies the computed calibration to the environment.
- Uses a timed trigger hold to continue.
- Uses a timed hold on button `One` to reset the full calibration flow.

## Important Settings

Two settings directly affect the current Quest calibration behavior:

| Setting | Current Meaning |
| --- | --- |
| `Settings.calibration.controllerHeight` | Vertical offset from the controller to the floor marker during floor placement. |
| `Settings.ui.calibrationTriggerTime` | Hold duration required for confirm-style calibration actions. |

## HUD Behavior During Calibration

During calibration, `CalibrateTask` temporarily changes the display profile:

- it forces `HUD` mode to `Follow`
- it hides the normal confirmation button
- it shows the progress bar for timed hold actions

The calibration tasks then update progress based on how long the participant holds the relevant button.

## Applying Existing Calibration Later

If calibration has already been completed in the current run, `ApplyCalibrationTask` can reapply the saved transform without rerunning the full process.

This is useful when:

- a later scene should reuse the same calibrated play space
- the environment is loaded after calibration and still needs alignment

## Practical Setup Tips

- Keep the `CalibrateTask` near the start of the task tree if the scene depends on physical alignment.
- Tag the movable experiment parent `Environment`, not each child object independently.
- Do not parent the calibration space under `Environment`; both are positioned from the calibration result.
- Verify the VR rig has a usable right-hand anchor because `CalibrateTask` reads that transform directly.
