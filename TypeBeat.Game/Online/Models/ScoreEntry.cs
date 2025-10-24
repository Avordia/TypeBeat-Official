using System;
using System.Collections.Generic;
using Postgrest.Attributes;
using Postgrest.Models;

#nullable enable

namespace TypeBeat.Game.Online.Models
{
    [Table("scores")]
    public class ScoreEntry : BaseModel
    {
        [PrimaryKey("beatmap_id")]
        public long BeatmapId { get; set; }

        [PrimaryKey("player_id")]
        public string PlayerId { get; set; } = string.Empty;

        [Column("score")]
        public long ScoreValue { get; set; }

        [Column("accuracy")]
        public double Accuracy { get; set; }

        [Column("max_combo")]
        public int MaxCombo { get; set; }

        [Column("mods")]
        public List<string> Mods { get; set; } = new List<string>();

        [Column("game_timestamp")]
        public DateTime GameTimestamp { get; set; }

        public UserProfile? Profile { get; set; }
    }
}
