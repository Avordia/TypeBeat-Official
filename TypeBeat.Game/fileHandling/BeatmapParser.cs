using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using TypeBeat.Game.Beatmaps;
// Add this for logging
using System.Diagnostics;

namespace TypeBeat.Game.Filehandling
{
    /// <summary>
    /// Manifest structure from new .tbbp format
    /// </summary>
    public class BeatpackManifest
    {
        [JsonProperty("audio_files")]
        public List<AudioFileInfo> AudioFiles { get; set; }
        
        [JsonProperty("background_images")]
        public List<BackgroundInfo> BackgroundImages { get; set; }
        
        [JsonProperty("video_files")]
        public List<VideoFileInfo> VideoFiles { get; set; }
        
        [JsonProperty("custom_sounds")]
        public CustomSoundsConfig CustomSounds { get; set; }
    }
    
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
                                    Debug.WriteLine($"[BeatmapParser]    AudioFilename: {beatmap.AudioFilename ?? "(null)"}");
                                    Debug.WriteLine($"[BeatmapParser]    CustomSounds.Enabled: {beatmap.CustomSounds?.Enabled.ToString() ?? "(null)"}");
                                    Debug.WriteLine($"[BeatmapParser]    CustomSounds.TypeNoteSoundpack: {beatmap.CustomSounds?.TypeNoteSoundpack ?? "(null)"}");
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

                    // Try to load manifest.json for new format
                    var manifestEntry = archive.GetEntry("manifest.json");
                    if (manifestEntry != null)
                    {
                        Debug.WriteLine($"[BeatmapParser] Found manifest.json - loading new format");
                        using (var stream = manifestEntry.Open())
                        using (var reader = new StreamReader(stream))
                        {
                            var json = reader.ReadToEnd();
                            var manifest = JsonConvert.DeserializeObject<BeatpackManifest>(json);
                            
                            if (manifest != null)
                            {
                                beatpack.AudioFiles = manifest.AudioFiles ?? new List<AudioFileInfo>();
                                beatpack.BackgroundImages = manifest.BackgroundImages ?? new List<BackgroundInfo>();
                                beatpack.VideoFiles = manifest.VideoFiles ?? new List<VideoFileInfo>();
                                beatpack.CustomSounds = manifest.CustomSounds ?? new CustomSoundsConfig();

                                // Set legacy paths for backward compatibility with existing consumers
                                if (beatpack.AudioFiles.Any())
                                    beatpack.MusicPath = beatpack.AudioFiles[0].Path;
                                
                                if (beatpack.BackgroundImages.Any())
                                    beatpack.BackgroundImagePath = beatpack.BackgroundImages[0].Path;
                                
                                if (beatpack.VideoFiles.Any())
                                    beatpack.VideoPath = beatpack.VideoFiles[0].Path;

                                Debug.WriteLine($"[BeatmapParser] Loaded {beatpack.AudioFiles.Count} audio files, {beatpack.BackgroundImages.Count} backgrounds, {beatpack.VideoFiles.Count} videos");
                                Debug.WriteLine($"[BeatmapParser] Manifest CustomSounds.Enabled: {beatpack.CustomSounds?.Enabled.ToString() ?? "(null)"}");
                                Debug.WriteLine($"[BeatmapParser] Manifest CustomSounds.TypeNoteSoundpack: {beatpack.CustomSounds?.TypeNoteSoundpack ?? "(null)"}");
                                
                                // List all audio files for debugging
                                foreach (var audioFile in beatpack.AudioFiles)
                                {
                                    Debug.WriteLine($"[BeatmapParser]   - Audio: {audioFile.Filename} => {audioFile.Path}");
                                }
                                
                                // List all video files for debugging
                                foreach (var videoFile in beatpack.VideoFiles)
                                {
                                    Debug.WriteLine($"[BeatmapParser]   - Video: {videoFile.Filename} => {videoFile.Path}");
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"[BeatmapParser] No manifest.json - using old format");
                        // OLD FORMAT: Look for single audio file (try .mp3 first, then .ogg)
                        var musicEntry = archive.Entries.FirstOrDefault(e => e.Name.Equals("audio.mp3", StringComparison.OrdinalIgnoreCase));
                        if (musicEntry == null)
                        {
                            musicEntry = archive.Entries.FirstOrDefault(e => e.Name.Equals("audio.ogg", StringComparison.OrdinalIgnoreCase));
                        }
                        beatpack.MusicPath = musicEntry?.Name; 

                        var backgroundEntry = archive.Entries.FirstOrDefault(e =>
                            e.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                            e.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase));

                        beatpack.BackgroundImagePath = backgroundEntry?.Name;
                    }

                    // OLD FORMAT: Look for video file (check videos folder first, then root)
                    var videoEntry = archive.Entries.FirstOrDefault(e => 
                        e.FullName.StartsWith("videos/", StringComparison.OrdinalIgnoreCase) && 
                        (e.Name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) || 
                         e.Name.EndsWith(".webm", StringComparison.OrdinalIgnoreCase)));
                    
                    if (videoEntry == null)
                    {
                        // Fallback: check root for video files
                        videoEntry = archive.Entries.FirstOrDefault(e => 
                            !e.FullName.Contains("/") && 
                            (e.Name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) || 
                             e.Name.EndsWith(".webm", StringComparison.OrdinalIgnoreCase)));
                    }
                    
                    beatpack.VideoPath = videoEntry?.FullName;

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