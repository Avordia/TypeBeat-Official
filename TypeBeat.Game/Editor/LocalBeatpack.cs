// Copyright (c) TypeBeat. Licensed under the MIT Licence.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TypeBeat.Game.Editor
{
    /// <summary>
    /// Represents a local work-in-progress beatpack in the editor.
    /// </summary>
    public class LocalBeatpack
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("lastModified")]
        public DateTime LastModified { get; set; }

        [JsonProperty("isFinished")]
        public bool IsFinished { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("artist")]
        public string Artist { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [JsonProperty("localBeatmaps")]
        public List<LocalBeatmap> LocalBeatmaps { get; set; }

        [JsonProperty("musicFilePath")]
        public string MusicFilePath { get; set; }

        [JsonProperty("backgroundImagePath")]
        public string BackgroundImagePath { get; set; }

        [JsonProperty("videoPath")]
        public string VideoPath { get; set; }

        public LocalBeatpack()
        {
            Id = Guid.NewGuid().ToString();
            Name = "New Beatpack";
            LastModified = DateTime.Now;
            IsFinished = false;
            Tags = new List<string>();
            LocalBeatmaps = new List<LocalBeatmap>();
            Title = "";
            Artist = "";
            Description = "";
            MusicFilePath = "";
            BackgroundImagePath = "";
            VideoPath = "";
        }

        /// <summary>
        /// Serializes this LocalBeatpack to a JSON string.
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Deserializes a LocalBeatpack from a JSON string.
        /// </summary>
        public static LocalBeatpack FromJson(string json)
        {
            return JsonConvert.DeserializeObject<LocalBeatpack>(json);
        }
    }
}


