using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using QualTrack.Core.Services;

namespace QualTrack.UI.Services
{
    public static class StorageSetupService
    {
        private const string ConfigFileName = "storage.json";
        private const string SharedSettingsFileName = "qualtrack.settings.json";

        public static string? GetOrPromptForBasePath(Window? owner)
        {
            var existingPath = TryLoadBasePath();
            if (!string.IsNullOrWhiteSpace(existingPath) && Directory.Exists(existingPath))
            {
                return existingPath;
            }

            var selectedPath = PromptForBasePath(owner);
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return null;
            }

            SaveBasePath(selectedPath);
            WriteSharedSettings(selectedPath);

            return selectedPath;
        }

        public static void InitializeStorage(string basePath)
        {
            StoragePathService.Initialize(basePath);
            WriteSharedSettings(basePath);
        }

        private static string? TryLoadBasePath()
        {
            var configPath = GetLocalConfigPath();
            if (!File.Exists(configPath))
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<StorageConfig>(json);
                return config?.BasePath;
            }
            catch
            {
                return null;
            }
        }

        private static void SaveBasePath(string basePath)
        {
            var configPath = GetLocalConfigPath();
            var configDir = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrWhiteSpace(configDir) && !Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            var config = new StorageConfig { BasePath = basePath };
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);
        }

        private static string? PromptForBasePath(Window? owner)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select the shared QualTrack base folder (e.g., a shared drive location).",
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Select folder"
            };

            var result = owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
            if (result != true)
            {
                return null;
            }

            return Path.GetDirectoryName(dialog.FileName);
        }

        private static void WriteSharedSettings(string basePath)
        {
            try
            {
                var settingsPath = Path.Combine(basePath, SharedSettingsFileName);
                var settings = new SharedStorageSettings
                {
                    BasePath = basePath,
                    CreatedAt = DateTime.Now
                };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
            }
            catch
            {
                // Best-effort write for shared settings
            }
        }

        private static string GetLocalConfigPath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "QualTrack", ConfigFileName);
        }

        private sealed class StorageConfig
        {
            public string BasePath { get; set; } = string.Empty;
        }

        private sealed class SharedStorageSettings
        {
            public string BasePath { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }

    }
}
