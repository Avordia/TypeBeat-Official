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
    /// Spawns and manages DrawableMusicNote objects for the active segment.
    /// </summary>
    public partial class TypeNoteScheduler : CompositeDrawable
    {
        private readonly TypeNoteLayoutConfig layout;
        private WordSegment segment;
        private readonly HashSet<Note> spawned = new HashSet<Note>();
        private readonly List<DrawableMusicNote> activeNotes = new List<DrawableMusicNote>();
        private int currentNoteIndex = 0; // Tracks which note in the segment we're on

        /// <summary>
        /// Offset to subtract from Clock.CurrentTime for gameplay timing.
        /// </summary>
        public double TimeOffsetMs { get; set; } = 0;

        public TypeNoteScheduler(TypeNoteLayoutConfig layout)
        {
            this.layout = layout;
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.TopLeft;
            Origin = Anchor.TopLeft;
        }

        public void LoadSegment(WordSegment segment)
        {
            this.segment = segment;
            spawned.Clear();
            activeNotes.Clear();
            currentNoteIndex = 0;
            ClearInternal(); // Clear all old notes
            
            if (segment != null)
                Logger.Log($"[TypeNoteScheduler] Loaded segment with {segment.Notes?.Count ?? 0} notes", LoggingTarget.Runtime, LogLevel.Important);
        }

        /// <summary>
        /// Notify the current note to disappear (called when player hits it).
        /// </summary>
        public void HitCurrentNote()
        {
            if (currentNoteIndex < activeNotes.Count)
            {
                activeNotes[currentNoteIndex].OnHit();
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

            // This logic is simple, but if segments are long, it can be slow.
            // It's okay for now.
            foreach (var n in segment.Notes)
            {
                if (spawned.Contains(n)) continue;

                if (now >= n.StartTime)
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