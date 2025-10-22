using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using TypeBeat.Game.Beatmaps;
// Add this for logging
using System.Diagnostics;

namespace TypeBeat.Game.Filehandling
{
    public static class BeatmapParser
    {
        public static Beatpack ParseBeatpack(string filePath)
        {
            // --- DEBUG LINE ---
            Debug.WriteLine($"[BeatmapParser] Starting to parse beatpack: {Path.GetFileName(filePath)}");

            var beatpack = new Beatpack
            {
                FilePath = filePath
            };

            try
            {
                using (var archive = ZipFile.OpenRead(filePath))
                {
                    var tbmdEntries = archive.Entries.Where(e => e.Name.EndsWith(".tbmd")).ToList();

                    // --- DEBUG LINE ---
                    Debug.WriteLine($"[BeatmapParser] Found {tbmdEntries.Count} beatmap file(s).");

                    if (!tbmdEntries.Any())
                        throw new FileNotFoundException($"Beatpack '{Path.GetFileName(filePath)}' does not contain any .tbmd files.");

                    foreach (var tbmdEntry in tbmdEntries)
                    {
                        // --- DEBUG LINE ---
                        Debug.WriteLine($"[BeatmapParser] --- Parsing entry: {tbmdEntry.FullName}");

                        using (var stream = tbmdEntry.Open())
                        using (var reader = new StreamReader(stream))
                        {
                            string jsonContent = reader.ReadToEnd();
                            
                            try
                            {
                                var beatmap = JsonConvert.DeserializeObject<Beatmap>(jsonContent);
                                
                                // --- DEBUG LINES ---
                                if (beatmap != null)
                                {
                                    Debug.WriteLine($"[BeatmapParser] Successfully parsed beatmap:");
                                    Debug.WriteLine($"[BeatmapParser]    Difficulty: {beatmap.DifficultyName}");
                                    Debug.WriteLine($"[BeatmapParser]    Gamemode:   {beatmap.Gamemode}");
                                }
                                else
                                {
                                    Debug.WriteLine($"[BeatmapParser] Error: Parsed '{tbmdEntry.FullName}' but the beatmap object was null.");
                                }
                                // ---

                                beatpack.Beatmaps.Add(beatmap);
                            }
                            catch (JsonException jsonEx)
                            {
                                // --- DEBUG LINE ---
                                Debug.WriteLine($"[BeatmapParser] JSON Error parsing '{tbmdEntry.FullName}': {jsonEx.Message}");
                                throw new JsonException($"Invalid JSON in '{tbmdEntry.FullName}' from beatpack '{Path.GetFileName(filePath)}': {jsonEx.Message}", jsonEx);
                            }
                        }
                    }

                    beatpack.Beatmap = beatpack.Beatmaps.FirstOrDefault();

                    // --- DEBUG LINE ---
                    Debug.WriteLine($"[BeatmapParser] Finished parsing. Total beatmaps loaded: {beatpack.Beatmaps.Count}");

                    var musicEntry = archive.Entries.FirstOrDefault(e => e.Name.Equals("audio.ogg", StringComparison.OrdinalIgnoreCase));
                    beatpack.MusicPath = musicEntry?.Name; 

                    var backgroundEntry = archive.Entries.FirstOrDefault(e =>
                        e.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        e.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase));

                    beatpack.BackgroundImagePath = backgroundEntry?.Name;

                    beatpack.VideoPath = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))?.Name;

                    beatpack.KeyPressSoundPath = archive.Entries.FirstOrDefault(e => e.FullName.Equals("hitsounds/key-press.ogg", StringComparison.OrdinalIgnoreCase))?.Name;
                    beatpack.SpacePressSoundPath = archive.Entries.FirstOrDefault(e => e.FullName.Equals("hitsounds/space-press.ogg", StringComparison.OrdinalIgnoreCase))?.Name;
                }
            }
            catch (InvalidDataException zipEx)
            {
                // --- DEBUG LINE ---
                Debug.WriteLine($"[BeatmapParser] ZIP Error for '{Path.GetFileName(filePath)}': {zipEx.Message}");
                throw new InvalidDataException($"Failed to open ZIP archive '{Path.GetFileName(filePath)}': File may be corrupted or not a valid ZIP file.", zipEx);
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