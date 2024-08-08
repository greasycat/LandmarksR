using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace LandmarksR.Scripts.Experiment.Log
{
    /// <summary>
    /// Sends serialized JSON records to the configured remote endpoint.
    /// </summary>
    public class RemoteLogger
    {
        private readonly LoggerQueue<string> _loggerQueue;
        private static readonly HttpClient HttpClient = new();
        private readonly string _logUrl;
        private readonly string _filePath;
        private bool _ready;

        public RemoteLogger(string filePath, string statusUrl, string logUrl, int flushingInterval = 100)
        {
            _filePath = filePath;
            _logUrl = logUrl;
            _loggerQueue = new LoggerQueue<string>(WriteLogAsync, flushingInterval);

            ValidateApiAsync(statusUrl).ContinueWith(task =>
            {
                if (!task.Result)
                {
                    Debug.LogWarning("Invalid status url or server is currently down.");
                    return;
                }

                _ready = true;
                _loggerQueue.StartProcessingTask();
            });
        }

        public void Log(string recordJson)
        {
            if (string.IsNullOrWhiteSpace(recordJson))
            {
                return;
            }

            _loggerQueue.EnqueueMessage(recordJson);
        }

        private async Task WriteLogAsync(string recordJson)
        {
            var payload = JsonConvert.SerializeObject(new
            {
                filePath = _filePath,
                record = JsonConvert.DeserializeObject(recordJson)
            });

            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            try
            {
                var response = await HttpClient.PostAsync(_logUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogWarning("Failed to log message remotely.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception occurred while logging remotely: {ex.Message}");
            }
        }

        public async Task StopAsync()
        {
            if (!_ready)
            {
                return;
            }

            await _loggerQueue.StopAsync();
        }

        private static async Task<bool> ValidateApiAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            try
            {
                var response = await HttpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
                var status = JsonConvert.DeserializeObject<Dictionary<string, string>>(result);
                return status.ContainsKey("status") && status["status"] == "ok";
            }
            catch
            {
                return false;
            }
        }
    }
}
