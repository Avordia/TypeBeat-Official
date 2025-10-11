using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Logging; // <-- Add this using statement
using osu.Framework.Platform;
using TypeBeat.Game.fileHandling;

namespace TypeBeat.Game.Beatmaps
{
    public partial class BeatpackManager : Component
    {
        private readonly List<Beatpack> beatpacks = new List<Beatpack>();
        private int currentIndex = -1;

        public readonly Bindable<Beatpack> CurrentBeatpack = new Bindable<Beatpack>();

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
                    var beatpack = BeatmapParser.ParseBeatpack(fullPath);
                    if (beatpack != null)
                    {
                        beatpacks.Add(beatpack);
                        Logger.Log($"Successfully loaded beatpack: {file}");
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"Failed to load beatpack {file}");
                }
            }

            Logger.Log($"Total beatpacks loaded: {beatpacks.Count}");

            if (beatpacks.Any())
            {
                currentIndex = 0;
                CurrentBeatpack.Value = beatpacks[currentIndex];
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