using System;
using System.ComponentModel;
using Emby.Web.GenericEdit;
using MediaBrowser.Model.Attributes;

namespace Emby.Plugin.AudioMuseAi.Configuration
{
    public class PluginConfiguration : EditableOptionsBase
    {
        public override string EditorTitle => "AudioMuse AI";

        public override string EditorDescription => "Configure the connection to your self-hosted AudioMuse AI backend.";

        [DisplayName("Backend URL")]
        [Description("Enter the full base URL of your AudioMuse AI backend (e.g., http://127.0.0.1:8000).")]
        [Required]
        public string BackendUrl { get; set; } = "http://127.0.0.1:8000";
    }
}