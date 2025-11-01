using System;
using System.Threading.Tasks;
using osu.Framework.Logging;
using Postgrest.Attributes;
using Postgrest.Models;
using static Postgrest.Constants;

#nullable enable

namespace TypeBeat.Game.Online
{
    /// <summary>
    /// Model for score submission to Supabase
    /// </summary>
    [Table("scores")]
    public class ScoreEntry : BaseModel
    {
        // --- CHANGED ---
        // Changed from 'long' to 'string' to match Supabase's UUIDs
        [Column("beatmap_id")]
        public string BeatmapId { get; set; } = string.Empty;
        // --- END CHANGE ---

        [Column("player_id")]
        public string PlayerId { get; set; } = string.Empty;

        [Column("score")]
        public long Score { get; set; }

        [Column("accuracy")]
        public double Accuracy { get; set; }

        [Column("max_combo")]
        public int MaxCombo { get; set; }

        [Column("mods")]
        public string[]? Mods { get; set; }

        [Column("game_timestamp")]
        public DateTime? GameTimestamp { get; set; }
    }

    /// <summary>
    /// Service for submitting scores to Supabase
    /// </summary>
    public class ScoreSubmissionService
    {
        /// <summary>
        /// Submit a score to the database
        /// </summary>
        public async Task<(bool success, string message)> SubmitScoreAsync(
            // --- CHANGED ---
            // Changed from 'long' to 'string?' to accept the OnlineBeatmapID
            string? beatmapId,
            // --- END CHANGE ---
            string playerId,
            long score,
            double accuracy,
            int maxCombo,
            string[]? mods = null)
        {
            try
            {
                if (BackendClient.Client == null)
                {
                    return (false, "Backend not available");
                }

                // --- CHANGED ---
                // This now checks if the ID is null or empty, which correctly
                // skips draft maps.
                if (string.IsNullOrEmpty(beatmapId))
                {
                    return (false, "Invalid beatmap - this beatmap is not uploaded online");
                }
                // --- END CHANGE ---

                var scoreEntry = new ScoreEntry
                {
                    BeatmapId = beatmapId, // This is now a string
                    PlayerId = playerId,
                    Score = score,
                    Accuracy = accuracy,
                    MaxCombo = maxCombo,
                    Mods = mods,
                    GameTimestamp = DateTime.UtcNow
                };

                Logger.Log($"Submitting score: beatmap={beatmapId}, score={score}, accuracy={accuracy:F2}%, combo={maxCombo}",
                    LoggingTarget.Runtime, LogLevel.Important);

                var response = await BackendClient.Client
                    .From<ScoreEntry>()
                    .Insert(scoreEntry);

                if (response != null)
                {
                    Logger.Log("Score submitted successfully!", LoggingTarget.Runtime, LogLevel.Important);
                    return (true, "Score submitted successfully!");
                }

                return (false, "Failed to submit score");
            }
            catch (Exception ex)
            {
                Logger.Log($"Score submission failed: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
                return (false, $"Failed to submit score: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the player's best score for a beatmap
        /// </summary>
        public async Task<ScoreEntry?> GetPersonalBestAsync(string? beatmapId, string playerId) // <-- Also changed this parameter to string?
        {
            try
            {
                // --- CHANGED ---
                if (BackendClient.Client == null || string.IsNullOrEmpty(beatmapId))
                    return null;
                // --- END CHANGE ---

                var response = await BackendClient.Client
                    .From<ScoreEntry>()
                    .Select("*")
                    .Filter("beatmap_id", Operator.Equals, beatmapId) // This filter now works with a string
                    .Filter("player_id", Operator.Equals, playerId)
                    .Order("score", Ordering.Descending)
                    .Limit(1)
                    .Single();

                return response;
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to get personal best: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
                return null;
            }
        }
    }
}