using System;
using System.Collections.Generic;
using LandmarksR.Scripts.Experiment.Data;
using UnityEngine;

namespace LandmarksR.Scripts.Experiment.Tasks.Cognitive
{
    [Serializable]
    public class ResponseBinding
    {
        public string response;
        public KeyCode key = KeyCode.Space;
    }

    public static class CognitiveTrialColumns
    {
        public const string TrialId = "trial_id";
        public const string TaskType = "task_type";
        public const string Condition = "condition";
        public const string Stimulus = "stimulus";
        public const string SelectedResponse = "selected_response";
        public const string CorrectResponse = "correct_response";
        public const string IsCorrect = "is_correct";
        public const string HasResponse = "has_response";
        public const string ReactionTimeMs = "reaction_time_ms";
        public const string FixationMs = "fixation_ms";
        public const string StimulusMs = "stimulus_ms";
        public const string ResponseWindowMs = "response_window_ms";
    }

    public sealed class CognitiveTrialResult
    {
        public CognitiveTrialResult(string selectedResponse, string correctResponse, bool hasResponse, bool isCorrect,
            float? reactionTimeMs)
        {
            SelectedResponse = selectedResponse ?? string.Empty;
            CorrectResponse = correctResponse ?? string.Empty;
            HasResponse = hasResponse;
            IsCorrect = isCorrect;
            ReactionTimeMs = reactionTimeMs;
        }

        public string SelectedResponse { get; }
        public string CorrectResponse { get; }
        public bool HasResponse { get; }
        public bool IsCorrect { get; }
        public float? ReactionTimeMs { get; }
    }

    public static class CognitiveTrialEvaluator
    {
        public static CognitiveTrialResult Evaluate(string selectedResponse, string correctResponse, float? reactionTimeMs)
        {
            var normalizedSelected = Normalize(selectedResponse);
            var normalizedCorrect = Normalize(correctResponse);
            var hasResponse = !string.IsNullOrWhiteSpace(normalizedSelected);
            var isCorrect = hasResponse &&
                            !string.IsNullOrWhiteSpace(normalizedCorrect) &&
                            string.Equals(normalizedSelected, normalizedCorrect, StringComparison.OrdinalIgnoreCase);

            return new CognitiveTrialResult(normalizedSelected, normalizedCorrect, hasResponse, isCorrect,
                hasResponse ? reactionTimeMs : null);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public static class CognitiveTrialDataUtilities
    {
        public static string GetRequiredString(DataFrame dataFrame, string columnName, string ownerName)
        {
            if (!dataFrame.HasColumn(columnName))
            {
                throw new ArgumentException($"{ownerName} requires column '{columnName}'.");
            }

            var value = dataFrame.GetFirstInColumn<object>(columnName)?.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{ownerName} requires a non-empty value in column '{columnName}'.");
            }

            return value.Trim();
        }

        public static string GetOptionalString(DataFrame dataFrame, string columnName, string fallback = "")
        {
            if (!dataFrame.HasColumn(columnName))
            {
                return fallback;
            }

            var value = dataFrame.GetFirstInColumn<object>(columnName)?.ToString();
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        public static int GetOptionalInt(DataFrame dataFrame, string columnName, int fallback)
        {
            var value = GetOptionalString(dataFrame, columnName);
            return int.TryParse(value, out var result) ? result : fallback;
        }

        public static string ToLogString(float? value)
        {
            return value.HasValue ? Mathf.RoundToInt(value.Value).ToString() : string.Empty;
        }

        public static Color ParseColor(string rawValue, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return fallback;
            }

            if (ColorUtility.TryParseHtmlString(rawValue, out var htmlColor))
            {
                return htmlColor;
            }

            return rawValue.Trim().ToLowerInvariant() switch
            {
                "black" => Color.black,
                "blue" => Color.blue,
                "cyan" => Color.cyan,
                "green" => Color.green,
                "grey" => Color.grey,
                "gray" => Color.gray,
                "magenta" => Color.magenta,
                "orange" => new Color(1f, 0.5f, 0f),
                "red" => Color.red,
                "white" => Color.white,
                "yellow" => Color.yellow,
                _ => fallback
            };
        }
    }
}
