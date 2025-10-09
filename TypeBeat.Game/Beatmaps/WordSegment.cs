using System.Collections.Generic;
using Newtonsoft.Json;

namespace TypeBeat.Game.Beatmaps
{
    public class WordSegment
    {
        [JsonProperty("Notes")]
        public List<Note> Notes { get; set; }
    }
}