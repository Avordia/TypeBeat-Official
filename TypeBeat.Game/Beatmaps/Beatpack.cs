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
        
        // --- ADDED ---
        [JsonProperty("beatpack_id")]
    public string OnlineBeatpackID { get; set; } // Null for drafts (nullable context not enabled)
        // --- END ADD ---

        // Single beatmap (for backward compatibility)
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
        
        // Custom sounds
        public CustomSoundsConfig CustomSounds { get; set; }
        
        public string VideoPath { get; set; }
        public string KeyPressSoundPath { get; set; }
        public string SpacePressSoundPath { get; set; }
        
        /// <summary>
        /// Check if this is a new format beatpack
        /// </summary>
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
        
        public Stream GetAudioStreamForBeatmap(Beatmap beatmap)
        {
            string audioPath;
            
            if (IsNewFormat)
            {
                var audioIndex = beatmap.AudioIndex ?? 0;
                if (audioIndex >= AudioFiles.Count)
                    audioIndex = 0;
                audioPath = AudioFiles[audioIndex].Path;
            }
            else
            {
                audioPath = MusicPath;
            }
            
            return GetStream(audioPath);
        }
        
        public Stream GetBackgroundStreamForBeatmap(Beatmap beatmap)
        {
            string bgPath;
            
            if (IsNewFormat && BackgroundImages != null && BackgroundImages.Count > 0)
            {
                var bgIndex = beatmap.BackgroundIndex ?? 0;
                if (bgIndex >= BackgroundImages.Count)
                    bgIndex = 0;
                bgPath = BackgroundImages[bgIndex].Path;
            }
            else
            {
                bgPath = BackgroundImagePath;
            }
            
            return string.IsNullOrEmpty(bgPath) ? null : GetStream(bgPath);
        }

        public IEnumerable<CustomSoundFile> GetCustomSoundsForGamemode(string gamemode)
        {
            if (CustomSounds == null || !CustomSounds.Enabled)
                return Array.Empty<CustomSoundFile>();

            var sounds = gamemode?.Equals("TypeNote", StringComparison.OrdinalIgnoreCase) == true
                ? CustomSounds.TypeNoteSounds
                : CustomSounds.TypeBeatSounds;

            return sounds ?? Enumerable.Empty<CustomSoundFile>();
        }

        public Stream GetCustomSoundStream(CustomSoundFile sound)
        {
            if (sound == null || string.IsNullOrEmpty(sound.Path))
                return null;

            return GetStream(sound.Path);
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