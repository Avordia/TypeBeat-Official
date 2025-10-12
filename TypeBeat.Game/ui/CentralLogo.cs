using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace TypeBeat.Game.ui
{
    public partial class CentralLogo : CompositeDrawable
    {
        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            InternalChild = new ClickableContainer
            {
                Children = new Drawable[]
                {
                    new Sprite
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre, 
                        Texture = textures.Get("images/logo/LogoWithText.png"),                    },
                }
            };
        }
    }
}