using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace TypeBeat.Desktop
{
    /// <summary>
    /// Handles file associations for .tbbp files (TypeBeat Beatmap Package)
    /// </summary>
    public static class FileAssociations
    {
        private const string FileExtension = ".tbbp";
        private const string ProgId = "TypeBeat.Beatmap";
        private const string FileTypeDescription = "TypeBeat Beatmap Package";
        
        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);
        
        private const int SHCNE_ASSOCCHANGED = 0x08000000;
        private const int SHCNF_IDLIST = 0x0000;

        /// <summary>
        /// Register .tbbp file association with the application
        /// </summary>
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public static bool RegisterFileAssociation()
        {
            try
            {
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath))
                    return false;

                string exeDir = Path.GetDirectoryName(exePath);
                string iconPath = Path.Combine(exeDir, "tbbp.ico");
                
                // If tbbp.ico doesn't exist, fall back to main exe icon
                if (!File.Exists(iconPath))
                    iconPath = exePath;

                // Register file extension
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{FileExtension}"))
                {
                    key?.SetValue("", ProgId);
                }

                // Register program ID
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}"))
                {
                    key?.SetValue("", FileTypeDescription);
                    key?.SetValue("AppUserModelID", "TypeBeat.Desktop");
                }

                // Set default icon
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\DefaultIcon"))
                {
                    key?.SetValue("", $"\"{iconPath}\",0");
                }

                // Set open command
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\shell\open\command"))
                {
                    key?.SetValue("", $"\"{exePath}\" \"%1\"");
                }

                // Notify Windows that file associations have changed
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to register file association: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unregister .tbbp file association
        /// </summary>
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public static bool UnregisterFileAssociation()
        {
            try
            {
                // Remove file extension association
                Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{FileExtension}", false);
                
                // Remove program ID
                Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ProgId}", false);

                // Notify Windows that file associations have changed
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to unregister file association: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if .tbbp files are currently associated with this application
        /// </summary>
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public static bool IsFileAssociationRegistered()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{FileExtension}"))
                {
                    string progId = key?.GetValue("")?.ToString();
                    return progId == ProgId;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
