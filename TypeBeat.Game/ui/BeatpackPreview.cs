using System;
using System.IO;
using System.IO.Compression;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
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
        private readonly SpriteText difficultyNameText;
        private readonly Container difficultyBarContainer;
        private readonly Box difficultyBarFill;
        private readonly SpriteText starRatingText;
        private readonly ClickableContainer playButton;
        
        [Resolved]
        private IRenderer renderer { get; set; }
        
        [Resolved]
        private TextureStore textures { get; set; }
        
        public BeatpackPreview()
        {
            Masking = true;
            CornerRadius = 30;
            BorderThickness = 3;
            BorderColour = Colour4.FromHex("#4A9EFF"); // Blue border like in Figma

            Children = new Drawable[]
            {
                // Background image
                backgroundSprite = new Sprite
                {
                    RelativeSizeAxes = Axes.Both,
                    FillMode = FillMode.Fill,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Alpha = 0
                },
                // Dark overlay for better text readability
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colour4.Black,
                    Alpha = 0.3f
                },
                // Right side: Title and Artist
                new Container
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    RelativeSizeAxes = Axes.Both,
                    Width = 0.4f, // Take 40% of right side
                    Padding = new MarginPadding { Right = 30 },
                    Child = new FillFlowContainer
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        Direction = FillDirection.Vertical,
                        AutoSizeAxes = Axes.Y,
                        RelativeSizeAxes = Axes.X,
                        Spacing = new Vector2(0, 10),
                        Children = new Drawable[]
                        {
                            titleText = new SpriteText
                            {
                                Anchor = Anchor.TopRight,
                                Origin = Anchor.TopRight,
                                Font = new FontUsage("Inter", size: 42, weight: "Bold"),
                                Colour = Colour4.White,
                                Spacing = new Vector2(0.5f, 0), // 50% spacing
                                Alpha = 0
                            },
                            artistText = new SpriteText
                            {
                                Anchor = Anchor.TopRight,
                                Origin = Anchor.TopRight,
                                Font = new FontUsage("Inter", size: 28),
                                Colour = Colour4.White.Opacity(0.8f),
                                Spacing = new Vector2(0.25f, 0), // 25% spacing
                                Alpha = 0
                            }
                        }
                    }
                },
                // Bottom-left: Difficulty button and bar
                new Container
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    AutoSizeAxes = Axes.Both,
                    Padding = new MarginPadding(30),
                    Child = new FillFlowContainer
                    {
                        Direction = FillDirection.Vertical,
                        AutoSizeAxes = Axes.Both,
                        Spacing = new Vector2(0, 15),
                        Children = new Drawable[]
                        {
                            // Difficulty button
                            new Container
                            {
                                AutoSizeAxes = Axes.Both,
                                Masking = true,
                                CornerRadius = 20,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = Colour4.FromHex("#2C2C2C") // Dark gray background
                                    },
                                    difficultyNameText = new SpriteText
                                    {
                                        Font = new FontUsage("Inter", size: 24),
                                        Colour = Colour4.White,
                                        Spacing = new Vector2(0.25f, 0), // 25% spacing
                                        Margin = new MarginPadding { Horizontal = 30, Vertical = 10 },
                                        Alpha = 0
                                    }
                                }
                            },
                            // Difficulty bar with star rating
                            new FillFlowContainer
                            {
                                Direction = FillDirection.Vertical,
                                AutoSizeAxes = Axes.Y,
                                Width = 300,
                                Spacing = new Vector2(0, 8),
                                Children = new Drawable[]
                                {
                                    starRatingText = new SpriteText
                                    {
                                        Font = new FontUsage("Inter", size: 16),
                                        Colour = Colour4.White,
                                        Spacing = new Vector2(0.25f, 0), // 25% spacing
                                        Alpha = 0
                                    },
                                    difficultyBarContainer = new Container
                                    {
                                        Height = 10,
                                        Width = 300,
                                        Masking = true,
                                        CornerRadius = 5,
                                        Children = new Drawable[]
                                        {
                                            new Box
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Colour = Colour4.FromHex("#2C2C2C") // Dark background
                                            },
                                            difficultyBarFill = new Box
                                            {
                                                RelativeSizeAxes = Axes.Y,
                                                Width = 0, // Will be set based on star rating
                                                Colour = Colour4.White,
                                                Alpha = 0
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                // Bottom-right: Play button
                playButton = new ClickableContainer
                {
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                    Size = new Vector2(80, 80),
                    Margin = new MarginPadding(30),
                    Masking = true,
                    CornerRadius = 40,
                    Alpha = 0,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Colour4.FromHex("#4CAF50") // Green
                        },
                        new SpriteIcon
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Icon = FontAwesome.Solid.Play,
                            Size = new Vector2(30),
                            Colour = Colour4.White,
                            X = 3 // Slight offset to center the play icon visually
                        }
                    }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // No longer loading logo here - it's in SongSelectionScreen
        }

        public void ShowBeatpack(Beatpack beatpack)
        {
            if (beatpack == null)
                return;

            // Update text fields
            if (beatpack.Beatmap != null)
            {
                titleText.Text = (beatpack.Beatmap.Title ?? "Unknown Title").ToUpperInvariant();
                artistText.Text = beatpack.Beatmap.Artist ?? "Unknown Artist";
                difficultyNameText.Text = (beatpack.Beatmap.DifficultyName ?? "NORMAL").ToUpperInvariant();
                
                // Update star rating and bar
                float starRating = beatpack.Beatmap.StarRating;
                starRatingText.Text = $"STAR RATING: {starRating:F1}";
                
                // Calculate bar fill percentage (max is 10 stars)
                float fillPercentage = Math.Clamp(starRating / 10f, 0f, 1f);
                difficultyBarFill.ResizeWidthTo(fillPercentage, 500, Easing.OutQuint);

                // Fade in elements
                titleText.FadeIn(300, Easing.OutQuint);
                artistText.FadeIn(300, Easing.OutQuint);
                difficultyNameText.FadeIn(300, Easing.OutQuint);
                starRatingText.FadeIn(300, Easing.OutQuint);
                difficultyBarFill.FadeIn(300, Easing.OutQuint);
                playButton.FadeIn(300, Easing.OutQuint);
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

        public void SetPlayButtonAction(Action action)
        {
            playButton.Action = action;
        }
    }
}