using System;
using Postgrest.Models;
using Postgrest.Attributes;

namespace TypeBeat.Game.Online.Models
{
    [Table("scores")]
    public class ScoreEntry : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        // --- ADDED ---
        [Column("beatmap_id")]
        public string BeatmapId { get; set; } // The online ID of the beatmap
        // --- END ADD ---

        [Column("score")]
        public long Score { get; set; }

        [Column("accuracy")]
        public double Accuracy { get; set; }

        [Column("combo")]
        public int Combo { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}