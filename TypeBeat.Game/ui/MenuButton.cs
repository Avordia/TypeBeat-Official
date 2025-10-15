using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Screens;
using osuTK;
using osuTK.Graphics;

namespace TypeBeat.Game.Ui
{
    public partial class MenuButton : ClickableContainer
    {
        private readonly Box background;
        private readonly SpriteText text;
        private readonly Screen targetScreen;
        private readonly Action customAction;
        
        public Color4 ButtonColor
        {
            set => background.FadeColour(value, 200);
        }

        public string Text
        {
            get => text.Text.ToString();
            set => text.Text = value;
        }

        public float TextSize
        {
            set => text.Font = text.Font.With(size: value);
        }

        public MenuButton(string buttonText, Color4 color, float size = 30f, Screen target = null, Action onClick = null, Vector2? dimensions = null)
        {
            targetScreen = target;
            customAction = onClick;
            
            if (dimensions.HasValue)
            {
                Width = dimensions.Value.X;
                Height = dimensions.Value.Y;
                RelativeSizeAxes = Axes.None;
            }
            else
            {
                AutoSizeAxes = Axes.Both;
            }
            
            Masking = true;
            CornerRadius = 5f;

            Children = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = color
                },
                text = new SpriteText
                {
                    Text = buttonText,
                    Font = new FontUsage(size: size),
                    Padding = new MarginPadding { Horizontal = 20, Vertical = -20},
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
    
                }
            };
        }

        protected override bool OnHover(HoverEvent e)
        {
            this.ScaleTo(1.1f, 200, Easing.OutQuint);
            background.FadeTo(0.8f, 200);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            this.ScaleTo(1f, 200, Easing.OutQuint);
            background.FadeTo(1f, 200);
            base.OnHoverLost(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            this.ScaleTo(0.9f, 100, Easing.OutQuint)
                .Then()
                .ScaleTo(1f, 100, Easing.OutQuint);

            if (targetScreen != null)
            {
                var current = Parent;
                while (current != null && !(current is Screen))
                    current = current.Parent;

                if (current is Screen screen)
                    screen.Push(targetScreen);
            }

            customAction?.Invoke();
            return base.OnClick(e);//
        }
    }
}