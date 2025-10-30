using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.Gameplay.Appearance;
using TypeBeat.Game.Gameplay.Layout;
using TypeBeat.Game.Gameplay.Objects;
using TypeBeat.Game.Gameplay.Typing;
using osu.Framework.Logging;

namespace TypeBeat.Game.Gameplay.Scheduling
{
    /// <summary>
    /// Spawns mirrored visual note pairs for all segments based on timing.
    /// Uses StartTime as spawn time and EndTime as arrival time.
    /// Notes spawn strictly based on their StartTime regardless of current segment.
    /// </summary>
    public partial class NoteScheduler : CompositeDrawable
    {
        private readonly LayoutConfig layout;
        private readonly NoteAppearanceConfig appearance;

        private List<WordSegment> allSegments = new List<WordSegment>();
        private readonly HashSet<Note> spawned = new HashSet<Note>();
        private readonly List<DrawableNotePair> activePairs = new List<DrawableNotePair>();
        private int currentNoteIndex = 0; // Track which note in the segment we're on

        /// <summary>
        /// Spawn notes slightly before their StartTime to ensure smooth entry.
        /// </summary>
    public double PreloadMs { get; set; } = 0; // default: spawn at StartTime exactly per spec

    /// <summary>
    /// Offset to subtract from Clock.CurrentTime so gameplay uses a relative zero point.
    /// Typically set when the screen enters.
    /// </summary>
    public double TimeOffsetMs { get; set; } = 0;

    /// <summary>
    /// Pause/unpause the scheduler (stops spawning and updating notes)
    /// </summary>
    public bool IsPaused { get; set; } = false;

    public int SpawnedCount => spawned.Count;

        /// <summary>
        /// If set, this specific note will not be visually spawned by the scheduler.
        /// Useful for replacing the first visual cue with a custom standalone drawable.
        /// </summary>
        public Note ExcludedVisualNote { get; set; }

        public NoteScheduler(LayoutConfig layout, NoteAppearanceConfig appearance)
        {
            this.layout = layout;
            this.appearance = appearance;
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
            activePairs.Clear();
            currentNoteIndex = 0;
            ClearInternal();
            
            int totalNotes = 0;
            foreach (var seg in allSegments)
            {
                totalNotes += seg.Notes?.Count ?? 0;
            }
            Logger.Log($"[NoteScheduler] Loaded {allSegments.Count} segments with {totalNotes} total notes", LoggingTarget.Runtime, LogLevel.Important);
        }

        /// <summary>
        /// Legacy method - still used for segment tracking. Now only updates current segment index.
        /// </summary>
        public void LoadSegment(WordSegment segment)
        {
            // This method is kept for compatibility but doesn't affect spawning anymore
            // Notes spawn based on time from all segments loaded via LoadAllSegments
            Logger.Log($"[NoteScheduler] Current segment changed (spawning continues across all segments)", LoggingTarget.Runtime, LogLevel.Important);
        }

        /// <summary>
        /// Notify the current note pair to disappear immediately (called when player presses correct key).
        /// </summary>
        public void HitCurrentNote()
        {
            if (currentNoteIndex < activePairs.Count)
            {
                activePairs[currentNoteIndex].OnHit();
                currentNoteIndex++;
            }
        }

        protected override void Update()
        {
            base.Update();

            // Stop updating when paused
            if (IsPaused)
                return;

            if (allSegments == null || allSegments.Count == 0)
                return;

            double now = Clock.CurrentTime - TimeOffsetMs;
            
            // Check ALL segments and spawn notes based on time, not segment state
            foreach (var segment in allSegments)
            {
                if (segment?.Notes == null) continue;
                
                foreach (var n in segment.Notes)
                {
                    if (spawned.Contains(n)) continue;
                    if (ExcludedVisualNote != null && ReferenceEquals(n, ExcludedVisualNote)) continue;
                    
                    double spawnAt = n.StartTime - PreloadMs;
                    
                    if (now >= spawnAt)
                    {
                        bool isSpace = !string.IsNullOrEmpty(n.Character) && n.Character[0] == TypingConstants.SpaceToken;
                        
                        var pair = new DrawableNotePair(n.StartTime, n.EndTime, isSpace, layout, appearance)
                        {
                            TimeOffsetMs = TimeOffsetMs
                        };
                        AddInternal(pair);
                        activePairs.Add(pair); // Track for hit notification
                        spawned.Add(n);
                        
                        Logger.Log($"[NoteScheduler] Spawn note: now={now:F1} start={n.StartTime:F1} end={n.EndTime:F1} isSpace={isSpace}", LoggingTarget.Runtime, LogLevel.Important);
                    }
                }
            }
        }
    }
}
