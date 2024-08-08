using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace LandmarksR.Scripts.Experiment.Log
{
    /// <summary>
    /// Manages a queue of messages to be processed asynchronously.
    /// </summary>
    public class LoggerQueue<T>
    {
        private readonly ConcurrentQueue<T> _messages = new();
        private readonly Func<T, Task> _writeActionAsync;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly int _flushingInterval;
        private Task _processingTask;

        public LoggerQueue(Func<T, Task> writeActionAsync, int flushingInterval = 100)
        {
            _writeActionAsync = writeActionAsync ?? throw new ArgumentNullException(nameof(writeActionAsync));
            _flushingInterval = flushingInterval;
        }

        public void StartProcessingTask()
        {
            _processingTask = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    if (_messages.TryDequeue(out var message))
                    {
                        await _writeActionAsync(message);
                    }
                    else
                    {
                        await Task.Delay(_flushingInterval, _cancellationTokenSource.Token);
                    }
                }

                while (_messages.TryDequeue(out var remainingMessage))
                {
                    await _writeActionAsync(remainingMessage);
                }
            }, _cancellationTokenSource.Token);
        }

        public void EnqueueMessage(T message)
        {
            _messages.Enqueue(message);
        }

        public async Task StopAsync()
        {
            _cancellationTokenSource.Cancel();
            try
            {
                if (_processingTask != null)
                {
                    await _processingTask;
                }
            }
            catch (TaskCanceledException)
            {
            }
        }
    }
}
