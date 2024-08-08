using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace LandmarksR.Scripts.Experiment.Log
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    public static class StructuredLoggingDefaults
    {
        public const string JsonLineExtension = "jsonl";
        public const string EventBaseFileName = "events";
        public static readonly string[] EventColumns =
        {
            "ts_utc",
            "ts_unix_ms",
            "level",
            "source",
            "event_name",
            "run_session_id",
            "subject_id",
            "task_name",
            "dataset_name",
            "repeat_index",
            "subtask_index",
            "trial_id",
            "message",
            "payload_json"
        };

        public static readonly string[] DatasetEnvelopeColumns =
        {
            ExperimentMetadataColumns.RunSessionId,
            ExperimentMetadataColumns.SubjectId,
            "ts_utc",
            "ts_unix_ms",
            "dataset_name"
        };
    }

    public sealed class LogTimestamp
    {
        public string UtcIso8601 { get; private set; }
        public long UnixMilliseconds { get; private set; }

        public static LogTimestamp CreateNow()
        {
            var now = DateTimeOffset.UtcNow;
            return new LogTimestamp
            {
                UtcIso8601 = now.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
                UnixMilliseconds = now.ToUnixTimeMilliseconds()
            };
        }
    }

    [Serializable]
    public sealed class EventLogRecord
    {
        public string record_type = "event";
        public string source;
        public string level;
        public string event_name;
        public string ts_utc;
        public long ts_unix_ms;
        public string run_session_id;
        public string subject_id;
        public string task_name;
        public string dataset_name;
        public int? repeat_index;
        public int? subtask_index;
        public string trial_id;
        public string message;
        public Dictionary<string, object> payload = new();
    }

    [Serializable]
    public sealed class DatasetLogRecord
    {
        public string record_type = "dataset_row";
        public string source = "dataset";
        public string dataset_name;
        public string ts_utc;
        public long ts_unix_ms;
        public string run_session_id;
        public string subject_id;
        public Dictionary<string, string> row = new();
    }

    public static class StructuredLogging
    {
        public static string SerializeRecord(object record)
        {
            return JsonConvert.SerializeObject(record, Formatting.None);
        }

        public static string SerializePayload(Dictionary<string, object> payload)
        {
            return payload == null || payload.Count == 0
                ? string.Empty
                : JsonConvert.SerializeObject(payload, Formatting.None);
        }

        public static string ToLevelString(LogLevel level)
        {
            return level.ToString().ToLowerInvariant();
        }

        public static List<string> BuildDatasetColumns(IEnumerable<string> datasetColumns)
        {
            var columns = new List<string>(StructuredLoggingDefaults.DatasetEnvelopeColumns);
            foreach (var column in datasetColumns.Where(column => !string.IsNullOrWhiteSpace(column)))
            {
                if (columns.Contains(column))
                {
                    continue;
                }

                columns.Add(column);
            }

            return columns;
        }

        public static Dictionary<string, string> BuildEventRow(EventLogRecord record)
        {
            return new Dictionary<string, string>
            {
                ["ts_utc"] = record.ts_utc ?? string.Empty,
                ["ts_unix_ms"] = record.ts_unix_ms.ToString(CultureInfo.InvariantCulture),
                ["level"] = record.level ?? string.Empty,
                ["source"] = record.source ?? string.Empty,
                ["event_name"] = record.event_name ?? string.Empty,
                ["run_session_id"] = record.run_session_id ?? string.Empty,
                ["subject_id"] = record.subject_id ?? string.Empty,
                ["task_name"] = record.task_name ?? string.Empty,
                ["dataset_name"] = record.dataset_name ?? string.Empty,
                ["repeat_index"] = record.repeat_index?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                ["subtask_index"] = record.subtask_index?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                ["trial_id"] = record.trial_id ?? string.Empty,
                ["message"] = record.message ?? string.Empty,
                ["payload_json"] = SerializePayload(record.payload)
            };
        }

        public static Dictionary<string, string> BuildDatasetRow(DatasetLogRecord record, IEnumerable<string> datasetColumns)
        {
            var row = new Dictionary<string, string>
            {
                [ExperimentMetadataColumns.RunSessionId] = record.run_session_id ?? string.Empty,
                [ExperimentMetadataColumns.SubjectId] = record.subject_id ?? string.Empty,
                ["ts_utc"] = record.ts_utc ?? string.Empty,
                ["ts_unix_ms"] = record.ts_unix_ms.ToString(CultureInfo.InvariantCulture),
                ["dataset_name"] = record.dataset_name ?? string.Empty
            };

            foreach (var column in datasetColumns)
            {
                if (row.ContainsKey(column))
                {
                    continue;
                }

                row[column] = record.row.GetValueOrDefault(column, string.Empty) ?? string.Empty;
            }

            return row;
        }

        public static string FormatDelimitedRow(IReadOnlyList<string> columns, IReadOnlyDictionary<string, string> row, char delimiter)
        {
            var values = columns.Select(column => EscapeField(row.GetValueOrDefault(column, string.Empty) ?? string.Empty, delimiter));
            return string.Join(delimiter.ToString(), values);
        }

        public static string FormatDelimitedRow(IReadOnlyList<string> columns, char delimiter)
        {
            var header = columns.ToDictionary(column => column, column => column);
            return FormatDelimitedRow(columns, header, delimiter);
        }

        private static string EscapeField(string value, char delimiter)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var requiresQuotes = value.IndexOfAny(new[] { delimiter, '"', '\n', '\r' }) >= 0;
            if (!requiresQuotes)
            {
                return value;
            }

            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }
    }

    public static class LegacyEventAdapter
    {
        public static EventLogRecord Create(string source, LogLevel level, string message, string runSessionId, string subjectId)
        {
            var timestamp = LogTimestamp.CreateNow();
            var record = new EventLogRecord
            {
                source = source ?? string.Empty,
                level = StructuredLogging.ToLevelString(level),
                event_name = "message_logged",
                ts_utc = timestamp.UtcIso8601,
                ts_unix_ms = timestamp.UnixMilliseconds,
                run_session_id = runSessionId ?? string.Empty,
                subject_id = subjectId ?? string.Empty,
                message = message ?? string.Empty
            };

            if (TryPopulateTaskLifecycle(record, message) ||
                TryPopulateSubjectRegistration(record, message) ||
                TryPopulateRunSession(record, message) ||
                TryPopulateApplicationEvent(record, message) ||
                TryPopulateSceneCounters(record, message))
            {
                return record;
            }

            return record;
        }

        private static bool TryPopulateTaskLifecycle(EventLogRecord record, string message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length < 12 || message[0] != '(')
            {
                return false;
            }

            const string startedSuffix = ") Started";
            const string finishedSuffix = ") Finished";

            if (message.EndsWith(startedSuffix, StringComparison.Ordinal))
            {
                record.task_name = message.Substring(1, message.Length - startedSuffix.Length - 1);
                record.event_name = "task_started";
                return true;
            }

            if (message.EndsWith(finishedSuffix, StringComparison.Ordinal))
            {
                record.task_name = message.Substring(1, message.Length - finishedSuffix.Length - 1);
                record.event_name = "task_finished";
                return true;
            }

            return false;
        }

        private static bool TryPopulateSubjectRegistration(EventLogRecord record, string message)
        {
            const string prefix = "SubjectRegistered:";
            if (!message.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            var subjectId = message.Substring(prefix.Length).Trim();
            record.event_name = "subject_registered";
            record.subject_id = subjectId;
            record.payload["subject_id"] = subjectId;
            return true;
        }

        private static bool TryPopulateRunSession(EventLogRecord record, string message)
        {
            const string prefix = "RunSessionStarted:";
            if (!message.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            var runSessionId = message.Substring(prefix.Length).Trim();
            record.event_name = "run_session_started";
            record.run_session_id = runSessionId;
            record.payload["run_session_id"] = runSessionId;
            return true;
        }

        private static bool TryPopulateApplicationEvent(EventLogRecord record, string message)
        {
            switch (message)
            {
                case "Start Application":
                    record.event_name = "application_started";
                    return true;
                case "Finish Application":
                    record.event_name = "application_finished";
                    return true;
                case "Load Next Scene":
                    record.event_name = "load_next_scene";
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryPopulateSceneCounters(EventLogRecord record, string message)
        {
            const string nextScenePrefix = "Next Scene Index: ";
            if (message.StartsWith(nextScenePrefix, StringComparison.Ordinal))
            {
                record.event_name = "next_scene_index";
                record.payload["next_scene_index"] = message.Substring(nextScenePrefix.Length).Trim();
                return true;
            }

            const string totalScenesPrefix = "Total Scenes: ";
            if (!message.StartsWith(totalScenesPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            record.event_name = "total_scenes";
            record.payload["total_scenes"] = message.Substring(totalScenesPrefix.Length).Trim();
            return true;
        }
    }
}
