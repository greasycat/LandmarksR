using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LandmarksR.Scripts.Experiment.Log
{
    /// <summary>
    /// Asynchronously writes text lines to a local file.
    /// </summary>
    public class LocalLogger : IAsyncDisposable
    {
        private readonly LoggerQueue<string> _loggerQueue;
        private readonly SemaphoreSlim _asyncLock = new(1, 1);
        private StreamWriter _streamWriter;
        private readonly bool _ready;

        public LocalLogger(string fileName, int flushingInterval = 100)
        {
            if (!ValidateAndSetOutputFile(fileName))
            {
                return;
            }

            _streamWriter = new StreamWriter(fileName, append: true)
            {
                AutoFlush = true
            };

            _loggerQueue = new LoggerQueue<string>(WriteLogAsync, flushingInterval);
            _loggerQueue.StartProcessingTask();
            _ready = true;
        }

        public void Log(string line)
        {
            if (!_ready || line == null)
            {
                return;
            }

            _loggerQueue.EnqueueMessage(line);
        }

        private async Task WriteLogAsync(string line)
        {
            await _asyncLock.WaitAsync();
            try
            {
                await _streamWriter.WriteLineAsync(line);
                await _streamWriter.FlushAsync();
            }
            finally
            {
                _asyncLock.Release();
            }
        }

        public async Task StopAsync()
        {
            if (!_ready)
            {
                return;
            }

            await _loggerQueue.StopAsync();
            await DisposeAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _asyncLock.WaitAsync();
            try
            {
                if (_streamWriter != null)
                {
                    await _streamWriter.DisposeAsync();
                    _streamWriter = null;
                }
            }
            finally
            {
                _asyncLock.Release();
                _asyncLock.Dispose();
            }
        }

        private static bool ValidateAndSetOutputFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            try
            {
                var directory = Path.GetDirectoryName(fileName);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!File.Exists(fileName))
                {
                    File.Create(fileName).Close();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
