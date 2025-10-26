// Copyright (c) TypeBeat. Licensed under the MIT Licence.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;

namespace TypeBeat.Game.Editor.UI
{
    /// <summary>
    /// A button that opens a file picker dialog when clicked.
    /// </summary>
    public partial class FilePickerButton : Container
    {
        public Action<string> OnFileSelected { get; set; }

        private readonly string title;
        private readonly string filter;
        private readonly string initialDirectory;
        private readonly string displayText;
        private Box background;
        private SpriteText buttonText;

        [Resolved]
        private GameHost host { get; set; }

        public FilePickerButton(string displayText, string filter, string initialDirectory = null, string title = null)
        {
            this.displayText = displayText;
            this.filter = filter;
            this.initialDirectory = initialDirectory;
            this.title = title ?? $"Select {displayText}";

            // Size should be set by parent container
            Masking = true;
            CornerRadius = 8;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Children = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(44, 44, 44, 255)
                },
                buttonText = new SpriteText
                {
                    Text = displayText,
                    Font = FontUsage.Default.With(family: "Inter-Bold", size: 16),
                    Colour = Color4.White,
                    Padding = new MarginPadding { Horizontal = 20, Vertical = 12 },
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
                }
            };
        }

        protected override bool OnClick(ClickEvent e)
        {
            // Immediate visual feedback
            this.ScaleTo(0.9f, 80, Easing.OutQuint).Then().ScaleTo(1f, 150, Easing.OutBack);

            // Open file dialog asynchronously
            Task.Run(() => openFileDialog());
            return true;
        }

        protected override bool OnHover(HoverEvent e)
        {
            // Smooth hover effect
            this.ScaleTo(1.05f, 150, Easing.OutQuint);
            background.FadeColour(new Color4(54, 54, 54, 255), 150); // Lighter on hover
            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            // Reset scale and color on hover lost
            this.ScaleTo(1f, 150, Easing.OutQuint);
            background.FadeColour(new Color4(44, 44, 44, 255), 150); // Back to default
        }

        private void openFileDialog()
        {
            try
            {
                Schedule(() => buttonText.Text = "Browsing...");
                
                // Use host's OpenFileExternally capability with a simple file browser
                // For now, we'll use a cross-platform approach with Environment variables
                string selectedPath = BrowseForFile();
                
                if (!string.IsNullOrEmpty(selectedPath) && File.Exists(selectedPath))
                {
                    Schedule(() =>
                    {
                        buttonText.Text = Path.GetFileName(selectedPath);
                        OnFileSelected?.Invoke(selectedPath);
                        Logger.Log($"[FilePickerButton] Selected file: {selectedPath}", LoggingTarget.Runtime, LogLevel.Debug);
                    });
                }
                else
                {
                    Schedule(() => buttonText.Text = displayText);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[FilePickerButton] Error opening file dialog: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
                Schedule(() => buttonText.Text = displayText);
            }
        }

        private string BrowseForFile()
        {
            try
            {
                // Use native file dialog through process invocation (cross-platform)
                if (OperatingSystem.IsWindows())
                {
                    return BrowseForFileWindows();
                }
                else if (OperatingSystem.IsMacOS())
                {
                    return BrowseForFileMacOS();
                }
                else if (OperatingSystem.IsLinux())
                {
                    return BrowseForFileLinux();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[FilePickerButton] Platform-specific browser error: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
            }

            return string.Empty;
        }

        private string BrowseForFileWindows()
        {
            // Use PowerShell to open native Windows file dialog
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"Add-Type -AssemblyName System.Windows.Forms; $f = New-Object System.Windows.Forms.OpenFileDialog; $f.Title = '{title}'; $f.InitialDirectory = '{initialDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}'; $f.ShowDialog() | Out-Null; $f.FileName\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = System.Diagnostics.Process.Start(psi))
            {
                string result = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return result;
            }
        }

        private string BrowseForFileMacOS()
        {
            // Use AppleScript for native macOS file dialog
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/usr/bin/osascript",
                Arguments = $"-e 'POSIX path of (choose file with prompt \"{title}\")'",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = System.Diagnostics.Process.Start(psi))
            {
                string result = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return result;
            }
        }

        private string BrowseForFileLinux()
        {
            // Use zenity for Linux file dialog (most common)
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "zenity",
                Arguments = $"--file-selection --title=\"{title}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = System.Diagnostics.Process.Start(psi))
            {
                string result = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return result;
            }
        }
    }
}
