# Data and Logging

LandmarksR separates trial definition from task logic. Tables describe rows of input data, `RepeatTask` advances through those rows, and `ExperimentLogger` writes output rows with stable run metadata.

## `DataFrame`

`DataFrame` is the in-memory table structure used throughout the framework. It stores rows as `List<object?>`, tracks column names through `ColumnNameMap`, and supports:

- row append
- column append
- lookup by row and column index
- lookup by row and column name
- row extraction as a one-row `DataFrame`
- horizontal and vertical merge operations

It is intentionally lightweight. It is a runtime container for experiment data, not a full analysis library.

## `TextTable`

`TextTable` is the main way to load trial rows without writing custom parsing code.

Current capabilities:

- inline rows serialized in the inspector
- loading from `dataPath`
- optional header row handling
- configurable delimiter through `DelimiterOption`
- exclusion of row slices with `indexesToExclude`

Important current detail: the inspector exposes a `randomize` field, but row randomization inside `TextTable.Parse()` is still marked `TODO` and is not currently implemented. If you need randomized iteration, do not rely on `TextTable.randomize` yet.

## `MergedTable`

`MergedTable` combines multiple source tables.

Current options:

- horizontal merge: combine columns from each table row-by-row
- vertical merge: append rows from multiple tables
- hard merge: build a new merged `DataFrame`
- soft merge: iterate through source `DataFrame` objects through `DataEnumerator`
- optional randomized enumeration order through a random seed

Use `MergedTable` when a single trial row should be assembled from multiple table sources or when multiple trial blocks should behave like one logical table.

## `DataEnumerator`

`RepeatTask` does not read tables directly. It uses `DataEnumerator`, which supports:

- sequential iteration over a single `DataFrame`
- iteration over multiple `DataFrame` objects in vertical or horizontal merge mode
- optional randomized row order when a non-zero seed is supplied

For merged tables, `GetCurrent()` returns the active logical row, and `GetCurrentByTable(tableIndex)` lets code inspect the source-specific row.

## `RepeatTask` as the Trial Driver

`RepeatTask` is the main consumer-facing table task.

It can run in two modes:

- repeat by fixed count
- repeat by row using a `Table`

When `useTable` is enabled and a table is assigned:

1. The table is prepared.
2. `numberOfRepeat` is set to the table row count.
3. Each `MoveNext()` advances to the next current row.
4. Child tasks can read the current row through `CurrentData`.

`RepeatTask` also exposes:

- `CurrentTable`
- `CurrentData`
- `CurrentDataByTable(int tableIndex)`
- `Context`, a per-repeat dictionary used to accumulate output values

## Reading Table Values in Tasks

Tasks usually read values from the current repeat row.

Example:

```csharp
protected override void Prepare()
{
    SetTaskType(TaskType.Interactive);
    base.Prepare();

    var repeatTask = GetComponentInParent<RepeatTask>();
    var currentData = repeatTask.CurrentData;
    var targetName = currentData.GetFirstInColumn<string>("Target");

    HUD.SetTitle("Navigation")
        .SetContent($"Find target: {targetName}")
        .ShowAll();
}
```

The exact helper methods depend on the `DataFrame` API, but the pattern is always the same: a task under `RepeatTask` reads the current row and uses those values to configure behavior.

## Dataset Output

`RepeatTask` opens a dataset in `Prepare()` by calling `Logger.BeginDataset(outputSetName, outputColumns)`.

Current output behavior:

- `run_session_id` is always included
- `subject_id` is always included
- tasks implementing `RepeatTask.IRepeatTaskOutputProvider` can add required columns automatically
- one output row is written per repeat iteration
- `Context` is cleared after each repeat

For example, `CognitiveTrialTaskBase` contributes columns such as:

- `trial_id`
- `task_type`
- `condition`
- `stimulus`
- `selected_response`
- `correct_response`
- `is_correct`
- `has_response`
- `reaction_time_ms`
- task-specific columns such as `word`, `ink_color`, or `n_value`

## Event Logs vs Dataset Logs

LandmarksR now has two logging layers:

### Event logs

These are written by `ExperimentLogger` for lifecycle and general runtime events.

Typical examples:

- run session started
- task started
- task finished
- dataset started
- dataset finished

### Dataset logs

These are tabular outputs created through `BeginDataset`, `SetData`, `LogDataRow`, and `EndDataset`.

Each row is wrapped with envelope columns such as:

- `run_session_id`
- `subject_id`
- `ts_utc`
- `ts_unix_ms`
- `dataset_name`

## Output Formats

Current logging settings support:

- JSON Lines for canonical structured event output
- TSV exports
- CSV exports

Export options are controlled by `Settings.logging`.

## Where Output Is Written

By default, run output is created under:

`Application.persistentDataPath/<runSessionId>/`

That directory contains the event log and any datasets opened during the run.
