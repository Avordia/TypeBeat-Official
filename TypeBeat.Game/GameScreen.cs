using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Screens;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Input;
using osu.Framework.Logging;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.Ui;

namespace TypeBeat.Game
{
    public partial class GameScreen : Screen
    {
        private readonly Beatpack beatpack;
        private readonly Beatmap beatmap;
        private readonly Sprite backgroundSprite;
        private readonly SpriteText accuracyText;
        private float currentAccuracy = 100.0f;

        [Resolved]
        private IRenderer renderer { get; set; }

        public GameScreen(Beatpack beatpack, Beatmap beatmap)
        {
            this.beatpack = beatpack;
            this.beatmap = beatmap;

            InternalChildren = new Drawable[]
            {
                // Background
                backgroundSprite = new Sprite
                {
                    RelativeSizeAxes = Axes.Both,
                    FillMode = FillMode.Fill,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Alpha = 0.5f // Dimmed for gameplay visibility
                },
                // HitDetector at center
                new HitDetector(),
                // Top-right UI container
                new Container
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    AutoSizeAxes = Axes.Both,
                    Padding = new MarginPadding(30),
                    Child = new FillFlowContainer
                    {
                        Direction = FillDirection.Vertical,
                        AutoSizeAxes = Axes.Both,
                        Spacing = new Vector2(0, 10),
                        Children = new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                Direction = FillDirection.Horizontal,
                                AutoSizeAxes = Axes.Both,
                                Spacing = new Vector2(10, 0),
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = "Accuracy:",
                                        Font = new FontUsage("Kodchasan", size: 28, weight: "Bold"),
                                        Colour = Colour4.White,
                                        Shadow = true,
                                        ShadowColour = Colour4.Black
                                    },
                                    accuracyText = new SpriteText
                                    {
                                        Text = "100.0%",
                                        Font = new FontUsage("Kodchasan", size: 28, weight: "Bold"),
                                        Colour = Colour4.Lime,
                                        Shadow = true,
                                        ShadowColour = Colour4.Black
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // Load background image
            if (!string.IsNullOrEmpty(beatpack.BackgroundImagePath))
            {
                Schedule(() =>
                {
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
                                    var texture = Texture.FromStream(renderer, memoryStream);
                                    backgroundSprite.Texture = texture;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Failed to load background image");
                    }
                });
            }

            Logger.Log($"GameScreen loaded: {beatmap?.Title} - {beatmap?.DifficultyName}", LoggingTarget.Runtime, LogLevel.Important);
            Logger.Log($"Map has {beatmap?.MapData?.Count ?? 0} segments", LoggingTarget.Runtime, LogLevel.Important);
        }

        public void UpdateAccuracy(float accuracy)
        {
            currentAccuracy = accuracy;
            accuracyText.Text = $"{accuracy:F1}%";
            
            // Color code based on accuracy
            if (accuracy >= 95)
                accuracyText.Colour = Colour4.Lime;
            else if (accuracy >= 90)
                accuracyText.Colour = Colour4.Yellow;
            else if (accuracy >= 80)
                accuracyText.Colour = Colour4.Orange;
            else
                accuracyText.Colour = Colour4.Red;
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);
            this.FadeInFromZero(300);
            Logger.Log($"GameScreen entered with beatmap: {beatmap?.Title}", LoggingTarget.Runtime, LogLevel.Important);
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            this.FadeOut(300);
            return base.OnExiting(e);
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == Key.Escape)
            {
                this.Exit();
                return true;
            }

            return base.OnKeyDown(e);
        }
    }
}
