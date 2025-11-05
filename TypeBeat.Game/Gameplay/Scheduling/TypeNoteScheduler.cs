using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.Gameplay.Layout;
using TypeBeat.Game.Gameplay.Objects;
using osu.Framework.Logging;
using System.Linq;

namespace TypeBeat.Game.Gameplay.Scheduling
{
    /// <summary>
    /// Spawns and manages DrawableMusicNote objects for all segments based on timing.
    /// Notes spawn strictly based on their StartTime regardless of current segment.
    /// </summary>
    public partial class TypeNoteScheduler : CompositeDrawable
    {
        private readonly TypeNoteLayoutConfig layout;
        private List<WordSegment> allSegments = new List<WordSegment>();
        private readonly HashSet<Note> spawned = new HashSet<Note>();
        private readonly List<DrawableMusicNote> activeNotes = new List<DrawableMusicNote>();
        private int currentNoteIndex = 0;

        public Note ExcludedVisualNote { get; set; }

        public double PreloadMs { get; set; } = 0;

        public double TimeOffsetMs { get; set; } = 0;

        public TypeNoteScheduler(TypeNoteLayoutConfig layout)
        {
            this.layout = layout;
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.TopLeft;
            Origin = Anchor.TopLeft;
        }

        /// <summary>
        /// Load all segments from the beatmap. Notes will spawn based on time across all segments.
        /// </summary>
        public void LoadAllSegments(List<WordSegment> segments)
        {
            this.allSegments = segments ?? new List<WordSegment>();
            spawned.Clear();
            activeNotes.Clear();
            currentNoteIndex = 0;
            ClearInternal(); // Clear all old notes
            
            int totalNotes = 0;
            foreach (var seg in allSegments)
            {
                totalNotes += seg.Notes?.Count ?? 0;
            }
            Logger.Log($"[TypeNoteScheduler] Loaded {allSegments.Count} segments with {totalNotes} total notes", LoggingTarget.Runtime, LogLevel.Important);
        }

        /// <summary>
        /// Legacy method - still used for segment tracking. Now only updates current segment index.
        /// </summary>
        public void LoadSegment(WordSegment segment)
        {
            // This method is kept for compatibility but doesn't affect spawning anymore
            // Notes spawn based on time from all segments loaded via LoadAllSegments
            Logger.Log($"[TypeNoteScheduler] Current segment changed (spawning continues across all segments)", LoggingTarget.Runtime, LogLevel.Important);
        }

        /// <summary>
        /// Notify the current note to disappear (called when player hits it) with a known judgement.
        /// </summary>
        public void HitCurrentNote(TypeBeat.Game.Gameplay.Judgement.JudgementType judgement)
        {
            if (currentNoteIndex < activeNotes.Count)
            {
                activeNotes[currentNoteIndex].OnHit(judgement);
                currentNoteIndex++;
            }
        }

        /// <summary>
        /// Backward-compatible hit without explicit judgement.
        /// </summary>
        public void HitCurrentNote()
        {
            HitCurrentNote(TypeBeat.Game.Gameplay.Judgement.JudgementType.Good100);
        }

        protected override void Update()
        {
            base.Update();

            if (allSegments == null || allSegments.Count == 0)
                return;

            double now = Clock.CurrentTime - TimeOffsetMs;
            // Allow negative time during grace period - don't clamp!

            // Check ALL segments and spawn notes based on time, not segment state
            foreach (var segment in allSegments)
            {
                if (segment?.Notes == null) continue;
                
                foreach (var n in segment.Notes)
                {
                    if (spawned.Contains(n)) continue;

                    // Skip the excluded note (first note cue shown separately)
                    if (ExcludedVisualNote != null && n == ExcludedVisualNote)
                    {
                        spawned.Add(n); // Mark as spawned but don't create drawable
                        continue;
                    }

                    double spawnAt = n.StartTime - PreloadMs;
                    if (now >= spawnAt)
                    {
                        var drawableNote = new DrawableMusicNote(n, layout, TimeOffsetMs)
                        {
                            TimeOffsetMs = this.TimeOffsetMs // Pass the offset
                        };

                        AddInternal(drawableNote);
                        activeNotes.Add(drawableNote); // Track for hit notification
                        spawned.Add(n);
                    }
                }
            }
        }
    }
}