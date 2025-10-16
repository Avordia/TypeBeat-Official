using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;

namespace TypeBeat.Game.Ui
{
    public partial class HitDetector : Container
    {
        private Sprite hitDetectorSprite;

        public HitDetector()
        {
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            AutoSizeAxes = Axes.Both;
            Scale = new osuTK.Vector2(1.2f); // Make it 1.5x larger

            InternalChild = hitDetectorSprite = new Sprite
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            };
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            hitDetectorSprite.Texture = textures.Get("images/HitDetector");
        }
    }
}
