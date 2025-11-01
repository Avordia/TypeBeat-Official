using System.Collections.Generic;
using Newtonsoft.Json;

namespace TypeBeat.Game.Beatmaps
{
    public class Beatmap
    {
        // --- ADDED ---
        [JsonProperty("beatmap_id")]
        public string? OnlineBeatmapID { get; set; } // Null for drafts
        // --- END ADD ---
        
        [JsonProperty("Title")]
        public string Title { get; set; }

        [JsonProperty("Artist")]
        public string Artist { get; set; }

        [JsonProperty("DifficultyName")]
        public string DifficultyName { get; set; }

        [JsonProperty("StarRating")]
        public double StarRating { get; set; }

        [JsonProperty("Creators")]
        public List<string> Creators { get; set; } = new List<string>();

        [JsonProperty("Source")]
        public string Source { get; set; }

        [JsonProperty("Tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [JsonProperty("PreviewTime")]
        public int PreviewTime { get; set; }

        [JsonProperty("Gamemode")]
        public string Gamemode { get; set; }

        // Unique attributes for TypeNote
        [JsonProperty("MusicKey")]
        public string? MusicKey { get; set; }

        [JsonProperty("Clef")]
        public string? Clef { get; set; }

        [JsonProperty("BPM")]
        public double Bpm { get; set; }
        
        // Optional media fields for site/export
        [JsonProperty("BackgroundImage")]
        public string? BackgroundImage { get; set; }

        [JsonProperty("Video")]
        public string? Video { get; set; }
        
        // These index fields may be set when reading from a beatpack manifest
        [JsonProperty("audio_index")]
        public int? AudioIndex { get; set; }
        
        [JsonProperty("background_index")]
        public int? BackgroundIndex { get; set; }

        [JsonProperty("MapData")]
        public List<WordSegment> MapData { get; set; } = new List<WordSegment>();

        [JsonIgnore]
        public string BeatmapFileName { get; set; }
    }
}