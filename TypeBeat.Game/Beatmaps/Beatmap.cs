using System.Collections.Generic;
using Newtonsoft.Json;

namespace TypeBeat.Game.Beatmaps
{
    public class Beatmap
    {
        [JsonProperty("Artist")]
        public string Artist { get; set; }

        [JsonProperty("Title")]
        public string Title { get; set; }

        [JsonProperty("BPM")]
        public double BPM { get; set; }

        [JsonProperty("Creators")]
        public List<string> Creators { get; set; }

        [JsonProperty("Source")]
        public string Source { get; set; }

        [JsonProperty("Tags")]
        public List<string> Tags { get; set; }

        [JsonProperty("PreviewTime")]
        public int PreviewTime { get; set; }

        [JsonProperty("DifficultyName")]
        public string DifficultyName { get; set; }
        
        [JsonProperty("BackgroundImage")]
        public string BackgroundImage { get; set; }

        [JsonProperty("Video")]
        public string Video { get; set; }

        [JsonProperty("MapData")]
        public List<WordSegment> MapData { get; set; }

        [JsonProperty("Audio")]
        public string Audio { get; set; }
        
    }
}