using System;
using System.Collections.Generic;
using Postgrest.Attributes;
using Postgrest.Models;

namespace TypeBeat.Game.Online.Models
{
    [Table("profiles")]
    public class UserProfile : BaseModel
    {
        [PrimaryKey("id")]
        public string Id { get; set; } = string.Empty;

        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("level")]
        public int Level { get; set; }

        [Column("xp")]
        public long Xp { get; set; }

        [Column("avatar_url")]
        public string AvatarUrl { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
