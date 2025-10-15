using System.IO;
using System.IO.Compression;
using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osuTK;
using TypeBeat.Game.Beatmaps;

namespace TypeBeat.Game.Ui
{
    public partial class SongThumbnail : ClickableContainer
    {
        private readonly Container content;
        private readonly Container borderContainer;
        private readonly Sprite thumbnail;
        private readonly Beatpack beatpack;
        
        // Event that fires when this thumbnail is selected
        public event Action<Beatpack> OnSelected;

        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                isSelected = value;
                updateSelectionState();
            }
        }

        public SongThumbnail(Beatpack beatpack, TextureStore textures)
        {
            this.beatpack = beatpack;
            
            Size = new Vector2(100);

            Children = new Drawable[]
            {
                borderContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = 10,
                    BorderThickness = 20,
                    BorderColour = Colour4.White,
                    Alpha = 0,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0,
                        AlwaysPresent = true
                    }
                },
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

        private void updateSelectionState()
        {
            if (isSelected)
                borderContainer.FadeIn(200, Easing.OutQuint);
            else
                borderContainer.FadeOut(200, Easing.OutQuint);
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
            this.ScaleTo(1.1f, 200, Easing.OutQuint);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            this.ScaleTo(1f, 200, Easing.OutQuint);
            base.OnHoverLost(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            this.ScaleTo(0.95f, 100, Easing.OutQuint)
                .Then()
                .ScaleTo(1f, 100, Easing.OutQuint);

            // Trigger selection event with this beatpack
            OnSelected?.Invoke(beatpack);

            return base.OnClick(e);
        }
    }
}