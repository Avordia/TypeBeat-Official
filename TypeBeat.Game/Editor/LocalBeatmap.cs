// Copyright (c) TypeBeat. Licensed under the MIT Licence.

using System.Collections.Generic;
using Newtonsoft.Json;
using TypeBeat.Game.Beatmaps;

namespace TypeBeat.Game.Editor
{
    /// <summary>
    /// Represents a local work-in-progress beatmap in the editor.
    /// </summary>
    public class LocalBeatmap
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("difficultyName")]
        public string DifficultyName { get; set; }

        [JsonProperty("bpm")]
        public double BPM { get; set; }

        [JsonProperty("creators")]
        public List<string> Creators { get; set; }

        [JsonProperty("starRating")]
        public float StarRating { get; set; }

        [JsonProperty("gamemode")]
        public string Gamemode { get; set; }

        [JsonProperty("mapData")]
        public List<WordSegment> MapData { get; set; }

        [JsonProperty("artist")]
        public string Artist { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("audio")]
        public string Audio { get; set; }

        [JsonProperty("backgroundImage")]
        public string BackgroundImage { get; set; }

        [JsonProperty("video")]
        public string Video { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [JsonProperty("previewTime")]
        public int PreviewTime { get; set; }

        public LocalBeatmap()
        {
            Id = System.Guid.NewGuid().ToString();
            DifficultyName = "Normal";
            BPM = 120.0;
            Creators = new List<string>();
            StarRating = 0.0f;
            Gamemode = "TypeBeat";
            MapData = new List<WordSegment>();
            Artist = "";
            Title = "";
            Audio = "";
            BackgroundImage = "";
            Video = "";
            Source = "";
            Tags = new List<string>();
            PreviewTime = 0;
        }

        /// <summary>
        /// Serializes this LocalBeatmap to a JSON string.
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Deserializes a LocalBeatmap from a JSON string.
        /// </summary>
        public static LocalBeatmap FromJson(string json)
        {
            return JsonConvert.DeserializeObject<LocalBeatmap>(json);
        }
    }
}


