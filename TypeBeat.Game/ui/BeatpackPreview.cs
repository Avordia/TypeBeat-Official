using System;
using System.IO;
using System.IO.Compression;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;
using TypeBeat.Game.Beatmaps;

namespace TypeBeat.Game.Ui
{
    public partial class BeatpackPreview : Container
    {
        private readonly Sprite backgroundSprite;
        private readonly SpriteText titleText;
        private readonly SpriteText artistText;
        private readonly SpriteText difficultyText;
        private readonly SpriteText starRatingText;
        
        [Resolved]
        private IRenderer renderer { get; set; }
        
        public BeatpackPreview()
        {
            // Sizing is controlled by parent, don't set here
            Masking = true;
            CornerRadius = 20;

            Children = new Drawable[]
            {
                backgroundSprite = new Sprite
                {
                    RelativeSizeAxes = Axes.Both,
                    FillMode = FillMode.Fill,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Alpha = 0
                },
                // Top-left: Title and Artist
                new Container
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    AutoSizeAxes = Axes.Both,
                    Padding = new MarginPadding(20),
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            AutoSizeAxes = Axes.Both,
                            Masking = true,
                            CornerRadius = 10,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Colour4.Black,
                                    Alpha = 0.5f
                                },
                                new FillFlowContainer
                                {
                                    Direction = FillDirection.Vertical,
                                    AutoSizeAxes = Axes.Both,
                                    Spacing = new Vector2(0, 5),
                                    Padding = new MarginPadding(10),
                                    Children = new Drawable[]
                                    {
                                        titleText = new SpriteText
                                        {
                                            Font = new FontUsage("Kodchasan", size: 40, weight: "Bold"),
                                            Colour = Colour4.White,
                                            Alpha = 0
                                        },
                                        artistText = new SpriteText
                                        {
                                            Font = new FontUsage("Kodchasan", size: 28),
                                            Colour = Colour4.White.Opacity(0.9f),
                                            Alpha = 0
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                // Bottom-left: Difficulty and Stars
                new Container
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    AutoSizeAxes = Axes.Both,
                    Padding = new MarginPadding(20),
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            AutoSizeAxes = Axes.Both,
                            Masking = true,
                            CornerRadius = 10,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Colour4.Black,
                                    Alpha = 0.5f
                                },
                                new FillFlowContainer
                                {
                                    Direction = FillDirection.Vertical,
                                    AutoSizeAxes = Axes.Both,
                                    Spacing = new Vector2(0, 5),
                                    Padding = new MarginPadding(10),
                                    Children = new Drawable[]
                                    {
                                        difficultyText = new SpriteText
                                        {
                                            Font = new FontUsage("Kodchasan", size: 32, weight: "Bold"),
                                            Colour = Colour4.White,
                                            Alpha = 0
                                        },
                                        starRatingText = new SpriteText
                                        {
                                            Font = new FontUsage("Kodchasan", size: 24),
                                            Colour = Colour4.Yellow,
                                            Alpha = 0
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        public void ShowBeatpack(Beatpack beatpack)
        {
            if (beatpack == null)
                return;

            // Update text fields
            if (beatpack.Beatmap != null)
            {
                titleText.Text = beatpack.Beatmap.Title ?? "Unknown Title";
                artistText.Text = beatpack.Beatmap.Artist ?? "Unknown Artist";
                difficultyText.Text = beatpack.Beatmap.DifficultyName ?? "Normal";
                
                // Show star rating if it exists (greater than 0)
                if (beatpack.Beatmap.StarRating > 0)
                {
                    starRatingText.Text = $"★ {beatpack.Beatmap.StarRating:F1}";
                }
                else
                {
                    starRatingText.Text = string.Empty;
                }

                // Fade in text
                titleText.FadeIn(300, Easing.OutQuint);
                artistText.FadeIn(300, Easing.OutQuint);
                difficultyText.FadeIn(300, Easing.OutQuint);
                if (beatpack.Beatmap.StarRating > 0)
                    starRatingText.FadeIn(300, Easing.OutQuint);
            }

            if (string.IsNullOrEmpty(beatpack.BackgroundImagePath))
                return;

            // Load texture asynchronously
            Schedule(() =>
            {
                Texture texture = null;
                
                try
                {
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
                                texture = Texture.FromStream(renderer, memoryStream);
                            }
                        }
                    }

                    if (texture != null)
                    {
                        backgroundSprite.Alpha = 0;
                        backgroundSprite.Texture = texture;
                        backgroundSprite.FadeIn(300, Easing.OutQuint);
                    }
                }
                catch
                {
                    // Silently fail if texture can't be loaded
                }
            });
        }
    }
}