using TypeBeat.Game.Gameplay.Judgement;

namespace TypeBeat.Game.Gameplay.Scoring
{
    public class ScoreProcessor
    {
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int TotalJudgements { get; private set; }
        public int AccumulatedAccuracyWeight { get; private set; }
        
        // Detailed judgement counts
        public int Perfect300 { get; private set; }
        public int Great200 { get; private set; }
        public int Good100 { get; private set; }
        public int Meh50 { get; private set; }
        public int Miss { get; private set; }
        
        // Total score
        public long TotalScore { get; private set; }

        public void Apply(JudgementType j)
        {
            TotalJudgements++;
            AccumulatedAccuracyWeight += HitWindows.GetAccuracyWeight(j);
            
            // Track individual judgement counts
            switch (j)
            {
                case JudgementType.Perfect300:
                    Perfect300++;
                    break;
                case JudgementType.Great200:
                    Great200++;
                    break;
                case JudgementType.Good100:
                    Good100++;
                    break;
                case JudgementType.Meh50:
                    Meh50++;
                    break;
                case JudgementType.Miss:
                    Miss++;
                    break;
            }

            if (j == JudgementType.Miss)
            {
                if (Combo > MaxCombo) MaxCombo = Combo;
                Combo = 0;
            }
            else
            {
                Combo++;
            }
            
            // Calculate score with combo multiplier
            int baseScore = HitWindows.GetAccuracyWeight(j);
            int comboMultiplier = System.Math.Min(Combo / 25, 4);
            TotalScore += baseScore * (1 + comboMultiplier);
        }

        public float GetAccuracyPercent()
        {
            // 300 is the max weight per note.
            if (TotalJudgements == 0) return 100f;
            double denom = 300.0 * TotalJudgements;
            double acc = (AccumulatedAccuracyWeight / denom) * 100.0;
            return (float)acc;
        }
    }
}
