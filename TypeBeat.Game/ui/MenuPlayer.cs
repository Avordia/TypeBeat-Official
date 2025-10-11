using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace TypeBeat.Game.ui
{
    public partial class MenuPlayer : CompositeDrawable
    {
        public Action OnPrevious;
        public Action OnPause;
        public Action OnNext;

        private readonly Button prevButton;
        private readonly Button pauseButton;
        private readonly Button nextButton;

        public MenuPlayer()
        {
            Anchor = Anchor.BottomCentre;
            Origin = Anchor.BottomCentre;
            Margin = new MarginPadding { Bottom = 50 };
            AutoSizeAxes = Axes.Y;
            Width = 300;

            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(10),
                Children = new Drawable[]
                {
                    prevButton = new Button("Prev") { Action = () => OnPrevious?.Invoke() },
                    pauseButton = new Button("Pause") { Action = () => OnPause?.Invoke() },
                    nextButton = new Button("Next") { Action = () => OnNext?.Invoke() }
                }
            };
        }

        private partial class Button : ClickableContainer
        {
            private readonly Box background;
            private readonly SpriteText text;

            public Button(string buttonText)
            {
                RelativeSizeAxes = Axes.X;
                Width = 0.33f;
                Height = 40;
                Colour = Color4.Gray;

                InternalChildren = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                        Alpha = 0.5f
                    },
                    text = new SpriteText
                    {
                        Text = buttonText,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    }
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                background.FadeTo(0.7f, 100);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                background.FadeTo(0.5f, 100);
                base.OnHoverLost(e);
            }
        }
    }
}