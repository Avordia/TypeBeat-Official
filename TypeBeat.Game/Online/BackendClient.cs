using System;
using System.Threading.Tasks;
using Supabase;
using osu.Framework.Logging;
using TypeBeat.Game.Configuration;

#nullable enable

namespace TypeBeat.Game.Online
{
    public static class BackendClient
    {
        public static Client? Client { get; private set; }

        public static async Task InitializeAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(BackendConfig.BackendUrl) || string.IsNullOrEmpty(BackendConfig.ApiKey))
                {
                    Logger.Log("Backend configuration is incomplete. Backend features will be disabled.", LoggingTarget.Runtime, LogLevel.Important);
                    return;
                }

                var options = new SupabaseOptions
                {
                    AutoRefreshToken = true,
                    AutoConnectRealtime = true
                };

                Client = new Client(BackendConfig.BackendUrl, BackendConfig.ApiKey, options);
                await Client.InitializeAsync();
                
                Logger.Log("Backend client initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to initialize backend client: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
                Client = null;
            }
        }
    }
}
