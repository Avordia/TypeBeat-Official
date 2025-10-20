using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace TypeBeat.Game.Ui
{
    /// <summary>
    /// Displays the player's health with a logo and parallelogram segments.
    /// </summary>
    public partial class HealthBar : Container
    {
        private const int segment_count = 15;
        private readonly Box[] healthSegments;

        public HealthBar(Texture logoTexture)
        {
            Position = new Vector2(90, 120);
            AutoSizeAxes = Axes.Both;

            healthSegments = new Box[segment_count];

            var healthBarContainer = new FillFlowContainer
            {
                Direction = FillDirection.Horizontal,
                AutoSizeAxes = Axes.Both,
                Spacing = new Vector2(5, 0),
                Children = new Drawable[]
                {
                    new Sprite
                    {
                        Texture = logoTexture,
                        Size = new Vector2(45),
                        FillMode = FillMode.Fit,
                        EdgeSmoothness = new Vector2(2.0f)
                    }
                }
            };

            // Create health bar segments
            for (int i = 0; i < segment_count; i++)
            {
                var segment = new Container
                {
                    Size = new Vector2(220 / segment_count, 15),
                    Masking = true,
                    Child = healthSegments[i] = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = getSegmentColour(i),
                        Alpha = getSegmentAlpha(i),
                        Shear = new Vector2(0.3f, 0),
                        EdgeSmoothness = new Vector2(2.0f)
                    }
                };

                healthBarContainer.Add(segment);
            }

            Child = healthBarContainer;
        }

        /// <summary>
        /// Updates the health bar visualization based on current health percentage.
        /// </summary>
        /// <param name="healthPercent">The health percentage (0-1).</param>
        public void UpdateHealth(double healthPercent)
        {
            int visibleSegments = (int)(healthPercent * segment_count);

            for (int i = 0; i < segment_count; i++)
            {
                healthSegments[i].Alpha = i < visibleSegments ? getSegmentAlpha(i) : 0f;
            }
        }

        private Colour4 getSegmentColour(int index)
        {
            // Gradient colors from red to purple/blue
            var gradientColors = new[]
            {
                Colour4.FromHex("#FF3333"), // Red
                Colour4.FromHex("#FF4D33"),
                Colour4.FromHex("#FF6633"),
                Colour4.FromHex("#FF8033"), // Orange
                Colour4.FromHex("#FF9933"),
                Colour4.FromHex("#FFB333"),
                Colour4.FromHex("#CC6699"), // Pink
                Colour4.FromHex("#B366AA"),
                Colour4.FromHex("#9966BB"), // Magenta/Purple
                Colour4.FromHex("#8066CC"),
                Colour4.FromHex("#6666DD"), // Purple
                Colour4.FromHex("#5555BB"),
                Colour4.FromHex("#444499"),
                Colour4.FromHex("#333377"), // Dark purple
                Colour4.FromHex("#222255")  // Dark blue
            };

            return gradientColors[index];
        }

        private float getSegmentAlpha(int index)
        {
            // 100% (left) to 10% (right)
            return 1.0f - (index / (float)(segment_count - 1)) * 0.9f;
        }
    }
}
