using System;

namespace HttpPing.Models
{
    [Serializable]
    public class ProbeConfig
    {
        public string Url { get; set; } = "";
        public string Alias { get; set; } = "";
        public bool IsEnabled { get; set; } = true;

        public string DisplayName => !string.IsNullOrEmpty(Alias) ? Alias : Url;
    }
}
