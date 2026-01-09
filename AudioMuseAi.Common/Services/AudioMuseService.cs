using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AudioMuseAi.Common.Services
{
    /// <summary>
    /// Concrete implementation of <see cref="IAudioMuseService"/>,
    /// handling all HTTP interactions with the AudioMuse backend.
    /// </summary>
    public class AudioMuseService : IAudioMuseService
    {
        private readonly HttpClient _http;

        public AudioMuseService(HttpClient httpClient)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            if (_http.BaseAddress is null)
            {
                throw new InvalidOperationException("AudioMuseAI: HttpClient.BaseAddress must be configured.");
            }
        }

        public Task<HttpResponseMessage> HealthCheckAsync(CancellationToken cancellationToken) =>
            _http.GetAsync("/", cancellationToken);

        public Task<HttpResponseMessage> GetPlaylistsAsync(CancellationToken cancellationToken) =>
            _http.GetAsync("/api/playlists", cancellationToken);

        public Task<HttpResponseMessage> StartAnalysisAsync(string jsonPayload, CancellationToken cancellationToken)
        {
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            return _http.PostAsync("/api/analysis/start", content, cancellationToken);
        }

        public Task<HttpResponseMessage> StartClusteringAsync(string jsonPayload, CancellationToken cancellationToken)
        {
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            return _http.PostAsync("/api/clustering/start", content, cancellationToken);
        }

        public Task<HttpResponseMessage> SearchTracksAsync(string? title, string? artist, CancellationToken cancellationToken)
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(title))
            {
                query.Add($"title={Uri.EscapeDataString(title)}");
            }

            if (!string.IsNullOrWhiteSpace(artist))
            {
                query.Add($"artist={Uri.EscapeDataString(artist)}");
            }

            var url = "/api/search_tracks";
            if (query.Count > 0)
            {
                url += "?" + string.Join("&", query);
            }

            return _http.GetAsync(url, cancellationToken);
        }

        public Task<HttpResponseMessage> ClapSearchAsync(string jsonPayload, CancellationToken cancellationToken)
        {
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            return _http.PostAsync("/api/clap/search", content, cancellationToken);
        }

        public Task<HttpResponseMessage> GetSimilarTracksAsync(string? item_id, string? title, string? artist, int n, string? eliminate_duplicates, CancellationToken cancellationToken)
        {
            var query = new List<string> { $"n={n}" };
            if (!string.IsNullOrWhiteSpace(item_id))
            {
                query.Add($"item_id={Uri.EscapeDataString(item_id)}");
            }
            else if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(artist))
            {
                query.Add($"title={Uri.EscapeDataString(title)}");
                query.Add($"artist={Uri.EscapeDataString(artist)}");
            }

            if (!string.IsNullOrWhiteSpace(eliminate_duplicates))
            {
                query.Add($"eliminate_duplicates={eliminate_duplicates.ToLowerInvariant()}");
            }

            var url = "/api/similar_tracks";
            if (query.Count > 1) // n is always present
            {
                url += "?" + string.Join("&", query);
            }

            return _http.GetAsync(url, cancellationToken);
        }

        public Task<HttpResponseMessage> GetSimilarArtistsAsync(string? artist, string? artist_id, int n, int? ef_search, bool? include_component_matches, CancellationToken cancellationToken)
        {
            var query = new List<string> { $"n={n}" };

            if (!string.IsNullOrWhiteSpace(artist_id))
            {
                query.Add($"artist_id={Uri.EscapeDataString(artist_id)}");
            }
            else if (!string.IsNullOrWhiteSpace(artist))
            {
                query.Add($"artist={Uri.EscapeDataString(artist)}");
            }

            if (ef_search.HasValue)
            {
                query.Add($"ef_search={ef_search.Value}");
            }

            if (include_component_matches.HasValue)
            {
                query.Add($"include_component_matches={include_component_matches.Value.ToString().ToLowerInvariant()}");
            }

            var url = "/api/similar_artists?" + string.Join("&", query);
            return _http.GetAsync(url, cancellationToken);
        }

        public Task<HttpResponseMessage> GetMaxDistanceAsync(string? item_id, CancellationToken cancellationToken)
        {
            var url = "/api/max_distance";
            if (!string.IsNullOrWhiteSpace(item_id))
            {
                url += "?item_id=" + Uri.EscapeDataString(item_id);
            }

            return _http.GetAsync(url, cancellationToken);
        }

        public Task<HttpResponseMessage> FindPathAsync(string start_song_id, string end_song_id, int? max_steps, CancellationToken cancellationToken)
        {
            var query = new List<string>
            {
                $"start_song_id={Uri.EscapeDataString(start_song_id)}",
                $"end_song_id={Uri.EscapeDataString(end_song_id)}"
            };

            if (max_steps.HasValue)
            {
                query.Add($"max_steps={max_steps.Value}");
            }

            var url = "/api/find_path?" + string.Join("&", query);
            return _http.GetAsync(url, cancellationToken);
        }

        public Task<HttpResponseMessage> CreatePlaylistAsync(string playlist_name, IEnumerable<string> track_ids, CancellationToken cancellationToken)
        {
            var payload = new
            {
                playlist_name,
                track_ids
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return _http.PostAsync("/api/create_playlist", content, cancellationToken);
        }

        public Task<HttpResponseMessage> GetTaskStatusAsync(string task_id, CancellationToken cancellationToken) =>
            _http.GetAsync($"/api/status/{task_id}", cancellationToken);

        public Task<HttpResponseMessage> CancelTaskAsync(string task_id, CancellationToken cancellationToken) =>
            _http.PostAsync($"/api/cancel/{task_id}", null, cancellationToken);

        public Task<HttpResponseMessage> CancelAllTasksByTypeAsync(string task_type_prefix, CancellationToken cancellationToken) =>
            _http.PostAsync($"/api/cancel_all/{task_type_prefix}", null, cancellationToken);

        public Task<HttpResponseMessage> GetLastTaskAsync(CancellationToken cancellationToken) =>
            _http.GetAsync("/api/last_task", cancellationToken);

        public Task<HttpResponseMessage> GetActiveTasksAsync(CancellationToken cancellationToken) =>
            _http.GetAsync("/api/active_tasks", cancellationToken);

        public Task<HttpResponseMessage> GetConfigAsync(CancellationToken cancellationToken) =>
            _http.GetAsync("/api/config", cancellationToken);

        public Task<HttpResponseMessage> GetChatConfigDefaultsAsync(CancellationToken cancellationToken) =>
            _http.GetAsync("/chat/api/config_defaults", cancellationToken);

        public Task<HttpResponseMessage> PostChatPlaylistAsync(string jsonPayload, CancellationToken cancellationToken)
        {
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            return _http.PostAsync("/chat/api/chatPlaylist", content, cancellationToken);
        }

        public Task<HttpResponseMessage> CreateChatPlaylistAsync(string jsonPayload, CancellationToken cancellationToken)
        {
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            return _http.PostAsync("/chat/api/create_playlist", content, cancellationToken);
        }

        public Task<HttpResponseMessage> GenerateSonicFingerprintAsync(string jellyfin_user_identifier, string? jellyfin_token, int? n, CancellationToken cancellationToken)
        {
            var query = new List<string>
            {
                $"jellyfin_user_identifier={Uri.EscapeDataString(jellyfin_user_identifier)}"
            };

            if (!string.IsNullOrWhiteSpace(jellyfin_token))
            {
                query.Add($"jellyfin_token={Uri.EscapeDataString(jellyfin_token)}");
            }

            if (n.HasValue)
            {
                query.Add($"n={n.Value}");
            }

            var url = "/api/sonic_fingerprint/generate?" + string.Join("&", query);
            return _http.GetAsync(url, cancellationToken);
        }

        public Task<HttpResponseMessage> AlchemyAsync(string jsonPayload, CancellationToken cancellationToken)
        {
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            return _http.PostAsync("/api/alchemy", content, cancellationToken);
        }
    }
}