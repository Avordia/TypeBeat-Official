using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;

namespace TypeBeat.Game.Ui
{
    /// <summary>
    /// Displays the current combo counter.
    /// </summary>
    public partial class ComboDisplay : Container
    {
        private readonly SpriteText comboText;

        public ComboDisplay()
        {
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            AutoSizeAxes = Axes.Both;
            Y = 60;

            Child = comboText = new SpriteText
            {
                Text = "0x",
                Font = new FontUsage("Inter", size: 24),
                Colour = Colour4.White
            };
        }

        /// <summary>
        /// Updates the displayed combo count.
        /// </summary>
        /// <param name="combo">The current combo count.</param>
        public void UpdateCombo(int combo)
        {
            comboText.Text = $"{combo}x";
        }
    }
}
