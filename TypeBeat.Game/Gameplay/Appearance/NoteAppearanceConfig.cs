using osu.Framework.Graphics;

namespace TypeBeat.Game.Gameplay.Appearance
{
    /// <summary>
    /// Centralizes colours for notes. We tint a single neutral note texture for both letters and spaces.
    /// </summary>
    public class NoteAppearanceConfig
    {
        /// <summary>
        /// Colour for letter (kick) notes. Default: white.
        /// </summary>
        public Colour4 LetterColour { get; set; } = Colour4.White;

        /// <summary>
        /// Colour for space (snare) notes. Default: gold.
        /// </summary>
        public Colour4 SpaceColour { get; set; } = Colour4.Gold;
    }
}
