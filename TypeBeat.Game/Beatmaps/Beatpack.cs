using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json; // <-- Make sure this is included

namespace TypeBeat.Game.Beatmaps
{
    public class Beatpack
    {
        public string FilePath { get; set; }
        
        [JsonProperty("beatpack_id")]
        public string OnlineBeatpackID { get; set; } 

        public Beatmap Beatmap { get; set; }
        
        // Multiple beatmaps (different difficulties)
        public List<Beatmap> Beatmaps { get; set; } = new List<Beatmap>();
        
        // OLD FORMAT: Single file paths (backward compatibility)
        public string MusicPath { get; set; }
        public string BackgroundImagePath { get; set; }
        
        // NEW FORMAT: Multiple audio files
        public List<AudioFileInfo> AudioFiles { get; set; } = new List<AudioFileInfo>();
        
        // NEW FORMAT: Multiple backgrounds
        public List<BackgroundInfo> BackgroundImages { get; set; } = new List<BackgroundInfo>();
        
        // NEW FORMAT: Multiple videos
        public List<VideoFileInfo> VideoFiles { get; set; } = new List<VideoFileInfo>();
        
        // Custom sounds
        public CustomSoundsConfig CustomSounds { get; set; }
        
        public string VideoPath { get; set; }
        public string KeyPressSoundPath { get; set; }
        public string SpacePressSoundPath { get; set; }
        
        public bool IsNewFormat => AudioFiles != null && AudioFiles.Count > 0;

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
        
        public string GetAudioPathForBeatmap(Beatmap beatmap)
        {
            System.Diagnostics.Debug.WriteLine($"[Beatpack] GetAudioPathForBeatmap - Beatmap: {beatmap?.DifficultyName ?? "(null)"}");
            
            // 1) Prefer explicit filename on the beatmap (new TBMD field: "audio")
            if (beatmap != null && !string.IsNullOrEmpty(beatmap.AudioFilename))
            {
                System.Diagnostics.Debug.WriteLine($"[Beatpack] Found explicit audio filename: '{beatmap.AudioFilename}'");
                
                // If pack is new-format and we know the exact path for this filename from manifest, use it;
                // otherwise default to audio/<filename>
                if (IsNewFormat && AudioFiles != null && AudioFiles.Count > 0)
                {
                    var fromManifest = AudioFiles.FirstOrDefault(a => a.Filename.Equals(beatmap.AudioFilename, StringComparison.OrdinalIgnoreCase));
                    if (fromManifest != null && !string.IsNullOrEmpty(fromManifest.Path))
                    {
                        System.Diagnostics.Debug.WriteLine($"[Beatpack] Using manifest path: '{fromManifest.Path}'");
                        return fromManifest.Path;
                    }
                }
                var defaultPath = $"audio/{beatmap.AudioFilename}";
                System.Diagnostics.Debug.WriteLine($"[Beatpack] Using default path: '{defaultPath}'");
                return defaultPath;
            }

            // 2) Back-compat: use index into manifest list if available
            if (IsNewFormat && AudioFiles != null && AudioFiles.Count > 0)
            {
                var audioIndex = beatmap?.AudioIndex ?? 0;
                if (audioIndex < 0 || audioIndex >= AudioFiles.Count)
                    audioIndex = 0;
                var byIndex = AudioFiles[audioIndex]?.Path;
                if (!string.IsNullOrEmpty(byIndex))
                    return byIndex;
            }

            // 3) Old format fallback
            if (!string.IsNullOrEmpty(MusicPath))
                return MusicPath;

            // 4) Last-resort heuristics
            // Prefer audio/audio.ogg if present, else first .ogg entry under audio/
            try
            {
                if (!string.IsNullOrEmpty(FilePath) && File.Exists(FilePath))
                {
                    using (var archive = ZipFile.OpenRead(FilePath))
                    {
                        var preferred = archive.Entries.FirstOrDefault(e => e.FullName.Equals("audio/audio.ogg", StringComparison.OrdinalIgnoreCase));
                        if (preferred != null) return preferred.FullName;

                        var anyOgg = archive.Entries.FirstOrDefault(e => e.FullName.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) && e.Name.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase));
                        if (anyOgg != null) return anyOgg.FullName;
                    }
                }
            }
            catch { }

            return null;
        }

        public Stream GetAudioStreamForBeatmap(Beatmap beatmap)
        {
            var audioPath = GetAudioPathForBeatmap(beatmap);
            if (!string.IsNullOrEmpty(audioPath))
            {
                // Keep MusicPath in sync for logging/UI purposes
                MusicPath = audioPath;
                return GetStream(audioPath);
            }
            return null;
        }
        
        public Stream GetBackgroundStreamForBeatmap(Beatmap beatmap)
        {
            string bgPath;
            
            // 1) Try the new format - multiple backgrounds list
            if (IsNewFormat && BackgroundImages != null && BackgroundImages.Count > 0)
            {
                var bgIndex = beatmap?.BackgroundIndex ?? 0;
                if (bgIndex < 0 || bgIndex >= BackgroundImages.Count)
                    bgIndex = 0;
                bgPath = BackgroundImages[bgIndex]?.Path;
                if (!string.IsNullOrEmpty(bgPath))
                {
                    BackgroundImagePath = bgPath; // keep in sync for UI/logging
                    return GetStream(bgPath);
                }
            }
            
            // 2) Back-compat: old single background field
            if (!string.IsNullOrEmpty(BackgroundImagePath))
            {
                return GetStream(BackgroundImagePath);
            }
            
            // 3) Last-resort heuristics
            bgPath = FindFirstMatchingFile("backgrounds/", new[] { ".jpg", ".jpeg", ".png" });
            if (!string.IsNullOrEmpty(bgPath))
            {
                BackgroundImagePath = bgPath;
                return GetStream(bgPath);
            }
            
            return null;
        }
        
        public string GetVideoPathForBeatmap(Beatmap beatmap)
        {
            // 1) Check if beatmap has explicit Video field
            if (beatmap != null && !string.IsNullOrEmpty(beatmap.Video))
            {
                // If new format has videos list, find matching path
                if (VideoFiles != null && VideoFiles.Count > 0)
                {
                    var fromManifest = VideoFiles.FirstOrDefault(v => v.Filename.Equals(beatmap.Video, StringComparison.OrdinalIgnoreCase));
                    if (fromManifest != null && !string.IsNullOrEmpty(fromManifest.Path))
                        return fromManifest.Path;
                }
                // Default to videos folder
                return $"videos/{beatmap.Video}";
            }
            
            // 2) Use first video from new format if available
            if (VideoFiles != null && VideoFiles.Count > 0 && !string.IsNullOrEmpty(VideoFiles[0].Path))
                return VideoFiles[0].Path;
            
            // 3) Last-resort: look for any video file in videos folder
            return FindFirstMatchingFile("videos/", new[] { ".mp4", ".webm", ".avi", ".mov" });
        }
        
        public Stream GetVideoStreamForBeatmap(Beatmap beatmap)
        {
            var videoPath = GetVideoPathForBeatmap(beatmap);
            if (!string.IsNullOrEmpty(videoPath))
                return GetStream(videoPath);
            return null;
        }

        public IEnumerable<CustomSoundFile> GetCustomSoundsForGamemode(string gamemode)
        {
            // Prefer per-beatmap custom sounds if provided; fallback to pack-level
            var cfg = Beatmap?.CustomSounds ?? CustomSounds;
            if (cfg == null || !cfg.Enabled)
                return Array.Empty<CustomSoundFile>();

            var sounds = gamemode?.Equals("TypeNote", StringComparison.OrdinalIgnoreCase) == true
                ? cfg.TypeNoteSounds
                : cfg.TypeBeatSounds;

            return sounds ?? Enumerable.Empty<CustomSoundFile>();
        }

        public Stream GetCustomSoundStream(CustomSoundFile sound)
        {
            if (sound == null || string.IsNullOrEmpty(sound.Path))
                return null;

            return GetStream(sound.Path);
        }
        
        /// <summary>
        /// Helper method to find the first file in a folder matching any of the given extensions
        /// </summary>
        private string FindFirstMatchingFile(string folderPrefix, string[] extensions)
        {
            if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
                return null;
                
            try
            {
                using (var archive = ZipFile.OpenRead(FilePath))
                {
                    foreach (var ext in extensions)
                    {
                        var entry = archive.Entries.FirstOrDefault(e => 
                            e.FullName.StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase) && 
                            e.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
                        if (entry != null)
                            return entry.FullName;
                    }
                }
            }
            catch { }
            
            return null;
        }
    }
    
    public class AudioFileInfo
    {
        [JsonProperty("filename")]
        public string Filename { get; set; }
        
        [JsonProperty("path")]
        public string Path { get; set; }
        
        [JsonProperty("index")]
        public int Index { get; set; }
    }
    
    public class BackgroundInfo
    {
        [JsonProperty("filename")]
        public string Filename { get; set; }
        
        [JsonProperty("path")]
        public string Path { get; set; }
        
        [JsonProperty("index")]
        public int Index { get; set; }
    }
    
    public class VideoFileInfo
    {
        [JsonProperty("filename")]
        public string Filename { get; set; }
        
        [JsonProperty("path")]
        public string Path { get; set; }
        
        [JsonProperty("index")]
        public int Index { get; set; }
    }
    
    public class CustomSoundsConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("typebeat_sounds")]
        public List<CustomSoundFile> TypeBeatSounds { get; set; } = new List<CustomSoundFile>();

        [JsonProperty("typenote_sounds")]
        public List<CustomSoundFile> TypeNoteSounds { get; set; } = new List<CustomSoundFile>();

        // Optional: name of the TypeNote soundpack to use (folder under audio/TypeNote/<name>/)
        [JsonProperty("typenote_soundpack")] 
        public string TypeNoteSoundpack { get; set; }
    }
    
    public class CustomSoundFile
    {
        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }
    }
}