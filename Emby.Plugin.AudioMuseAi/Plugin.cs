using System;
using AudioMuseAi.Common.Services;
using Emby.Plugin.AudioMuseAi.Configuration;
using Emby.Web.GenericEdit;
using MediaBrowser.Common; // For IApplicationHost
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Plugins;

namespace Emby.Plugin.AudioMuseAi
{
    /// <summary>
    /// The main plugin entry point (Emby).
    /// </summary>
    public class Plugin : BasePluginSimpleUI<PluginConfiguration>
    {
        public Plugin(IApplicationHost applicationHost)
            : base(applicationHost)
        {
            Instance = this;
        }

        public static Plugin? Instance { get; private set; }

        public PluginConfiguration Configuration => GetOptions();

        private IAudioMuseService? _audioMuseService;
        public IAudioMuseService AudioMuseService
        {
            get
            {
                if (_audioMuseService == null)
                {
                    _audioMuseService = new AudioMuseService(new System.Net.Http.HttpClient
                    {
                        BaseAddress = new Uri(Services.AudioMuseBackendUrl.Get())
                    });
                }
                return _audioMuseService;
            }
        }

        public override string Name => "AudioMuse AI";

        public override PluginInfo GetPluginInfo()
        {
            Console.WriteLine("AudioMuseAI: Getting plugin info.");
            var info = base.GetPluginInfo();
            info.Version = "1.0.0.2";
            return info;
        }

        public override Guid Id => Guid.Parse("e3831be1-c025-4ebc-bc79-121ad0dfc4e1");

        public override string Description => "Integrates Emby with an AudioMuse AI backend.";
    }
}
