using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace TypeBeat.Game.Ui
{
    /// <summary>
    /// Displays the accuracy percentage with a label at the top right.
    /// </summary>
    public partial class AccuracyDisplay : Container
    {
        private readonly SpriteText accuracyText;

        public AccuracyDisplay()
        {
            Anchor = Anchor.TopRight;
            Origin = Anchor.TopRight;
            AutoSizeAxes = Axes.Both;
            Padding = new MarginPadding(30);

            Child = new FillFlowContainer
            {
                Direction = FillDirection.Vertical,
                AutoSizeAxes = Axes.Both,
                Spacing = new Vector2(0, 10),
                Children = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        Direction = FillDirection.Horizontal,
                        AutoSizeAxes = Axes.Both,
                        Spacing = new Vector2(10, 0),
                        Children = new Drawable[]
                        {
                            new SpriteText
                            {
                                Text = "ACCURACY:",
                                Font = new FontUsage("Kodchasan", size: 34, weight: "Bold"),
                                Colour = Colour4.White
                            },
                            accuracyText = new SpriteText
                            {
                                Text = "100.0%",
                                Font = new FontUsage("Kodchasan", size: 34, weight: "Bold"),
                                Colour = Colour4.Lime
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Updates the displayed accuracy with color coding.
        /// </summary>
        /// <param name="accuracy">The accuracy percentage (0-100).</param>
        public void UpdateAccuracy(float accuracy)
        {
            accuracyText.Text = $"{accuracy:F1}%";

            // Color code based on accuracy
            if (accuracy >= 95)
                accuracyText.Colour = Colour4.Lime;
            else if (accuracy >= 90)
                accuracyText.Colour = Colour4.Yellow;
            else if (accuracy >= 80)
                accuracyText.Colour = Colour4.Orange;
            else
                accuracyText.Colour = Colour4.Red;
        }
    }
}
