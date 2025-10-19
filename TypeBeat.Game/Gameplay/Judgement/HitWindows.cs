using System;

namespace TypeBeat.Game.Gameplay.Judgement
{
    public enum JudgementType
    {
        Perfect300,
        Great200,
        Good100,
        Meh50,
        Miss
    }

    /// <summary>
    /// osu!-style hit windows. All values are milliseconds (absolute offset from the note's EndTime).
    /// Defaults can be tuned per difficulty later.
    /// </summary>
    public class HitWindows
    {
        // Default thresholds (two-sided): |offset| <= threshold => corresponding judgement
        // 300: 32ms, 200: 64ms, 100: 96ms, 50: 140ms
        public int Window300 { get; }
        public int Window200 { get; }
        public int Window100 { get; }
        public int Window50  { get; }

        public HitWindows(int window300 = 32, int window200 = 64, int window100 = 96, int window50 = 140)
        {
            if (window300 <= 0 || window200 <= 0 || window100 <= 0 || window50 <= 0)
                throw new ArgumentOutOfRangeException("Hit windows must be positive.");

            Window300 = window300;
            Window200 = window200;
            Window100 = window100;
            Window50  = window50;
        }

        public JudgementType Judge(double offsetMs)
        {
            var a = Math.Abs(offsetMs);
            if (a <= Window300) return JudgementType.Perfect300;
            if (a <= Window200) return JudgementType.Great200;
            if (a <= Window100) return JudgementType.Good100;
            if (a <= Window50)  return JudgementType.Meh50;
            return JudgementType.Miss;
        }

        public static int GetAccuracyWeight(JudgementType type)
        {
            return type switch
            {
                JudgementType.Perfect300 => 300,
                JudgementType.Great200   => 200,
                JudgementType.Good100    => 100,
                JudgementType.Meh50      => 50,
                _                        => 0,
            };
        }
    }
}
