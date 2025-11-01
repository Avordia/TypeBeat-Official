using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Audio.Sample;
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
using osu.Framework.IO.Stores;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.Ui;
using TypeBeat.Game.Gameplay.Judgement;
using TypeBeat.Game.Gameplay.Input;
using TypeBeat.Game.Online;
using TypeBeat.Game.Gameplay.Layout;
using TypeBeat.Game.Gameplay.Appearance;
using TypeBeat.Game.Gameplay.Scheduling;
using TypeBeat.Game.Gameplay.Typing;
using TypeBeat.Game.Gameplay.Objects;
using TypeBeat.Game.Filehandling;

namespace TypeBeat.Game
{
    public partial class GameScreen : Screen
    {
        private readonly Beatpack beatpack;
        private readonly Beatmap beatmap;
        private readonly Sprite backgroundSprite;
        private readonly SpriteText scoreText;
        private readonly SpriteText accuracyText;
        private readonly SpriteText comboText;
        private readonly Sprite judgementGlow;
        private readonly Sprite healthBarLogo;
        private readonly Container healthBarContainer;
        private readonly List<Box> healthBarSegments = new List<Box>();
        private const int healthSegmentCount = 15; // Number of parallelogram segments
        private const double gracePeriodMs = 2000; // 2 second grace period without countdown
        private float currentAccuracy = 100.0f;
        private int currentScore = 0;
        private double currentHealth = 1.0; // Health from 0.0 to 1.0 (100%)
        private const double max_health = 1.0;
        private const double health_drain_rate = 0.018; // Health drain per second (slightly reduced from 0.02)
        private double lastHealthDrainTime = 0;
        private readonly Ui.CentralWordContainer centralWord;
        private readonly Ui.WordPreviews wordPreviews;
        private readonly Container playfield;
        private readonly Container scorePopupContainer;
        private readonly LayoutConfig layoutConfig = new LayoutConfig
        {
            HalfGapXFraction = 0.2f 
        };
        private readonly NoteAppearanceConfig appearanceConfig = new NoteAppearanceConfig();
        private readonly NoteScheduler noteScheduler;
    private readonly SpriteText debugText;

    private readonly TypingManager typing = new TypingManager();
    private readonly HitWindows hitWindows = new HitWindows();
    private readonly TypeBeat.Game.Gameplay.Scoring.ScoreProcessor score = new TypeBeat.Game.Gameplay.Scoring.ScoreProcessor();
    private int currentSegmentIndex = 0;
    private Beatmaps.WordSegment[] segmentsArr = System.Array.Empty<Beatmaps.WordSegment>();

    private Container pauseOverlay;
    private Container countdownOverlay;
    private SpriteText countdownText;
    private bool isPaused = false;
    private bool isCountingDown = false;
    private double gameplayStartClockMs = 0;
    private double pauseTime = 0;
    private Track gameTrack;
    private Sample kickSound;
    private Sample snareSound;
    private Sample customTypeBeatSample;
    private Sample customSpaceSample;
    private FileStream customSampleFileStream;
    private ZipArchiveResourceStore customSampleResourceStore;
    private ISampleStore customSampleStore;
    private bool isGameOver = false;
    private Note firstNoteForCue;
    private Note secondNoteForVelocity;
    private DrawableNotePairAbsolute standaloneFirstCue;
    private bool firstCueActive = false;

        [Resolved]
        private IRenderer renderer { get; set; }

        [Resolved]
        private AudioManager audioManager { get; set; }

        [Resolved]
        private TextureStore textures { get; set; }
        
        [Resolved]
        private AuthenticationService authService { get; set; }
        
        [Resolved]
        private ScoreSubmissionService scoreService { get; set; }
        
        [Resolved]
        private Ui.LoginOverlay loginOverlay { get; set; }

        public GameScreen(Beatpack beatpack, Beatmap beatmap)
        {
            this.beatpack = beatpack;
            this.beatmap = beatmap;

            InternalChildren = new Drawable[]
            {
                backgroundSprite = new Sprite
                {
                    RelativeSizeAxes = Axes.Both,
                    FillMode = FillMode.Fill,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Alpha = 0.5f 
                },
                new Container
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    AutoSizeAxes = Axes.Both,
                    Padding = new MarginPadding(10),
                    Child = debugText = new SpriteText
                    {
                        Font = new FontUsage("Kodchasan", size: 16, weight: "Bold"),
                        Colour = Colour4.Yellow,
                        Text = "debug..."
                    }
                },
                healthBarContainer = new Container
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Position = new Vector2(90, 80), 
                    AutoSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            Direction = FillDirection.Horizontal,
                            AutoSizeAxes = Axes.Both,
                            Spacing = new Vector2(10, 0), 
                            Children = new Drawable[]
                            {
                                new Container
                                {
                                    Size = new Vector2(45, 45),
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Y = 3,
                                    Child = healthBarLogo = new Sprite
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        RelativeSizeAxes = Axes.Both,
                                        FillMode = FillMode.Fit,
                                        EdgeSmoothness = new Vector2(2.0f) // Anti-aliasing for smooth edges
                                    }
                                },

                                new Container
                                {
                                    Size = new Vector2(220, 15), // Much smaller bar
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Child = new FillFlowContainer
                                    {
                                        Direction = FillDirection.Horizontal,
                                        AutoSizeAxes = Axes.None,
                                        RelativeSizeAxes = Axes.Both,
                                        Spacing = new Vector2(9, 0) 
                                    }
                                }
                            }
                        }
                    }
                },
                // Score display at top center
                new Container
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    AutoSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Top = 30 },
                    Child = scoreText = new SpriteText
                    {
                        Text = "000000000000", // 12 digits
                        Font = new FontUsage("Kodchasan", size: 56, weight: "Bold"), // Larger font
                        Colour = Colour4.White,
                        Spacing = new Vector2(0.25f, 0) // 25% spacing
                    }
                },
                // HitDetector at center
                new HitDetector(),
                // Playfield (notes layer) between background/hit detector and UI
                playfield = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Children = new Drawable[]
                    {
                        noteScheduler = new NoteScheduler(layoutConfig, appearanceConfig)
                        {
                            RelativeSizeAxes = Axes.Both,
                            Anchor = Anchor.TopLeft,
                            Origin = Anchor.TopLeft
                        }
                    }
                },                centralWord = new Ui.CentralWordContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Y = 0
                },
                new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Y = 90,
                    AutoSizeAxes = Axes.Both,
                    Child = comboText = new SpriteText
                    {
                        Text = "x0",
                        Font = new FontUsage("Kodchasan", size: 48, weight: "Bold"),
                        Colour = Colour4.White
                    }
                },
                // Word previews stacked above the word container
                wordPreviews = new Ui.WordPreviews
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Y = -130
                },
                // Top-right UI container (accuracy only now)
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
                                        Text = "ACCURACY:", // All caps
                                        Font = new FontUsage("Kodchasan", size: 34, weight: "Bold"), // Larger font
                                        Colour = Colour4.White
                                    },
                                    accuracyText = new SpriteText
                                    {
                                        Text = "100.0%",
                                        Font = new FontUsage("Kodchasan", size: 34, weight: "Bold"), // Larger font
                                        Colour = Colour4.Lime
                                    }
                                }
                            }
                        }
                    }
                },
                judgementGlow = new Sprite
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.Centre,
                    Y = -100, 
                    Size = new Vector2(1200, 200), 
                    Alpha = 0,
                    Depth = -1000 
                },
                // Score feedback popup container at bottom center with judgement glow
                scorePopupContainer = new Container
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.Centre,
                    Y = -100, // Same as judgement glow
                    AutoSizeAxes = Axes.Both,
                    Depth = -2000 // Above everything
                }
            };

            // Pause overlay (hidden by default)
            AddInternal(pauseOverlay = createPauseOverlay());
            pauseOverlay.Alpha = 0;
            
            // Countdown overlay (hidden by default)
            AddInternal(countdownOverlay = createCountdownOverlay());
            countdownOverlay.Alpha = 0;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // Load sound effects
            Logger.Log("[GameScreen] Attempting to load sounds...", LoggingTarget.Runtime, LogLevel.Important);
            
            loadCustomSounds();
            
            // Load t6 logo for health bar
            healthBarLogo.Texture = textures.Get("images/logo/Logo");
            
            // Create parallelogram health bar segments with gradient colors
            // Navigate to the segment container: healthBarContainer → FillFlowContainer (horizontal) → Container (second child) → FillFlowContainer
            var horizontalFlow = healthBarContainer.Children.OfType<FillFlowContainer>().First();
            var healthBarSegmentsContainer = horizontalFlow.Children.OfType<Container>().Skip(1).First(); // Skip logo container, get health bar container
            var segmentContainer = healthBarSegmentsContainer.Child as FillFlowContainer;
            
            // Define gradient colors from red to purple/blue (Figma design)
            var gradientColors = new[]
            {
                Colour4.FromHex("#FF3333"), // Red
                Colour4.FromHex("#FF4D33"),
                Colour4.FromHex("#FF6633"),
                Colour4.FromHex("#FF8033"), // Orange
                Colour4.FromHex("#FF9933"),
                Colour4.FromHex("#FFB333"),
                Colour4.FromHex("#CC6699"), // Pink
                Colour4.FromHex("#B366AA"),
                Colour4.FromHex("#9966BB"), // Magenta/Purple
                Colour4.FromHex("#8066CC"),
                Colour4.FromHex("#6666DD"), // Purple
                Colour4.FromHex("#5555BB"),
                Colour4.FromHex("#444499"),
                Colour4.FromHex("#333377"), // Dark purple
                Colour4.FromHex("#222255")  // Dark blue
            };
            
            float segmentWidth = 13f;      // Smaller width
            float segmentHeight = 15f;     // Smaller height
            float skewAmount = 0.3f;       // Creates parallelogram effect
            
            for (int i = 0; i < healthSegmentCount; i++)
            {
                // Calculate gradient alpha: 100% (left) to 10% (right)
                float alphaGradient = 1.0f - (i / (float)(healthSegmentCount - 1)) * 0.9f;
                
                var segment = new Container
                {
                    Size = new Vector2(segmentWidth, segmentHeight),
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Shear = new Vector2(skewAmount, 0), // Creates parallelogram/chevron shape
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = gradientColors[i],
                        Alpha = alphaGradient, // Gradient alpha from left to right
                        EdgeSmoothness = new Vector2(2.0f) // Enhanced anti-aliasing for smoother edges
                    }
                };
                
                healthBarSegments.Add(segment.Child as Box);
                segmentContainer.Add(segment);
            }
            
            // Load judgment glow texture
            judgementGlow.Texture = textures.Get("images/JudgementGlow");
            Logger.Log($"[GameScreen] Judgement glow texture loaded: {judgementGlow.Texture != null}", LoggingTarget.Runtime, LogLevel.Important);
            
            // Load game audio from beatpack
            try
            {
                using (var stream = File.OpenRead(beatpack.FilePath))
                {
                    var beatmapAssetStorage = new ZipArchiveResourceStore(stream);
                    var trackStore = audioManager.GetTrackStore(beatmapAssetStorage);
                    
                    // Try MusicPath first (old format or manifest-specified path)
                    if (!string.IsNullOrEmpty(beatpack.MusicPath))
                    {
                        gameTrack = trackStore.Get(beatpack.MusicPath);
                    }
                    
                    // Fallback to audio.mp3 (new format default)
                    if (gameTrack == null)
                    {
                        gameTrack = trackStore.Get("audio.mp3");
                    }
                    
                    if (gameTrack != null)
                    {
                        gameTrack.Looping = false; // Don't loop gameplay music
                        Logger.Log($"[GameScreen] Loaded audio track: {beatpack.MusicPath ?? "audio.mp3"}", LoggingTarget.Runtime, LogLevel.Important);
                    }
                    else
                    {
                        Logger.Log($"[GameScreen] Failed to load audio track", LoggingTarget.Runtime, LogLevel.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load game audio");
            }
            
            // Load background image
            Schedule(() =>
            {
                try
                {
                    using (var stream = File.OpenRead(beatpack.FilePath))
                    using (var archive = new ZipArchive(stream))
                    {
                        ZipArchiveEntry entry = null;
                        
                        // Try BackgroundImagePath first (old format)
                        if (!string.IsNullOrEmpty(beatpack.BackgroundImagePath))
                        {
                            entry = archive.GetEntry(beatpack.BackgroundImagePath);
                        }
                        
                        // Fallback to cover.jpg (new format)
                        if (entry == null)
                        {
                            entry = archive.GetEntry("cover.jpg");
                        }
                        
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

            // Initialize center word and previews from beatmap
            try
            {
                var segments = beatmap?.MapData;
                if (segments != null && segments.Any())
                {
                    segmentsArr = segments.ToArray();
                    currentSegmentIndex = 0;
                    typing.SetSegment(segmentsArr[currentSegmentIndex]);

                    string word0 = toWord(segmentsArr.ElementAtOrDefault(0));
                    string word1 = toWord(segmentsArr.ElementAtOrDefault(1));
                    string word2 = toWord(segmentsArr.ElementAtOrDefault(2));
                    string word3 = toWord(segmentsArr.ElementAtOrDefault(3));

                    // Center word is current (index 0)
                    centralWord.SetWord(word0);
                    // Preview shows 3 upcoming words with gradual size/opacity increase
                    wordPreviews.SetPreviews(word1, word2, word3);

                    // Load ALL segments into the scheduler - notes will spawn based on time
                    noteScheduler.LoadAllSegments(segments);

                    // Determine first and second earliest notes (by EndTime)
                    var orderedByEnd = segments
                        .Where(s => s?.Notes != null)
                        .SelectMany(s => s.Notes)
                        .OrderBy(n => n.EndTime)
                        .ToList();
                    firstNoteForCue = orderedByEnd.ElementAtOrDefault(0);
                    secondNoteForVelocity = orderedByEnd.ElementAtOrDefault(1);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize word previews");
            }

            Logger.Log($"GameScreen loaded: {beatmap?.Title} - {beatmap?.DifficultyName}", LoggingTarget.Runtime, LogLevel.Important);
            Logger.Log($"Map has {beatmap?.MapData?.Count ?? 0} segments", LoggingTarget.Runtime, LogLevel.Important);
        }

        private static string toWord(Beatmaps.WordSegment segment)
        {
            if (segment?.Notes == null)
                return string.Empty;

            // Concatenate characters, force uppercase. '/' is kept as '/'.
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var n in segment.Notes)
            {
                if (string.IsNullOrEmpty(n.Character)) continue;
                sb.Append(n.Character.ToUpperInvariant());
            }
            return sb.ToString();
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

        private void UpdateScore(JudgementType judgement)
        {
            // Score calculation: Perfect=300, Great=200, Good=100, Meh=50, Miss=0
            int scoreGain = judgement switch
            {
                JudgementType.Perfect300 => 300,
                JudgementType.Great200 => 200,
                JudgementType.Good100 => 100,
                JudgementType.Meh50 => 50,
                _ => 0
            };

            // Add combo multiplier bonus (up to 4x at combo 100+)
            int comboMultiplier = Math.Min(score.Combo / 25, 4);
            currentScore += scoreGain * (1 + comboMultiplier);
            scoreText.Text = currentScore.ToString("D12"); // 12 digits
        }

        private void UpdateCombo()
        {
            comboText.Text = $"x{score.Combo}";
        }

        private void ShowJudgementGlow(JudgementType judgement)
        {
            Logger.Log($"[GameScreen] ShowJudgementGlow called with {judgement}, texture={judgementGlow.Texture != null}, alpha={judgementGlow.Alpha}", LoggingTarget.Runtime, LogLevel.Important);
            
            // Set color based on judgment type (osu! standard colors)
            Colour4 glowColor = judgement switch
            {
                JudgementType.Perfect300 => Colour4.FromHex("#66CCFF"), // Sky blue
                JudgementType.Great200 => Colour4.FromHex("#88FF88"),   // Light green
                JudgementType.Good100 => Colour4.FromHex("#FFDD00"),    // Yellow
                JudgementType.Meh50 => Colour4.FromHex("#FF8800"),      // Orange
                _ => Colour4.FromHex("#FF4444")                         // Red for miss
            };

            judgementGlow.Colour = glowColor;
            
            // Subtle flash animation with scale
            judgementGlow.ScaleTo(0.9f, 0)
                         .Then()
                         .ScaleTo(1.1f, 80, Easing.OutQuint)
                         .Then()
                         .ScaleTo(1.0f, 150, Easing.InQuint);
                         
            judgementGlow.FadeTo(0.4f, 40)
                         .Then()
                         .FadeOut(300, Easing.OutQuint);
        }

        private void ShowScorePopup(JudgementType judgement)
        {
            // Get score value and color based on judgement
            string scoreText = judgement switch
            {
                JudgementType.Perfect300 => "300",
                JudgementType.Great200 => "200",
                JudgementType.Good100 => "100",
                JudgementType.Meh50 => "50",
                _ => "MISS"
            };

            Colour4 scoreColor = judgement switch
            {
                JudgementType.Perfect300 => Colour4.FromHex("#66CCFF"), // Sky blue (osu! style)
                JudgementType.Great200 => Colour4.FromHex("#88FF88"),   // Light green
                JudgementType.Good100 => Colour4.FromHex("#FFDD00"),    // Yellow
                JudgementType.Meh50 => Colour4.FromHex("#FF8800"),      // Orange
                _ => Colour4.FromHex("#FF4444")                         // Red
            };

            // Create popup text with glow effect
            var popup = new Container
            {
                AutoSizeAxes = Axes.Both,
                Alpha = 0,
                Children = new Drawable[]
                {
                    // White glow background
                    new SpriteText
                    {
                        Text = scoreText,
                        Font = new FontUsage("Kodchasan", size: 72, weight: "Bold"),
                        Colour = Colour4.White,
                        Alpha = 0.4f,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Shadow = true,
                        ShadowColour = Colour4.White.Opacity(0.8f)
                    },
                    // Main colored text
                    new SpriteText
                    {
                        Text = scoreText,
                        Font = new FontUsage("Kodchasan", size: 64, weight: "Bold"),
                        Colour = scoreColor,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Shadow = true,
                        ShadowColour = Colour4.Black.Opacity(0.5f)
                    }
                }
            };

            scorePopupContainer.Add(popup);

            // Animation: fade in, scale up with pulse, fade out
            popup.FadeInFromZero(100)
                 .Then()
                 .FadeOut(500, Easing.OutQuint);
            
            popup.ScaleTo(0.5f)
                 .Then()
                 .ScaleTo(1.4f, 150, Easing.OutElastic)
                 .Then()
                 .ScaleTo(1.0f, 250, Easing.InOutQuint);

            // Remove after animation
            Scheduler.AddDelayed(() => popup.Expire(), 700);
        }

        private void UpdateHealth(JudgementType judgement)
        {
            double healthChange = judgement switch
            {
                JudgementType.Perfect300 => 0.012,  // +1.2% base health (slightly increased from 1%)
                JudgementType.Great200 => 0.006,    // +0.6% health (slightly increased from 0.5%)
                JudgementType.Good100 => 0.003,     // +0.3% health (slightly increased from 0.2%)
                JudgementType.Meh50 => -0.03,      // -0.8% health (slightly reduced penalty from -1%)
                JudgementType.Miss => -0.1,        // -6% health (VERY punishing)
                _ => 0
            };
            
            // Combo-scaled healing bonus for Perfect 300s only
            if (judgement == JudgementType.Perfect300 && score.Combo > 10)
            {
                // Add up to +0.5% extra healing at combo 50+
                // Combo 10: +0.1%, Combo 25: +0.25%, Combo 50+: +0.5% (max)
                double comboBonus = Math.Min(score.Combo / 100.0, 0.005);
                healthChange += comboBonus;
            }

            currentHealth = Math.Clamp(currentHealth + healthChange, 0.0, max_health);
            UpdateHealthBar();

            // Check for fail condition
            if (currentHealth <= 0)
            {
                OnHealthDepleted();
            }
        }

        private void UpdateHealthBar()
        {
            // Update each segment with smooth gradual fade based on health
            for (int i = 0; i < healthBarSegments.Count; i++)
            {
                // Calculate base gradient alpha: 100% (left) to 10% (right)
                float baseAlphaGradient = 1.0f - (i / (float)(healthSegmentCount - 1)) * 0.9f;
                
                // Calculate segment position in health bar (0.0 to 1.0)
                float segmentPosition = i / (float)(healthSegmentCount - 1);
                
                // Calculate smooth fade based on health percentage
                // This creates a gradual transition instead of a hard cut-off
                float healthPercentage = (float)currentHealth;
                float segmentAlpha;
                
                if (segmentPosition < healthPercentage)
                {
                    // Segment is fully visible (player has health here)
                    segmentAlpha = baseAlphaGradient;
                }
                else if (segmentPosition < healthPercentage + 0.2f) // 20% fade zone
                {
                    // Segment is in the fade zone - gradually become invisible
                    float fadeProgress = (segmentPosition - healthPercentage) / 0.2f;
                    segmentAlpha = baseAlphaGradient * (1.0f - fadeProgress);
                }
                else
                {
                    // Segment is fully invisible (health depleted)
                    segmentAlpha = 0.0f;
                }
                
                healthBarSegments[i].FadeTo(segmentAlpha, 200, Easing.OutQuint);
            }
        }

        private void OnHealthDepleted()
        {
            isPaused = true;
            gameTrack?.Stop();
            
            Logger.Log("[GameScreen] Health depleted - Game Over!", LoggingTarget.Runtime, LogLevel.Important);
            
            // Show game over overlay
            Schedule(() =>
            {
                var gameOverText = new SpriteText
                {
                    Text = "FAILED",
                    Font = new FontUsage("Kodchasan", size: 72, weight: "Bold"),
                    Colour = Colour4.Red,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Alpha = 0
                };

                AddInternal(gameOverText);
                gameOverText.FadeIn(500).Then().Delay(2000).Schedule(() => this.Exit());
            });
        }

        private void ShowCountdown()
        {
            // Prevent game updates during countdown
            isCountingDown = true;
            
            // Semi-transparent black background for countdown
            var countdownBackground = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Colour4.Black,
                Alpha = 0.7f,
                Depth = -1000 // Negative depth = topmost layer
            };
            
            var countdownText = new SpriteText
            {
                Font = new FontUsage("Kodchasan", size: 120, weight: "Bold"),
                Colour = Colour4.White,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Y = 120, // Position below center (below word container)
                Alpha = 0,
                Depth = -1000 // Topmost layer
            };
            
            AddInternal(countdownBackground);
            AddInternal(countdownText);
            
            // Fade out background and enable gameplay when countdown ends
            Scheduler.AddDelayed(() =>
            {
                countdownBackground.FadeOut(500).Expire();
                isCountingDown = false; // Allow game to start
            }, gracePeriodMs);
            
            // Show "3", "2", "1"
            Scheduler.AddDelayed(() =>
            {
                countdownText.Text = "3";
                countdownText.FadeIn(100).Then().ScaleTo(1.2f, 900, Easing.OutQuint).FadeOut(900, Easing.OutQuint);
            }, 0);
            
            Scheduler.AddDelayed(() =>
            {
                countdownText.Alpha = 0;
                countdownText.Scale = new Vector2(1f);
                countdownText.Text = "2";
                countdownText.FadeIn(100).Then().ScaleTo(1.2f, 900, Easing.OutQuint).FadeOut(900, Easing.OutQuint);
            }, 1000);
            
            Scheduler.AddDelayed(() =>
            {
                countdownText.Alpha = 0;
                countdownText.Scale = new Vector2(1f);
                countdownText.Text = "1";
                countdownText.FadeIn(100).Then().ScaleTo(1.2f, 900, Easing.OutQuint).FadeOut(900, Easing.OutQuint);
            }, 2000);
            
            // Clean up countdown text
            Scheduler.AddDelayed(() =>
            {
                countdownText.Expire();
            }, 3000);
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);
            this.FadeInFromZero(300);
            
            // Set gameplay start time in the future (creates grace period)
            gameplayStartClockMs = Clock.CurrentTime + gracePeriodMs;
            noteScheduler.TimeOffsetMs = gameplayStartClockMs;
            lastHealthDrainTime = Clock.CurrentTime + gracePeriodMs;
            
            // Start the music after the grace period
            if (gameTrack != null)
            {
                Scheduler.AddDelayed(() =>
                {
                    gameTrack?.Start();
                    Logger.Log("[GameScreen] Music started - gameplay begins!", LoggingTarget.Runtime, LogLevel.Important);
                }, gracePeriodMs);
            }

        // Spawn a standalone visual cue for the earliest note; exclude it from scheduler visuals
        if (firstNoteForCue != null)
        {
            noteScheduler.ExcludedVisualNote = firstNoteForCue;

            // Absolute timing: set start to match the second note's travel duration for consistent velocity
            double arriveAbs = gameplayStartClockMs + firstNoteForCue.EndTime;
            double desiredDuration = secondNoteForVelocity != null
                ? Math.Max(1, secondNoteForVelocity.EndTime - secondNoteForVelocity.StartTime)
                : 1000; // fallback 1s if no second note is available
            double startAbs = arriveAbs - desiredDuration;
            bool isSpace = !string.IsNullOrEmpty(firstNoteForCue.Character) && firstNoteForCue.Character[0] == TypingConstants.SpaceToken;
            standaloneFirstCue = new DrawableNotePairAbsolute(startAbs, arriveAbs, isSpace, layoutConfig, appearanceConfig);
            playfield.Add(standaloneFirstCue);
            firstCueActive = true;
            Logger.Log($"[GameScreen] Spawned standalone first-note cue startAbs={startAbs:F1} arriveAbs={arriveAbs:F1} (firstEnd={firstNoteForCue.EndTime:F1}, durationBasis={desiredDuration:F1})", LoggingTarget.Runtime, LogLevel.Important);
        }
            
            // No visible countdown; 2s grace before gameplay/music starts
            Logger.Log($"GameScreen entered with beatmap: {beatmap?.Title} (2s grace)", LoggingTarget.Runtime, LogLevel.Important);
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            // Stop and dispose the game audio when exiting
            if (gameTrack != null)
            {
                if (gameTrack.IsRunning)
                {
                    gameTrack.Stop();
                    Logger.Log("[GameScreen] Stopped gameplay audio", LoggingTarget.Runtime, LogLevel.Important);
                }
                gameTrack.Dispose();
                gameTrack = null;
                Logger.Log("[GameScreen] Disposed gameplay audio track", LoggingTarget.Runtime, LogLevel.Important);
            }
            
            disposeCustomSampleResources();
            customTypeBeatSample = null;
            customSpaceSample = null;
            
            this.FadeOut(300);
            return base.OnExiting(e);
        }

        protected override void Update()
        {
            base.Update();

            if (isPaused || isCountingDown) return;

            // Passive health drain over time (osu! style)
            double now = Clock.CurrentTime;
            if (lastHealthDrainTime > 0)
            {
                double deltaSeconds = (now - lastHealthDrainTime) / 1000.0;
                currentHealth = Math.Clamp(currentHealth - (health_drain_rate * deltaSeconds), 0.0, max_health);
                UpdateHealthBar();
                
                // Check for fail condition from drain
                if (currentHealth <= 0)
                {
                    OnHealthDepleted();
                    return;
                }
            }
            lastHealthDrainTime = now;

            // Auto-miss overdue notes (beyond late window) without key presses
            double nowRel = Clock.CurrentTime - gameplayStartClockMs;
            if (nowRel < 0) nowRel = 0;
            int autoMissed = typing.AutoConsumeMisses(nowRel, hitWindows, out bool segCompleted);
            if (autoMissed > 0)
            {
                // Apply misses and update HUD for each consumed character
                for (int i = 0; i < autoMissed; i++)
                {
                    if (firstCueActive)
                    {
                        // First note was missed by auto-consume: hide the standalone cue
                        standaloneFirstCue?.OnHit();
                        firstCueActive = false;
                    }
                    else
                    {
                        noteScheduler.HitCurrentNote(); // Make missed notes disappear too
                    }
                    score.Apply(JudgementType.Miss);
                    centralWord.ConsumeNext();
                    ShowJudgementGlow(JudgementType.Miss);
                    ShowScorePopup(JudgementType.Miss); // Show miss popup
                    UpdateHealth(JudgementType.Miss);
                }
                UpdateAccuracy(score.GetAccuracyPercent());
                UpdateCombo();
            }

            if (segCompleted)
            {
                currentSegmentIndex++;
                if (currentSegmentIndex < segmentsArr.Length)
                {
                    var seg = segmentsArr[currentSegmentIndex];
                    typing.SetSegment(seg);
                    string w0 = toWord(segmentsArr.ElementAtOrDefault(currentSegmentIndex));
                    string w1 = toWord(segmentsArr.ElementAtOrDefault(currentSegmentIndex + 1));
                    string w2 = toWord(segmentsArr.ElementAtOrDefault(currentSegmentIndex + 2));
                    string w3 = toWord(segmentsArr.ElementAtOrDefault(currentSegmentIndex + 3));
                    centralWord.SetWord(w0);
                    // Show 3 upcoming words with gradual size/opacity increase
                    wordPreviews.SetPreviews(w1, w2, w3);
                    noteScheduler.LoadSegment(seg);
                }
            }

            // Update debug HUD
            double rel = Clock.CurrentTime - gameplayStartClockMs; if (rel < 0) rel = 0;
            debugText.Text = $"t={rel:F0}ms seg={currentSegmentIndex+1}/{segmentsArr.Length} spawned={noteScheduler.SpawnedCount} drawChildren={playfield.Children.Count()}";
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == Key.Escape)
            {
                // Don't allow pausing during countdown
                if (isCountingDown)
                    return true;
                    
                // Toggle pause
                isPaused = !isPaused;
                if (isPaused)
                {
                    // Pause everything
                    pauseTime = Clock.CurrentTime;
                    pauseOverlay.FadeIn(150);
                    gameTrack?.Stop(); // Pause the music
                    noteScheduler.IsPaused = true; // Pause note spawning/movement
                    
                    Logger.Log("[GameScreen] Game paused at " + pauseTime, LoggingTarget.Runtime, LogLevel.Important);
                }
                else
                {
                    pauseOverlay.FadeOut(150);
                    startCountdown(); // Start countdown before resuming
                }
                return true;
            }

            // Ignore inputs while paused or counting down
            if (isPaused || isCountingDown)
                return true;

            // Accept only A-Z and Space
            char? keyChar = null;
            if (e.Key >= Key.A && e.Key <= Key.Z)
            {
                int offset = (int)e.Key - (int)Key.A;
                keyChar = (char)('A' + offset);
            }
            else if (e.Key == Key.Space)
            {
                // Map Space to the slash token for typing logic
                keyChar = '/';
            }

            if (keyChar == null)
                return base.OnKeyDown(e);

            // Handle typing
            double now = Clock.CurrentTime - gameplayStartClockMs; // gameplay-relative time
            if (now < 0) now = 0;
            var res = typing.HandleKeyPress(keyChar.Value, now, hitWindows);
            
            // Only process if the input was consumed (within active note window)
            if (!res.Consumed)
                return true; // Ignore keypresses outside note windows (QoL for ADHD players)
            
            // Key was consumed - make the visual note disappear
            if (firstCueActive)
            {
                standaloneFirstCue?.OnHit();
                firstCueActive = false;
            }
            else
            {
                noteScheduler.HitCurrentNote();
            }
            
            // Play sound effect
            if (keyChar == '/')
                playCustomOrDefault(snareSound, customSpaceSample);
            else
                playCustomOrDefault(kickSound, customTypeBeatSample);
            
            score.Apply(res.Judgement);
            UpdateScore(res.Judgement);
            UpdateAccuracy(score.GetAccuracyPercent());
            UpdateCombo();
            
            // Bounce combo text on successful hit
            comboText.ScaleTo(1.3f, 100, Easing.OutQuint).Then().ScaleTo(1f, 200, Easing.OutElastic);
            
            UpdateHealth(res.Judgement);
            ShowJudgementGlow(res.Judgement);
            ShowScorePopup(res.Judgement); // Show score feedback popup
            centralWord.ConsumeNext();
            centralWord.PlayBounceEffect(); // Subtle bounce on key press

            if (res.SegmentCompleted)
            {
                // Advance to next segment (previews shift here only)
                currentSegmentIndex++;
                if (currentSegmentIndex < segmentsArr.Length)
                {
                    var seg = segmentsArr[currentSegmentIndex];
                    typing.SetSegment(seg);
                    string w0 = toWord(segmentsArr.ElementAtOrDefault(currentSegmentIndex));
                    string w1 = toWord(segmentsArr.ElementAtOrDefault(currentSegmentIndex + 1));
                    string w2 = toWord(segmentsArr.ElementAtOrDefault(currentSegmentIndex + 2));
                    string w3 = toWord(segmentsArr.ElementAtOrDefault(currentSegmentIndex + 3));
                    centralWord.SetWord(w0);
                    // Show 3 upcoming words with gradual size/opacity increase
                    wordPreviews.SetPreviews(w1, w2, w3);
                    noteScheduler.LoadSegment(seg);
                }
            }

            return true;
        }

        private void loadCustomSounds()
        {
            // Load defaults first so we always have a fallback.
            kickSound = audioManager.Samples.Get("Samples/Kick") ?? audioManager.Samples.Get("Kick.ogg");
            snareSound = audioManager.Samples.Get("Samples/Snare") ?? audioManager.Samples.Get("Snare.ogg");

            customTypeBeatSample = null;
            customSpaceSample = null;

            disposeCustomSampleResources();

            if (beatpack?.CustomSounds?.Enabled != true)
                return;

            try
            {
                customSampleFileStream = File.OpenRead(beatpack.FilePath);
                customSampleResourceStore = new ZipArchiveResourceStore(customSampleFileStream);
                customSampleStore = audioManager.GetSampleStore(customSampleResourceStore);

                var typeBeatSounds = beatpack.GetCustomSoundsForGamemode("TypeBeat").ToList();

                foreach (var sound in typeBeatSounds)
                {
                    customTypeBeatSample = customSampleStore?.Get(sound.Path);
                    if (customTypeBeatSample != null)
                        break;
                }

                foreach (var sound in typeBeatSounds)
                {
                    var filename = sound.Filename?.ToLowerInvariant() ?? string.Empty;
                    if (!filename.Contains("space"))
                        continue;

                    customSpaceSample = customSampleStore?.Get(sound.Path);
                    if (customSpaceSample != null)
                        break;
                }

                if (customSpaceSample == null)
                {
                    foreach (var sound in typeBeatSounds)
                    {
                        customSpaceSample = customSampleStore?.Get(sound.Path);
                        if (customSpaceSample != null)
                            break;
                    }
                }

                Logger.Log($"[GameScreen] Custom sounds loaded - letters={customTypeBeatSample != null}, space={customSpaceSample != null}", LoggingTarget.Runtime, LogLevel.Important);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load custom TypeBeat sounds; using defaults");
                customTypeBeatSample = null;
                customSpaceSample = null;
                disposeCustomSampleResources();
            }
        }

        private void disposeCustomSampleResources()
        {
            customSampleStore?.Dispose();
            customSampleStore = null;

            customSampleResourceStore?.Dispose();
            customSampleResourceStore = null;

            customSampleFileStream?.Dispose();
            customSampleFileStream = null;
        }

        private void playCustomOrDefault(Sample fallbackSample, Sample customSample)
        {
            var sampleToPlay = customSample ?? fallbackSample;
            if (sampleToPlay == null)
                return;

            var channel = sampleToPlay.Play();
            if (channel != null)
                channel.Volume.Value = 1.0;
        }

        private Container createPauseOverlay()
        {
            var overlay = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Alpha = 0.0f,
                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both, Colour = Colour4.Black, Alpha = 0.6f },
                    new FillFlowContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Direction = FillDirection.Vertical,
                        AutoSizeAxes = Axes.Both,
                        Spacing = new Vector2(0, 10),
                        Children = new Drawable[]
                        {
                            new SpriteText { Text = "Paused", Font = new FontUsage("Kodchasan", size: 36, weight: "Bold"), Colour = Colour4.White },
                            new ClickableContainer
                            {
                                Action = () => {
                                    pauseOverlay.FadeOut(150);
                                    startCountdown();
                                },
                                AutoSizeAxes = Axes.Both,
                                Child = new SpriteText { Text = "Resume", Font = new FontUsage("Kodchasan", size: 28, weight: "Bold"), Colour = Colour4.White }
                            },
                            new ClickableContainer
                            {
                                Action = () => this.Exit(),
                                AutoSizeAxes = Axes.Both,
                                Child = new SpriteText { Text = "Exit", Font = new FontUsage("Kodchasan", size: 28, weight: "Bold"), Colour = Colour4.White }
                            }
                        }
                    }
                }
            };

            return overlay;
        }

        private Container createCountdownOverlay()
        {
            var overlay = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Alpha = 0.0f,
                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both, Colour = Colour4.Black, Alpha = 0.3f },
                    countdownText = new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Font = new FontUsage("Kodchasan", size: 120, weight: "Bold"),
                        Colour = Colour4.White,
                        Text = "3"
                    }
                }
            };

            return overlay;
        }

        private void startCountdown()
        {
            isCountingDown = true;
            isPaused = false;
            
            countdownOverlay.FadeIn(150);
            
            // 3
            countdownText.Text = "3";
            countdownText.ScaleTo(1.5f, 0).Then().ScaleTo(1.0f, 800, Easing.OutElastic);
            
            // 2
            this.Delay(1000).Schedule(() =>
            {
                countdownText.Text = "2";
                countdownText.ScaleTo(1.5f, 0).Then().ScaleTo(1.0f, 800, Easing.OutElastic);
            });
            
            // 1
            this.Delay(2000).Schedule(() =>
            {
                countdownText.Text = "1";
                countdownText.ScaleTo(1.5f, 0).Then().ScaleTo(1.0f, 800, Easing.OutElastic);
                
                // Resume game after "1"
                this.Delay(1000).Schedule(() =>
                {
                    countdownOverlay.FadeOut(300);
                    isCountingDown = false;
                    
                    // Adjust the gameplay start time to account for pause duration
                    double pauseDuration = Clock.CurrentTime - pauseTime;
                    gameplayStartClockMs += pauseDuration;
                    noteScheduler.TimeOffsetMs = gameplayStartClockMs;
                    noteScheduler.IsPaused = false; // Resume note spawning/movement
                    
                    gameTrack?.Start(); // Resume the music
                    Logger.Log($"[GameScreen] Countdown finished, game resumed (pause duration: {pauseDuration}ms)", LoggingTarget.Runtime, LogLevel.Important);
                });
            });
        }

        private void onBeatmapCompleted()
        {
            // Prevent multiple completion triggers
            if (isGameOver) return;
            
            isGameOver = true;
            isPaused = true;
            
            Logger.Log("[GameScreen] Beatmap completed! Waiting 2 seconds before showing results...", LoggingTarget.Runtime, LogLevel.Important);
            
            // Wait 2 seconds after last note, then transition to results screen
            Schedule(() =>
            {
                this.Delay(2000).Schedule(() =>
                {
                    noteScheduler.IsPaused = true;
                    gameTrack?.Stop();
                    
                    // Push to results screen with final score
                    this.Push(new ResultsScreen(beatmap, score, authService, scoreService, loginOverlay));
                });
            });
        }

        private static string calculateGrade(double accuracy)
        {
            if (accuracy >= 95.0) return "S";
            if (accuracy >= 90.0) return "A";
            if (accuracy >= 80.0) return "B";
            if (accuracy >= 70.0) return "C";
            return "D";
        }
    }
}