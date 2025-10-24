using System;
using System.Collections.Generic;
using Postgrest.Attributes;
using Postgrest.Models;

namespace TypeBeat.Game.Online.Models
{
    [Table("beatmaps")]
    public class BeatmapMetadata : BaseModel
    {
        [PrimaryKey("id", true)]
        public long Id { get; set; }

        [Column("beatpack_id")]
        public long BeatpackId { get; set; }

        [Column("creator_id")]
        public string CreatorId { get; set; } = string.Empty;

        [Column("difficulty_name")]
        public string DifficultyName { get; set; } = string.Empty;

        [Column("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [Column("md5_hash")]
        public string Md5Hash { get; set; } = string.Empty;

        [Column("bpm")]
        public int Bpm { get; set; }

        [Column("length_seconds")]
        public int LengthSeconds { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
