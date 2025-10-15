using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace TypeBeat.Game.Ui
{
    public partial class Footer : Container
    {
        private readonly Box background;
        private readonly FillFlowContainer content;

        public Footer()
        {
            RelativeSizeAxes = Axes.X;
            Height = 35;
            Anchor = Anchor.BottomCentre;
            Origin = Anchor.BottomCentre;
            Alpha = 0;

            InternalChildren = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colour4.Black,
                },
                content = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding { Horizontal = 20 },
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(20, 0),
                }
            };
        }

        public new void Show()
        {
            this.FadeIn(300, Easing.OutQuint);
        }

        public new void Hide()
        {
            this.FadeOut(300, Easing.OutQuint);
        }

        public new void Add(Drawable drawable)
        {
            content.Add(drawable);
        }
    }
}
