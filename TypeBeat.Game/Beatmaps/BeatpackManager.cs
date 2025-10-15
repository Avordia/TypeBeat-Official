using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Logging; // <-- Add this using statement
using osu.Framework.Platform;
using TypeBeat.Game.Filehandling;

namespace TypeBeat.Game.Beatmaps
{
    public partial class BeatpackManager : Component
    {
        private readonly List<Beatpack> beatpacks = new List<Beatpack>();
        private int currentIndex = -1;

        public readonly Bindable<Beatpack> CurrentBeatpack = new Bindable<Beatpack>();
        public IReadOnlyList<Beatpack> Beatpacks => beatpacks;

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            var songsStorage = host.Storage.GetStorageForDirectory("Songs");
            Logger.Log($"Scanning for beatpacks in: {songsStorage.GetFullPath(".")}");

            var files = songsStorage.GetFiles(".", "*.tbbp");
            Logger.Log($"Found {files.Count()} .tbbp file(s).");

            foreach (var file in files)
            {
                try
                {
                    var fullPath = songsStorage.GetFullPath(file);
                    Logger.Log($"Attempting to load: {fullPath}");
                    
                    // Check if file exists and is accessible
                    if (!System.IO.File.Exists(fullPath))
                    {
                        Logger.Log($"File not found: {fullPath}", LoggingTarget.Runtime, LogLevel.Error);
                        continue;
                    }
                    
                    // Check file size to detect empty/corrupted files
                    var fileInfo = new System.IO.FileInfo(fullPath);
                    Logger.Log($"File size: {fileInfo.Length} bytes");
                    
                    if (fileInfo.Length == 0)
                    {
                        Logger.Log($"File is empty: {fullPath}", LoggingTarget.Runtime, LogLevel.Error);
                        continue;
                    }
                    
                    var beatpack = BeatmapParser.ParseBeatpack(fullPath);
                    if (beatpack != null)
                    {
                        beatpacks.Add(beatpack);
                        var songName = beatpack.Beatmap?.Title ?? Path.GetFileNameWithoutExtension(file);
                        var starRating = beatpack.Beatmap?.StarRating ?? 0;
                        Logger.Log($"✓ Successfully loaded beatpack: {songName} (StarRating: {starRating})");
                    }
                    else
                    {
                        Logger.Log($"Parser returned null for: {file}", LoggingTarget.Runtime, LogLevel.Error);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"Failed to load beatpack {file}: {e.Message}");
                }
            }

            Logger.Log($"Total beatpacks loaded: {beatpacks.Count}");

            if (beatpacks.Any())
            {
                currentIndex = 0;
                CurrentBeatpack.Value = beatpacks[currentIndex];
            }
            else
            {
                Logger.Log("No beatpacks were successfully loaded!", LoggingTarget.Runtime, LogLevel.Error);
            }
        }

        public void Next()
        {
            if (currentIndex >= beatpacks.Count - 1) return;

            currentIndex++;
            CurrentBeatpack.Value = beatpacks[currentIndex];
        }

        public void Previous()
        {
            if (currentIndex <= 0) return;

            currentIndex--;
            CurrentBeatpack.Value = beatpacks[currentIndex];
        }
    }
}