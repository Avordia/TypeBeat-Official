using System;

namespace TypeBeat.Game.Configuration
{
    /// <summary>
    /// Configuration for the auto-update system
    /// </summary>
    public static class UpdateConfig
    {
        /// <summary>
        /// The URL where update packages are hosted
        /// Change this to your GitHub releases URL or custom update server
        /// </summary>
        public static string UpdateUrl { get; set; } = GetDefaultUpdateUrl();
        
        /// <summary>
        /// Whether to check for updates on startup
        /// </summary>
        public static bool CheckOnStartup { get; set; } = true;
        
        /// <summary>
        /// Whether to automatically download updates in the background
        /// </summary>
        public static bool AutoDownload { get; set; } = true;
        
        /// <summary>
        /// Whether to show a notification when an update is available
        /// </summary>
        public static bool ShowNotifications { get; set; } = true;
        
        /// <summary>
        /// Update check interval in milliseconds (default: 1 hour)
        /// Set to 0 to disable periodic checks
        /// </summary>
        public static int CheckIntervalMs { get; set; } = 3600000; // 1 hour
        
        private static string GetDefaultUpdateUrl()
        {
            // TODO: Replace with your actual update URL
            // Examples:
            // - GitHub: "https://github.com/YourUsername/TypeBeat/releases"
            // - Custom server: "https://updates.typebeat.com"
            // - Azure Blob: "https://yourstorage.blob.core.windows.net/updates"
            
            #if DEBUG
            // In debug mode, you can test with a local server
            // return "http://localhost:8000";
            #endif
            
            // For now, return null to disable updates in development
            // Change this before releasing!
            return "https://github.com/Avordia/TypeBeat-Official/releases";
        }
    }
}
