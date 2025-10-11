using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using TypeBeat.Game.Beatmaps;

namespace TypeBeat.Game.fileHandling
{
    public static class BeatmapParser
    {
        public static Beatpack ParseBeatpack(string filePath)
        {
            var beatpack = new Beatpack
            {
                FilePath = filePath
            };

            using (var archive = ZipFile.OpenRead(filePath))
            {
                var tbmdEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".tbmd"));
                if (tbmdEntry is null)
                    throw new FileNotFoundException("Beatpack does not contain a .tbmd file.");

                using (var stream = tbmdEntry.Open())
                using (var reader = new StreamReader(stream))
                {
                    string jsonContent = reader.ReadToEnd();
                    beatpack.Beatmap = JsonConvert.DeserializeObject<Beatmap>(jsonContent);
                }

                // Make audio optional by not throwing an error if it's missing.
                var musicEntry = archive.Entries.FirstOrDefault(e => e.Name.Equals("audio.ogg", StringComparison.OrdinalIgnoreCase));
                beatpack.MusicPath = musicEntry?.Name; // This will be null if not found, which is now acceptable.

                var backgroundEntry = archive.Entries.FirstOrDefault(e =>
                    e.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    e.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase));

                beatpack.BackgroundImagePath = backgroundEntry?.Name;

                beatpack.VideoPath = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))?.Name;

                beatpack.KeyPressSoundPath = archive.Entries.FirstOrDefault(e => e.FullName.Equals("hitsounds/key-press.ogg", StringComparison.OrdinalIgnoreCase))?.Name;
                beatpack.SpacePressSoundPath = archive.Entries.FirstOrDefault(e => e.FullName.Equals("hitsounds/space-press.ogg", StringComparison.OrdinalIgnoreCase))?.Name;
            }

            return beatpack;
        }

        public static Beatmap ParseTbmd(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The specified beatmap file was not found.", filePath);
            }

            string jsonContent = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<Beatmap>(jsonContent);
        }
    }
}