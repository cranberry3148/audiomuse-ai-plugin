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
    public class ClusteringScheduledTask : IScheduledTask
    {
        private readonly ILogger _logger;

        public ClusteringScheduledTask(ILogManager logManager)
        {
            _logger = logManager.GetLogger(GetType().Name);
        }

        public string Name => "AudioMuse AI Playlist Clustering";

        public string Key => "AudioMuseClustering";

        public string Description => "Periodically runs clustering on the library to generate and update playlists.";

        public string Category => "Library";

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerDaily,
                    TimeOfDayTicks = TimeSpan.FromHours(2).Ticks,
                    DayOfWeek = DayOfWeek.Sunday
                }
            };
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _logger.Info("Starting AudioMuse AI clustering scheduled task.");

            cancellationToken.ThrowIfCancellationRequested();

            var audioMuseService = Plugin.Instance?.AudioMuseService;
            if (audioMuseService == null)
            {
                _logger.Error("AudioMuseService is not available.");
                return;
            }

            var jsonPayload = JsonSerializer.Serialize(new { });
            var response = await audioMuseService.StartClusteringAsync(jsonPayload, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.Info("Successfully initiated AudioMuse AI clustering task.");
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.Error("Failed to start AudioMuse AI clustering task. Status Code: {0}. Response: {1}", response.StatusCode, errorBody);
            }

            progress.Report(100.0);
        }
    }
}