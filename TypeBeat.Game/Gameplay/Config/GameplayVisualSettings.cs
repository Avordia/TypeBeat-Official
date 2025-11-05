namespace TypeBeat.Game.Gameplay.Config
{
    /// <summary>
    /// Centralised toggles and knobs for gameplay visuals so experiments are easy to revert.
    /// </summary>
    public static class GameplayVisualSettings
    {
        // Judgement colours
        public static bool useOsuJudgementColors = true; // set false to revert to legacy palette

        // Late visual linger (prevents notes from expiring instantly at EndTime)
        public static bool enableLateVisualLinger = true;
        public static double lateVisualLingerMs = 120; // ms

        // Score popup positioning and travel
        public static float scorePopupBaseOffset = 40f; // pixels above the music sheet top
        public static float scorePopupRiseY = -100f;    // travel target relative to container
    }
}
