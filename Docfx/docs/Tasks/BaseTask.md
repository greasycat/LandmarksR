# BaseTask

`BaseTask` is the common lifecycle class for almost every experiment action in LandmarksR. If you write a custom task, this is the class you inherit from.

![BaseTask](../images/BaseTask.png)

## What `BaseTask` Provides

Every `BaseTask` instance gets:

- a resolved `Settings` singleton
- a resolved `Experiment` singleton
- the active `PlayerController`
- the active `PlayerEventController`
- the shared `HUD`
- the shared `ExperimentLogger`
- ordered child task discovery through `_subTasks`
- timer support
- task lifecycle state such as running, prepared, and completed flags

## Lifecycle

The current lifecycle is:

1. `Awake()` marks the task as enabled.
2. `Start()` collects child `BaseTask` components in hierarchy order.
3. `ExecuteAll()` calls `Prepare()`.
4. `Prepare()` resolves shared services, logs task start, resets elapsed time, and starts the timer.
5. The task stays alive while `isRunning` is true.
6. Once the task stops, `ExecuteAll()` runs child tasks in order unless a specialized task overrides that flow.
7. `Finish()` logs task completion and clears lifecycle flags.

## Task Types

`BaseTask` uses `TaskType` to control default execution behavior:

| Type | Behavior |
| --- | --- |
| `Structural` | `Prepare()` marks the task as not running, so `ExecuteAll()` immediately advances into child tasks. |
| `Functional` | Same as structural by default: perform setup work, then finish. |
| `Interactive` | Remains running until the timer ends or the task calls `StopCurrentTask()`. |
| `NotSet` | Logs an error and should be treated as a misconfigured task. |

That is why almost every derived task sets its type before calling `base.Prepare()`.

## Important Members

| Member | Purpose |
| --- | --- |
| `taskType` | Declares whether the task is structural, functional, or interactive. |
| `timer` | Maximum run time before the task auto-stops. |
| `randomizeTimer` | Enables randomized timer selection between `minTimer` and `maxTimer`. |
| `elapsedTime` | Tracks task time while the timer coroutine runs. |
| `_subTasks` | Ordered list of child tasks. |
| `Logger` | Shared `ExperimentLogger` instance. |
| `HUD` | Shared HUD controller. |
| `PlayerEvent` | Shared input registration layer. |

## Core Methods

### `Prepare()`

The base implementation:

- resolves the current scene services
- asserts that required references are present
- logs `(<taskName>) Started`
- initializes timer state
- starts the timer coroutine
- sets the initial running behavior from `taskType`

Derived tasks usually:

1. call `SetTaskType(...)`
2. call `base.Prepare()`
3. register handlers, set HUD state, or start coroutines

### `ExecuteAll()`

The default implementation:

- skips disabled tasks
- calls `Prepare()`
- waits until `isRunning` becomes false
- executes each child task in order
- calls `Finish()`

Specialized structural tasks such as `CollectionTask` and `RepeatTask` override this method because they need different child execution behavior.

### `Finish()`

The base implementation:

- logs `(<taskName>) Finished`
- marks the task completed
- clears the prepared flag

Derived tasks are responsible for undoing their own side effects, such as:

- unregistering input handlers
- clearing HUD text
- restoring hidden layers
- stopping or destroying temporary objects

### `Reset()`

`Reset()` clears completion and preparation state. `RepeatTask` uses this after each repeat so child tasks can run again.

### `StopCurrentTask()`

This is the normal way for an interactive task to finish early. It simply sets `isRunning` to `false`, allowing `ExecuteAll()` to continue.

### `IsTaskRunning()`

Returns the current run state. Tasks that update every frame, such as `GoToFootprintTask`, use this to avoid running update logic when inactive.

## Timer Behavior

`BaseTask` starts a timer coroutine in `Prepare()`. While both conditions are true:

- `elapsedTime < timer`
- `isRunning == true`

the task stays active and `elapsedTime` increases by `Time.deltaTime`.

When the timer completes, `isRunning` becomes `false`.

Implications:

- interactive tasks can naturally time out
- functional and structural tasks usually finish immediately because `Prepare()` sets `isRunning` to `false`
- tasks with custom coroutines can still call `StopCurrentTask()` before the timer expires

## Example Custom Task

```csharp
using LandmarksR.Scripts.Experiment.Tasks;
using UnityEngine;

public class SimpleInstructionTask : BaseTask
{
    protected override void Prepare()
    {
        SetTaskType(TaskType.Interactive);
        base.Prepare();

        HUD.SetTitle("Ready")
            .SetContent("Press Enter or the trigger to continue.")
            .ShowAll();

        PlayerEvent.RegisterConfirmHandler(OnConfirm);
    }

    public override void Finish()
    {
        base.Finish();
        PlayerEvent.UnregisterConfirmHandler(OnConfirm);
        HUD.ClearAllText();
    }

    private void OnConfirm()
    {
        StopCurrentTask();
    }
}
```

## When to Override `ExecuteAll()`

Most custom tasks should not override `ExecuteAll()`. Override it only when the task must control child execution itself, for example:

- iterating trials from a table, as `RepeatTask` does
- moving backward or forward through a linked child list, as `CollectionTask` does

For normal instruction, response, or stimulus tasks, overriding `Prepare()` and `Finish()` is enough.
