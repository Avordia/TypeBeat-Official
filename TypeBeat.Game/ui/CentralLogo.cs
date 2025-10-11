using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace TypeBeat.Game.ui
{
    public partial class CentralLogo : Container
    {
        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            Size = new Vector2(800, 200);
            Masking = true;
            CornerRadius = 20;

            InternalChildren = new Drawable[]
            {

                new Sprite
                {
                    Texture = textures.Get("images/logo/Logo.png"),
                    RelativeSizeAxes = Axes.Y, 
                    Height = 120,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Margin = new MarginPadding { Left = 40 }
                },

                new SpriteText
                {
                    Font = new FontUsage(family: "Kodchasan", size: 72),
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Margin = new MarginPadding { Left = 200 },
                    Text = "T Y P E B E A T",
                },

                new FillFlowContainer
                {
                    Direction = FillDirection.Vertical,
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Spacing = new Vector2(0, 10),
                    Margin = new MarginPadding { Right = 40 },
                    Children = new Drawable[]
                    {
                        // new MenuButton { Text = "Play" },
                        // new MenuButton { Text = "Settings" },
                        // new MenuButton { Text = "Exit" },
                    }
                }
            };
        }
    }
}