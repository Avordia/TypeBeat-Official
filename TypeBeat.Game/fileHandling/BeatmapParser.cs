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
            var beatpack = new Beatpack();

            using (var archive = ZipFile.OpenRead(filePath))
            {
                var tbmdEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".tbmd"));
                if (tbmdEntry == null)
                    throw new FileNotFoundException("Beatpack does not contain a .tbmd file.");

                using (var stream = tbmdEntry.Open())
                using (var reader = new StreamReader(stream))
                {
                    string jsonContent = reader.ReadToEnd();
                    beatpack.Beatmap = JsonConvert.DeserializeObject<Beatmap>(jsonContent);
                }

                var musicEntry = archive.Entries.FirstOrDefault(e => e.Name.Equals("audio.ogg", StringComparison.OrdinalIgnoreCase));
                if (musicEntry == null)
                    throw new FileNotFoundException("Beatpack does not contain an 'audio.ogg' music file.");

                beatpack.MusicPath = musicEntry.Name;

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
