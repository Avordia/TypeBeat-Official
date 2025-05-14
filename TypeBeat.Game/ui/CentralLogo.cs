using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes; // For Circle
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events; // For Hover and Click events
using System; // For Action delegate
using osuTK;
using osuTK.Graphics;

namespace TypeBeat.Game.ui
{
    public partial class CentralLogo : CircularContainer // Using CircularContainer for a round shape
    {
        public Action ClickAction { get; set; }

        private Sprite logoSprite;
        private Circle backgroundCircle;

        private const float default_size = 200; // Diameter of the logo

        public CentralLogo()
        {
            Size = new Vector2(default_size);
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            Masking = true; // Clips children to the circular shape

            Children = new Drawable[]
            {
                backgroundCircle = new Circle
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.DarkGray, // Placeholder color
                },
                // logoSprite will be loaded in LoadComplete
            };
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            AddInternal(logoSprite = new Sprite
            {
                RelativeSizeAxes = Axes.Both,
                FillMode = FillMode.Fit, 
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Texture = textures.Get("logo")
            });
        }

        protected override bool OnHover(HoverEvent e)
        {
            backgroundCircle.FadeColour(Color4.Gray, 50, Easing.OutQuint);
            this.ScaleTo(1.1f, 100, Easing.OutQuint);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            backgroundCircle.FadeColour(Color4.DarkGray, 200);
            this.ScaleTo(1f, 200, Easing.OutElastic);
            base.OnHoverLost(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            ClickAction?.Invoke();
            this.ScaleTo(0.9f, 50, Easing.OutQuint).Then().ScaleTo(1f, 500, Easing.OutElastic);
            return true; 
        }
    }
}