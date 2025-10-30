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
    
    public class HitWindows
    {
        // Default thresholds (two-sided): |offset| <= threshold => corresponding judgement
        // Very lenient timing windows for better player experience
        // 300: 120ms, 200: 160ms, 100: 200ms, 50: 240ms
        public int Window300 { get; }
        public int Window200 { get; }
        public int Window100 { get; }
        public int Window50  { get; }

        public HitWindows(int window300 = 120, int window200 = 160, int window100 = 200, int window50 = 240)
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
