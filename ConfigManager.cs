using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using HttpPing.Models;

namespace HttpPing
{
    [Serializable]
    public class AppSettings
    {
        public int IntervalSeconds { get; set; } = 5;
        public int TimeoutSeconds { get; set; } = 10;
        public string UserAgent { get; set; } = "HttpPing/1.0";
        public int ColumnCount { get; set; } = 3;
        public bool PopupAlerts { get; set; } = true;
        public bool SoundAlerts { get; set; } = false;
        public bool AlwaysOnTop { get; set; } = false;
        public bool MinimizeToTray { get; set; } = false;
        public bool UseBalloonNotifications { get; set; } = true;
        public int PopupDismissSeconds { get; set; } = 8;
    }

    [Serializable]
    public class FavoriteSet
    {
        public string Name { get; set; } = "";
        public List<ProbeConfig> Probes { get; set; } = new List<ProbeConfig>();
    }

    [Serializable]
    public class AppConfig
    {
        public AppSettings Settings { get; set; } = new AppSettings();
        public List<FavoriteSet> Favorites { get; set; } = new List<FavoriteSet>();
        public List<ProbeConfig> LastSession { get; set; } = new List<ProbeConfig>();
    }

    public static class ConfigManager
    {
        private static string GetConfigPath()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var portablePath = Path.Combine(appDir, "HttpPing.xml");
            if (File.Exists(portablePath))
                return portablePath;

            var localPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HttpPing", "HttpPing.xml");
            return localPath;
        }

        public static AppConfig Load()
        {
            try
            {
                var path = GetConfigPath();
                if (!File.Exists(path)) return new AppConfig();

                var serializer = new XmlSerializer(typeof(AppConfig));
                using (var stream = File.OpenRead(path))
                {
                    return (AppConfig)serializer.Deserialize(stream);
                }
            }
            catch
            {
                return new AppConfig();
            }
        }

        public static void Save(AppConfig config)
        {
            try
            {
                var path = GetConfigPath();
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var serializer = new XmlSerializer(typeof(AppConfig));
                using (var stream = File.Create(path))
                {
                    serializer.Serialize(stream, config);
                }
            }
            catch { }
        }

        public static void SavePortable(AppConfig config)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HttpPing.xml");
            try
            {
                var serializer = new XmlSerializer(typeof(AppConfig));
                using (var stream = File.Create(path))
                {
                    serializer.Serialize(stream, config);
                }
            }
            catch { }
        }
    }
}
