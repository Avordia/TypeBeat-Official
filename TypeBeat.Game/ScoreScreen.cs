using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Framework.Screens;
using osuTK;
using osuTK.Graphics;

namespace TypeBeat.Game
{
    public partial class ScoreScreen : Screen
    {
        private readonly string title;
        private readonly int score;
        private readonly double accuracy;
        private readonly int maxStreak;
        private readonly string grade;
        private readonly Sprite backgroundSprite;

        [Resolved]
        private TextureStore textures { get; set; }

        public ScoreScreen(string title, int score, double accuracy, int maxStreak, string grade = "A")
        {
            this.title = title;
            this.score = score;
            this.accuracy = accuracy;
            this.maxStreak = maxStreak;
            this.grade = grade;

            InternalChildren = new Drawable[]
            {
                // Background
                backgroundSprite = new Sprite
                {
                    RelativeSizeAxes = Axes.Both,
                    FillMode = FillMode.Fill,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                },
                // Dark overlay for better text visibility
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(0, 0, 0, 180),
                    Depth = -1
                },
                // Main content
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Top = 80, Bottom = 120 },
                    Child = new FillFlowContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Direction = FillDirection.Vertical,
                        AutoSizeAxes = Axes.Both,
                        Spacing = new Vector2(0, 20),
                        Children = new Drawable[]
                        {
                            // Title
                            new SpriteText
                            {
                                Text = title.ToUpper(),
                                Font = new FontUsage("Kodchasan", size: 36, weight: "Bold"),
                                Colour = Color4.White,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre
                            },
                            // Grade (large letter)
                            new Container
                            {
                                AutoSizeAxes = Axes.Both,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Margin = new MarginPadding { Vertical = 30 },
                                Masking = true,
                                CornerRadius = 15,
                                Children = new Drawable[]
                                {
                                    // Background box for grade
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = new Color4(255, 255, 255, 20)
                                    },
                                    new SpriteText
                                    {
                                        Text = grade,
                                        Font = new FontUsage("Kodchasan", size: 180, weight: "Bold"),
                                        Colour = GetGradeColor(grade),
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Margin = new MarginPadding(40)
                                    }
                                }
                            },
                            // Score
                            new SpriteText
                            {
                                Text = FormatScore(score),
                                Font = new FontUsage("Kodchasan", size: 56, weight: "Bold"),
                                Colour = Color4.White,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre
                            },
                            // Separator
                            new Box
                            {
                                Size = new Vector2(400, 2),
                                Colour = new Color4(255, 255, 255, 60),
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Margin = new MarginPadding { Vertical = 10 }
                            },
                            // Accuracy
                            new Container
                            {
                                AutoSizeAxes = Axes.Both,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Child = new FillFlowContainer
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(10, 0),
                                    Children = new Drawable[]
                                    {
                                        new SpriteText
                                        {
                                            Text = "Accuracy:",
                                            Font = new FontUsage("Kodchasan", size: 28),
                                            Colour = new Color4(200, 200, 200, 255)
                                        },
                                        new SpriteText
                                        {
                                            Text = $"{accuracy:F2}%",
                                            Font = new FontUsage("Kodchasan", size: 28, weight: "Bold"),
                                            Colour = Color4.White
                                        }
                                    }
                                }
                            },
                            // Max Streak
                            new Container
                            {
                                AutoSizeAxes = Axes.Both,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Child = new FillFlowContainer
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(10, 0),
                                    Children = new Drawable[]
                                    {
                                        new SpriteText
                                        {
                                            Text = "Highest Streak:",
                                            Font = new FontUsage("Kodchasan", size: 28),
                                            Colour = new Color4(200, 200, 200, 255)
                                        },
                                        new SpriteText
                                        {
                                            Text = $"x{maxStreak}",
                                            Font = new FontUsage("Kodchasan", size: 28, weight: "Bold"),
                                            Colour = Color4.White
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                // Submit button at bottom
                new ClickableContainer
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Size = new Vector2(300, 60),
                    Y = -50,
                    Masking = true,
                    CornerRadius = 10,
                    Action = () => this.Exit(),
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = new Color4(70, 130, 255, 255)
                        },
                        new SpriteText
                        {
                            Text = "SUBMIT",
                            Font = new FontUsage("Kodchasan", size: 28, weight: "Bold"),
                            Colour = Color4.White,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre
                        }
                    }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // Try to load background texture
            var bgTexture = textures.Get("images/backgrounds/default");
            if (bgTexture != null)
                backgroundSprite.Texture = bgTexture;
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);
            this.FadeInFromZero(500);
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            this.FadeOut(300);
            return base.OnExiting(e);
        }

        private static string FormatScore(int score)
        {
            // Format score with leading zeros (e.g., 0000123456)
            return score.ToString().PadLeft(15, '9'); // Use 9s like in the design
        }

        private static Color4 GetGradeColor(string grade)
        {
            return grade switch
            {
                "S" => new Color4(255, 215, 0, 255), // Gold
                "A" => new Color4(100, 255, 100, 255), // Green
                "B" => new Color4(100, 200, 255, 255), // Blue
                "C" => new Color4(255, 200, 100, 255), // Orange
                "D" => new Color4(255, 100, 100, 255), // Red
                _ => Color4.White
            };
        }

        private static string CalculateGrade(double accuracy)
        {
            if (accuracy >= 95.0) return "S";
            if (accuracy >= 90.0) return "A";
            if (accuracy >= 80.0) return "B";
            if (accuracy >= 70.0) return "C";
            return "D";
        }
    }
}
