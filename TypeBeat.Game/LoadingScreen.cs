using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Screens;
using osuTK;
using TypeBeat.Game.Beatmaps;

namespace TypeBeat.Game
{
    public partial class LoadingScreen : Screen
    {
        private readonly Beatpack beatpack;
        private readonly Beatmap beatmap;
        private SpriteText loadingText;
        private Box loadingBar;
        private float progress = 0f;

        public LoadingScreen(Beatpack beatpack, Beatmap beatmap)
        {
            this.beatpack = beatpack;
            this.beatmap = beatmap;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                // Dark background
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colour4.Black,
                    Alpha = 0.9f
                },
                // Loading content centered
                new FillFlowContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Direction = FillDirection.Vertical,
                    AutoSizeAxes = Axes.Both,
                    Spacing = new Vector2(0, 20),
                    Children = new Drawable[]
                    {
                        new SpriteText
                        {
                            Text = beatmap.Title ?? "Unknown Title",
                            Font = new FontUsage("Kodchasan", size: 48, weight: "Bold"),
                            Colour = Colour4.White,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre
                        },
                        new SpriteText
                        {
                            Text = beatmap.Artist ?? "Unknown Artist",
                            Font = new FontUsage("Kodchasan", size: 32, weight: "Regular"),
                            Colour = Colour4.Gray,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre
                        },
                        new SpriteText
                        {
                            Text = $"[{beatmap.DifficultyName ?? "Normal"}]",
                            Font = new FontUsage("Kodchasan", size: 24, weight: "Bold"),
                            Colour = Colour4.Yellow,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Margin = new MarginPadding { Top = 20 }
                        },
                        loadingText = new SpriteText
                        {
                            Text = "Loading...",
                            Font = new FontUsage("Kodchasan", size: 28, weight: "Regular"),
                            Colour = Colour4.White,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Margin = new MarginPadding { Top = 40 }
                        },
                        // Loading bar container
                        new Container
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Size = new Vector2(400, 6),
                            Margin = new MarginPadding { Top = 10 },
                            Children = new Drawable[]
                            {
                                // Background bar
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Colour4.Gray,
                                    Alpha = 0.3f
                                },
                                // Progress bar
                                loadingBar = new Box
                                {
                                    RelativeSizeAxes = Axes.Y,
                                    Width = 0,
                                    Colour = Colour4.Lime
                                }
                            }
                        }
                    }
                }
            };
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);
            this.FadeInFromZero(300);
            
            // Start fake loading animation
            Schedule(() => simulateLoading());
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            this.FadeOut(300);
            return base.OnExiting(e);
        }

        protected override void Update()
        {
            base.Update();
            
            // Animate loading bar
            if (progress < 1f)
            {
                progress += (float)(Clock.ElapsedFrameTime / 2000.0); // 2 seconds to complete
                progress = Math.Min(progress, 1f);
                loadingBar.Width = progress * 400f;
                
                // Update loading text
                int dots = (int)((Clock.CurrentTime / 300) % 4);
                loadingText.Text = "Loading" + new string('.', dots);
            }
        }

        private void simulateLoading()
        {
            Scheduler.AddDelayed(() =>
            {
                if (progress >= 0.99f)
                {
                    
                    Screen gameScreen;
                    
                    if (beatmap.Gamemode != null && beatmap.Gamemode.Equals("TypeNote", StringComparison.OrdinalIgnoreCase))
                    {
                        gameScreen = new GameScreenTN(beatpack, beatmap);
                    }
                    else
                    {
                        gameScreen = new GameScreen(beatpack, beatmap);
                    }
                    
                    this.Push(gameScreen);
                    
                    Schedule(() => this.Exit());
                }
            }, 2100); 
        }
    }
}
