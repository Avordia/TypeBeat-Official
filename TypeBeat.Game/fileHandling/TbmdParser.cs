using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace TypeBeat.Game.fileHandling
{
    public static class TbmdParser
    {
        public static Beatmap Parse(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The specified beatmap file was not found.", filePath);
            }
            string jsonContent = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<Beatmap>(jsonContent);
        }
    }
    
    public class Beatmap
    {
        [JsonProperty("Artist")]
        public string Artist { get; set; }

        [JsonProperty("Title")]
        public string Title { get; set; }

        [JsonProperty("BPM")]
        public double BPM { get; set; }

        [JsonProperty("MapData")]
        public List<WordSegment> MapData { get; set; }
    }

    public class WordSegment
    {
        [JsonProperty("Notes")]
        public List<Note> Notes { get; set; }
    }

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