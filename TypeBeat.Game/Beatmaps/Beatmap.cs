using System.Collections.Generic;
using Newtonsoft.Json;

#nullable enable

namespace TypeBeat.Game.Beatmaps
{
    public class Beatmap
    {
        [JsonProperty("Artist")]
        public string Artist { get; set; } = string.Empty;

        [JsonProperty("Title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("BPM")]
        public double BPM { get; set; }

        [JsonProperty("Creators")]
        public List<string> Creators { get; set; } = new List<string>();

        [JsonProperty("Source")]
        public string Source { get; set; } = string.Empty;

        [JsonProperty("Tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [JsonProperty("PreviewTime")]
        public int PreviewTime { get; set; }

        [JsonProperty("DifficultyName")]
        public string DifficultyName { get; set; } = string.Empty;
        public float StarRating { get; set; }

        [JsonProperty("Gamemode")]
        public string Gamemode { get; set; } = string.Empty;

        [JsonProperty("Sounds")]
        public string Sounds { get; set; } = string.Empty;

        [JsonProperty("MusicKey")]
        public string MusicKey { get; set; } = string.Empty;

        [JsonProperty("BackgroundImage")]
        public string BackgroundImage { get; set; } = string.Empty;

        [JsonProperty("Video")]
        public string Video { get; set; } = string.Empty;

        [JsonProperty("MapData")]
        public List<WordSegment> MapData { get; set; } = new List<WordSegment>();

        [JsonProperty("Audio")]
        public string Audio { get; set; } = string.Empty;

        [JsonProperty("OnlineBeatmapID")]
        public long? OnlineBeatmapID { get; set; } = null;

        [JsonProperty("CreatorID")]
        public string? CreatorID { get; set; } = null;
    }
}