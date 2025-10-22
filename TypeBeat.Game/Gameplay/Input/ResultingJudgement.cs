using TypeBeat.Game.Gameplay.Judgement;

namespace TypeBeat.Game.Gameplay
{    public readonly struct ResultingJudgement
    {
        public readonly JudgementType Judgement;
        public readonly bool Consumed;
        public readonly bool SegmentCompleted;
        public readonly double TimeOffsetMs;

        public ResultingJudgement(JudgementType judgement, bool consumed, bool segmentCompleted, double timeOffsetMs)
        {
            Judgement = judgement;
            Consumed = consumed;
            SegmentCompleted = segmentCompleted;
            TimeOffsetMs = timeOffsetMs;
        }
    }
}