using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LandmarksR.Scripts.Experiment.Log
{
    public sealed class StructuredLogFileSet<TRecord> : IAsyncDisposable
    {
        private readonly Func<TRecord, Dictionary<string, string>> _rowBuilder;
        private readonly LocalLogger _jsonlLogger;
        private readonly LocalLogger _tsvLogger;
        private readonly LocalLogger _csvLogger;
        private readonly RemoteLogger _remoteLogger;
        private readonly IReadOnlyList<string> _columns;

        public StructuredLogFileSet(string localDirectoryPath, string remoteDirectoryPath, string baseFileName,
            IReadOnlyList<string> columns,
            Func<TRecord, Dictionary<string, string>> rowBuilder, LoggingSettings loggingSettings)
        {
            loggingSettings.ApplyDefaults();
            _columns = columns;
            _rowBuilder = rowBuilder;
            var jsonFileName = $"{baseFileName}.{loggingSettings.GetJsonFileExtension()}";

            if (loggingSettings.localLogging)
            {
                _jsonlLogger = new LocalLogger(Path.Combine(localDirectoryPath, jsonFileName));

                if (loggingSettings.ShouldExportTsv())
                {
                    var tsvPath = Path.Combine(localDirectoryPath, $"{baseFileName}.tsv");
                    var needsHeader = IsFileEmpty(tsvPath);
                    _tsvLogger = new LocalLogger(tsvPath);
                    if (needsHeader)
                    {
                        _tsvLogger.Log(StructuredLogging.FormatDelimitedRow(columns, '\t'));
                    }
                }

                if (loggingSettings.ShouldExportCsv())
                {
                    var csvPath = Path.Combine(localDirectoryPath, $"{baseFileName}.csv");
                    var needsHeader = IsFileEmpty(csvPath);
                    _csvLogger = new LocalLogger(csvPath);
                    if (needsHeader)
                    {
                        _csvLogger.Log(StructuredLogging.FormatDelimitedRow(columns, ','));
                    }
                }
            }

            if (loggingSettings.remoteLogging)
            {
                var remoteFilePath = BuildRemoteFilePath(remoteDirectoryPath, jsonFileName);
                _remoteLogger = new RemoteLogger(remoteFilePath,
                    loggingSettings.remoteStatusUrl, loggingSettings.remoteLogUrl);
            }
        }

        public void Log(TRecord record)
        {
            var json = StructuredLogging.SerializeRecord(record);
            _jsonlLogger?.Log(json);
            _remoteLogger?.Log(json);

            var row = _rowBuilder(record);
            if (_tsvLogger != null)
            {
                _tsvLogger.Log(StructuredLogging.FormatDelimitedRow(_columns, row, '\t'));
            }

            if (_csvLogger != null)
            {
                _csvLogger.Log(StructuredLogging.FormatDelimitedRow(_columns, row, ','));
            }
        }

        public async Task StopAsync()
        {
            if (_jsonlLogger != null)
            {
                await _jsonlLogger.StopAsync();
            }

            if (_tsvLogger != null)
            {
                await _tsvLogger.StopAsync();
            }

            if (_csvLogger != null)
            {
                await _csvLogger.StopAsync();
            }

            if (_remoteLogger != null)
            {
                await _remoteLogger.StopAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
        }

        private static string BuildRemoteFilePath(string directoryPath, string fileName)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return fileName;
            }

            return Path.Combine(directoryPath, fileName).Replace('\\', '/');
        }

        private static bool IsFileEmpty(string filePath)
        {
            return !File.Exists(filePath) || new FileInfo(filePath).Length == 0;
        }
    }
}
