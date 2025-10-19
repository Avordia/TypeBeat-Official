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
    /// Spawns mirrored visual note pairs for the active word segment.
    /// Uses StartTime as spawn time and EndTime as arrival time.
    /// </summary>
    public partial class NoteScheduler : CompositeDrawable
    {
        private readonly LayoutConfig layout;
        private readonly NoteAppearanceConfig appearance;

    private WordSegment segment;
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

    public int SpawnedCount => spawned.Count;

        public NoteScheduler(LayoutConfig layout, NoteAppearanceConfig appearance)
        {
            this.layout = layout;
            this.appearance = appearance;
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.TopLeft;
            Origin = Anchor.TopLeft;
        }

        public void LoadSegment(WordSegment segment)
        {
            this.segment = segment;
            spawned.Clear();
            activePairs.Clear();
            currentNoteIndex = 0;
            ClearInternal();
            if (segment != null)
                Logger.Log($"[NoteScheduler] Loaded segment with {segment.Notes?.Count ?? 0} notes", LoggingTarget.Runtime, LogLevel.Important);
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

            if (segment == null || segment.Notes == null || segment.Notes.Count == 0)
                return;

            double now = Clock.CurrentTime - TimeOffsetMs;
            if (now < 0) now = 0;
            foreach (var n in segment.Notes)
            {
                if (spawned.Contains(n)) continue;
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
