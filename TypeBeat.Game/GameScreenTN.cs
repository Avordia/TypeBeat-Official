using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
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
using TypeBeat.Game.Gameplay.Objects;
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
        private readonly Container sharpIndicator;
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
        private readonly Container scorePopupContainer;
        private readonly TypeNoteScheduler noteScheduler; // <-- Replaced NoteScheduler
        private readonly TypeNoteLayoutConfig typeNoteLayoutConfig = new TypeNoteLayoutConfig
        {
            SpawnXFraction = 1.1f,      
            DestinationXFraction = 0.25f, 
            YBaseFraction = 0.595f,       
            YStepPx = 17f                
        };
        // ---------------------

    // Debug UI elements removed
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
        private bool isGameOver = false;
    private Note firstNoteForCue;
    private Note secondNoteForVelocity;
    private double computedGracePeriodMs = 5000; // Dynamically set from dominant note duration
        private DrawableMusicNoteAbsolute standaloneFirstCue;
        private bool firstCueActive = false;

        // Piano sample fields
        private readonly Dictionary<string, Sample> pianoSamples = new Dictionary<string, Sample>();
    private ISampleStore embeddedSampleStore; // dedicated store for embedded Samples/* assets
    private System.IO.Stream customSampleFileStream; // beatpack zip stream for custom TypeNote packs
    private ZipArchiveResourceStore customSampleResourceStore; // resource store over beatpack zip
    private ISampleStore customSampleStore; // sample store from beatpack zip
    private string selectedTypeNotePackName; // manifest-provided pack name

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

            // Enforce an early-hit guard for TypeNote so notes can only be judged within
            // the last 1500ms before their EndTime.
            noteQueue.EarlyHitGuardMs = 1500;

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

                playfield = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Child = noteScheduler = new TypeNoteScheduler(typeNoteLayoutConfig) // <-- Use new scheduler
                    {
                        PreloadMs = 1000 // First note appears 1 second before music starts
                    }
                },

                // (debug container removed)
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
                                    Size = new Vector2(36, 36),
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
                                    Size = new Vector2(200, 12),
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
                    Padding = new MarginPadding { Top = 50 },
                    Child = scoreText = new SpriteText
                    {
                        Text = "000000000000",
                        Font = new FontUsage("Kodchasan", size: 56, weight: "Bold"),
                        Colour = Colour4.White,
                        Spacing = new Vector2(0.25f, 0)
                    }
                },
                // Score popup container for floating score text (positioned above music sheet)
                scorePopupContainer = new Container
                {
                    RelativeSizeAxes = Axes.None,
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Depth = -500 // Above most elements but below HUD
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
                // Accuracy display (top-right)
                new Container
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    AutoSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Top = 70, Right = 30 },
                    Child = new FillFlowContainer
                    {
                        Direction = FillDirection.Horizontal,
                        AutoSizeAxes = Axes.Both,
                        Spacing = new Vector2(10, 0),
                        Children = new Drawable[]
                        {
                            new SpriteText
                            {
                                Text = "ACCURACY:",
                                Font = new FontUsage("Kodchasan", size: 28, weight: "Bold"),
                                Colour = Colour4.White
                            },
                            accuracyText = new SpriteText
                            {
                                Text = "100.0%",
                                Font = new FontUsage("Kodchasan", size: 28, weight: "Bold"),
                                Colour = Colour4.Lime
                            }
                        }
                    }
                },
                // Sharp Mode Indicator (at bottom of screen)
                sharpIndicator = new Container
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Y = -50, // ADJUST THIS VALUE: negative = higher, positive = lower
                    Size = new Vector2(80, 80),
                    Masking = true,
                    CornerRadius = 40,
                    BorderThickness = 4,
                    BorderColour = Colour4.White,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = new Colour4(100, 100, 100, 255)
                        },
                        new SpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = "#",
                            Font = new FontUsage(size: 48, weight: "Bold"),
                            Colour = Colour4.White
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
            // CRITICAL DEBUG: Check if audioManager is null
            Console.WriteLine($"===== LOAD METHOD: audioManager is null? {audioManager == null} =====");
            Console.WriteLine($"===== LOAD METHOD: audioManager.Samples is null? {audioManager?.Samples == null} =====");
            Console.WriteLine($"===== LOAD METHOD: textures is null? {textures == null} =====");

            // Ensure we have a SampleStore bound to our Game Resources (embedded TypeBeat.Resources)
            // This bypasses any global store wiring differences between screens.
            try
            {
                // Build a resource store directly from the embedded resource assembly to avoid any ambiguity with Drawable.Resources
                var embeddedResourceStore = new DllResourceStore(TypeBeat.Resources.TypeBeatResources.ResourceAssembly);
                embeddedSampleStore = audioManager.GetSampleStore(embeddedResourceStore);
                Logger.Log($"[GameScreenTN] embeddedSampleStore created: {embeddedSampleStore != null}", LoggingTarget.Runtime, LogLevel.Important);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[GameScreenTN] Failed to create embeddedSampleStore from ResourceAssembly");
            }

            // If beatpack requests a TypeNote soundpack, prepare a custom sample store from the beatpack archive
            try
            {
                // Prefer per-beatmap selected TypeNote soundpack (use the specific beatmap passed to constructor)
                selectedTypeNotePackName = beatmap?.CustomSounds?.TypeNoteSoundpack
                                           ?? beatpack?.CustomSounds?.TypeNoteSoundpack;
                Logger.Log($"[GameScreenTN] Selected TypeNote soundpack for {beatmap?.DifficultyName}: '{selectedTypeNotePackName ?? "(none)"}'", LoggingTarget.Runtime, LogLevel.Important);

                if (!string.IsNullOrEmpty(selectedTypeNotePackName) && beatpack?.FilePath != null)
                {
                    customSampleFileStream = File.OpenRead(beatpack.FilePath);
                    customSampleResourceStore = new ZipArchiveResourceStore(customSampleFileStream);
                    customSampleStore = audioManager.GetSampleStore(customSampleResourceStore);
                    Logger.Log($"[GameScreenTN] Custom TypeNote pack store ready: {selectedTypeNotePackName}", LoggingTarget.Runtime, LogLevel.Important);

                    // Probe a few expected files in the archive to aid debugging
                    foreach (var probe in new[] { "C0", "Cs0", "D0" })
                    {
                        var p1 = $"audio/TypeNote/{selectedTypeNotePackName}/{probe}.ogg";
                        using (var s = beatpack.GetStream(p1))
                        {
                            Logger.Log($"[GameScreenTN] Probe exists? {p1} -> {(s != null ? "YES" : "NO")}", LoggingTarget.Runtime, LogLevel.Important);
                        }
                    }
                }
                else
                {
                    Logger.Log("[GameScreenTN] No TypeNote soundpack specified; will use embedded defaults", LoggingTarget.Runtime, LogLevel.Important);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[GameScreenTN] Failed to initialise custom TypeNote soundpack store");
                disposeCustomSampleResources();
            }
            
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
            float segmentWidth = 11f, segmentHeight = 12f, skewAmount = 0.3f;
            for (int i = 0; i < healthSegmentCount; i++) { float alphaGradient = 1.0f - (i / (float)(healthSegmentCount - 1)) * 0.9f; var segment = new Container { Size = new Vector2(segmentWidth, segmentHeight), Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Shear = new Vector2(skewAmount, 0), Child = new Box { RelativeSizeAxes = Axes.Both, Colour = gradientColors[i], Alpha = alphaGradient, EdgeSmoothness = new Vector2(2.0f) } }; healthBarSegments.Add(segment.Child as Box); segmentContainer.Add(segment); }

            // Load judgement glow
            judgementGlow.Texture = textures.Get("images/JudgementGlow");
            Logger.Log($"[GameScreenTN] Judgement glow texture loaded: {judgementGlow.Texture != null}", LoggingTarget.Runtime, LogLevel.Important);

            // Load piano samples (from embedded resources via embeddedSampleStore)
            loadPianoSamples();
            
            // TEST: Verify sample loading works at all
            var testKickGlobal = audioManager.Samples.Get("Samples/Kick");
            var testKickEmbedded = embeddedSampleStore?.Get("Samples/Kick") ?? embeddedSampleStore?.Get("Kick.ogg");
            Logger.Log($"[GameScreenTN] TEST - Kick (global) loaded: {testKickGlobal != null}", LoggingTarget.Runtime, LogLevel.Important);
            Logger.Log($"[GameScreenTN] TEST - Kick (embedded) loaded: {testKickEmbedded != null}", LoggingTarget.Runtime, LogLevel.Important);
            var testPianoC0Global = audioManager.Samples.Get("Samples/Piano_C0");
            var testPianoC0Embedded = embeddedSampleStore?.Get("Samples/Piano_C0") ?? embeddedSampleStore?.Get("Piano_C0.ogg") ?? embeddedSampleStore?.Get("Piano_C0");
            Logger.Log($"[GameScreenTN] TEST - Piano_C0 (global) loaded: {testPianoC0Global != null}", LoggingTarget.Runtime, LogLevel.Important);
            Logger.Log($"[GameScreenTN] TEST - Piano_C0 (embedded) loaded: {testPianoC0Embedded != null}", LoggingTarget.Runtime, LogLevel.Important);

            // Debug output note text removed

            // Load audio
            try
            {
                using (var stream = File.OpenRead(beatpack.FilePath))
                {
                    var beatmapAssetStorage = new ZipArchiveResourceStore(stream);
                    var trackStore = audioManager.GetTrackStore(beatmapAssetStorage);

                    // Prefer per-beatmap audio filename if provided (use the specific beatmap passed to constructor)
                    var resolvedAudioPath = beatpack.GetAudioPathForBeatmap(beatmap);
                    Logger.Log($"[GameScreenTN] Resolved audio path for {beatmap.DifficultyName}: {resolvedAudioPath}", LoggingTarget.Runtime, LogLevel.Important);
                    if (!string.IsNullOrEmpty(resolvedAudioPath))
                        gameTrack = trackStore.Get(resolvedAudioPath);

                    // Fallbacks
                    if (!string.IsNullOrEmpty(beatpack.MusicPath))
                        gameTrack ??= trackStore.Get(beatpack.MusicPath);

                    gameTrack ??= trackStore.Get("audio.mp3");

                    if (gameTrack != null)
                    {
                        gameTrack.Looping = false;
                        var pathForLog = resolvedAudioPath ?? beatpack.MusicPath ?? "audio.mp3";
                        Logger.Log($"[GameScreenTN] Loaded audio track: {pathForLog}", LoggingTarget.Runtime, LogLevel.Important);
                    }
                    else
                    {
                        Logger.Log($"[GameScreenTN] Failed to load audio track", LoggingTarget.Runtime, LogLevel.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load game audio");
            }

            // Load background
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
                        entry ??= archive.GetEntry("cover.jpg");
                        
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

            // Initialize note queue AND scheduler
            try
            {
                var segments = beatmap?.MapData;
                if (segments != null && segments.Any())
                {
                    segmentsArr = segments.ToArray();
                    currentSegmentIndex = 0;

                    // Determine first note and calculate dominant note duration
                    var orderedByEnd = segments
                        .Where(s => s?.Notes != null)
                        .SelectMany(s => s.Notes)
                        .OrderBy(n => n.EndTime)
                        .ToList();
                    firstNoteForCue = orderedByEnd.ElementAtOrDefault(0);
                    secondNoteForVelocity = orderedByEnd.ElementAtOrDefault(1);

                    // Calculate dominant note duration (mode) by finding most common duration
                    // This reads the actual note durations from the beatmap
                    var allDurations = orderedByEnd
                        .Select(n => Math.Max(1, n.EndTime - n.StartTime))
                        .ToList();
                    
                    if (allDurations.Count > 0)
                    {
                        // Bucket by 10ms to avoid fragmentation
                        var dominant = allDurations
                            .GroupBy(d => Math.Round(d / 10.0) * 10.0)
                            .OrderByDescending(g => g.Count())
                            .ThenBy(g => g.Key)
                            .First().Key;
                        computedGracePeriodMs = Math.Max(1000, dominant);
                        Logger.Log($"[GameScreenTN] Calculated dominant note duration: {computedGracePeriodMs}ms from {allDurations.Count} notes", LoggingTarget.Runtime, LogLevel.Important);
                        Logger.Log($"[GameScreenTN] Example durations: {string.Join(", ", allDurations.Take(5).Select(d => $"{d}ms"))}", LoggingTarget.Runtime, LogLevel.Debug);
                    }
                    else
                    {
                        computedGracePeriodMs = 5000; // Default
                        Logger.Log($"[GameScreenTN] No notes found, using default grace period: {computedGracePeriodMs}ms", LoggingTarget.Runtime, LogLevel.Important);
                    }
                    
                    // Notes already have correct StartTime/EndTime from MapMaker - no adjustment needed!

                    // NOW load the segments with adjusted timings into the scheduler
                    var firstSegment = segmentsArr[currentSegmentIndex];
                    noteQueue.SetSegment(firstSegment);
                    noteScheduler.LoadAllSegments(segments);
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

          private void updateScore(JudgementType judgement)
        {
              int scoreGain = judgement switch { JudgementType.Perfect300 => 300, JudgementType.Great200 => 200, JudgementType.Good100 => 100, JudgementType.Meh50 => 50, _ => 0 }; int comboMultiplier = Math.Min(score.Combo / 25, 4); int totalScoreGain = scoreGain * (1 + comboMultiplier); currentScore += totalScoreGain; scoreText.Text = currentScore.ToString("D12"); if (scoreGain > 0) showScorePopup(totalScoreGain, judgement);
        }

          private void updateCombo()
        {
           comboText.Text = $"COMBO: X{score.Combo}";
        }

          private void showScorePopup(int scoreValue, JudgementType judgement)
        {
            // Use centralised judgement colours
            Colour4 color = JudgementColors.Get(judgement);

            var popupContainer = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Alpha = 0
            };

            var glowLayers = new Drawable[]
            {
                new SpriteText { Text = $"+{scoreValue}", Font = new FontUsage("Roboto", size: 52, weight: "Bold"), Colour = Colour4.White, Anchor = Anchor.Centre, Origin = Anchor.Centre, Alpha = 0.3f },
                new SpriteText { Text = $"+{scoreValue}", Font = new FontUsage("Roboto", size: 50, weight: "Bold"), Colour = Colour4.White, Anchor = Anchor.Centre, Origin = Anchor.Centre, Alpha = 0.5f },
                new SpriteText { Text = $"+{scoreValue}", Font = new FontUsage("Roboto", size: 48, weight: "Bold"), Colour = color, Anchor = Anchor.Centre, Origin = Anchor.Centre, Shadow = true, ShadowColour = Colour4.Black.Opacity(0.5f) }
            };

            popupContainer.Children = glowLayers;
            scorePopupContainer.Add(popupContainer);

            popupContainer
                .FadeInFromZero(100)
                .Then()
                .MoveToY(Gameplay.Config.GameplayVisualSettings.scorePopupRiseY, 800, Easing.OutQuint)
                .FadeOut(400, Easing.InQuint)
                .Finally(_ => popupContainer.Expire());

            popupContainer
                .ScaleTo(1.3f, 100, Easing.OutQuint)
                .Then()
                .ScaleTo(1f, 150, Easing.InOutQuint);
        }

          private void showJudgementGlow(JudgementType judgement)
          {
              Logger.Log($"[GameScreenTN] ShowJudgementGlow called with {judgement}, texture={judgementGlow.Texture != null}, alpha={judgementGlow.Alpha}", LoggingTarget.Runtime, LogLevel.Important);
              judgementGlow.Colour = JudgementColors.Get(judgement);
              judgementGlow.ScaleTo(0.8f, 0)
                                .Then()
                                .ScaleTo(1.2f, 100, Easing.OutQuint)
                                .Then()
                                .ScaleTo(1.0f, 200, Easing.InQuint);
              judgementGlow.FadeTo(0.9f, 50)
                                .Then()
                                .FadeOut(400, Easing.OutQuint);
          }

        // Health mechanics temporarily disabled for testing
        private void updateHealth(JudgementType judgement)
        {
            // no-op
        }

        private void updateHealthBar()
        {
            // no-op while health mechanics are disabled
        }

        private void onHealthDepleted()
        {
            // no-op while health mechanics are disabled
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);
            this.FadeInFromZero(300);
            
            // Set gameplay timing offset using dominant note duration (creates grace period)
            gameplayStartClockMs = Clock.CurrentTime + computedGracePeriodMs;
            noteScheduler.TimeOffsetMs = gameplayStartClockMs;
            noteScheduler.PreloadMs = computedGracePeriodMs; // spawn notes with approach equal to grace/dominant duration
            lastHealthDrainTime = Clock.CurrentTime + computedGracePeriodMs;
            
            // Delay music start by the computed grace period
            if (gameTrack != null)
            {
                Scheduler.AddDelayed(() =>
                {
                    gameTrack.Start();
                    Logger.Log($"[GameScreenTN] Started gameplay audio after {computedGracePeriodMs} ms grace period!", LoggingTarget.Runtime, LogLevel.Important);
                }, computedGracePeriodMs);
            }

            // Do NOT use a standalone first-note cue; render all notes uniformly via the scheduler
            noteScheduler.ExcludedVisualNote = null;
            
            Logger.Log($"GameScreenTN entered with beatmap: {beatmap?.Title}", LoggingTarget.Runtime, LogLevel.Important);
            Logger.Log($"[GameScreenTN] Using dynamic grace period and preload = {computedGracePeriodMs} ms (dominant duration)", LoggingTarget.Runtime, LogLevel.Important);
            Logger.Log($"[GameScreenTN] Grace period: First note appears with consistent velocity", LoggingTarget.Runtime, LogLevel.Important);
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
           if (gameTrack != null) { if (gameTrack.IsRunning) { gameTrack.Stop(); Logger.Log("[GameScreenTN] Stopped gameplay audio", LoggingTarget.Runtime, LogLevel.Important); } gameTrack.Dispose(); gameTrack = null; Logger.Log("[GameScreenTN] Disposed gameplay audio track", LoggingTarget.Runtime, LogLevel.Important); } this.FadeOut(300); return base.OnExiting(e);
        }

        protected override void Update()
        {
            base.Update();

            // Update music sheet color based on Shift key (high octave indicator)
            InputState inputState = GetContainingInputManager().CurrentState;
            var keyState = inputState.Keyboard;
            bool isOctaveUp = keyState.Keys.IsPressed(Key.ShiftLeft) || keyState.Keys.IsPressed(Key.ShiftRight);
            bool isSharpActive = keyState.Keys.IsPressed(Key.Space);
            
            if (isOctaveUp)
            {
                musicSheetSprite.Colour = Colour4.FromHex("#FFD700"); // Gold
            }
            else
            {
                musicSheetSprite.Colour = Colour4.White; // Normal
            }
            
            // Update sharp indicator color based on Space key
            if (isSharpActive)
            {
                ((Box)sharpIndicator.Children[0]).Colour = Colour4.White; // White when sharp is active
            }
            else
            {
                ((Box)sharpIndicator.Children[0]).Colour = new Colour4(100, 100, 100, 255); // Grey
            }

            if (isPaused || isCountingDown) return;

            // Passive health drain DISABLED for testing
            // ...

            // Auto-miss overdue notes
            double nowRel = Clock.CurrentTime - gameplayStartClockMs; // Allow negative time during grace period
            int autoMissed = noteQueue.AutoConsumeMisses(nowRel, hitWindows, out bool segCompleted);
            if (autoMissed > 0)
            {
                for (int i = 0; i < autoMissed; i++)
                {
                    noteScheduler.HitCurrentNote(JudgementType.Miss); // Make the missed note disappear
                    
                    score.Apply(JudgementType.Miss);
                    showJudgementGlow(JudgementType.Miss);
                    updateHealth(JudgementType.Miss);
                }
                UpdateAccuracy(score.GetAccuracyPercent());
                updateCombo();
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

            // Debug HUD removed

            // Keep the score popup container positioned just above the music sheet
            // musicSheetSprite is centred and sized to 0.8 of screen with Fit, so top is approximately:
            float sheetTop = (float)((DrawSize.Y - musicSheetSprite.DrawSize.Y) / 2.0);
            // Place the popup container slightly above that line (raise a bit higher)
            // Controlled by GameplayVisualSettings for easy tweaking/revert
            scorePopupContainer.Position = new Vector2(0, sheetTop - Gameplay.Config.GameplayVisualSettings.scorePopupBaseOffset);
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

            // Debug output note removed
            
            // Play piano sound immediately when any note key is pressed
            // This happens before checking if it's correct, so you hear feedback on every press
            playPianoNote(inputNote);

            double now = Clock.CurrentTime - gameplayStartClockMs; // Allow negative time during grace period
            var res = noteQueue.HandleNotePress(inputNote, now, hitWindows); // Assuming NoteManager is correct class

            if (!res.Consumed) return true;

            // Key was consumed, so make the visual note disappear
            noteScheduler.HitCurrentNote(res.Judgement);

            // Update UI
            score.Apply(res.Judgement); updateScore(res.Judgement); UpdateAccuracy(score.GetAccuracyPercent()); updateCombo(); updateHealth(res.Judgement); showJudgementGlow(res.Judgement);

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

    private void loadPianoSamples()
        {
            Console.WriteLine("===== LoadPianoSamples CALLED =====");
            Logger.Log("[GameScreenTN] Loading piano samples from Samples/ (with Piano_ prefix)", LoggingTarget.Runtime, LogLevel.Important);
            
            // Load piano samples from embedded resources
            // All sample files use "s" for sharp (e.g., "Cs0" for C#0, "As0" for A#0)
            // Samples are in root Samples folder with "Piano_" prefix (e.g., "Samples/Piano_C0")
            
            // Map: file name (with 's' for sharp) -> normalized note name (with '#' for sharp)
            var sampleMappings = new Dictionary<string, string>
            {
                // Octave 0
                { "C0", "C0" },
                { "Cs0", "C#0" },
                { "D0", "D0" },
                { "Ds0", "D#0" },
                { "E0", "E0" },
                { "F0", "F0" },
                { "Fs0", "F#0" },
                { "G0", "G0" },
                { "Gs0", "G#0" },
                { "A0", "A0" },
                { "As0", "A#0" },  // Changed to 's' convention
                { "B0", "B0" },
                // Octave 1
                { "C1", "C1" },
                { "Cs1", "C#1" },  // Changed to 's' convention
                { "D1", "D1" },
                { "Ds1", "D#1" },
                { "E1", "E1" },
                { "F1", "F1" },
                { "Fs1", "F#1" },
                { "G1", "G1" },
                { "Gs1", "G#1" },
                { "A1", "A1" },
                { "As1", "A#1" },
                { "B1", "B1" }
            };

            Console.WriteLine($"===== Attempting to load {sampleMappings.Count} piano samples =====");

            foreach (var mapping in sampleMappings)
            {
                string fileName = mapping.Key;
                string normalizedKey = mapping.Value;
                
                try
                {
                    // 1) Try custom TypeNote soundpack inside the beatpack (audio/TypeNote/<Pack>/<Note>.ogg)
                    if (customSampleStore != null && !string.IsNullOrEmpty(selectedTypeNotePackName))
                    {
                        string customPath1 = $"audio/TypeNote/{selectedTypeNotePackName}/{fileName}.ogg";
                        Console.WriteLine($"Attempting to load (custom pack): {customPath1}");
                        var sCustom = customSampleStore.Get(customPath1);
                        if (sCustom == null)
                        {
                            // Also try without extension in case store matches raw
                            string customPath2 = $"audio/TypeNote/{selectedTypeNotePackName}/{fileName}";
                            Console.WriteLine($"Fallback try (custom pack no ext): {customPath2}");
                            sCustom = customSampleStore.Get(customPath2);
                        }
                        if (sCustom != null)
                        {
                            pianoSamples[normalizedKey] = sCustom;
                            Console.WriteLine($"✓ SUCCESS (custom): Loaded {normalizedKey} from pack '{selectedTypeNotePackName}'");
                            Logger.Log($"[GameScreenTN] ✓ Loaded custom pack note: {normalizedKey}", LoggingTarget.Runtime, LogLevel.Debug);
                            continue; // next mapping
                        }
                    }

                    // Attempt using embeddedSampleStore first to avoid reliance on global store wiring.
                    // Primary path: root Samples folder with Piano_ prefix
                    string pathPrimary = $"Samples/Piano_{fileName}";
                    Console.WriteLine($"Attempting to load (embedded): {pathPrimary}");
                    Sample sample = embeddedSampleStore?.Get(pathPrimary);
                    
                    // Fallbacks: extension / no Samples prefix / DefaultPiano folder
                    if (sample == null)
                    {
                        string pathExt = $"Piano_{fileName}.ogg";
                        Console.WriteLine($"Fallback try (embedded): {pathExt}");
                        sample = embeddedSampleStore?.Get(pathExt);
                    }

                    if (sample == null)
                    {
                        string pathNoPrefix = $"Piano_{fileName}";
                        Console.WriteLine($"Fallback try (embedded): {pathNoPrefix}");
                        sample = embeddedSampleStore?.Get(pathNoPrefix);
                    }

                    if (sample == null)
                    {
                        string defaultFolderPath = $"Samples/DefaultPiano/{fileName}";
                        Console.WriteLine($"Fallback try (embedded DefaultPiano): {defaultFolderPath}");
                        sample = embeddedSampleStore?.Get(defaultFolderPath) ?? embeddedSampleStore?.Get($"{defaultFolderPath}.ogg");
                    }

                    // Final fallback: try global store in case it's wired at runtime
                    if (sample == null)
                    {
                        Console.WriteLine($"Fallback try (global): {pathPrimary}");
                        sample = audioManager.Samples.Get(pathPrimary) ?? audioManager.Samples.Get($"Piano_{fileName}.ogg") ?? audioManager.Samples.Get($"Piano_{fileName}");
                    }
                    
                    if (sample != null)
                    {
                        pianoSamples[normalizedKey] = sample;
                        Console.WriteLine($"✓ SUCCESS: Loaded {normalizedKey} (Piano_{fileName})");
                        Logger.Log($"[GameScreenTN] ✓ Loaded: {normalizedKey} (Piano_{fileName})", LoggingTarget.Runtime, LogLevel.Debug);
                    }
                    else
                    {
                        Console.WriteLine($"✗ FAILED: Could not load Piano_{fileName} from embedded/global stores");
                        Logger.Log($"[GameScreenTN] ✗ Failed to load Piano_{fileName}", LoggingTarget.Runtime, LogLevel.Error);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ EXCEPTION loading Piano_{fileName}: {ex.Message}");
                    Logger.Log($"[GameScreenTN] Exception loading Piano_{fileName}: {ex.Message}", LoggingTarget.Runtime, LogLevel.Error);
                }
            }

            Console.WriteLine($"===== FINAL RESULT: Loaded {pianoSamples.Count}/24 piano samples =====");
            Logger.Log($"[GameScreenTN] Loaded {pianoSamples.Count}/24 piano samples", LoggingTarget.Runtime, LogLevel.Important);
        }

    private void playPianoNote(string noteCharacter)
        {
            Console.WriteLine($"===== PlayPianoNote CALLED with: '{noteCharacter}' =====");
            
            if (string.IsNullOrEmpty(noteCharacter))
            {
                Console.WriteLine("===== Note character is empty! =====");
                Logger.Log("[GameScreenTN] PlayPianoNote called with empty note", LoggingTarget.Runtime, LogLevel.Debug);
                return;
            }

            Console.WriteLine($"===== Looking for sample in dictionary. Dict has {pianoSamples.Count} samples =====");
            Logger.Log($"[GameScreenTN] Attempting to play piano note: {noteCharacter}", LoggingTarget.Runtime, LogLevel.Debug);

            if (pianoSamples.TryGetValue(noteCharacter, out var sample) && sample != null)
            {
                Console.WriteLine($"===== FOUND SAMPLE! Calling Play() =====");
                Logger.Log($"[GameScreenTN] Found sample for {noteCharacter}, playing...", LoggingTarget.Runtime, LogLevel.Important);
                var channel = sample.Play();
                if (channel != null)
                {
                    Console.WriteLine($"===== Channel created! Setting volume to 0.7 =====");
                    channel.Volume.Value = 0.7; // Adjust volume as needed
                    Logger.Log($"[GameScreenTN] Successfully played {noteCharacter}", LoggingTarget.Runtime, LogLevel.Important);
                }
                else
                {
                    Console.WriteLine($"===== ERROR: Channel is NULL! =====");
                    Logger.Log($"[GameScreenTN] Channel was null for {noteCharacter}", LoggingTarget.Runtime, LogLevel.Error);
                }
            }
            else
            {
                Console.WriteLine($"===== SAMPLE NOT FOUND! Available keys: {string.Join(", ", pianoSamples.Keys)} =====");
                Logger.Log($"[GameScreenTN] Piano sample not found for note: {noteCharacter}. Available: {string.Join(", ", pianoSamples.Keys)}", LoggingTarget.Runtime, LogLevel.Error);
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            embeddedSampleStore?.Dispose();
            embeddedSampleStore = null;
            disposeCustomSampleResources();
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
    }
}