using System;
using System.Linq;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.Gameplay.Judgement;
using TypeBeat.Game.Gameplay.Typing;

namespace TypeBeat.Game.Gameplay.Input
{
    public struct TypingResult
    {
        public bool Consumed;              // Whether a queue character was consumed
        public bool SegmentCompleted;      // True if this press completed the segment (after '/')
        public JudgementType Judgement;    // Miss/300/etc.
        public bool WasSpace;              // Whether the consumed char was '/'
    }

    /// <summary>
    /// Manages typing for the active WordSegment. Strictly next-letter-only.
    /// Any key press consumes the next character: correct => judged; wrong => Miss.
    /// </summary>
    public class TypingManager
    {
        private WordSegment? segment;
        private int index; // index into segment.Notes

        public void SetSegment(WordSegment? seg)
        {
            segment = seg;
            index = 0;
        }

        public bool IsSegmentCompleted => segment?.Notes == null || index >= segment.Notes.Count;

        public TypingResult HandleKeyPress(char keyUpper, double pressTimeMs, HitWindows windows)
        {
            var res = new TypingResult { Consumed = false, SegmentCompleted = false, Judgement = JudgementType.Miss, WasSpace = false };
            if (segment?.Notes == null || index >= segment.Notes.Count)
                return res; // nothing to consume

            var note = segment.Notes[index];
            char expected = normalize(note.Character);
            res.WasSpace = expected == TypingConstants.SpaceToken;

            // Quality of life: Only accept inputs during the note's active window (StartTime to EndTime + late window)
            // This prevents accidental keypresses before the note appears or after it's too late
            double earlyBoundary = note.StartTime; // Don't accept before note spawns
            double lateBoundary = note.EndTime + windows.Window50; // Accept until late window ends
            
            if (pressTimeMs < earlyBoundary || pressTimeMs > lateBoundary)
            {
                // Key press is outside the active window - ignore it completely
                return res; // Not consumed, no judgement
            }

            // Consume the note (input is within active window)
            res.Consumed = true;

            if (keyUpper == expected)
            {
                // correct key -> judge by offset
                double offset = pressTimeMs - note.EndTime;
                res.Judgement = windows.Judge(offset);
            }
            else
            {
                // wrong key -> miss
                res.Judgement = JudgementType.Miss;
            }

            index++;
            res.SegmentCompleted = index >= segment.Notes.Count;
            return res;
        }

        /// <summary>
        /// Automatically consume overdue notes as Misses if their late window has already passed.
        /// Returns the number of auto-missed notes. Sets segmentCompleted if we reached the end.
        /// </summary>
        public int AutoConsumeMisses(double nowMs, HitWindows windows, out bool segmentCompleted)
        {
            int consumedCount = 0;
            segmentCompleted = false;
            if (segment?.Notes == null) return 0;

            while (index < segment.Notes.Count)
            {
                var note = segment.Notes[index];
                double lateBoundary = note.EndTime + windows.Window50; // after 50 window ends, count as Miss
                if (nowMs >= lateBoundary)
                {
                    // Auto-miss and advance
                    index++;
                    consumedCount++;
                }
                else break;
            }

            if (index >= segment.Notes.Count)
                segmentCompleted = true;

            return consumedCount;
        }

        private static char normalize(string? ch)
        {
            if (string.IsNullOrEmpty(ch)) return '\0';
            char c = ch[0];
            if (c == TypingConstants.SpaceToken) return TypingConstants.SpaceToken; // '/'
            return char.ToUpperInvariant(c);
        }
    }
}
