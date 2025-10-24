using System;
using System.Collections.Generic;
using Postgrest.Attributes;
using Postgrest.Models;

namespace TypeBeat.Game.Online.Models
{
    [Table("beatpacks")]
    public class BeatpackMetadata : BaseModel
    {
        [PrimaryKey("id", true)]
        public long Id { get; set; }

        [Column("creator_id")]
        public string CreatorId { get; set; } = string.Empty;

        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("artist")]
        public string Artist { get; set; } = string.Empty;

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [Column("preview_image_url")]
        public string PreviewImageUrl { get; set; } = string.Empty;

        [Column("beatpack_file_url")]
        public string BeatpackFileUrl { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public List<BeatmapMetadata> Beatmaps { get; set; } = new List<BeatmapMetadata>();
    }
}
