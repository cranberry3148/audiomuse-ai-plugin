using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AudioMuseAi.Common.Services
{
    /// <summary>
    /// Defines the interface for the AudioMuse service client.
    /// </summary>
    public interface IAudioMuseService
    {
        Task<HttpResponseMessage> HealthCheckAsync(CancellationToken cancellationToken);

        Task<HttpResponseMessage> GetPlaylistsAsync(CancellationToken cancellationToken);

        Task<HttpResponseMessage> StartAnalysisAsync(string jsonPayload, CancellationToken cancellationToken);

        Task<HttpResponseMessage> StartClusteringAsync(string jsonPayload, CancellationToken cancellationToken);

        Task<HttpResponseMessage> SearchTracksAsync(string? title, string? artist, CancellationToken cancellationToken);

        Task<HttpResponseMessage> ClapSearchAsync(string jsonPayload, CancellationToken cancellationToken);

        Task<HttpResponseMessage> GetSimilarTracksAsync(string? item_id, string? title, string? artist, int n, string? eliminate_duplicates, CancellationToken cancellationToken);

        Task<HttpResponseMessage> GetSimilarArtistsAsync(string? artist, string? artist_id, int n, int? ef_search, bool? include_component_matches, CancellationToken cancellationToken);

        Task<HttpResponseMessage> GetMaxDistanceAsync(string? item_id, CancellationToken cancellationToken);

        Task<HttpResponseMessage> FindPathAsync(string start_song_id, string end_song_id, int? max_steps, CancellationToken cancellationToken);

        Task<HttpResponseMessage> CreatePlaylistAsync(string playlist_name, IEnumerable<string> track_ids, CancellationToken cancellationToken);

        Task<HttpResponseMessage> GetTaskStatusAsync(string task_id, CancellationToken cancellationToken);

        Task<HttpResponseMessage> CancelTaskAsync(string task_id, CancellationToken cancellationToken);

        Task<HttpResponseMessage> CancelAllTasksByTypeAsync(string task_type_prefix, CancellationToken cancellationToken);

        Task<HttpResponseMessage> GetLastTaskAsync(CancellationToken cancellationToken);

        Task<HttpResponseMessage> GetActiveTasksAsync(CancellationToken cancellationToken);

        Task<HttpResponseMessage> GetConfigAsync(CancellationToken cancellationToken);

        Task<HttpResponseMessage> GetChatConfigDefaultsAsync(CancellationToken cancellationToken);

        Task<HttpResponseMessage> PostChatPlaylistAsync(string jsonPayload, CancellationToken cancellationToken);

        Task<HttpResponseMessage> CreateChatPlaylistAsync(string jsonPayload, CancellationToken cancellationToken);

        Task<HttpResponseMessage> GenerateSonicFingerprintAsync(string jellyfin_user_identifier, string? jellyfin_token, int? n, CancellationToken cancellationToken);

        Task<HttpResponseMessage> AlchemyAsync(string jsonPayload, CancellationToken cancellationToken);
    }
}

