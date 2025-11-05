using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TypeBeat.Game.Online.Models;

namespace TypeBeat.Game.Online
{
    public class Room
    {
        [JsonProperty("id")]
        public long Id { get; set; }
        
        [JsonProperty("host_id")]
        public string HostId { get; set; }
        
        [JsonProperty("room_name")]
        public string RoomName { get; set; }
        
        [JsonProperty("status")]
        public string Status { get; set; } // "waiting", "playing", "finished"
        
        [JsonProperty("selected_beatmap_id")]
        public long? SelectedBeatmapId { get; set; }
        
        [JsonProperty("selected_beatpack_id")]
        public long? SelectedBeatpackId { get; set; }
        
        [JsonProperty("max_players")]
        public int MaxPlayers { get; set; }
        
        [JsonProperty("room_password")]
        public string RoomPassword { get; set; }
        
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
        
        // Navigation properties
        public Models.UserProfile Host { get; set; }
        public BeatmapInfo SelectedBeatmap { get; set; }
        public BeatpackInfo SelectedBeatpack { get; set; }
        public List<RoomParticipant> Participants { get; set; }
    }
    
    public class RoomParticipant
    {
        [JsonProperty("room_id")]
        public long RoomId { get; set; }
        
        [JsonProperty("user_id")]
        public string UserId { get; set; }
        
        [JsonProperty("status")]
        public string Status { get; set; } // "ready", "not_ready", "playing"
        
        [JsonProperty("joined_at")]
        public DateTime JoinedAt { get; set; }
        
        // Navigation property
        public Models.UserProfile User { get; set; }
    }
    
    public class BeatmapInfo
    {
        [JsonProperty("id")]
        public long Id { get; set; }
        
        [JsonProperty("beatpack_id")]
        public long BeatpackId { get; set; }
        
        [JsonProperty("difficulty_name")]
        public string DifficultyName { get; set; }
        
        [JsonProperty("bpm")]
        public int Bpm { get; set; }
        
        [JsonProperty("length_seconds")]
        public int LengthSeconds { get; set; }
        
        // Navigation property
        public BeatpackInfo Beatpack { get; set; }
    }
    
    public class BeatpackInfo
    {
        [JsonProperty("id")]
        public long Id { get; set; }
        
        [JsonProperty("title")]
        public string Title { get; set; }
        
        [JsonProperty("artist")]
        public string Artist { get; set; }
        
        [JsonProperty("creator_id")]
        public string CreatorId { get; set; }
        
        [JsonProperty("beatpack_file_url")]
        public string BeatpackFileUrl { get; set; }
        
        // Navigation property
        public Models.UserProfile Creator { get; set; }
    }
    
    public class MatchScore
    {
        [JsonProperty("id")]
        public long Id { get; set; }
        
        [JsonProperty("room_id")]
        public long RoomId { get; set; }
        
        [JsonProperty("user_id")]
        public string UserId { get; set; }
        
        [JsonProperty("beatmap_id")]
        public long BeatmapId { get; set; }
        
        [JsonProperty("score")]
        public long Score { get; set; }
        
        [JsonProperty("accuracy")]
        public float Accuracy { get; set; }
        
        [JsonProperty("max_combo")]
        public int MaxCombo { get; set; }
        
        [JsonProperty("played_at")]
        public DateTime PlayedAt { get; set; }
        
        // For live display
        public Models.UserProfile User { get; set; }
    }
    
    public class CreateRoomRequest
    {
        [JsonProperty("room_name")]
        public string RoomName { get; set; }
        
        [JsonProperty("max_players")]
        public int MaxPlayers { get; set; }
        
        [JsonProperty("room_password")]
        public string RoomPassword { get; set; }
        
        [JsonProperty("selected_beatmap_id")]
        public long? SelectedBeatmapId { get; set; }
    }
}
