using System.IO;
using System.IO.Compression;
using System;
using System.Linq;
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
using osu.Framework.Logging; // <-- Added for logging errors

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

            // --- CHANGED ---
            // Removed the old "loadBackgroundAsync(textures);" call from here.
            // All loading is now handled in the 'load' method.
            // --- END CHANGE ---
        }

        private void updateSelectionState()
        {
            if (isSelected)
                borderContainer.FadeIn(200, Easing.OutQuint);
            else
                borderContainer.FadeOut(200, Easing.OutQuint);
        }

        // --- CHANGED ---
        // Removed the unused 'loadBackgroundAsync' method.
        // --- END CHANGE ---

        [BackgroundDependencyLoader]
        private void load(IRenderer renderer)
        {
            // --- CHANGED ---
            // This logic now supports both new and old beatpack formats.
            // --- START CHANGE ---
            
            string backgroundPath = null;

            // Check for new format first
            if (beatpack.IsNewFormat && beatpack.BackgroundImages != null && beatpack.BackgroundImages.Count > 0)
            {
                // NEW FORMAT: Get the first background from the list
                backgroundPath = beatpack.BackgroundImages[0].Path;
            }
            // Fallback to old format
            else if (!string.IsNullOrEmpty(beatpack.BackgroundImagePath))
            {
                // OLD FORMAT: Use the single background path
                backgroundPath = beatpack.BackgroundImagePath;
            }

            // Load the texture from the beatpack .tbbp (zip) file
            try
            {
                // Use File.OpenRead for non-exclusive read access
                using (var stream = File.OpenRead(beatpack.FilePath))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read)) // Specify Read mode
                {
                    ZipArchiveEntry entry = null;
                    
                    // Try the specified background path first
                    if (!string.IsNullOrEmpty(backgroundPath))
                    {
                        entry = archive.Entries.FirstOrDefault(e => e.FullName.Equals(backgroundPath, StringComparison.OrdinalIgnoreCase));
                    }
                    
                    // Fallback to cover.jpg (new format default)
                    if (entry == null)
                    {
                        entry = archive.Entries.FirstOrDefault(e => e.FullName.Equals("cover.jpg", StringComparison.OrdinalIgnoreCase));
                    }
                    
                    if (entry != null)
                    {
                        using (var imageStream = entry.Open())
                        using (var memoryStream = new MemoryStream())
                        {
                            imageStream.CopyTo(memoryStream);
                            memoryStream.Position = 0; // Rewind stream to the beginning
                            var texture = Texture.FromStream(renderer, memoryStream);
                            Schedule(() => thumbnail.Texture = texture);
                        }
                    }
                    else
                    {
                        Logger.Log($"No background image found in beatpack: {beatpack.FilePath}", LoggingTarget.Runtime, LogLevel.Debug);
                    }
                }
            }
            catch (Exception e)
            {
                // Handle cases where the file path is wrong, file is locked, or the zip is corrupt
                Logger.Log($"Failed to load thumbnail for {beatpack.FilePath}: {e.Message}", LoggingTarget.Runtime, LogLevel.Error);
            }
            // --- END CHANGE ---
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