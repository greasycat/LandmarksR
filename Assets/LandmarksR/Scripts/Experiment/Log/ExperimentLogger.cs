using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LandmarksR.Scripts.Experiment.Log
{
    /// <summary>
    /// Manages logging for the experiment, including general logging and data logging.
    /// </summary>
    public class ExperimentLogger : MonoBehaviour
    {
        public static ExperimentLogger Instance { get; private set; }

        [SerializeField] private List<string> tagToPrint = new() { "default" };

        private bool CheckTag(string messageTag) => tagToPrint.IndexOf(messageTag) >= 0;

        private StructuredLogFileSet<EventLogRecord> _generalLogger;
        private Settings _settings;
        private readonly Dictionary<string, DataLogger> _dataLoggers = new();
        private string _runDirectoryPath;
        private string _runDirectoryRelativePath;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(Instance.gameObject);
            }
            else
            {
                Instance = this;
            }

            Init();
            DontDestroyOnLoad(this);
        }

        private void Init()
        {
            _settings = Settings.Instance;
            _settings.logging.ApplyDefaults();
            _settings.experiment.StartNewRunSession();
            _runDirectoryPath = GetRunDirectoryPath();
            _runDirectoryRelativePath = GetRunDirectoryRelativePath();
            _generalLogger = new StructuredLogFileSet<EventLogRecord>(_runDirectoryPath, _runDirectoryRelativePath,
                _settings.logging.GetEventBaseFileName(), StructuredLoggingDefaults.EventColumns,
                StructuredLogging.BuildEventRow, _settings.logging);

            LogStructuredEvent("session", LogLevel.Info, "run_session_started", "Run session started",
                payload: new Dictionary<string, object>
                {
                    [ExperimentMetadataColumns.RunSessionId] = _settings.experiment.GetRunSessionIdOrCreate()
                });
        }

        public void BeginDataset(string setName, List<string> columnNames)
        {
            if (string.IsNullOrWhiteSpace(setName))
            {
                E("output", "Dataset name is required.");
                return;
            }

            if (_dataLoggers.ContainsKey(setName))
            {
                E("output", $"Data logger for {setName} already exists.");
                return;
            }

            var normalizedColumns = (columnNames ?? new List<string>())
                .Where(column => !string.IsNullOrWhiteSpace(column))
                .Distinct()
                .ToList();

            var dataLogger = new DataLogger(_runDirectoryPath, _runDirectoryRelativePath, setName, normalizedColumns,
                _settings);
            _dataLoggers.Add(setName, dataLogger);

            LogStructuredEvent("output", LogLevel.Info, "dataset_started", $"Dataset started: {setName}",
                datasetName: setName,
                payload: new Dictionary<string, object>
                {
                    ["column_count"] = normalizedColumns.Count,
                    ["columns"] = normalizedColumns
                });
        }

        public void SetData(string setName, string column, string value)
        {
            if (!_dataLoggers.ContainsKey(setName))
            {
                E("output", $"Data logger for {setName} not found.");
                return;
            }

            _dataLoggers[setName].SetValue(column, value);
        }

        public void LogDataRow(string setName)
        {
            if (!_dataLoggers.ContainsKey(setName))
            {
                E("output", $"Data logger for {setName} not found.");
                return;
            }

            _dataLoggers[setName].Log();
        }

        public void EndDataset(string setName)
        {
            if (!_dataLoggers.ContainsKey(setName))
            {
                E("output", $"Data logger for {setName} not found.");
                return;
            }

            var dataLogger = _dataLoggers[setName];
            LogStructuredEvent("output", LogLevel.Info, "dataset_finished", $"Dataset finished: {setName}",
                datasetName: setName,
                payload: new Dictionary<string, object>
                {
                    ["row_count"] = dataLogger.RowCount
                });
            dataLogger.End();
            _dataLoggers.Remove(setName);
        }

        public void I(string messageTag, object message)
        {
            LogLegacyEvent(messageTag, LogLevel.Info, message);
#if UNITY_EDITOR
            if (CheckTag(messageTag))
                Debug.Log($"[LMR] <color=green>INFO</color> | {messageTag} | {message}");
#endif
        }

        public void W(string messageTag, object message)
        {
            LogLegacyEvent(messageTag, LogLevel.Warning, message);
#if UNITY_EDITOR
            Debug.LogWarning($"[LMR] <color=yellow>WARNING</color> | {messageTag} | {message}");
#endif
        }

        public void E(string messageTag, object message)
        {
            LogLegacyEvent(messageTag, LogLevel.Error, message);
#if UNITY_EDITOR
            Debug.LogError($"[LMR] <color=red>ERROR</color> | {messageTag} | {message}");
            EditorApplication.isPlaying = false;
#endif
        }

        private void LogLegacyEvent(string messageTag, LogLevel level, object message)
        {
            if (_generalLogger == null)
            {
                return;
            }

            var record = LegacyEventAdapter.Create(messageTag, level, message?.ToString() ?? string.Empty,
                _settings.experiment.GetRunSessionIdOrCreate(),
                _settings.experiment.GetSubjectIdOrDefault());
            _generalLogger.Log(record);
        }

        private void LogStructuredEvent(string source, LogLevel level, string eventName, string message,
            string taskName = null, string datasetName = null, string trialId = null, int? repeatIndex = null,
            int? subtaskIndex = null, Dictionary<string, object> payload = null)
        {
            if (_generalLogger == null)
            {
                return;
            }

            var timestamp = LogTimestamp.CreateNow();
            var record = new EventLogRecord
            {
                source = source ?? string.Empty,
                level = StructuredLogging.ToLevelString(level),
                event_name = eventName ?? "message_logged",
                ts_utc = timestamp.UtcIso8601,
                ts_unix_ms = timestamp.UnixMilliseconds,
                run_session_id = _settings.experiment.GetRunSessionIdOrCreate(),
                subject_id = _settings.experiment.GetSubjectIdOrDefault(),
                task_name = taskName,
                dataset_name = datasetName,
                repeat_index = repeatIndex,
                subtask_index = subtaskIndex,
                trial_id = trialId,
                message = message ?? string.Empty,
                payload = payload ?? new Dictionary<string, object>()
            };

            _generalLogger.Log(record);
        }

        private string GetRunDirectoryPath()
        {
            var runSessionId = _settings.experiment.GetRunSessionIdOrCreate();
            return Path.Combine(Application.persistentDataPath, runSessionId);
        }

        private string GetRunDirectoryRelativePath()
        {
            var runSessionId = _settings.experiment.GetRunSessionIdOrCreate();
            return $"{Application.productName}/{runSessionId}";
        }

        private async void OnDisable()
        {
            if (_generalLogger != null)
            {
                await _generalLogger.StopAsync();
            }

            foreach (var dataLogger in _dataLoggers.Values.ToList())
            {
                await dataLogger.StopAsync();
            }
        }
    }
}
