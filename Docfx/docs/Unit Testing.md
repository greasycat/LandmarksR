# Unit Testing

LandmarksR currently includes an editor-only NUnit test assembly for core data, logging, cognitive evaluation, and primitive-sequence parsing logic.

## Test Assembly

The tests live under:

`Assets/LandmarksR/Scripts/Tests/`

The current assembly definition:

- name: `Tests`
- references: `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `LandmarksR`
- platform scope: `Editor`
- define constraint: `UNITY_INCLUDE_TESTS`

This means the shipped test assembly is intended for Unity Editor test runs, not runtime device builds.

## Current Test Coverage

The current repository includes tests for:

- `ExperimentSettings`
- structured logging defaults and event mapping
- dataset envelope column generation
- `CognitiveTrialEvaluator`
- `PrimitiveSequenceParser`
- `ColumnNameMap`
- `DataFrameBuilder`

Coverage is strongest around pure logic and serialization-style code, and thinner around scene wiring or input-heavy runtime behavior.

## Representative Test Files

- `Assets/LandmarksR/Scripts/Tests/Data/ExperimentSettingsTests.cs`
- `Assets/LandmarksR/Scripts/Tests/Data/StructuredLoggingTests.cs`
- `Assets/LandmarksR/Scripts/Tests/Cognitive/CognitiveTrialEvaluatorTests.cs`
- `Assets/LandmarksR/Scripts/Tests/Primitives/PrimitiveSequenceParserTests.cs`

## How to Run Tests in Unity

1. Open the project in the Unity Editor.
2. Open `Window > General > Test Runner`.
3. Select `EditMode`.
4. Run the full suite or a specific test fixture.

Because the test assembly is editor-only, the tests should be run from `EditMode`, not `PlayMode`.

## What Is Not Well Covered Yet

The current tests do not heavily exercise:

- full scene bootstrap and task hierarchy execution
- VR-specific input flows
- calibration task behavior inside live scenes
- HUD visual behavior
- end-to-end file output on device

Those areas still need manual scene testing in addition to unit tests.
