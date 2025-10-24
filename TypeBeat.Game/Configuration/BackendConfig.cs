using System;
using System.IO;
using Newtonsoft.Json;
using osu.Framework.Logging;

#nullable enable

namespace TypeBeat.Game.Configuration
{
    public static class BackendConfig
    {
        public static string BackendUrl { get; private set; } = string.Empty;
        public static string ApiKey { get; private set; } = string.Empty;

        static BackendConfig()
        {
            loadConfiguration();
        }

        private static void loadConfiguration()
        {
            // Try to load from appsettings.json first
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configuration", "appsettings.json");
            
            if (File.Exists(configPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(configPath);
                    var config = JsonConvert.DeserializeObject<ConfigModel>(jsonContent);
                    
                    if (config != null)
                    {
                        BackendUrl = config.BackendUrl ?? string.Empty;
                        ApiKey = config.BackendApiKey ?? string.Empty;
                        Logger.Log($"Loaded backend configuration from {configPath}");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to load configuration from {configPath}: {ex.Message}");
                }
            }

            // Fallback to environment variables
            BackendUrl = Environment.GetEnvironmentVariable("TYPEBEAT_BACKEND_URL") ?? string.Empty;
            ApiKey = Environment.GetEnvironmentVariable("TYPEBEAT_BACKEND_KEY") ?? string.Empty;

            if (!string.IsNullOrEmpty(BackendUrl) && !string.IsNullOrEmpty(ApiKey))
            {
                Logger.Log("Loaded backend configuration from environment variables");
            }
            else
            {
                Logger.Log("No backend configuration found. The app will work offline only.", LoggingTarget.Runtime, LogLevel.Important);
            }
        }

        private class ConfigModel
        {
            public string? BackendUrl { get; set; }
            public string? BackendApiKey { get; set; }
        }
    }
}
