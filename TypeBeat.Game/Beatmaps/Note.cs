using Newtonsoft.Json;

namespace TypeBeat.Game.Beatmaps
{
    public class Note
    {
        [JsonProperty("Character")]
        public string Character { get; set; }

        [JsonProperty("StartTime")]
        public double StartTime { get; set; }

        [JsonProperty("EndTime")]
        public double EndTime { get; set; }
    }
}