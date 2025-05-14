using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes; // For Circle
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events; // For Hover and Click events
using osuTK;
using osuTK.Graphics;
using System;

namespace TypeBeat.Game.UI
{
    public partial class MenuButton : CircularContainer
    {
        public Action Action { get; set; }
        private readonly string text;

        private SpriteText buttonText;
        private Circle buttonBackground;

        private Color4 normalColour = Color4.DodgerBlue;
        private Color4 hoverColour = Color4.LightSkyBlue;
        private Color4 clickColour = Color4.RoyalBlue;

        public MenuButton(string text)
        {
            this.text = text;
            Size = new Vector2(150); // Diameter of the button
            Masking = true; // Clip children to the circular shape
            BorderColour = Color4.White;
            BorderThickness = 3;

            Children = new Drawable[]
            {
                buttonBackground = new Circle
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = normalColour,
                },
                buttonText = new SpriteText
                {
                    Text = text,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Font = FontUsage.Default.With(size: 28),
                    Colour = Color4.White
                }
            };
        }
        
        [BackgroundDependencyLoader]
        private void load()
        {
            // You can load custom fonts or other dependencies here if needed
        }

        protected override bool OnHover(HoverEvent e)
        {
            buttonBackground.FadeColour(hoverColour, 50, Easing.OutQuint);
            this.ScaleTo(1.05f, 100, Easing.OutQuint);
            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            buttonBackground.FadeColour(normalColour, 200);
            this.ScaleTo(1f, 200, Easing.OutElastic);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (e.Button == osuTK.Input.MouseButton.Left)
            {
                buttonBackground.Colour = clickColour;
                this.ScaleTo(0.95f, 50, Easing.OutQuint);
            }
            return base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            if (e.Button == osuTK.Input.MouseButton.Left)
            {
                buttonBackground.FadeColour(IsHovered ? hoverColour : normalColour, 50);
                this.ScaleTo(IsHovered ? 1.05f : 1f, 50, Easing.OutQuint);
            }
            base.OnMouseUp(e);
        }
        
        protected override bool OnClick(ClickEvent e)
        {
            Action?.Invoke();
            // Optional: Add a visual/sound effect on click completion if not handled by MouseDown/Up
            return true;
        }
    }
}