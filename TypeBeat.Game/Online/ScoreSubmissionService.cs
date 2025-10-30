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
        [Column("beatmap_id")]
        public long BeatmapId { get; set; }

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
            long beatmapId,
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

                if (beatmapId <= 0)
                {
                    return (false, "Invalid beatmap - this beatmap is not uploaded online");
                }

                var scoreEntry = new ScoreEntry
                {
                    BeatmapId = beatmapId,
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
        public async Task<ScoreEntry?> GetPersonalBestAsync(long beatmapId, string playerId)
        {
            try
            {
                if (BackendClient.Client == null || beatmapId <= 0)
                    return null;

                var response = await BackendClient.Client
                    .From<ScoreEntry>()
                    .Select("*")
                    .Filter("beatmap_id", Operator.Equals, beatmapId)
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
