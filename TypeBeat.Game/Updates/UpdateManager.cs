using System;
using System.Threading.Tasks;
using osu.Framework.Logging;
using Squirrel;
using TypeBeat.Game.Configuration;

namespace TypeBeat.Game.Updates
{
    public class UpdateManager : IDisposable
    {
        private readonly string updateUrl;
        private Squirrel.UpdateManager squirrelUpdateManager;
        
        public UpdateManager(string updateUrl = null)
        {
            // Use provided URL or fall back to configuration
            this.updateUrl = updateUrl ?? UpdateConfig.UpdateUrl;
        }
        
        /// <summary>
        /// Checks for updates and returns the update information
        /// </summary>
        public async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(updateUrl))
                {
                    Logger.Log("Update URL not configured. Updates disabled.", LoggingTarget.Runtime, LogLevel.Debug);
                    return null;
                }
                
                squirrelUpdateManager ??= new Squirrel.UpdateManager(updateUrl);
                
                var updateInfo = await squirrelUpdateManager.CheckForUpdate();
                
                if (updateInfo.ReleasesToApply.Count > 0)
                {
                    Logger.Log($"Update available: {updateInfo.FutureReleaseEntry.Version}", LoggingTarget.Runtime, LogLevel.Important);
                    return updateInfo;
                }
                else
                {
                    Logger.Log("No updates available", LoggingTarget.Runtime, LogLevel.Debug);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to check for updates: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
                return null;
            }
        }
        
        /// <summary>
        /// Downloads and applies the latest update
        /// </summary>
        public async Task<bool> DownloadAndApplyUpdateAsync(UpdateInfo updateInfo)
        {
            try
            {
                if (updateInfo == null)
                    return false;
                
                Logger.Log("Downloading update...", LoggingTarget.Runtime, LogLevel.Important);
                
                await squirrelUpdateManager.DownloadReleases(updateInfo.ReleasesToApply);
                
                Logger.Log("Applying update...", LoggingTarget.Runtime, LogLevel.Important);
                
                await squirrelUpdateManager.ApplyReleases(updateInfo);
                
                Logger.Log("Update applied successfully. Restart required.", LoggingTarget.Runtime, LogLevel.Important);
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to apply update: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
                return false;
            }
        }
        
        /// <summary>
        /// Downloads and applies updates in one call
        /// </summary>
        public async Task<bool> UpdateAsync()
        {
            var updateInfo = await CheckForUpdatesAsync();
            
            if (updateInfo == null)
                return false;
            
            return await DownloadAndApplyUpdateAsync(updateInfo);
        }
        
        /// <summary>
        /// Gets the currently installed version
        /// </summary>
        public string GetCurrentVersion()
        {
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return version?.ToString() ?? "0.0.0";
            }
            catch
            {
                return "0.0.0";
            }
        }
        
        /// <summary>
        /// Restarts the application to apply updates
        /// </summary>
        public void RestartApplication()
        {
            try
            {
                Squirrel.UpdateManager.RestartApp();
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to restart application: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
            }
        }
        
        public void Dispose()
        {
            squirrelUpdateManager?.Dispose();
        }
    }
}
