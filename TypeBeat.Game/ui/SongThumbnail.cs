using System.IO;
using System.IO.Compression;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osuTK;
using TypeBeat.Game.Beatmaps;

namespace TypeBeat.Game.ui
{
    public partial class SongThumbnail : ClickableContainer
    {
        private readonly Container content;
        private readonly Sprite thumbnail;
        private readonly Beatpack beatpack;

        public SongThumbnail(Beatpack beatpack, TextureStore textures)
        {
            this.beatpack = beatpack;
            
            Size = new Vector2(200);
            Masking = true;
            CornerRadius = 10;

            Children = new Drawable[]
            {
                content = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = 10,
                    Child = thumbnail = new Sprite
                    {
                        RelativeSizeAxes = Axes.Both,
                        FillMode = FillMode.Fill,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    }
                }
            };

            // Load the background image
            if (beatpack.BackgroundImagePath != null)
            {
                loadBackgroundAsync(textures);
            }
        }

        private void loadBackgroundAsync(TextureStore textures)
        {
            var texture = textures.Get(beatpack.BackgroundImagePath);
            if (texture != null)
                Schedule(() => thumbnail.Texture = texture);
        }

        [BackgroundDependencyLoader]
        private void load(IRenderer renderer)
        {
            if (string.IsNullOrEmpty(beatpack.BackgroundImagePath))
                return;

            using (var stream = File.OpenRead(beatpack.FilePath))
            using (var archive = new ZipArchive(stream))
            {
                var entry = archive.GetEntry(beatpack.BackgroundImagePath);
                if (entry != null)
                {
                    using (var imageStream = entry.Open())
                    using (var memoryStream = new MemoryStream())
                    {
                        imageStream.CopyTo(memoryStream);
                        memoryStream.Position = 0;
                        var texture = Texture.FromStream(renderer, memoryStream);
                        Schedule(() => thumbnail.Texture = texture);
                    }
                }
            }
        }

        protected override bool OnHover(HoverEvent e)
        {
            content.ScaleTo(1.1f, 200, Easing.OutQuint);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            content.ScaleTo(1f, 200, Easing.OutQuint);
            base.OnHoverLost(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            this.ScaleTo(0.95f, 100, Easing.OutQuint)
                .Then()
                .ScaleTo(1f, 100, Easing.OutQuint);

            return base.OnClick(e);
        }
    }
}