using osu.Framework.Graphics;
using TypeBeat.Game.Gameplay.Config;

namespace TypeBeat.Game.Gameplay.Judgement
{
    /// <summary>
    /// Centralised colours for judgement feedback so UI elements stay consistent.
    /// </summary>
    public static class JudgementColors
    {
        /// <summary>
        /// Returns the colour to use for a given judgement.
        /// Note: "skyblue" variants are normalised to Blue for clarity.
        /// </summary>
        public static Colour4 Get(JudgementType judgement)
        {
            if (GameplayVisualSettings.useOsuJudgementColors)
            {
                // osu!-style palette
                return judgement switch
                {
                    // 300 (Perfect): Sky Blue
                    JudgementType.Perfect300 => Colour4.FromHex("#33CCFF"),
                    // 200 (Great): Cyan
                    JudgementType.Great200   => Colour4.FromHex("#00FFFF"),
                    // 100 (Good): Green
                    JudgementType.Good100    => Colour4.FromHex("#00FF00"),
                    // 50 (Meh): Yellow-Orange
                    JudgementType.Meh50      => Colour4.FromHex("#FFCC00"),
                    // Miss: Red
                    _                        => Colour4.FromHex("#FF0000"),
                };
            }

            // Legacy palette (previous behavior)
            return judgement switch
            {
                JudgementType.Perfect300 => Colour4.FromHex("#FFD700"), // Gold
                JudgementType.Great200   => Colour4.FromHex("#00FF00"), // Green
                JudgementType.Good100    => Colour4.Blue,                // SkyBlue -> Blue
                JudgementType.Meh50      => Colour4.FromHex("#FF8800"), // Orange
                _                        => Colour4.FromHex("#FF0000"), // Red
            };
        }
    }
}
