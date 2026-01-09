using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AudioMuseAi.Common.Models;
using AudioMuseAi.Common.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Entities;

namespace Emby.Plugin.AudioMuseAi.Api
{
    [Route("/AudioMuseAI/InstantMix/{Id}", "GET")]
    public class GetAudioMuseInstantMix : IReturn<object>
    {
        [ApiMember(Name = "Id", ParameterType = "path", IsRequired = true)]
        public string Id { get; set; }

        [ApiMember(Name = "UserId", ParameterType = "query", IsRequired = false)]
        public string? UserId { get; set; }

        [ApiMember(Name = "Limit", ParameterType = "query", IsRequired = false)]
        public int? Limit { get; set; }

        [ApiMember(Name = "Fields", ParameterType = "query", IsRequired = false)]
        public string? Fields { get; set; }
    }

    [Route("/AudioMuseAI/info", "GET")]
    public class GetInfo : IReturn<string>
    {
    }

    [Route("/AudioMuseAI/health", "GET")]
    public class GetHealth : IReturnVoid
    {
    }

    [Route("/AudioMuseAI/playlists", "GET")]
    public class GetPlaylists : IReturn<string>
    {
    }

    [Route("/AudioMuseAI/active_tasks", "GET")]
    public class GetActiveTasks : IReturn<string>
    {
    }

    [Route("/AudioMuseAI/last_task", "GET")]
    public class GetLastTask : IReturn<string>
    {
    }

    [Route("/AudioMuseAI/status/{TaskId}", "GET")]
    public class GetTaskStatus : IReturn<string>
    {
        public string TaskId { get; set; }
    }

    [Route("/AudioMuseAI/analysis", "POST")]
    public class StartAnalysis : IReturn<string>
    {
    }

    [Route("/AudioMuseAI/clustering", "POST")]
    public class StartClustering : IReturn<string>
    {
    }

    [Route("/AudioMuseAI/search_tracks", "GET")]
    public class SearchTracks : IReturn<string>
    {
        public string? title { get; set; }
        public string? artist { get; set; }
    }

    [Route("/AudioMuseAI/similar_tracks", "GET")]
    public class GetSimilarTracks : IReturn<string>
    {
        public string? item_id { get; set; }
        public string? title { get; set; }
        public string? artist { get; set; }
        public int n { get; set; }
        public string? eliminate_duplicates { get; set; }
    }

    [Route("/AudioMuseAI/similar_artists", "GET")]
    public class GetSimilarArtists : IReturn<string>
    {
        public string? artist { get; set; }
        public string? artist_id { get; set; }
        public int n { get; set; }
        public int? ef_search { get; set; }
        public bool? include_component_matches { get; set; }
    }

    [Route("/AudioMuseAI/max_distance", "GET")]
    public class GetMaxDistance : IReturn<string>
    {
        public string? item_id { get; set; }
    }

    [Route("/AudioMuseAI/find_path", "GET")]
    public class FindPath : IReturn<string>
    {
        public string start_song_id { get; set; }
        public string end_song_id { get; set; }
        public int? max_steps { get; set; }
    }

    [Route("/AudioMuseAI/create_playlist", "POST")]
    public class CreatePlaylist : CreatePlaylistModel, IReturn<string>
    {
    }

    [Route("/AudioMuseAI/cancel/{TaskId}", "POST")]
    public class CancelTask : IReturn<string>
    {
        public string TaskId { get; set; }
    }

    [Route("/AudioMuseAI/cancel_all/{TaskTypePrefix}", "POST")]
    public class CancelAllTasksByType : IReturn<string>
    {
        public string TaskTypePrefix { get; set; }
    }

    [Route("/AudioMuseAI/chat/playlist", "POST")]
    public class PostChatPlaylist : IReturn<string>
    {
    }

    [Route("/AudioMuseAI/chat/create_playlist", "POST")]
    public class CreateChatPlaylist : IReturn<string>
    {
    }

    [Route("/AudioMuseAI/alchemy", "POST")]
    public class Alchemy : IReturn<string>
    {
    }

    [Route("/AudioMuseAI/sonic_fingerprint/generate", "GET")]
    public class GenerateSonicFingerprint : IReturn<string>
    {
        public string jellyfin_user_identifier { get; set; }
        public string jellyfin_token { get; set; }
        public int? n { get; set; }
    }

    /// <summary>
    /// The AudioMuse AI API service for Emby.
    /// Implements IService to be automatically discovered.
    /// </summary>
    public class AudioMuseApiService : IService, IRequiresRequest
    {
        private readonly ILogger _logger;
        private readonly IAudioMuseService _audioMuseService;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IDtoService _dtoService;

        public IRequest Request { get; set; }

        public AudioMuseApiService(
            ILogManager logManager,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IDtoService dtoService)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _logger.Info("AudioMuseApiService created.");
            Console.WriteLine("AudioMuseAI: AudioMuseApiService created (Console).");
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dtoService = dtoService;
            _audioMuseService = Plugin.Instance?.AudioMuseService ?? throw new InvalidOperationException("AudioMuseAI: Plugin instance or service not initialized.");
        }

        private async Task<object> ForwardResponse(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            _logger.Info("AudioMuseAI Backend response: {0} Length: {1}", response.StatusCode, content.Length);
            return content;
        }

        private async Task<string> GetRequestBodyAsync()
        {
            if (Request?.InputStream == null) return "{}";

            if (Request.InputStream.CanSeek)
            {
                Request.InputStream.Position = 0;
            }

            using (var reader = new StreamReader(Request.InputStream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
            {
                var body = await reader.ReadToEndAsync().ConfigureAwait(false);
                // Log truncated body to avoid massive logs if needed, but for now full body for debug
                _logger.Info("Request Body: {0}", body);
                return body;
            }
        }

        private List<Audio> GetSongs(BaseItem item, User? user)
        {
            var query = new InternalItemsQuery
            {
                User = user,
                IncludeItemTypes = new[] { "Audio" },
                Recursive = true
            };

            if (item is MusicArtist artist)
            {
                query.ArtistIds = new[] { artist.InternalId };
            }
            else
            {
                query.Parent = item;
            }

            return _libraryManager.GetItemList(query).OfType<Audio>().ToList();
        }

        private async Task ResolveAndAddItems(string json, User? user, int limit, List<BaseItem> finalItems, HashSet<Guid> finalItemIds)
        {
            var candidates = new List<(string Id, string Title, string Artist)>();
            using (var doc = JsonDocument.Parse(json))
            {
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        var idStr = el.TryGetProperty("item_id", out var idProp) ? idProp.GetString() : null;
                        if (string.IsNullOrEmpty(idStr) && el.TryGetProperty("Id", out var idProp2)) idStr = idProp2.GetString();

                        var title = el.TryGetProperty("title", out var tProp) ? tProp.GetString() : null;
                        if (string.IsNullOrEmpty(title)) title = el.TryGetProperty("name", out var t2) ? t2.GetString() : null;
                        if (string.IsNullOrEmpty(title)) title = el.TryGetProperty("Name", out var t3) ? t3.GetString() : null;

                        var artist = el.TryGetProperty("artist", out var aProp) ? aProp.GetString() : null;
                        if (string.IsNullOrEmpty(artist)) artist = el.TryGetProperty("Artist", out var a2) ? a2.GetString() : null;

                        if (string.IsNullOrEmpty(artist) && el.TryGetProperty("Artists", out var artistsProp) && artistsProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var a in artistsProp.EnumerateArray())
                            {
                                if (a.ValueKind == JsonValueKind.String) { artist = a.GetString(); break; }
                                if (a.ValueKind == JsonValueKind.Object && a.TryGetProperty("Name", out var an)) { artist = an.GetString(); break; }
                            }
                        }

                        if (!string.IsNullOrEmpty(idStr))
                        {
                            candidates.Add((idStr, title ?? string.Empty, artist ?? string.Empty));
                        }
                    }
                }
            }

            Console.WriteLine($"AudioMuseAI: Parsed {candidates.Count} similar tracks from backend.");

            foreach (var (id, title, artist) in candidates)
            {
                if (finalItems.Count >= limit) break;

                // Try both Guid and numeric string lookup
                BaseItem? item = null;
                if (Guid.TryParse(id, out var guid)) item = _libraryManager.GetItemById(guid);
                if (item == null) item = _libraryManager.GetItemById(id);

                if (item == null && !string.IsNullOrEmpty(title))
                {
                    var query = new InternalItemsQuery
                    {
                        User = user,
                        SearchTerm = title,
                        IncludeItemTypes = new[] { "Audio" },
                        Recursive = true,
                        Limit = 5
                    };

                    var foundItems = _libraryManager.GetItemList(query);
                    if (foundItems != null && foundItems.Any())
                    {
                        if (!string.IsNullOrEmpty(artist))
                        {
                            var match = foundItems.FirstOrDefault(i =>
                                i is Audio a && a.Artists.Any(ar => ar.Contains(artist, StringComparison.OrdinalIgnoreCase))
                            );
                            if (match != null) item = match;
                            else item = foundItems.FirstOrDefault();
                        }
                        else
                        {
                            item = foundItems.FirstOrDefault();
                        }
                        
                        if (item != null) Console.WriteLine($"AudioMuseAI: Resolved '{title}' via metadata search.");
                    }
                }

                if (item != null && !finalItemIds.Contains(item.Id) && (user == null || item.IsVisible(user)))
                {
                    finalItems.Add(item);
                    finalItemIds.Add(item.Id);
                }
            }
        }

        public async Task<object> Get(GetAudioMuseInstantMix request)
        {
            try
            {
                Console.WriteLine($">>> AudioMuseAI: InstantMix requested for Item {request.Id} <<<");
                _logger.Info("AudioMuseAI: Intercepted Instant Mix request for Item {0}", request.Id);

                var user = request.UserId != null && Guid.TryParse(request.UserId, out var userIdVal)
                    ? _userManager.GetUserById(userIdVal)
                    : null;

                var originalItem = _libraryManager.GetItemById(request.Id);
                if (originalItem == null)
                {
                    Console.WriteLine($"AudioMuseAI: Error - Item {request.Id} not found in library.");
                    return new QueryResult<BaseItemDto>();
                }

                Console.WriteLine($"AudioMuseAI: Original Item is '{originalItem.Name}' (Type: {originalItem.GetType().Name}, InternalId: {originalItem.InternalId})");

                var resultLimit = request.Limit ?? 50;
                var finalItems = new List<BaseItem>();
                var finalItemIds = new HashSet<Guid>();

                BaseItem? initialSong = null;
                var seedSongs = new List<Audio>();

                if (originalItem is Audio song)
                {
                    initialSong = song;
                    seedSongs.Add(song);
                }
                else if (originalItem is MusicAlbum album)
                {
                    var songs = GetSongs(album, user);
                    if (songs.Any())
                    {
                        initialSong = songs.OrderBy(_ => Guid.NewGuid()).First();
                        seedSongs = songs.OrderBy(_ => Guid.NewGuid()).ToList();
                    }
                }
                else if (originalItem is MusicArtist artist || originalItem.GetType().Name.Contains("Artist"))
                {
                    var songs = GetSongs(originalItem, user);
                    if (songs.Any())
                    {
                        initialSong = songs.OrderBy(_ => Guid.NewGuid()).First();
                        seedSongs = songs.OrderBy(_ => Guid.NewGuid()).Take(20).ToList();
                    }
                }
                else if (originalItem.GetType().Name.Contains("Playlist") || originalItem is Folder)
                {
                    var songs = GetSongs(originalItem, user);
                    if (songs.Any())
                    {
                        initialSong = songs.OrderBy(_ => Guid.NewGuid()).First();
                        seedSongs = songs.OrderBy(_ => Guid.NewGuid()).Take(20).ToList();
                    }
                }

                if (initialSong != null)
                {
                    finalItems.Add(initialSong);
                    finalItemIds.Add(initialSong.Id);
                }

                if (seedSongs.Any() && finalItems.Count < resultLimit)
                {
                    var remainingNeeded = resultLimit - finalItems.Count;
                    var songsToFetchPerSeed = (int)Math.Ceiling((decimal)remainingNeeded / seedSongs.Count);
                    if (seedSongs.Count > 1) songsToFetchPerSeed *= 2;

                    foreach (var seed in seedSongs)
                    {
                        if (finalItems.Count >= resultLimit) break;

                        try
                        {
                            // Use original numeric ID for the first seed if it matches the request
                            string backendId = (seed.Id == originalItem.Id) ? request.Id : seed.InternalId.ToString();

                            Console.WriteLine($"AudioMuseAI: Requesting similar tracks for seed '{seed.Name}' (ID: {backendId})...");
                            var response = await _audioMuseService.GetSimilarTracksAsync(backendId, null, null, songsToFetchPerSeed, null, CancellationToken.None).ConfigureAwait(false);
                            if (response != null && response.IsSuccessStatusCode)
                            {
                                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                                await ResolveAndAddItems(json, user, resultLimit, finalItems, finalItemIds);
                            }
                            else
                            {
                                Console.WriteLine($"AudioMuseAI: Backend error {response?.StatusCode} for seed {seed.Name}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error("AudioMuseAI: Error fetching similar tracks for seed {0}: {1}", seed.Id, ex.Message);
                            Console.WriteLine($"AudioMuseAI: Exception during backend call for {seed.Name}: {ex.Message}");
                        }
                    }
                }

                // Fallback: If still empty or very short, add some random items but only as a last resort
                if (finalItems.Count < 5)
                {
                    Console.WriteLine($"AudioMuseAI: Results extremely low ({finalItems.Count}). Adding random library tracks as fallback.");
                    var fallbackQuery = new InternalItemsQuery
                    {
                        User = user,
                        IncludeItemTypes = new[] { "Audio" },
                        Recursive = true,
                        Limit = resultLimit - finalItems.Count
                    };
                    var fallbackSongs = _libraryManager.GetItemList(fallbackQuery);
                    foreach (var s in fallbackSongs.OrderBy(_ => Guid.NewGuid()))
                    {
                        if (finalItems.Count < resultLimit && finalItemIds.Add(s.Id))
                        {
                            finalItems.Add(s);
                        }
                    }
                }

                var dtos = _dtoService.GetBaseItemDtos(finalItems.ToArray(), new DtoOptions { EnableImages = true }, user);
                Console.WriteLine($"AudioMuseAI: Returning {dtos.Length} items for Instant Mix.");
                return new QueryResult<BaseItemDto> { Items = dtos, TotalRecordCount = dtos.Length };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AudioMuseAI: CRITICAL FAILURE in Get(GetAudioMuseInstantMix): {ex}");
                _logger.Error("AudioMuseAI: CRITICAL FAILURE in Get(GetAudioMuseInstantMix). {0}", ex);
                return new QueryResult<BaseItemDto>();
            }
        }

        public object Get(GetInfo request)
        {
            _logger.Info("AudioMuseAI: GetInfo called");
            return new
            {
                Version = Plugin.Instance?.Version.ToString() ?? "Unknown",
                AvailableEndpoints = new[] { "GET /health", "GET /playlists", "GET /active_tasks", "POST /analysis", "POST /clustering" }
            };
        }

        public async Task Get(GetHealth request)
        {
            _logger.Info("AudioMuseAI: GetHealth called");
            await _audioMuseService.HealthCheckAsync(CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<object> Get(GetPlaylists request)
        {
            _logger.Info("AudioMuseAI: GetPlaylists called");
            var resp = await _audioMuseService.GetPlaylistsAsync(CancellationToken.None).ConfigureAwait(false);
            return await ForwardResponse(resp);
        }

        public async Task<object> Get(GetActiveTasks request)
        {
            _logger.Info("AudioMuseAI: GetActiveTasks called");
            var resp = await _audioMuseService.GetActiveTasksAsync(CancellationToken.None).ConfigureAwait(false);
            return await ForwardResponse(resp);
        }

        public async Task<object> Get(GetLastTask request)
        {
            _logger.Info("AudioMuseAI: GetLastTask called");
            var resp = await _audioMuseService.GetLastTaskAsync(CancellationToken.None).ConfigureAwait(false);
            return await ForwardResponse(resp);
        }

        public async Task<object> Get(GetTaskStatus request)
        {
            _logger.Info("AudioMuseAI: GetTaskStatus called for {0}", request.TaskId);
            var resp = await _audioMuseService.GetTaskStatusAsync(request.TaskId ?? string.Empty, CancellationToken.None).ConfigureAwait(false);
            return await ForwardResponse(resp);
        }

        public async Task<object> Post(StartAnalysis request)
        {
            _logger.Info("AudioMuseAI: StartAnalysis called");
            var json = await GetRequestBodyAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json)) json = "{}";
            var resp = await _audioMuseService.StartAnalysisAsync(json, CancellationToken.None).ConfigureAwait(false);
            return await ForwardResponse(resp);
        }

        public async Task<object> Post(StartClustering request)
        {
            _logger.Info("AudioMuseAI: StartClustering called");
            var json = await GetRequestBodyAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json)) json = "{}";
            var resp = await _audioMuseService.StartClusteringAsync(json, CancellationToken.None).ConfigureAwait(false);
            return await ForwardResponse(resp);
        }

        public async Task<object> Get(SearchTracks request)
        {
            _logger.Info("AudioMuseAI: SearchTracks called Title: {0}, Artist: {1}", request.title, request.artist);
            var resp = await _audioMuseService.SearchTracksAsync(request.title, request.artist, CancellationToken.None).ConfigureAwait(false);
            return await ForwardResponse(resp);
        }

        public async Task<object> Get(GetSimilarTracks request)
        {
            _logger.Info("AudioMuseAI: GetSimilarTracks called ItemId: {0}, Title: {1}", request.item_id, request.title);
            var resp = await _audioMuseService.GetSimilarTracksAsync(request.item_id, request.title, request.artist, request.n, request.eliminate_duplicates, CancellationToken.None).ConfigureAwait(false);
            return await ForwardResponse(resp);
        }

        public async Task<object> Get(GetSimilarArtists request)
        {
            _logger.Info("AudioMuseAI: GetSimilarArtists called Artist: {0}", request.artist);
            var resp = await _audioMuseService.GetSimilarArtistsAsync(request.artist, request.artist_id, request.n, request.ef_search, request.include_component_matches, CancellationToken.None).ConfigureAwait(false);
            return await ForwardResponse(resp);
        }

        public async Task<object> Get(GetMaxDistance request)
        {
            _logger.Info("AudioMuseAI: GetMaxDistance called ItemId: {0}", request.item_id);
            var resp = await _audioMuseService.GetMaxDistanceAsync(request.item_id, CancellationToken.None).ConfigureAwait(false);
            return await ForwardResponse(resp);
        }

        public async Task<object> Get(FindPath request)
        {
            _logger.Info("AudioMuseAI: FindPath called Start: {0}, End: {1}", request.start_song_id, request.end_song_id);
            var resp = await _audioMuseService.FindPathAsync(request.start_song_id ?? string.Empty, request.end_song_id ?? string.Empty, request.max_steps, CancellationToken.None).ConfigureAwait(false);
            return await ForwardResponse(resp);
        }

        public async Task<object> Post(CreatePlaylist request)
        {
            _logger.Info("AudioMuseAI: CreatePlaylist called Name: {0}", request.playlist_name);
            var resp = await _audioMuseService.CreatePlaylistAsync(request.playlist_name ?? string.Empty, request.track_ids ?? Enumerable.Empty<string>(), CancellationToken.None).ConfigureAwait(false);
            return await ForwardResponse(resp);
        }

        public async Task<object> Post(CancelTask request)
        {
            _logger.Info("AudioMuseAI: CancelTask called TaskId: {0}", request.TaskId);
            var resp = await _audioMuseService.CancelTaskAsync(request.TaskId ?? string.Empty, CancellationToken.None).ConfigureAwait(false);
            return await ForwardResponse(resp);
        }

        public async Task<object> Post(CancelAllTasksByType request)
        {
            _logger.Info("AudioMuseAI: CancelAllTasksByType called Prefix: {0}", request.TaskTypePrefix);
            var resp = await _audioMuseService.CancelAllTasksByTypeAsync(request.TaskTypePrefix ?? string.Empty, CancellationToken.None).ConfigureAwait(false);
            return await ForwardResponse(resp);
        }

        public async Task<object> Post(PostChatPlaylist request)
        {
            _logger.Info("AudioMuseAI: PostChatPlaylist called");
            var json = await GetRequestBodyAsync().ConfigureAwait(false);
            var resp = await _audioMuseService.PostChatPlaylistAsync(json, CancellationToken.None).ConfigureAwait(false);
            return await ForwardResponse(resp);
        }

        public async Task<object> Post(CreateChatPlaylist request)
        {
            _logger.Info("AudioMuseAI: CreateChatPlaylist called");
            var json = await GetRequestBodyAsync().ConfigureAwait(false);
            var resp = await _audioMuseService.CreateChatPlaylistAsync(json, CancellationToken.None).ConfigureAwait(false);
            return await ForwardResponse(resp);
        }

        public async Task<object> Post(Alchemy request)
        {
            _logger.Info("AudioMuseAI: Alchemy called");
            var json = await GetRequestBodyAsync().ConfigureAwait(false);
            var resp = await _audioMuseService.AlchemyAsync(json, CancellationToken.None).ConfigureAwait(false);
            return await ForwardResponse(resp);
        }

        public async Task<object> Get(GenerateSonicFingerprint request)
        {
            _logger.Info("AudioMuseAI: GenerateSonicFingerprint called User: {0}", request.jellyfin_user_identifier);
            var resp = await _audioMuseService.GenerateSonicFingerprintAsync(request.jellyfin_user_identifier ?? string.Empty, request.jellyfin_token ?? string.Empty, request.n, CancellationToken.None).ConfigureAwait(false);
            return await ForwardResponse(resp);
        }
    }
}
