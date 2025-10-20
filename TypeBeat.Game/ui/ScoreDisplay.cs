using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace TypeBeat.Game.Ui
{
    /// <summary>
    /// Displays the current score at the top center of the screen.
    /// </summary>
    public partial class ScoreDisplay : Container
    {
        private readonly SpriteText scoreText;

        public ScoreDisplay()
        {
            Anchor = Anchor.TopCentre;
            Origin = Anchor.TopCentre;
            AutoSizeAxes = Axes.Both;
            Padding = new MarginPadding { Top = 30 };

            Child = scoreText = new SpriteText
            {
                Text = "000000000000", // 12 digits
                Font = new FontUsage("Kodchasan", size: 56, weight: "Bold"),
                Colour = Colour4.White,
                Spacing = new Vector2(0.25f, 0) // 25% spacing
            };
        }

        /// <summary>
        /// Updates the displayed score.
        /// </summary>
        /// <param name="score">The score value to display.</param>
        public void UpdateScore(int score)
        {
            scoreText.Text = score.ToString("D12");
        }
    }
}
