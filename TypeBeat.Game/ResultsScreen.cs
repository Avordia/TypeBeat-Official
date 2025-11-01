using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Framework.Screens;
using osuTK;
using osuTK.Graphics;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.Online;
using TypeBeat.Game.Gameplay.Scoring;
using TypeBeat.Game.Ui;

namespace TypeBeat.Game
{
    public partial class ResultsScreen : Screen
    {
        private readonly Beatmap beatmap;
        private readonly ScoreProcessor finalScore;
        private readonly AuthenticationService authService;
        private readonly ScoreSubmissionService scoreService;
        private readonly LoginOverlay loginOverlay;

        private SpriteText statusText;
        private MenuButton submitButton;
        private MenuButton retryButton;
        private MenuButton exitButton;
        private bool scoreSubmitted = false;
        private bool isSubmitting = false;

        public ResultsScreen(Beatmap beatmap, ScoreProcessor finalScore, 
            AuthenticationService authService, 
            ScoreSubmissionService scoreService,
            LoginOverlay loginOverlay)
        {
            this.beatmap = beatmap;
            this.finalScore = finalScore;
            this.authService = authService;
            this.scoreService = scoreService;
            this.loginOverlay = loginOverlay;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // Calculate rating
            string rating = GetRating(finalScore.GetAccuracyPercent());
            Color4 ratingColor = GetRatingColor(rating);

            InternalChildren = new Drawable[]
            {
                // Dark background
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(0, 0, 0, 0.95f)
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding(50),
                    Child = new FillFlowContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Direction = FillDirection.Vertical,
                        AutoSizeAxes = Axes.Both,
                        Spacing = new Vector2(20),
                        Children = new Drawable[]
                        {
                            // Title
                            new SpriteText
                            {
                                Text = "Results",
                                Font = new FontUsage("Kodchasan", size: 60, weight: "Bold"),
                                Colour = Color4.White,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre
                            },
                            // Song info
                            new SpriteText
                            {
                                Text = $"{beatmap.Artist} - {beatmap.Title}",
                                Font = new FontUsage("Kodchasan", size: 30),
                                Colour = Color4.LightGray,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre
                            },
                            new SpriteText
                            {
                                Text = $"[{beatmap.DifficultyName}]",
                                Font = new FontUsage("Kodchasan", size: 20),
                                Colour = Color4.Gray,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre
                            },
                            // Rating
                            new SpriteText
                            {
                                Text = rating,
                                Font = new FontUsage("Kodchasan", size: 80, weight: "Bold"),
                                Colour = ratingColor,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Margin = new MarginPadding { Vertical = 20 }
                            },
                            // Stats container
                            new Container
                            {
                                AutoSizeAxes = Axes.Both,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Child = new FillFlowContainer
                                {
                                    Direction = FillDirection.Vertical,
                                    AutoSizeAxes = Axes.Both,
                                    Spacing = new Vector2(15),
                                    Children = new Drawable[]
                                    {
                                        CreateStatRow("Score", finalScore.TotalScore.ToString("N0")),
                                        CreateStatRow("Accuracy", $"{finalScore.GetAccuracyPercent():F2}%"),
                                        CreateStatRow("Max Combo", $"x{finalScore.MaxCombo}"),
                                        CreateStatRow("Perfect 300", finalScore.Perfect300.ToString()),
                                        CreateStatRow("Great 200", finalScore.Great200.ToString()),
                                        CreateStatRow("Good 100", finalScore.Good100.ToString()),
                                        CreateStatRow("Meh 50", finalScore.Meh50.ToString()),
                                        CreateStatRow("Miss", finalScore.Miss.ToString())
                                    }
                                }
                            },
                            // Status text
                            statusText = new SpriteText
                            {
                                Font = new FontUsage("Kodchasan", size: 18),
                                Colour = Color4.Yellow,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Margin = new MarginPadding { Top = 20 }
                            },
                            // Buttons
                            new FillFlowContainer
                            {
                                Direction = FillDirection.Horizontal,
                                AutoSizeAxes = Axes.Both,
                                Spacing = new Vector2(20),
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Margin = new MarginPadding { Top = 20 },
                                Children = new Drawable[]
                                {
                                    submitButton = new MenuButton(
                                        "Submit Score",
                                        Color4.LimeGreen,
                                        24f,
                                        dimensions: new Vector2(180, 45),
                                        onClick: () => SubmitScore()
                                    ),
                                    retryButton = new MenuButton(
                                        "Retry",
                                        Color4.Orange,
                                        24f,
                                        dimensions: new Vector2(180, 45),
                                        onClick: () => Retry()
                                    ),
                                    exitButton = new MenuButton(
                                        "Exit",
                                        Color4.Gray,
                                        24f,
                                        dimensions: new Vector2(180, 45),
                                        onClick: () => Exit()
                                    )
                                }
                            }
                        }
                    }
                }
            };

            // Set initial status text
            if (string.IsNullOrEmpty(beatmap.OnlineBeatmapID))
            {
                statusText.Text = "Local beatmap - scores cannot be submitted";
                submitButton.Action = null; // Disable submit
            }
            else if (!authService.IsLoggedIn)
            {
                statusText.Text = "Press Submit to login and save your score";
            }
            else
            {
                statusText.Text = "Press Submit to save your score";
            }
        }

        private Container CreateStatRow(string label, string value)
        {
            return new Container
            {
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        Direction = FillDirection.Horizontal,
                        AutoSizeAxes = Axes.Both,
                        Spacing = new Vector2(20),
                        Children = new Drawable[]
                        {
                            new SpriteText
                            {
                                Text = label + ":",
                                Font = new FontUsage("Kodchasan", size: 24),
                                Colour = Color4.LightGray,
                                Width = 200,
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft
                            },
                            new SpriteText
                            {
                                Text = value,
                                Font = new FontUsage("Kodchasan", size: 24, weight: "Bold"),
                                Colour = Color4.White,
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft
                            }
                        }
                    }
                }
            };
        }

        private async void SubmitScore()
        {
            if (scoreSubmitted)
            {
                statusText.Text = "Score already submitted!";
                return;
            }
            
            if (isSubmitting)
            {
                return;
            }

            if (string.IsNullOrEmpty(beatmap.OnlineBeatmapID))
            {
                statusText.Text = "This is a local beatmap - cannot submit score";
                return;
            }
            
            // If not logged in, show login overlay
            if (!authService.IsLoggedIn)
            {
                statusText.Text = "Please login to submit your score";
                statusText.Colour = Color4.Yellow;
                loginOverlay.Show();
                return;
            }

            isSubmitting = true;
            statusText.Text = "Submitting score...";
            statusText.Colour = Color4.Yellow;
            submitButton.Action = null; // Disable button during submission

            var result = await scoreService.SubmitScoreAsync(
                beatmap.OnlineBeatmapID,
                authService.CurrentUser.Value!.Id,
                finalScore.TotalScore,
                finalScore.GetAccuracyPercent(),
                finalScore.MaxCombo,
                null // No mods for now
            );

            if (result.success)
            {
                statusText.Text = "Score submitted successfully! Returning to song select...";
                statusText.Colour = Color4.LimeGreen;
                scoreSubmitted = true;
                Logger.Log("Score submitted successfully", LoggingTarget.Runtime, LogLevel.Important);
                
                // Return to song selection after a short delay
                Schedule(() =>
                {
                    this.Delay(1500).Schedule(() =>
                    {
                        // Exit back to song selection (goes back in screen stack)
                        this.Exit();
                    });
                });
            }
            else
            {
                statusText.Text = $"Failed: {result.message}";
                statusText.Colour = Color4.Red;
                isSubmitting = false;
                submitButton.Action = () => SubmitScore(); // Re-enable button
                Logger.Log($"Score submission failed: {result.message}", LoggingTarget.Runtime, LogLevel.Error);
            }
        }

        private void Retry()
        {
            this.Exit();
            // The GameScreen will be popped, and the song select should handle retry
        }

        private void Exit()
        {
            this.Exit();
        }

        private string GetRating(double accuracy)
        {
            if (accuracy >= 95) return "SS";
            if (accuracy >= 90) return "S";
            if (accuracy >= 80) return "A";
            if (accuracy >= 70) return "B";
            if (accuracy >= 60) return "C";
            return "D";
        }

        private Color4 GetRatingColor(string rating)
        {
            return rating switch
            {
                "SS" => Color4.Gold,
                "S" => Color4.Yellow,
                "A" => Color4.LimeGreen,
                "B" => Color4.LightBlue,
                "C" => Color4.Orange,
                _ => Color4.Gray
            };
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);
            this.FadeInFromZero(300);
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            this.FadeOut(300);
            return base.OnExiting(e);
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == osuTK.Input.Key.Escape)
            {
                Exit();
                return true;
            }

            return base.OnKeyDown(e);
        }
    }
}
