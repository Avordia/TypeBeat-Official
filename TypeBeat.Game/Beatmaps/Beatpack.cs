using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace TypeBeat.Game.Beatmaps
{
    public class Beatpack
    {
        public string FilePath { get; set; }
        
        // Single beatmap (for backward compatibility)
        public Beatmap Beatmap { get; set; }
        
        // Multiple beatmaps (different difficulties)
        public List<Beatmap> Beatmaps { get; set; } = new List<Beatmap>();
        
        public string MusicPath { get; set; }
        public string BackgroundImagePath { get; set; }
        public string VideoPath { get; set; }
        public string KeyPressSoundPath { get; set; }
        public string SpacePressSoundPath { get; set; }

        public Stream GetStream(string path)
        {
            if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath) || string.IsNullOrEmpty(path))
                return null;

            using (var archive = ZipFile.OpenRead(FilePath))
            {
                var entry = archive.Entries.FirstOrDefault(e => e.FullName.Equals(path, StringComparison.OrdinalIgnoreCase));

                if (entry == null)
                    return null;

                var ms = new MemoryStream();
                using (var stream = entry.Open())
                {
                    stream.CopyTo(ms);
                }
                ms.Position = 0;
                return ms;
            }
        }
    }
}