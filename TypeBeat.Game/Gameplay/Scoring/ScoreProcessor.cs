using TypeBeat.Game.Gameplay.Judgement;

namespace TypeBeat.Game.Gameplay.Scoring
{
    public class ScoreProcessor
    {
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int TotalJudgements { get; private set; }
        public int AccumulatedAccuracyWeight { get; private set; }

        public void Apply(JudgementType j)
        {
            TotalJudgements++;
            AccumulatedAccuracyWeight += HitWindows.GetAccuracyWeight(j);

            if (j == JudgementType.Miss)
            {
                if (Combo > MaxCombo) MaxCombo = Combo;
                Combo = 0;
            }
            else
            {
                Combo++;
            }
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
