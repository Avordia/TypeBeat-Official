using System;
using osu.Framework.Logging;
using TypeBeat.Game.Gameplay.Judgement;

namespace TypeBeat.Game.Gameplay.Input
{
    public class NoteManager
    {
        private Beatmaps.Note[] allNotes = System.Array.Empty<Beatmaps.Note>();
        private int currentNoteIndex = 0;

        /// <summary>
        /// Optional early-hit guard: do not allow a note to be judged/consumed until
        /// this many milliseconds before its EndTime. Useful for TypeNote to avoid mispresses.
        /// 0 disables the guard (default behavior).
        /// </summary>
        public double EarlyHitGuardMs { get; set; } = 0;

        public void SetSegment(Beatmaps.WordSegment segment)
        {
            if (segment?.Notes == null)
            {
                allNotes = System.Array.Empty<Beatmaps.Note>();
                Logger.Log("[NoteQueueManager] Segment is null or has no notes.", LoggingTarget.Runtime, LogLevel.Error);
            }
            else
            {
                allNotes = segment.Notes.ToArray();
                Logger.Log($"[NoteQueueManager] Loaded new segment with {allNotes.Length} notes.", LoggingTarget.Runtime, LogLevel.Important);
            }
            
            currentNoteIndex = 0;
        }

        // --- UPDATED METHOD ---
        public ResultingJudgement HandleNotePress(string inputNote, double currentTime, HitWindows hitWindows)
        {
            if (currentNoteIndex >= allNotes.Length)
            {
                // No notes left to hit
                return new ResultingJudgement(JudgementType.Miss, false, false, 0);
            }

            var currentNote = allNotes[currentNoteIndex];

            // 1. Define the "active window" (like in TypingManager)
            // A key press is only considered if it's within the active window.
            // For TypeNote, we additionally enforce an early-hit guard so judgement can only occur
            // within [EndTime - EarlyHitGuardMs, EndTime + Window50].
            // This avoids consuming notes due to very early key presses.
            double earlyBoundary = Math.Max(currentNote.StartTime, currentNote.EndTime - EarlyHitGuardMs);
            double lateBoundary = currentNote.EndTime + hitWindows.Window50;

            if (currentTime < earlyBoundary || currentTime > lateBoundary)
            {
                // Key press is completely outside the note's lifetime.
                // Not consumed, no judgement.
                return new ResultingJudgement(JudgementType.Miss, false, false, 0);
            }

            // 2. The key press is "active". Now, judge it.
            // The judgement offset MUST be relative to the ARRIVAL time (EndTime).
            double timeOffset = currentTime - currentNote.EndTime;

            if (string.Equals(inputNote, currentNote.Character, StringComparison.OrdinalIgnoreCase))
            {
                // CORRECT note
                var judgement = hitWindows.Judge(timeOffset); // Judge based on EndTime offset

                currentNoteIndex++;
                bool segmentCompleted = currentNoteIndex >= allNotes.Length;

                // Return Consumed = true
                return new ResultingJudgement(judgement, true, segmentCompleted, timeOffset);
            }
            else
            {
                // WRONG note
                currentNoteIndex++;
                bool segmentCompleted = currentNoteIndex >= allNotes.Length;
                
                Logger.Log($"[NoteQueueManager] Wrong note! Expected: {currentNote.Character}, Got: {inputNote}", LoggingTarget.Runtime, LogLevel.Debug);
                
                // It's a "Miss", but it was still "Consumed" (it used up the note).
                // Return Consumed = true
                return new ResultingJudgement(JudgementType.Miss, true, segmentCompleted, 0);
            }
        }

        public int AutoConsumeMisses(double currentTime, HitWindows hitWindows, out bool segmentCompleted)
        {
            segmentCompleted = false;
            int missedCount = 0;

            if (currentNoteIndex >= allNotes.Length)
            {
                segmentCompleted = true; 
                return 0;
            }

            var currentNote = allNotes[currentNoteIndex];
            
            // --- FIX ---
            // A note is missed if we are past its EndTime + the miss window
            double missTime = currentNote.EndTime + hitWindows.Window50; 

            while (currentTime > missTime)
            {
                missedCount++;
                currentNoteIndex++;

                if (currentNoteIndex >= allNotes.Length)
                {
                    segmentCompleted = true;
                    return missedCount;
                }
                
                currentNote = allNotes[currentNoteIndex];
                missTime = currentNote.EndTime + hitWindows.Window50; // Use EndTime here too
            }

            return missedCount;
        }
    }
}