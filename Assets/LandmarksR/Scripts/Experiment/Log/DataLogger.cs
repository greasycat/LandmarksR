using System.Collections.Generic;
using System.Linq;

namespace LandmarksR.Scripts.Experiment.Log
{
    /// <summary>
    /// Writes structured dataset rows to JSONL, TSV, and CSV.
    /// </summary>
    public class DataLogger
    {
        private readonly StructuredLogFileSet<DatasetLogRecord> _logFileSet;
        private readonly HashSet<string> _datasetColumns;
        private readonly List<string> _orderedDatasetColumns;
        private readonly Dictionary<string, string> _currentRow = new();
        private readonly string _datasetName;
        private readonly Settings _settings;
        private string _currentSubjectId = string.Empty;
        private string _currentRunSessionId = string.Empty;
        private int _rowCount;

        public DataLogger(string localDirectoryPath, string remoteDirectoryPath, string datasetName,
            IReadOnlyList<string> columns, Settings settings)
        {
            _datasetName = datasetName;
            _settings = settings;
            _orderedDatasetColumns = columns
                .Where(column => !string.IsNullOrWhiteSpace(column))
                .Where(column => column != ExperimentMetadataColumns.SubjectId &&
                                 column != ExperimentMetadataColumns.RunSessionId)
                .ToList();
            _datasetColumns = new HashSet<string>(_orderedDatasetColumns);

            var fileColumns = StructuredLogging.BuildDatasetColumns(columns);
            _logFileSet = new StructuredLogFileSet<DatasetLogRecord>(localDirectoryPath, remoteDirectoryPath,
                datasetName, fileColumns,
                record => StructuredLogging.BuildDatasetRow(record, _orderedDatasetColumns), settings.logging);
        }

        public int RowCount => _rowCount;

        public void SetValue(string column, string value)
        {
            var normalizedValue = NormalizeValue(value);

            if (column == ExperimentMetadataColumns.SubjectId)
            {
                _currentSubjectId = normalizedValue;
                return;
            }

            if (column == ExperimentMetadataColumns.RunSessionId)
            {
                _currentRunSessionId = normalizedValue;
                return;
            }

            if (!_datasetColumns.Contains(column))
            {
                throw new KeyNotFoundException($"Column {column} not found in initialized dataset columns");
            }

            _currentRow[column] = normalizedValue;
        }

        public void Log()
        {
            var timestamp = LogTimestamp.CreateNow();
            var record = new DatasetLogRecord
            {
                dataset_name = _datasetName,
                ts_utc = timestamp.UtcIso8601,
                ts_unix_ms = timestamp.UnixMilliseconds,
                run_session_id = string.IsNullOrWhiteSpace(_currentRunSessionId)
                    ? _settings.experiment.GetRunSessionIdOrCreate()
                    : _currentRunSessionId,
                subject_id = string.IsNullOrWhiteSpace(_currentSubjectId)
                    ? _settings.experiment.GetSubjectIdOrDefault()
                    : _currentSubjectId,
                row = new Dictionary<string, string>(_currentRow)
            };

            _logFileSet.Log(record);
            _currentRow.Clear();
            _currentSubjectId = string.Empty;
            _currentRunSessionId = string.Empty;
            _rowCount++;
        }

        public async void End()
        {
            await StopAsync();
        }

        public async System.Threading.Tasks.Task StopAsync()
        {
            await _logFileSet.StopAsync();
        }

        private static string NormalizeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            {
                return value.Substring(1, value.Length - 2).Replace("\"\"", "\"");
            }

            return value;
        }
    }
}
