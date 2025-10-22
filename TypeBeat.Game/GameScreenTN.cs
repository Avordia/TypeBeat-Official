using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Screens;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Input; // Ensure this is present
using osu.Framework.Logging;
using osu.Framework.IO.Stores;
using TypeBeat.Game.Beatmaps;
using TypeBeat.Game.Ui;
using TypeBeat.Game.Gameplay.Judgement;
using TypeBeat.Game.Gameplay.Input;
using TypeBeat.Game.Gameplay.Layout;
using TypeBeat.Game.Gameplay.Appearance;
using TypeBeat.Game.Gameplay.Scheduling; // <--- MODIFIED
using TypeBeat.Game.Filehandling;
using System.Text;
using osu.Framework.Input.States;

namespace TypeBeat.Game
{
    public partial class GameScreenTN : Screen
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
        private float currentAccuracy = 100.0f;
        private int currentScore = 0;
        private double currentHealth = 1.0; // Health from 0.0 to 1.0 (100%)
        private const double max_health = 1.0;
        private const double health_drain_rate = 0.02; // Health drain per second
        private double lastHealthDrainTime = 0;

        // --- NEW VISUALS ---
        private readonly Container playfield;
        private readonly TypeNoteScheduler noteScheduler; // <-- Replaced NoteScheduler
        private readonly TypeNoteLayoutConfig typeNoteLayoutConfig = new TypeNoteLayoutConfig
        {
            // --- YOU CAN ADJUST YOUR LAYOUT HERE ---
            SpawnXFraction = 1.1f,       // Spawn 10% off-screen right
            DestinationXFraction = 0.25f, // Hit line is 25% from left
            YBaseFraction = 0.6f,        // C0 note is at 60% from top
            YStepPx = 8f                 // 8 pixels per semitone
        };
        // ---------------------

        private readonly SpriteText debugText; // Existing debug text

        // --- ADD THESE FIELDS for the new debug display ---
        private readonly SpriteText debugSharpStateText;
        private readonly SpriteText debugOctaveStateText;
        private readonly SpriteText debugOutputNoteText;
        // --------------------------------------------------

        private readonly NoteManager noteQueue = new NoteManager(); // Assuming NoteManager.cs is the correct name now
        private readonly HitWindows hitWindows = new HitWindows();
        private readonly TypeBeat.Game.Gameplay.Scoring.ScoreProcessor score = new TypeBeat.Game.Gameplay.Scoring.ScoreProcessor();
        private readonly Sprite musicSheetSprite;
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

        [Resolved]
        private IRenderer renderer { get; set; }

        [Resolved]
        private AudioManager audioManager { get; set; }

        [Resolved]
        private TextureStore textures { get; set; }

        // Renamed constructor
        public GameScreenTN(Beatpack beatpack, Beatmap beatmap)
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
                musicSheetSprite = new Sprite
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both, // Fill both axes for aspect ratio
                    Size = new Vector2(0.8f, 0.8f),
                    FillMode = FillMode.Fit,
                    Depth = -10,
                    Alpha = 0.95f,
                },

                // --- ADDED PLAYFIELD ---
                // This container will hold all the scrolling notes.
                // It sits on top of the background/music sheet, but behind the UI.
                playfield = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Child = noteScheduler = new TypeNoteScheduler(typeNoteLayoutConfig) // <-- Use new scheduler
                },
                // -------------------------

                new Container // Existing debug container
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
                                        EdgeSmoothness = new Vector2(2.0f)
                                    }
                                },
                                new Container
                                {
                                    Size = new Vector2(220, 15),
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
                        Text = "000000000000",
                        Font = new FontUsage("Kodchasan", size: 56, weight: "Bold"),
                        Colour = Colour4.White,
                        Spacing = new Vector2(0.25f, 0)
                    }
                },
                new Container // Combo Text
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Y = 200,
                    AutoSizeAxes = Axes.Both,
                    Child = comboText = new SpriteText
                    {
                        Text = "COMBO: X0",
                        Font = new FontUsage("Inter", size: 24),
                        Colour = Colour4.White
                    }
                },
                // --- MODIFIED Top-right UI container (accuracy and NEW debug state) ---
                new Container
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    AutoSizeAxes = Axes.Both,
                    Padding = new MarginPadding(30),
                    Child = new FillFlowContainer // Changed outer container to FillFlow for easier layout
                    {
                        Direction = FillDirection.Vertical, // Stack vertically
                        AutoSizeAxes = Axes.Both,
                        Anchor = Anchor.TopRight, // Anchor this flow to top right
                        Origin = Anchor.TopRight, // Origin top right
                        Spacing = new Vector2(0, 10), // Space between rows
                        Children = new Drawable[]
                        {
                            // Existing Accuracy Row
                            new FillFlowContainer
                            {
                                Direction = FillDirection.Horizontal,
                                AutoSizeAxes = Axes.Both,
                                Spacing = new Vector2(10, 0),
                                Anchor = Anchor.TopRight, // Anchor this row
                                Origin = Anchor.TopRight,
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = "ACCURACY:",
                                        Font = new FontUsage("Kodchasan", size: 34, weight: "Bold"),
                                        Colour = Colour4.White
                                    },
                                    accuracyText = new SpriteText
                                    {
                                        Text = "100.0%",
                                        Font = new FontUsage("Kodchasan", size: 34, weight: "Bold"),
                                        Colour = Colour4.Lime
                                    }
                                }
                            },
                            // --- NEW Debug State Rows ---
                            new FillFlowContainer
                            {
                                Direction = FillDirection.Horizontal,
                                AutoSizeAxes = Axes.Both,
                                Spacing = new Vector2(5, 0),
                                Anchor = Anchor.TopRight, // Anchor this row
                                Origin = Anchor.TopRight,
                                Children = new Drawable[]
                                {
                                    new SpriteText { Text = "isSharp=", Font = FontUsage.Default.With(size: 14), Colour = Colour4.LightGray },
                                    debugSharpStateText = new SpriteText { Text = "?", Font = FontUsage.Default.With(size: 14), Colour = Colour4.Yellow },
                                }
                            },
                            new FillFlowContainer
                            {
                                Direction = FillDirection.Horizontal,
                                AutoSizeAxes = Axes.Both,
                                Spacing = new Vector2(5, 0),
                                Anchor = Anchor.TopRight, // Anchor this row
                                Origin = Anchor.TopRight,
                                Children = new Drawable[]
                                {
                                    new SpriteText { Text = "isOctaveUp=", Font = FontUsage.Default.With(size: 14), Colour = Colour4.LightGray }, // Renamed label
                                    debugOctaveStateText = new SpriteText { Text = "?", Font = FontUsage.Default.With(size: 14), Colour = Colour4.Yellow },
                                }
                            },
                             new FillFlowContainer
                            {
                                Direction = FillDirection.Horizontal,
                                AutoSizeAxes = Axes.Both,
                                Spacing = new Vector2(5, 0),
                                Anchor = Anchor.TopRight, // Anchor this row
                                Origin = Anchor.TopRight,
                                Children = new Drawable[]
                                {
                                    new SpriteText { Text = "Output=", Font = FontUsage.Default.With(size: 14), Colour = Colour4.LightGray }, // Renamed label
                                    debugOutputNoteText = new SpriteText { Text = "(A0)", Font = FontUsage.Default.With(size: 14), Colour = Colour4.Cyan },
                                }
                            }
                            // -----------------------------
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
                }
            };

            // Pause overlay setup
            AddInternal(pauseOverlay = createPauseOverlay());
            pauseOverlay.Alpha = 0;

            // Countdown overlay setup
            AddInternal(countdownOverlay = createCountdownOverlay());
            countdownOverlay.Alpha = 0;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // Load logo
            healthBarLogo.Texture = textures.Get("images/logo/Logo");
            var msTex = textures.Get("images/MusicSheet");
            if (msTex == null)
            {
                // Try with extension as a fallback
                msTex = textures.Get("images/MusicSheet.png");
            }
            musicSheetSprite.Texture = msTex;
            musicSheetSprite.Alpha = 1f;
            Logger.Log($"[GameScreenTN] MusicSheet texture loaded: {musicSheetSprite.Texture != null}", LoggingTarget.Runtime, LogLevel.Important);

            // Create health bar segments
            var horizontalFlow = healthBarContainer.Children.OfType<FillFlowContainer>().First();
            var healthBarSegmentsContainer = horizontalFlow.Children.OfType<Container>().Skip(1).First();
            var segmentContainer = healthBarSegmentsContainer.Child as FillFlowContainer;
            var gradientColors = new[] { Colour4.FromHex("#FF3333"), Colour4.FromHex("#FF4D33"), Colour4.FromHex("#FF6633"), Colour4.FromHex("#FF8033"), Colour4.FromHex("#FF9933"), Colour4.FromHex("#FFB333"), Colour4.FromHex("#CC6699"), Colour4.FromHex("#B366AA"), Colour4.FromHex("#9966BB"), Colour4.FromHex("#8066CC"), Colour4.FromHex("#6666DD"), Colour4.FromHex("#5555BB"), Colour4.FromHex("#444499"), Colour4.FromHex("#333377"), Colour4.FromHex("#222255") };
            float segmentWidth = 13f, segmentHeight = 15f, skewAmount = 0.3f;
            for (int i = 0; i < healthSegmentCount; i++) { float alphaGradient = 1.0f - (i / (float)(healthSegmentCount - 1)) * 0.9f; var segment = new Container { Size = new Vector2(segmentWidth, segmentHeight), Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Shear = new Vector2(skewAmount, 0), Child = new Box { RelativeSizeAxes = Axes.Both, Colour = gradientColors[i], Alpha = alphaGradient, EdgeSmoothness = new Vector2(2.0f) } }; healthBarSegments.Add(segment.Child as Box); segmentContainer.Add(segment); }

            // Load judgement glow
            judgementGlow.Texture = textures.Get("images/JudgementGlow");
            Logger.Log($"[GameScreenTN] Judgement glow texture loaded: {judgementGlow.Texture != null}", LoggingTarget.Runtime, LogLevel.Important);

            // --- MODIFICATION ---
            // Set default debug text to (none)
            debugOutputNoteText.Text = "(none)";
            // --------------------

            // Load audio
            if (!string.IsNullOrEmpty(beatpack.MusicPath)) { try { using (var stream = File.OpenRead(beatpack.FilePath)) { var beatmapAssetStorage = new ZipArchiveResourceStore(stream); var trackStore = audioManager.GetTrackStore(beatmapAssetStorage); gameTrack = trackStore.Get(beatpack.MusicPath); if (gameTrack != null) { gameTrack.Looping = false; Logger.Log($"[GameScreenTN] Loaded audio track: {beatpack.MusicPath}", LoggingTarget.Runtime, LogLevel.Important); } else { Logger.Log($"[GameScreenTN] Failed to load audio track: {beatpack.MusicPath}", LoggingTarget.Runtime, LogLevel.Error); } } } catch (Exception ex) { Logger.Error(ex, "Failed to load game audio"); } }

            // Load background
            if (!string.IsNullOrEmpty(beatpack.BackgroundImagePath)) { Schedule(() => { try { using (var stream = File.OpenRead(beatpack.FilePath)) using (var archive = new ZipArchive(stream)) { var entry = archive.GetEntry(beatpack.BackgroundImagePath); if (entry != null) { using (var imageStream = entry.Open()) using (var memoryStream = new MemoryStream()) { imageStream.CopyTo(memoryStream); memoryStream.Position = 0; var texture = Texture.FromStream(renderer, memoryStream); backgroundSprite.Texture = texture; } } } } catch (Exception ex) { Logger.Error(ex, "Failed to load background image"); } }); }

            // Initialize note queue AND scheduler
            try
            {
                var segments = beatmap?.MapData;
                if (segments != null && segments.Any())
                {
                    segmentsArr = segments.ToArray();
                    currentSegmentIndex = 0;
                    var firstSegment = segmentsArr[currentSegmentIndex];
                    
                    noteQueue.SetSegment(firstSegment);
                    
                    // --- ADD THIS ---
                    noteScheduler.LoadSegment(firstSegment);
                    // ----------------
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize note queue or scheduler"); // Updated log
            }

            Logger.Log($"GameScreenTN loaded: {beatmap?.Title} - {beatmap?.DifficultyName}", LoggingTarget.Runtime, LogLevel.Important);
            Logger.Log($"Map has {beatmap?.MapData?.Count ?? 0} segments", LoggingTarget.Runtime, LogLevel.Important);
        }

        private static string toWord(Beatmaps.WordSegment segment)
        {
             if (segment?.Notes == null) return string.Empty;
             System.Text.StringBuilder sb = new System.Text.StringBuilder();
             foreach (var n in segment.Notes) { if (string.IsNullOrEmpty(n.Character)) continue; sb.Append(n.Character.ToUpperInvariant()); } return sb.ToString();
        }

        public void UpdateAccuracy(float accuracy)
        {
            currentAccuracy = accuracy; accuracyText.Text = $"{accuracy:F1}%"; if (accuracy >= 95) accuracyText.Colour = Colour4.Lime; else if (accuracy >= 90) accuracyText.Colour = Colour4.Yellow; else if (accuracy >= 80) accuracyText.Colour = Colour4.Orange; else accuracyText.Colour = Colour4.Red;
        }

        private void UpdateScore(JudgementType judgement)
        {
           int scoreGain = judgement switch { JudgementType.Perfect300 => 300, JudgementType.Great200 => 200, JudgementType.Good100 => 100, JudgementType.Meh50 => 50, _ => 0 }; int comboMultiplier = Math.Min(score.Combo / 25, 4); currentScore += scoreGain * (1 + comboMultiplier); scoreText.Text = currentScore.ToString("D12");
        }

        private void UpdateCombo()
        {
           comboText.Text = $"COMBO: X{score.Combo}";
        }

        private void ShowJudgementGlow(JudgementType judgement)
        {
           Logger.Log($"[GameScreenTN] ShowJudgementGlow called with {judgement}, texture={judgementGlow.Texture != null}, alpha={judgementGlow.Alpha}", LoggingTarget.Runtime, LogLevel.Important); Colour4 glowColor = judgement switch { JudgementType.Perfect300 => Colour4.FromHex("#FFD700"), JudgementType.Great200 => Colour4.FromHex("#00FF00"), JudgementType.Good100 => Colour4.FromHex("#00FFFF"), JudgementType.Meh50 => Colour4.FromHex("#FF8800"), _ => Colour4.FromHex("#FF0000") }; judgementGlow.Colour = glowColor; judgementGlow.ScaleTo(0.8f, 0).Then().ScaleTo(1.2f, 100, Easing.OutQuint).Then().ScaleTo(1.0f, 200, Easing.InQuint); judgementGlow.FadeTo(0.9f, 50).Then().FadeOut(400, Easing.OutQuint);
        }

        // Health mechanics temporarily disabled for testing
        private void UpdateHealth(JudgementType judgement)
        {
            // no-op
        }

        private void UpdateHealthBar()
        {
            // no-op while health mechanics are disabled
        }

        private void OnHealthDepleted()
        {
            // no-op while health mechanics are disabled
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);
            this.FadeInFromZero(300);
            
            if (gameTrack != null)
            {
                gameTrack.Start();
                Logger.Log("[GameScreenTN] Started gameplay audio - game has begun!", LoggingTarget.Runtime, LogLevel.Important);
            }
            
            gameplayStartClockMs = Clock.CurrentTime;

            // --- ADD THIS ---
            noteScheduler.TimeOffsetMs = gameplayStartClockMs;
            // ----------------
            
            lastHealthDrainTime = Clock.CurrentTime;
            Logger.Log($"GameScreenTN entered with beatmap: {beatmap?.Title}", LoggingTarget.Runtime, LogLevel.Important);
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
           if (gameTrack != null) { if (gameTrack.IsRunning) { gameTrack.Stop(); Logger.Log("[GameScreenTN] Stopped gameplay audio", LoggingTarget.Runtime, LogLevel.Important); } gameTrack.Dispose(); gameTrack = null; Logger.Log("[GameScreenTN] Disposed gameplay audio track", LoggingTarget.Runtime, LogLevel.Important); } this.FadeOut(300); return base.OnExiting(e);
        }

        protected override void Update()
        {
            base.Update();

            // --- MODIFIED Debug State Update ---
            if (debugSharpStateText != null && debugOctaveStateText != null && debugOutputNoteText != null) // Check if loaded
            {
                // Get the current overall input state
                InputState inputState = GetContainingInputManager().CurrentState;
                var keyState = inputState.Keyboard;
                // Check modifier states using Keys.IsPressed
                bool isSharp = keyState.Keys.IsPressed(Key.Space);
                bool isOctaveUp = keyState.Keys.IsPressed(Key.ShiftLeft) || keyState.Keys.IsPressed(Key.ShiftRight); // Renamed variable

                // Update the debug text
                debugSharpStateText.Text = isSharp.ToString();
                debugOctaveStateText.Text = isOctaveUp.ToString(); // Use renamed variable

                // --- REMOVED hard-coded (A0) output ---
            }
            // ------------------------------------


            if (isPaused || isCountingDown) return;

            // Passive health drain DISABLED for testing
            // ...

            // Auto-miss overdue notes
            double nowRel = Clock.CurrentTime - gameplayStartClockMs; if (nowRel < 0) nowRel = 0;
            int autoMissed = noteQueue.AutoConsumeMisses(nowRel, hitWindows, out bool segCompleted);
            if (autoMissed > 0)
            {
                for (int i = 0; i < autoMissed; i++)
                {
                    // --- ADD THIS ---
                    noteScheduler.HitCurrentNote(); // Make the missed note disappear
                    // ----------------
                    score.Apply(JudgementType.Miss);
                    ShowJudgementGlow(JudgementType.Miss);
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
                    noteQueue.SetSegment(seg);
                    
                    // --- ADD THIS ---
                    noteScheduler.LoadSegment(seg);
                    // ----------------
                }
            }

            // Update existing debug text
            double rel = Clock.CurrentTime - gameplayStartClockMs; if (rel < 0) rel = 0;
            debugText.Text = $"t={rel:F0}ms seg={currentSegmentIndex + 1}/{segmentsArr.Length}";
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == Key.Escape)
            {
                if (isCountingDown) return true;
                isPaused = !isPaused;
                if (isPaused) { pauseTime = Clock.CurrentTime; pauseOverlay.FadeIn(150); gameTrack?.Stop(); Logger.Log("[GameScreenTN] Game paused at " + pauseTime, LoggingTarget.Runtime, LogLevel.Important); } else { pauseOverlay.FadeOut(150); startCountdown(); } return true;
            }

            if (isPaused || isCountingDown) return true;

            // --- TypeNote Input Logic ---
            if (e.Key == Key.ShiftLeft || e.Key == Key.ShiftRight || e.Key == Key.Space || e.Key == Key.Unknown) { return base.OnKeyDown(e); }

            char? noteChar = null;
            if (e.Key >= Key.A && e.Key <= Key.G) { int offset = (int)e.Key - (int)Key.A; noteChar = (char)('A' + offset); }

            if (noteChar == null) return base.OnKeyDown(e);

            var keyState = e.CurrentState.Keyboard;

            // Use IsPressed via the 'Keys' field
            bool isSharp = keyState.Keys.IsPressed(Key.Space);
            bool isOctaveUp = keyState.Keys.IsPressed(Key.ShiftLeft) || keyState.Keys.IsPressed(Key.ShiftRight); // Renamed variable

            StringBuilder sb = new StringBuilder(); sb.Append(noteChar.Value); if (isSharp) sb.Append('#'); sb.Append(isOctaveUp ? '1' : '0'); string inputNote = sb.ToString(); // Use renamed variable

            // --- MODIFICATION ---
            // Update the debug text to show the note you just pressed
            debugOutputNoteText.Text = $"({inputNote})";
            // --------------------

            double now = Clock.CurrentTime - gameplayStartClockMs; if (now < 0) now = 0;
            var res = noteQueue.HandleNotePress(inputNote, now, hitWindows); // Assuming NoteManager is correct class

            if (!res.Consumed) return true;

            // --- ADD THIS ---
            // Key was consumed, so make the visual note disappear
            noteScheduler.HitCurrentNote();
            // ----------------

            // Update UI
            score.Apply(res.Judgement); UpdateScore(res.Judgement); UpdateAccuracy(score.GetAccuracyPercent()); UpdateCombo(); UpdateHealth(res.Judgement); ShowJudgementGlow(res.Judgement);

            if (res.SegmentCompleted)
            {
                currentSegmentIndex++;
                if (currentSegmentIndex < segmentsArr.Length)
                {
                    var seg = segmentsArr[currentSegmentIndex];
                    noteQueue.SetSegment(seg);
                    
                    // --- ADD THIS ---
                    noteScheduler.LoadSegment(seg);
                    // ----------------
                }
            }

            return true;
        }

        private Container createPauseOverlay()
        {
           return new Container { RelativeSizeAxes = Axes.Both, Anchor = Anchor.Centre, Origin = Anchor.Centre, Alpha = 0.0f, Children = new Drawable[] { new Box { RelativeSizeAxes = Axes.Both, Colour = Colour4.Black, Alpha = 0.6f }, new FillFlowContainer { Anchor = Anchor.Centre, Origin = Anchor.Centre, Direction = FillDirection.Vertical, AutoSizeAxes = Axes.Both, Spacing = new Vector2(0, 10), Children = new Drawable[] { new SpriteText { Text = "Paused", Font = new FontUsage("Kodchasan", size: 36, weight: "Bold"), Colour = Colour4.White }, new ClickableContainer { Action = () => { pauseOverlay.FadeOut(150); startCountdown(); }, AutoSizeAxes = Axes.Both, Child = new SpriteText { Text = "Resume", Font = new FontUsage("Kodchasan", size: 28, weight: "Bold"), Colour = Colour4.White } }, new ClickableContainer { Action = () => this.Exit(), AutoSizeAxes = Axes.Both, Child = new SpriteText { Text = "Exit", Font = new FontUsage("Kodchasan", size: 28, weight: "Bold"), Colour = Colour4.White } } } } } };
        }

        private Container createCountdownOverlay()
        {
            return new Container { RelativeSizeAxes = Axes.Both, Anchor = Anchor.Centre, Origin = Anchor.Centre, Alpha = 0.0f, Children = new Drawable[] { new Box { RelativeSizeAxes = Axes.Both, Colour = Colour4.Black, Alpha = 0.3f }, countdownText = new SpriteText { Anchor = Anchor.Centre, Origin = Anchor.Centre, Font = new FontUsage("Kodchasan", size: 120, weight: "Bold"), Colour = Colour4.White, Text = "3" } } };
        }

        private void startCountdown()
        {
            isCountingDown = true;
            isPaused = false;
            countdownOverlay.FadeIn(150);
            
            countdownText.Text = "3";
            countdownText.ScaleTo(1.5f, 0).Then().ScaleTo(1.0f, 800, Easing.OutElastic);
            
            this.Delay(1000).Schedule(() =>
            {
                countdownText.Text = "2";
                countdownText.ScaleTo(1.5f, 0).Then().ScaleTo(1.0f, 800, Easing.OutElastic);
            });
            
            this.Delay(2000).Schedule(() =>
            {
                countdownText.Text = "1";
                countdownText.ScaleTo(1.5f, 0).Then().ScaleTo(1.0f, 800, Easing.OutElastic);
                this.Delay(1000).Schedule(() =>
                {
                    countdownOverlay.FadeOut(300);
                    isCountingDown = false;
                    double pauseDuration = Clock.CurrentTime - pauseTime;
                    gameplayStartClockMs += pauseDuration;

                    // --- ADD THIS ---
                    // Re-sync the scheduler after pause
                    noteScheduler.TimeOffsetMs = gameplayStartClockMs;
                    // ----------------
                    
                    gameTrack?.Start();
                    Logger.Log($"[GameScreenTN] Countdown finished, game resumed (pause duration: {pauseDuration}ms)", LoggingTarget.Runtime, LogLevel.Important);
                });
            });
        }
    }
}