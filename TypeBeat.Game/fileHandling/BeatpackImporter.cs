using System;
using TypeBeat.Game.Beatmaps;

namespace TypeBeat.Game.Filehandling
{
    public class BeatpackImporter
    {
        public Beatpack Import(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("path cannot be null or empty", nameof(path));
            return BeatmapParser.ParseBeatpack(path);
        }
    }
}