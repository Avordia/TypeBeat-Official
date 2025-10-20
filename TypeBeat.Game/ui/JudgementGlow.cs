using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;

namespace TypeBeat.Game.Ui
{
    /// <summary>
    /// Displays a visual glow effect for judgement feedback.
    /// </summary>
    public partial class JudgementGlow : Container
    {
        private readonly Circle glowCircle;

        public enum JudgementType
        {
            Perfect,
            Great,
            Good,
            Miss
        }

        public JudgementGlow()
        {
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            Size = new osuTK.Vector2(400, 400);
            Alpha = 0;

            Child = glowCircle = new Circle
            {
                RelativeSizeAxes = Axes.Both,
                Alpha = 0.6f
            };
        }

        /// <summary>
        /// Shows a glow effect based on the judgement type.
        /// </summary>
        /// <param name="judgementType">The type of judgement to display.</param>
        public void ShowGlow(JudgementType judgementType)
        {
            // Set color based on judgement
            glowCircle.Colour = judgementType switch
            {
                JudgementType.Perfect => Colour4.Cyan,
                JudgementType.Great => Colour4.Lime,
                JudgementType.Good => Colour4.Yellow,
                JudgementType.Miss => Colour4.Red,
                _ => Colour4.White
            };

            // Animate the glow
            this.FadeIn(50)
                .Then()
                .ScaleTo(1.2f, 200, Easing.OutQuint)
                .FadeOut(200, Easing.OutQuint)
                .OnComplete(_ =>
                {
                    this.ScaleTo(1.0f);
                });
        }
    }
}
