using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AudioMuseAi.Common.Services;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Logging;

namespace Emby.Plugin.AudioMuseAi.Tasks
{
    public class AnalysisScheduledTask : IScheduledTask
    {
        private readonly ILogger _logger;

        public AnalysisScheduledTask(ILogManager logManager)
        {
            _logger = logManager.GetLogger(GetType().Name);
        }

        public string Name => "AudioMuse AI Library Analysis";

        public string Key => "AudioMuseAnalysis";

        public string Description => "Periodically analyzes the music library to generate and update AudioMuse AI data.";

        public string Category => "Library";

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerDaily,
                    TimeOfDayTicks = TimeSpan.FromHours(2).Ticks
                }
            };
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _logger.Info("Starting AudioMuse AI analysis scheduled task.");

            cancellationToken.ThrowIfCancellationRequested();

            var audioMuseService = Plugin.Instance?.AudioMuseService;
            if (audioMuseService == null)
            {
                _logger.Error("AudioMuseService is not available.");
                return;
            }

            var jsonPayload = JsonSerializer.Serialize(new { });
            var response = await audioMuseService.StartAnalysisAsync(jsonPayload, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.Info("Successfully initiated AudioMuse AI analysis task.");
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.Error("Failed to start AudioMuse AI analysis task. Status Code: {0}. Response: {1}", response.StatusCode, errorBody);
            }

            progress.Report(100.0);
        }
    }
}