using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LandmarksR.Scripts.Experiment.Data;
using UnityEngine;

namespace LandmarksR.Scripts.Experiment.Tasks.Primitives
{
    public static class PrimitiveSequenceParser
    {
        private static readonly string[] RequiredColumns = { "sequence", "timestamp_ms", "target_id", "operation" };

        public static List<PrimitiveSequenceStep> Parse(DataFrame dataFrame)
        {
            ValidateRequiredColumns(dataFrame);

            var parsedRows = new List<PrimitiveSequenceRow>();
            for (var rowIndex = 0; rowIndex < dataFrame.RowCount; rowIndex++)
            {
                parsedRows.Add(ParseRow(dataFrame, rowIndex));
            }

            return parsedRows
                .OrderBy(row => row.Sequence)
                .ThenBy(row => row.TimestampMs)
                .GroupBy(row => (row.Sequence, row.TimestampMs))
                .Select(group => new PrimitiveSequenceStep(group.Key.Sequence, group.Key.TimestampMs, group.ToList()))
                .ToList();
        }

        private static PrimitiveSequenceRow ParseRow(DataFrame dataFrame, int rowIndex)
        {
            var rawValues = dataFrame.GetColumnNames()
                .ToDictionary(column => column, column => GetString(dataFrame, rowIndex, column));

            var sequence = ParseRequiredInt(rawValues["sequence"], rowIndex, "sequence");
            var timestampMs = ParseRequiredInt(rawValues["timestamp_ms"], rowIndex, "timestamp_ms");
            var targetId = RequireNonEmpty(rawValues["target_id"], rowIndex, "target_id");
            var operation = ParseOperation(rawValues["operation"], rowIndex);
            var durationMs = ParseOptionalInt(GetString(rawValues, "duration_ms"), 0, rowIndex, "duration_ms");
            var space = ParseSpace(GetString(rawValues, "space"));
            var customKey = GetString(rawValues, "custom_key");
            var customValue = GetString(rawValues, "custom_value");

            var vector = Vector3.zero;
            var color = Color.white;
            var visible = true;

            switch (operation)
            {
                case PrimitiveOperation.Translate:
                case PrimitiveOperation.Rotate:
                case PrimitiveOperation.Scale:
                    vector = ParseVector(rawValues, rowIndex);
                    break;
                case PrimitiveOperation.Color:
                    color = ParseColor(rawValues, rowIndex);
                    break;
                case PrimitiveOperation.Visibility:
                    visible = ParseVisible(GetString(rawValues, "visible"), rowIndex);
                    break;
                case PrimitiveOperation.Custom:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new PrimitiveSequenceRow(sequence, timestampMs, targetId, operation, vector, color, visible,
                durationMs, space, customKey, customValue, rawValues);
        }

        private static void ValidateRequiredColumns(DataFrame dataFrame)
        {
            var missingColumns = RequiredColumns.Where(column => !dataFrame.HasColumn(column)).ToList();
            if (missingColumns.Count == 0)
            {
                return;
            }

            throw new ArgumentException("Primitive sequence is missing required columns: " +
                                        string.Join(", ", missingColumns));
        }

        private static string GetString(DataFrame dataFrame, int rowIndex, string columnName)
        {
            if (!dataFrame.HasColumn(columnName))
            {
                return string.Empty;
            }

            return dataFrame.GetValue<object>(rowIndex, columnName)?.ToString()?.Trim() ?? string.Empty;
        }

        private static string GetString(IReadOnlyDictionary<string, string> rawValues, string columnName)
        {
            return rawValues.TryGetValue(columnName, out var value) ? value : string.Empty;
        }

        private static string RequireNonEmpty(string value, int rowIndex, string columnName)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }

            throw new ArgumentException($"Row {rowIndex + 1} requires a non-empty '{columnName}' value.");
        }

        private static int ParseRequiredInt(string value, int rowIndex, string columnName)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            throw new ArgumentException($"Row {rowIndex + 1} has an invalid integer in '{columnName}': '{value}'.");
        }

        private static int ParseOptionalInt(string value, int fallback, int rowIndex, string columnName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            throw new ArgumentException($"Row {rowIndex + 1} has an invalid integer in '{columnName}': '{value}'.");
        }

        private static float ParseRequiredFloat(string value, int rowIndex, string columnName)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            throw new ArgumentException($"Row {rowIndex + 1} has an invalid float in '{columnName}': '{value}'.");
        }

        private static PrimitiveOperation ParseOperation(string value, int rowIndex)
        {
            if (Enum.TryParse(value, true, out PrimitiveOperation operation))
            {
                return operation;
            }

            throw new ArgumentException($"Row {rowIndex + 1} has an invalid operation '{value}'.");
        }

        private static PrimitiveCoordinateSpace ParseSpace(string value)
        {
            return Enum.TryParse(value, true, out PrimitiveCoordinateSpace space)
                ? space
                : PrimitiveCoordinateSpace.Local;
        }

        private static Vector3 ParseVector(IReadOnlyDictionary<string, string> rawValues, int rowIndex)
        {
            return new Vector3(
                ParseRequiredFloat(RequireNonEmpty(GetString(rawValues, "x"), rowIndex, "x"), rowIndex, "x"),
                ParseRequiredFloat(RequireNonEmpty(GetString(rawValues, "y"), rowIndex, "y"), rowIndex, "y"),
                ParseRequiredFloat(RequireNonEmpty(GetString(rawValues, "z"), rowIndex, "z"), rowIndex, "z"));
        }

        private static Color ParseColor(IReadOnlyDictionary<string, string> rawValues, int rowIndex)
        {
            var r = NormalizeColor(ParseRequiredFloat(RequireNonEmpty(GetString(rawValues, "r"), rowIndex, "r"), rowIndex,
                "r"));
            var g = NormalizeColor(ParseRequiredFloat(RequireNonEmpty(GetString(rawValues, "g"), rowIndex, "g"), rowIndex,
                "g"));
            var b = NormalizeColor(ParseRequiredFloat(RequireNonEmpty(GetString(rawValues, "b"), rowIndex, "b"), rowIndex,
                "b"));
            var aValue = GetString(rawValues, "a");
            var a = string.IsNullOrWhiteSpace(aValue)
                ? 1f
                : NormalizeColor(ParseRequiredFloat(aValue, rowIndex, "a"));

            return new Color(r, g, b, a);
        }

        private static float NormalizeColor(float value)
        {
            return value > 1f ? value / 255f : value;
        }

        private static bool ParseVisible(string value, int rowIndex)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"Row {rowIndex + 1} requires a non-empty 'visible' value.");
            }

            if (bool.TryParse(value, out var boolValue))
            {
                return boolValue;
            }

            return value.Trim().ToLowerInvariant() switch
            {
                "1" => true,
                "0" => false,
                "yes" => true,
                "no" => false,
                _ => throw new ArgumentException(
                    $"Row {rowIndex + 1} has an invalid boolean in 'visible': '{value}'.")
            };
        }
    }
}
