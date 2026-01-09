using System;

namespace Emby.Plugin.AudioMuseAi.Services
{
    internal static class AudioMuseBackendUrl
    {
        public static string Get()
        {
            var config = Plugin.Instance?.Configuration;
            var backendUrl = !string.IsNullOrWhiteSpace(config?.BackendUrl)
                ? config.BackendUrl.TrimEnd('/')
                : new Configuration.PluginConfiguration().BackendUrl.TrimEnd('/');

            if (!Uri.IsWellFormedUriString(backendUrl, UriKind.Absolute))
            {
                throw new InvalidOperationException(
                    $"AudioMuseAI: BackendUrl is invalid ('{backendUrl}'). " +
                    "Please configure a valid absolute URL in Emby plugin settings.");
            }

            return backendUrl;
        }
    }
}

